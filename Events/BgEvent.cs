using BoardGame.Core;
using BoardGame.Potions;
using BoardGame.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Events;

// A card from the board game's event deck. Its text lives in the events table under its ID:
//   BG_X.title, BG_X.pages.<PAGE>.description, BG_X.pages.<PAGE>.options.<OPTION>.title/.description
// Each event borrows the portrait of an STS2 event.
public abstract class BgEvent : EventModel, IBorrowsAssets
{
    // The acts whose event deck includes this event.
    public abstract IReadOnlyList<Type> Acts { get; }

    // The STS2 event whose portrait is shown.
    public abstract string BorrowedEntry { get; }

    // Ascension 3 ("unfavorable events") shuffles some events into the event decks.
    public virtual int MinAscension => 0;

    public override bool IsAllowed(IRunState runState) =>
        BoardGameRun.IsActive(runState) && BgAscension.Level(runState) >= MinAscension;

    protected Player Player => Owner!;

    protected EventOption Option(string name, Func<Task>? onChosen, string page = "INITIAL") =>
        new(this, onChosen, $"{Id.Entry}.pages.{page}.options.{name}");

    // An option that can be chosen only when the condition holds; otherwise it is shown locked.
    protected EventOption Option(string name, bool available, Func<Task> onChosen, string page = "INITIAL") =>
        Option(name, available ? onChosen : null, page);

    protected EventOption Leave(string page = "INITIAL") => Option("LEAVE", () =>
    {
        Finish("LEAVE");
        return Task.CompletedTask;
    }, page);

    protected LocString Page(string page) => L10NLookup($"{Id.Entry}.pages.{page}.description");

    protected void Show(string page, params EventOption[] options) => SetEventState(Page(page), options);

    protected void Finish(string page) => SetEventFinished(Page(page));

    // Common effects of event options.

    protected Task LoseHp(int amount) =>
        CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Player.Creature, amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);

    protected Task Heal(decimal amount) => CreatureCmd.Heal(Player.Creature, amount);

    protected Task HealToFull() => Heal(Player.Creature.MaxHp - Player.Creature.CurrentHp);

    protected Task GainGold(int amount) => PlayerCmd.GainGold(amount, Player);

    protected Task LoseGold(int amount) => PlayerCmd.LoseGold(Math.Min(amount, Player.Gold), Player, GoldLossType.Spent);

    protected Task GainCurse() => RewardDeck.GainCurses(Player);

    // A relic from the top of the relic deck.
    protected Task GainRelic() => RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Player).ToMutable(), Player);

    protected Task Offer(params Reward[] rewards) => RewardsCmd.OfferCustom(Player, rewards.ToList());

    protected Task OfferCards(int count = 3, bool rare = false) => Offer(RewardDeck.Reward(Player, count, rare));

    protected Task OfferPotions(int count) => Offer(Enumerable.Range(0, count).Select(_ => (Reward)new PotionReward(Player)).ToArray());

    protected async Task GainRandomCards(int count, bool rare = false)
    {
        foreach (var card in RewardDeck.Draw(Player, count, rare))
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
        }
    }

    protected async Task Upgrade(int count = 1)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, count);
        CardCmd.Upgrade((await CardSelectCmd.FromDeckForUpgrade(Player, prefs)).ToList(), MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.HorizontalLayout);
    }

    protected void UpgradeRandom(int count)
    {
        var cards = Player.Deck.Cards.Where(c => c.IsUpgradable).ToList().UnstableShuffle(Rng).Take(count).ToList();
        CardCmd.Upgrade(cards, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.HorizontalLayout);
    }

    protected async Task Remove(int count = 1, Func<CardModel, bool>? filter = null)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, count);
        await CardPileCmd.RemoveFromDeck((await CardSelectCmd.FromDeckForRemoval(Player, prefs, filter)).ToList());
    }

    protected async Task Transform(int count = 1)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count);
        foreach (var card in (await CardSelectCmd.FromDeckForTransformation(Player, prefs)).ToList())
        {
            await RewardDeck.Transform(card);
        }
    }

    protected bool HasUpgradableCards => Player.Deck.Cards.Any(c => c.IsUpgradable);

    protected bool HasRemovableCards => Player.Deck.Cards.Any(c => c.IsRemovable);

    // Relics from the relic deck that can be given up to pay for something (not starters or boss relics).
    protected IReadOnlyList<RelicModel> PayableRelics =>
        Player.Relics.Where(r => r is BgRelic && r.Rarity == RelicRarity.Common).ToList();

    // The player picks one of their relics on the relic selection screen and gives it up.
    protected async Task<RelicModel?> ChooseRelicToGive()
    {
        var relic = PayableRelics.Count == 0 ? null : await RelicSelectCmd.FromChooseARelicScreen(Player, PayableRelics);
        if (relic != null)
        {
            await RelicCmd.Remove(relic);
        }
        return relic;
    }

    protected async Task<PotionModel?> ChoosePotionToGive(string prompt)
    {
        var potion = await Choice.Choose(new BlockingPlayerChoiceContext(), Player, prompt, Player.Potions.ToList(),
            p => new Choice.Option(p.Title.GetFormattedText(), p.DynamicDescription.GetFormattedText()));
        if (potion != null)
        {
            await PotionCmd.Discard(potion);
        }
        return potion;
    }

    private int _rolls;

    // Rolls the die for this event. The player may change the result with Gambling Chip, The Abacus, Toolbox, or
    // Gambler's Brew. In a shared event the party shares one roll: the first living player rolls, and may change it
    // with any party member's relics and potions; everyone else's copy of the event waits for the result.
    protected async Task<int> RollDie(Func<int, string> describe)
    {
        if (!IsShared || Player.RunState.Players.Count == 1)
        {
            return await EventDie.Roll(Player, Rng, describe, [Player]);
        }
        var players = Player.RunState.Players;
        var roller = players.FirstOrDefault(p => p.Creature.IsAlive) ?? players[0];
        var key = $"{Player.RunState.Rng.Seed}:{Player.RunState.TotalFloor}:{Id.Entry}:{_rolls++}";
        var shared = EventDie.SharedRoll(key);
        if (Player == roller)
        {
            shared.TrySetResult(await EventDie.Roll(Player, Rng, describe, players));
        }
        var roll = await shared.Task;
        EventDie.Forget(key, players.Count);
        return roll;
    }
}

