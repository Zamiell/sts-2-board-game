using BoardGame.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Events;

// Neow's blessing in the board game: a random Neow card offers three choices. The first two are free; the third
// is better but has a drawback. After Act I Neow only restores your health.
[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
internal static class BgNeowPatch
{
    // Each Neow card: two free rewards, then a drawback and a reward.
    private static readonly string[] Cards =
    [
        "= t DX", "u r DX", "u ? CX", "= r D!", "u P C!", "5 ? G!", "= ? G%", "u 5 C%", "P c G%", "r t D%",
        "= u GR", "t 5 GR", "? c DR", "? t CU", "r 5 GU", "u P DU", "= P CT", "r t 3T", "t 5 C)", "r P CW",
    ];

    private static readonly Dictionary<char, string> Rewards = new()
    {
        ['4'] = "Gain 4 Gold.",
        ['5'] = "Gain 5 Gold.",
        ['8'] = "Gain 8 Gold.",
        ['X'] = "Gain 10 Gold.",
        ['r'] = "Remove a card from your deck.",
        ['R'] = "Remove 2 cards from your deck.",
        ['t'] = "Transform a card.",
        ['T'] = "Transform 2 cards.",
        ['u'] = "Upgrade a card.",
        ['U'] = "Upgrade 2 random cards.",
        ['c'] = "Choose a card to add to your deck.",
        ['C'] = "Add 2 random cards to your deck.",
        ['P'] = "Gain 3 potions.",
        ['?'] = "Add a random Rare card to your deck.",
        ['!'] = "Choose a Rare card to add to your deck.",
        ['%'] = "Obtain a relic.",
        ['='] = "Choose a colorless card to add to your deck.",
        ['+'] = "Add 2 random colorless cards to your deck.",
        ['V'] = "Choose 2 cards to add to your deck.",
        ['W'] = "Choose a card to add to your deck. Gain 5 Gold.",
        [')'] = "Choose 2 colorless cards to add to your deck.",
    };

    private static readonly Dictionary<char, string> Drawbacks = new()
    {
        ['D'] = "Lose 2 HP.",
        ['3'] = "Lose 3 HP.",
        ['G'] = "Lose 3 Gold.",
        ['C'] = "Gain a Curse.",
    };

    private static readonly AccessTools.FieldRef<EventModel, MegaCrit.Sts2.Core.Random.Rng> EventRng =
        AccessTools.FieldRefAccess<EventModel, MegaCrit.Sts2.Core.Random.Rng>("<Rng>k__BackingField");

    private static readonly Action<EventModel, LocString> SetEventFinished =
        AccessTools.MethodDelegate<Action<EventModel, LocString>>(AccessTools.Method(typeof(EventModel), "SetEventFinished"));

    private static bool Prefix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        var player = __instance.Owner!;
        if (!BoardGameRun.IsActive(player.RunState))
        {
            return true;
        }
        if (player.RunState.CurrentActIndex > 0)
        {
            __result = [new EventOption(__instance, NEventRoom.Proceed, "PROCEED", false, true)];
            return false;
        }
        var card = EventRng(__instance).NextItem(Cards)!.Split(' ');
        __result =
        [
            Make(__instance, player, null, card[0][0]),
            Make(__instance, player, null, card[1][0]),
            Make(__instance, player, card[2][0], card[2][1]),
        ];
        return false;
    }

    private static EventOption Make(Neow neow, Player player, char? drawback, char reward)
    {
        var text = drawback is { } d ? $"{Drawbacks[d]} {Rewards[reward]}" : Rewards[reward];
        var title = new LocString("ancients", "BG_NEOW.option.title");
        title.Add("Text", drawback == null ? "Blessing" : "Bargain");
        var description = new LocString("ancients", "BG_NEOW.option.description");
        description.Add("Text", text);
        return new EventOption(neow, async () =>
        {
            if (drawback is { } cost)
            {
                await PayDrawback(player, cost);
            }
            await Grant(player, reward);
            SetEventFinished(neow, new LocString("ancients", "NEOW.pages.DONE.POSITIVE.description"));
        }, title, description, $"BG_NEOW.{(drawback == null ? "" : drawback + "_")}{reward}", []);
    }

    private static Task PayDrawback(Player player, char drawback) => drawback switch
    {
        'D' => LoseHp(player, 2),
        '3' => LoseHp(player, 3),
        'G' => PlayerCmd.LoseGold(Math.Min(3, player.Gold), player, GoldLossType.Lost),
        _ => RewardDeck.GainCurses(player),
    };

    private static Task LoseHp(Player player, int amount) =>
        CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), player.Creature, amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);

    private static async Task Grant(Player player, char reward)
    {
        switch (reward)
        {
            case '4':
            case '5':
            case '8':
                await PlayerCmd.GainGold(reward - '0', player);
                break;
            case 'X':
                await PlayerCmd.GainGold(10, player);
                break;
            case 'r':
            case 'R':
                await CardPileCmd.RemoveFromDeck((await CardSelectCmd.FromDeckForRemoval(player,
                    new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, reward == 'r' ? 1 : 2))).ToList());
                break;
            case 't':
            case 'T':
                foreach (var card in (await CardSelectCmd.FromDeckForTransformation(player,
                    new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, reward == 't' ? 1 : 2))).ToList())
                {
                    await RewardDeck.Transform(card);
                }
                break;
            case 'u':
                CardCmd.Upgrade((await CardSelectCmd.FromDeckForUpgrade(player,
                    new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).ToList(),
                    MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.HorizontalLayout);
                break;
            case 'U':
                CardCmd.Upgrade(player.Deck.Cards.Where(c => c.IsUpgradable).ToList().UnstableShuffle(player.RunState.Rng.Niche).Take(2).ToList(),
                    MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.HorizontalLayout);
                break;
            case 'c':
                await RewardsCmd.OfferCustom(player, [RewardDeck.Reward(player)]);
                break;
            case 'V':
                await RewardsCmd.OfferCustom(player, [RewardDeck.Reward(player), RewardDeck.Reward(player)]);
                break;
            case 'W':
                await RewardsCmd.OfferCustom(player, [RewardDeck.Reward(player), new GoldReward(5, player)]);
                break;
            case 'C':
                await AddToDeck(RewardDeck.Draw(player, 2));
                break;
            case '?':
                await AddToDeck(RewardDeck.Draw(player, 1, rare: true));
                break;
            case '!':
                await RewardsCmd.OfferCustom(player, [RewardDeck.Reward(player, rare: true)]);
                break;
            case 'P':
                await RewardsCmd.OfferCustom(player, [new PotionReward(player), new PotionReward(player), new PotionReward(player)]);
                break;
            case '%':
                await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(player).ToMutable(), player);
                break;
            case '=':
                await RewardsCmd.OfferCustom(player, [ColorlessReward(player)]);
                break;
            case ')':
                await RewardsCmd.OfferCustom(player, [ColorlessReward(player), ColorlessReward(player)]);
                break;
            case '+':
                await AddToDeck(RewardDeck.DrawColorless(player, 2));
                break;
        }
    }

    private static CardReward ColorlessReward(Player player) =>
        new(RewardDeck.DrawColorless(player, 3), CardCreationSource.Encounter, player,
            CardCreationOptions.ForNonCombatWithDefaultOdds([player.Character.CardPool]));

    private static async Task AddToDeck(IEnumerable<CardModel> cards)
    {
        foreach (var card in cards)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, MegaCrit.Sts2.Core.Entities.Cards.PileType.Deck));
        }
    }
}

