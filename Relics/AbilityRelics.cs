using BoardGame.Cards;
using BoardGame.Cards.Watcher;
using BoardGame.Core;
using BoardGame.Potions;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Relics;

// The board game's "once per combat" relics are used by clicking them. STS2 relics can't be clicked, so each one puts
// an ability card in your hand at the start of combat. Ability cards are free, Retained, and leave combat when used.
// They are Quest cards, so they don't lock in the die or count as cards played.
public abstract class BgAbilityCard() : BgTokenCard(0, CardType.Quest, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override CardLocation GetResultLocationForCardPlay() => new(Owner, PileType.None, CardPilePosition.Bottom);

    // Some abilities can only be used in certain situations.
    protected virtual bool CanUse => true;

    protected override bool IsPlayable => CanUse;

    protected BgRules? Rules => BgRules.For(Owner.RunState);
}

public abstract class BgAbilityRelic<TCard> : BgDeckRelic where TCard : BgAbilityCard
{
    protected virtual int Copies => 1;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || player.PlayerCombatState?.TurnNumber != 1 || Owner.Creature.CombatState is not { } combat)
        {
            return;
        }
        Flash();
        for (var i = 0; i < Copies; i++)
        {
            await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<TCard>(Owner), PileType.Hand, Owner);
        }
    }
}

public sealed class BgAkabeko : BgAbilityRelic<BgAkabekoAbility>
{
    public override int Price => 6;
}

public sealed class BgAkabekoAbility : BgAbilityCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        PowerCmd.Apply<BgAkabekoPower>(choiceContext, Me, 1, Me, null);
}

public sealed class BgBlueCandle : BgAbilityRelic<BgBlueCandleAbility>;

public sealed class BgBlueCandleAbility : BgAbilityCard
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var prefs = new CardSelectorPrefs(Choice.PromptText("Blue Candle: Exhaust up to 2 cards"), 0, 2);
        foreach (var card in (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, c => c is not BgTokenCard, this)).ToList())
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
    }
}

public sealed class BgCalipers : BgAbilityRelic<BgCalipersAbility>
{
    public override int Price => 6;
}

public sealed class BgCalipersAbility : BgAbilityCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        PowerCmd.Apply<BgCalipersPower>(choiceContext, Me, 1, Me, null);
}

public sealed class BgCentennialPuzzle : BgAbilityRelic<BgCentennialPuzzleAbility>
{
    public override int Price => 8;
}

public sealed class BgCentennialPuzzleAbility : BgAbilityCard
{
    protected override bool CanUse => Rules?.LostHpThisCombat(Owner) == true;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Draw(choiceContext, 3);
}

public sealed class BgDeadBranch : BgAbilityRelic<BgDeadBranchAbility>
{
    public override int Price => 9;
}

public sealed class BgDeadBranchAbility : BgAbilityCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Draw(choiceContext, PileType.Exhaust.GetPile(Owner).Cards.Count);
}

public sealed class BgGoldenEye : BgAbilityRelic<BgGoldenEyeAbility>;

public sealed class BgGoldenEyeAbility : BgAbilityCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Scry.Run(choiceContext, Owner, 3);
}

public sealed class BgMummifiedHand : BgAbilityRelic<BgMummifiedHandAbility>
{
    public override int Price => 10;
}

public sealed class BgMummifiedHandAbility : BgAbilityCard
{
    protected override bool CanUse => Rules?.PlayedPowerThisTurn(Owner) == true;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => PlayerCmd.GainEnergy(2, Owner);
}

public sealed class BgRunicPyramid : BgAbilityRelic<BgRunicPyramidAbility>
{
    public override int Price => 6;
}

public sealed class BgRunicPyramidAbility : BgAbilityCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        PowerCmd.Apply<BgTemporaryRetainPower>(choiceContext, Me, 10, Me, null);
}

public sealed class BgSelfFormingClay : BgAbilityRelic<BgSelfFormingClayAbility>
{
    public override int Price => 8;
}

public sealed class BgSelfFormingClayAbility : BgAbilityCard
{
    protected override bool CanUse => Rules?.LostHpThisCombat(Owner) == true;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        CreatureCmd.GainBlock(Me, 3, ValueProp.Unpowered, null);
}

