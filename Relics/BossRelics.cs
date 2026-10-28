using BoardGame.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Relics;

// Boss relics that give 1 Energy at the start of each turn, with a drawback.
public abstract class BgEnergyBossRelic : BgBossRelic
{
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1 : amount;
}

public sealed class BgCoffeeDripper : BgEnergyBossRelic
{
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options) =>
        player == Owner && RemoveAll<HealRestSiteOption>(options);

    internal static bool RemoveAll<TOption>(ICollection<RestSiteOption> options)
    {
        var remove = options.OfType<TOption>().Cast<RestSiteOption>().ToList();
        foreach (var option in remove)
        {
            options.Remove(option);
        }
        return remove.Count > 0;
    }
}

public sealed class BgFusionHammer : BgEnergyBossRelic
{
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options) =>
        player == Owner && BgCoffeeDripper.RemoveAll<SmithRestSiteOption>(options);
}

public sealed class BgEctoplasm : BgEnergyBossRelic
{
    public override decimal ModifyGoldGained(Player player, decimal amount) => player == Owner ? 0m : amount;
}

public sealed class BgSozu : BgEnergyBossRelic
{
    public override bool ShouldProcurePotion(PotionModel potion, Player player) => player != Owner;
}

public sealed class BgCursedKey : BgEnergyBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained() => PickupEffects.GainRandomCurses(Owner, 2);
}

public sealed class BgMarkOfPain : BgEnergyBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        await CreatureCmd.SetMaxHp(Owner.Creature, 6);
        if (Owner.Creature.CurrentHp > 6)
        {
            await CreatureCmd.SetCurrentHp(Owner.Creature, 6);
        }
    }
}

public sealed class BgBlackBlood : BgBossRelic
{
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (!Owner.Creature.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner.Creature, 2);
        }
    }
}

public sealed class BgFrozenCore : BgBossRelic
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner.Creature))
        {
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, 1, ValueProp.Unpowered, null);
        }
    }
}

public sealed class BgRingOfTheSerpent : BgBossRelic
{
    public override string BorrowedEntry => "RING_OF_THE_DRAKE";

    public override decimal ModifyHandDraw(Player player, decimal count) => player == Owner ? count + 1 : count;
}

public sealed class BgWhiteBeast : BgBossRelic
{
    public override string BorrowedEntry => "WHITE_BEAST_STATUE";

    public override bool ShouldForcePotionReward(Player player, RoomType roomType) => player == Owner && roomType.IsCombatRoom();
}

// Wrist Blade: your 0-cost Attacks (including Shivs) deal 1 more damage per hit.
public sealed class BgWristBlade : BgBossRelic
{
    public override string BorrowedEntry => "KUNAI";

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay) =>
        props.IsPoweredAttack() && cardSource is { Type: CardType.Attack } card && card.Owner == Owner
        && !card.EnergyCost.CostsX && card.EnergyCost.GetResolved() == 0 ? 1m : 0m;
}

public sealed class BgAstrolabe : BgBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 3);
        foreach (var card in await CardSelectCmd.FromDeckForUpgrade(Owner, prefs))
        {
            CardCmd.Upgrade(card);
        }
    }
}

public sealed class BgCallingBell : BgBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        await PickupEffects.GainRandomCurses(Owner, 1);
        await RewardsCmd.OfferCustom(Owner,
            [new RelicReward(RelicRarity.Common, Owner), new RelicReward(RelicRarity.Common, Owner), new RelicReward(RelicRarity.Common, Owner)]);
    }
}

public sealed class BgEmptyCage : BgBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2);
        foreach (var card in await CardSelectCmd.FromDeckForRemoval(Owner, prefs))
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
    }
}

// Enchiridion: look at 5 rare cards and add one to your deck.
public sealed class BgEnchiridion : BgBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained()
    {
        var options = CardCreationOptions.ForNonCombatWithUniformOdds([Owner.Character.CardPool], c => c.Rarity == CardRarity.Rare);
        return RewardsCmd.OfferCustom(Owner, [new CardReward(options, 5, Owner)]);
    }
}

public sealed class BgOrrery : BgBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained() =>
        RewardsCmd.OfferCustom(Owner, Enumerable.Range(0, 4).Select(_ => (Reward)PickupEffects.CardReward(Owner)).ToList());
}

public sealed class BgPandorasBox : BgBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 3);
        foreach (var card in (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList())
        {
            await CardCmd.Transform(card, CardFactory.CreateRandomCardForTransform(card, false, Owner.RunState.Rng.Niche));
        }
    }
}

// Tiny House: a card reward, a potion, 3 gold, and upgrade a card.
public sealed class BgTinyHouse : BgBossRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        foreach (var card in await CardSelectCmd.FromDeckForUpgrade(Owner, prefs))
        {
            CardCmd.Upgrade(card);
        }
        await RewardsCmd.OfferCustom(Owner, [PickupEffects.CardReward(Owner), new PotionReward(Owner), new GoldReward(3, Owner)]);
    }
}

// Relics from the relic deck that are used as soon as you get them.

public sealed class BgOldCoin : BgDeckRelic
{
    public override bool HasUponPickupEffect => true;
    public override bool IsAllowedInShops => false;

    public override Task AfterObtained() => PlayerCmd.GainGold(10, Owner);
}

public sealed class BgWarPaint : BgDeckRelic
{
    public override int Price => 8;
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained() => PickupEffects.UpgradeStarterAndAnother(Owner, CardTag.Defend, CardType.Skill);
}

public sealed class BgWhetstone : BgDeckRelic
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained() => PickupEffects.UpgradeStarterAndAnother(Owner, CardTag.Strike, CardType.Attack);
}

// Prismatic Shard (from an event): 2 more orb slots in every combat.
public sealed class BgPrismaticShard : BgRelic
{
    public override string BorrowedEntry => "PRISMATIC_GEM";
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsAllowedInShops => false;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await OrbCmd.AddSlots(Owner, 2);
    }
}

internal static class PickupEffects
{
    public static CardReward CardReward(Player player) =>
        new(CardCreationOptions.ForNonCombatWithDefaultOdds([player.Character.CardPool]), 3, player);

    public static async Task GainRandomCurses(Player player, int count)
    {
        var curses = Core.Content.Models<BgCurseCard, CardModel>().Where(c => c is not BgAscendersBane).ToList();
        var chosen = Enumerable.Range(0, count).Select(_ => player.RunState.Rng.Niche.NextItem(curses)!).ToList();
        await CardPileCmd.AddCursesToDeck(chosen, player);
    }

    // War Paint and Whetstone: upgrade a starter Defend (Strike), then choose another Skill (Attack) to upgrade.
    public static async Task UpgradeStarterAndAnother(Player player, CardTag starterTag, CardType type)
    {
        var deck = PileType.Deck.GetPile(player).Cards;
        if (deck.FirstOrDefault(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(starterTag) && c.IsUpgradable) is { } starter)
        {
            CardCmd.Upgrade(starter);
        }
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
        foreach (var card in await CardSelectCmd.FromDeckGeneric(player, prefs, c => c.Type == type && c.IsUpgradable))
        {
            CardCmd.Upgrade(card);
        }
    }
}