internal static class EventDie
{
    private static readonly Dictionary<string, (TaskCompletionSource<int> Roll, int Readers)> Shared = [];

    public static TaskCompletionSource<int> SharedRoll(string key)
    {
        if (!Shared.TryGetValue(key, out var entry))
        {
            Shared[key] = entry = (new TaskCompletionSource<int>(), 0);
        }
        return entry.Roll;
    }

    public static void Forget(string key, int players)
    {
        if (Shared.TryGetValue(key, out var entry) && (Shared[key] = (entry.Roll, entry.Readers + 1)).Readers >= players)
        {
            Shared.Remove(key);
        }
    }

    // The player rolls; the relics and potions of the given players can change the roll.
    public static async Task<int> Roll(Player player, MegaCrit.Sts2.Core.Random.Rng rng, Func<int, string> describe, IReadOnlyList<Player> helpers)
    {
        var roll = rng.NextInt(1, 7);
        var usedChip = false;
        var usedAbacus = false;
        var usedToolbox = false;
        while (true)
        {
            var actions = new List<(Choice.Option Option, Func<Task<bool>> Run)>
            {
                (new($"Keep {roll}", describe(roll)), () => Task.FromResult(true)),
            };
            if (!usedChip && helpers.Any(p => p.Relics.OfType<BgGamblingChip>().Any()))
            {
                actions.Add((new("Gambling Chip", "Reroll the die."), () =>
                {
                    usedChip = true;
                    roll = rng.NextInt(1, 7);
                    return Task.FromResult(false);
                }));
            }
            if (!usedAbacus && helpers.Any(p => p.Relics.OfType<BgTheAbacus>().Any()))
            {
                var next = roll % 6 + 1;
                actions.Add((new($"The Abacus: {next}", describe(next)), () =>
                {
                    usedAbacus = true;
                    roll = next;
                    return Task.FromResult(false);
                }));
            }
            if (!usedToolbox && helpers.Any(p => p.Relics.OfType<BgToolbox>().Any()))
            {
                var previous = (roll + 4) % 6 + 1;
                actions.Add((new($"Toolbox: {previous}", describe(previous)), () =>
                {
                    usedToolbox = true;
                    roll = previous;
                    return Task.FromResult(false);
                }));
            }
            if (helpers.SelectMany(p => p.Potions.OfType<BgGamblersBrew>()).FirstOrDefault() is { } brew)
            {
                actions.Add((new("Gambler's Brew", "Discard it and choose the result."), async () =>
                {
                    await PotionCmd.Discard(brew);
                    var faces = Enumerable.Range(1, 6).Select(f => new Choice.Option(f.ToString(), describe(f))).ToList();
                    var chosen = await Choice.Choose(new BlockingPlayerChoiceContext(), player, "Choose the result", faces);
                    roll = chosen < 0 ? roll : chosen + 1;
                    return true;
                }));
            }
            if (actions.Count == 1)
            {
                return roll;
            }
            var index = await Choice.Choose(new BlockingPlayerChoiceContext(), player, $"You rolled {roll}",
                actions.Select(a => a.Option).ToList());
            if (index < 0 || await actions[index].Run())
            {
                return roll;
            }
        }
    }
}