// Boss relic: twice per combat, gain 1 Energy.
public sealed class BgHolyWater : BgAbilityRelic<BgHolyWaterAbility>
{
    public override MegaCrit.Sts2.Core.Entities.Relics.RelicRarity Rarity => MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Ancient;
    public override bool IsAllowedInShops => false;
    protected override int Copies => 2;
}

public sealed class BgHolyWaterAbility : BgAbilityCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => PlayerCmd.GainEnergy(1, Owner);
}

// Ninja Scroll: once per combat, two separate 0-cost attacks that deal 1 damage.
public sealed class BgNinjaScroll : BgDeckRelic
{
    public override int Price => 6;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && player.PlayerCombatState?.TurnNumber == 1 && Owner.Creature.CombatState is { } combat)
        {
            Flash();
            for (var i = 0; i < 2; i++)
            {
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<BgHit>(Owner), PileType.Hand, Owner);
            }
        }
    }
}

public sealed class BgHit() : BgTokenCard(0, CardType.Attack, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
}

// Die modifiers: once per combat, before the die locks in, change the roll.
public abstract class BgDieModifierAbility : BgAbilityCard
{
    protected override bool CanUse => Rules is { Roll: > 0 } rules && !rules.IsLockedIn(Owner);

    protected abstract int NewRoll(BgRules rules);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Rules is { } rules && CombatState is { } combat)
        {
            await rules.SetRoll(combat, NewRoll(rules));
        }
    }
}

public sealed class BgGamblingChip : BgAbilityRelic<BgGamblingChipAbility>
{
    public override int Price => 6;
}

public sealed class BgGamblingChipAbility : BgDieModifierAbility
{
    protected override int NewRoll(BgRules rules) => Owner.RunState.Rng.MonsterAi.NextInt(1, 7);
}

public sealed class BgTheAbacus : BgAbilityRelic<BgTheAbacusAbility>
{
    public override int Price => 6;
}

public sealed class BgTheAbacusAbility : BgDieModifierAbility
{
    protected override int NewRoll(BgRules rules) => rules.Roll % 6 + 1;
}

public sealed class BgToolbox : BgAbilityRelic<BgToolboxAbility>;

public sealed class BgToolboxAbility : BgDieModifierAbility
{
    protected override int NewRoll(BgRules rules) => (rules.Roll + 4) % 6 + 1;
}

// The Courier: once per combat, look at the top card of the potion or relic deck, then buy it or discard it.
public sealed class BgTheCourier : BgAbilityRelic<BgTheCourierAbility>
{
    public override int Price => 6;
}

public sealed class BgTheCourierAbility : BgAbilityCard
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Using The Courier locks in the die first, like a potion.
        if (Rules is { } rules)
        {
            await rules.LockIn(choiceContext, Owner);
        }
        var hasPotions = Owner.Character.PotionPool.AllPotions.Any();
        var deck = hasPotions
            ? await Choice.Choose(choiceContext, Owner, "The Courier: look at the top card of a deck",
                [new Choice.Option("Relic deck", "Look at the next relic."), new Choice.Option("Potion deck", "Look at the next potion.")])
            : 0;
        if (deck == 0)
        {
            var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
            var price = relic is BgRelic bgRelic ? bgRelic.Price : relic.MerchantCost;
            if (await Offer(choiceContext, relic.Title.GetFormattedText(), relic.DynamicDescription.GetFormattedText(), price))
            {
                await RelicCmd.Obtain(relic, Owner);
            }
        }
        else if (deck == 1)
        {
            var potion = PotionFactory.CreateRandomPotionOutOfCombat(Owner, Owner.RunState.Rng.Niche);
            var price = potion is BgPotion bgPotion ? bgPotion.Price : 3;
            if (await Offer(choiceContext, potion.Title.GetFormattedText(), potion.DynamicDescription.GetFormattedText(), price))
            {
                await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
            }
        }
    }

    private async Task<bool> Offer(PlayerChoiceContext choiceContext, string title, string description, int price)
    {
        var canAfford = Owner.Gold >= price;
        List<Choice.Option> options = canAfford
            ? [new($"Buy {title} ({price} gold)", description), new("Discard it", $"Discard {title}.")]
            : [new("Discard it", $"You can't afford {title} ({price} gold).")];
        var chosen = await Choice.Choose(choiceContext, Owner, $"The Courier: {title}", options);
        if (canAfford && chosen == 0)
        {
            await PlayerCmd.LoseGold(price, Owner, GoldLossType.Spent);
            return true;
        }
        return false;
    }
}