// STS2's Neow empties your HP and then fills it, because it starts the run; from Ascension 9 the board game starts
// you 1 HP down. In later acts Neow stands in for the board game's act transition: heal to full, or only 4 HP from
// Ascension 6.
[HarmonyPatch(typeof(AncientEventModel), "BeforeEventStarted")]
internal static class BgNeowHealPatch
{
    private static bool Prefix(AncientEventModel __instance, bool isPreFinished, ref Task __result)
    {
        if (!Applies(__instance, isPreFinished, out var player) || player.RunState.CurrentActIndex == 0)
        {
            return true;
        }
        var missing = player.Creature.MaxHp - player.Creature.CurrentHp;
        var amount = BgAscension.Level(player.RunState) >= BgAscension.HealLessAfterBoss
            ? Math.Min(missing, BgAscension.ActHealAtA6)
            : missing;
        __result = CreatureCmd.Heal(player.Creature, amount, playAnim: false);
        return false;
    }

    private static void Postfix(AncientEventModel __instance, bool isPreFinished, ref Task __result)
    {
        if (Applies(__instance, isPreFinished, out var player) && player.RunState.CurrentActIndex == 0
            && BgAscension.Level(player.RunState) >= BgAscension.StartDamaged)
        {
            __result = StartDamaged(__result, player);
        }
    }

    private static async Task StartDamaged(Task neowHeal, Player player)
    {
        await neowHeal;
        player.Creature.SetCurrentHpInternal(Math.Max(1, player.Creature.MaxHp - 1));
    }

    private static bool Applies(AncientEventModel ancient, bool isPreFinished, out Player player)
    {
        player = ancient.Owner!;
        return ancient is Neow && !isPreFinished && player != null && BoardGameRun.IsActive(player.RunState);
    }
}
