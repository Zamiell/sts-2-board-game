using BoardGame.Cards;
using BoardGame.Core;
using BoardGame.Powers;
using BoardGame.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Potions;

// The board game's potion deck. Potions that appear twice in the deck are twice as likely to be drawn (Copies).

public sealed class BgAncientPotion : BgPotion
{
    public override string BorrowedEntry => "CURE_ALL";
    public override int Price => 2;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PowerCmd.Remove<WeakPower>(Owner.Creature);
        await PowerCmd.Remove<VulnerablePower>(Owner.Creature);
    }
}

public sealed class BgAttackPotion : BgPotion
{
    public override int Price => 2;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        PowerCmd.Apply<BgDoubleAttackPower>(choiceContext, Owner.Creature, 1, Owner.Creature, null);
}

public sealed class BgSkillPotion : BgPotion
{
    public override int Price => 2;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        PowerCmd.Apply<BgDoubleSkillPower>(choiceContext, Owner.Creature, 1, Owner.Creature, null);
}

public sealed class BgBlockPotion : BgPotion
{
    public override int Price => 2;
    public override int Copies => 2;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        CreatureCmd.GainBlock(target ?? Owner.Creature, 2, ValueProp.Unpowered, null);
}

public sealed class BgBloodPotion : BgPotion
{
    public override int Price => 3;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) => CreatureCmd.Heal(Owner.Creature, 2);
}

// Cunning Potion: three separate 0-cost Attacks that deal 1 damage.
public sealed class BgCunningPotion : BgPotion
{
    public override int Price => 3;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner.Creature.CombatState is { } combat)
        {
            for (var i = 0; i < 3; i++)
            {
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<BgHit>(Owner), PileType.Hand, Owner);
            }
        }
    }
}

// Distilled Chaos: draw 3 cards and play them for free.
public sealed class BgDistilledChaos : BgPotion
{
    public override int Price => 3;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var drawn = (await CardPileCmd.Draw(choiceContext, 3, Owner)).ToList();
        foreach (var card in drawn.Where(c => c.Pile?.Type == PileType.Hand))
        {
            Creature? cardTarget = null;
            if (card.TargetType == TargetType.AnyEnemy)
            {
                cardTarget = await Rows.ChooseEnemy(choiceContext, Owner, $"Distilled Chaos: choose a target for {card.Title}");
            }
            await CardCmd.AutoPlay(choiceContext, card, cardTarget);
        }
    }
}

public sealed class BgElixir : BgPotion
{
    public override string BorrowedEntry => "CLARITY";
    public override int Price => 2;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var prefs = new CardSelectorPrefs(Choice.PromptText("Elixir: Exhaust up to 3 cards"), 0, 3);
        foreach (var card in (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, c => c is not BgTokenCard, this)).ToList())
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
    }
}

public sealed class BgEnergyPotion : BgPotion
{
    public override int Price => 2;
    public override int Copies => 2;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) => PlayerCmd.GainEnergy(2, Owner);
}

// Entropic Brew: gain 2 potions.
public sealed class BgEntropicBrew : BgPotion
{
    public override int Price => 3;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        foreach (var potion in PotionFactory.CreateRandomPotionsOutOfCombat(Owner, 2, Owner.RunState.Rng.CombatPotionGeneration).ToList())
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }
}

public sealed class BgExplosivePotion : BgPotion
{
    public override string BorrowedEntry => "EXPLOSIVE_AMPOULE";
    public override int Price => 2;
    public override int Copies => 2;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        Rows.DamageAnyRow(choiceContext, Owner, 2, "Explosive Potion");
}

// Fairy Potion: when you would die, heal to 2 HP instead and discard this potion.
public sealed class BgFairyPotion : BgPotion
{
    public override string BorrowedEntry => "FAIRY_IN_A_BOTTLE";
    public override int Price => 3;
    public override PotionUsage Usage => PotionUsage.Automatic;
    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var creature = target ?? Owner.Creature;
        await CreatureCmd.Heal(creature, Math.Max(0, 2 - creature.CurrentHp));
    }

    public override bool ShouldDie(Creature creature) => creature != Owner.Creature;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        await OnUseWrapper(new ThrowingPlayerChoiceContext(), creature);
    }
}

public sealed class BgFearPotion : BgPotion
{
    public override string BorrowedEntry => "VULNERABLE_POTION";
    public override int Price => 2;
    public override int Copies => 2;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        PowerCmd.Apply<VulnerablePower>(choiceContext, target!, 1, Owner.Creature, null);
}

public sealed class BgWeakPotion : BgPotion
{
    public override int Price => 2;
    public override int Copies => 2;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        PowerCmd.Apply<WeakPower>(choiceContext, target!, 2, Owner.Creature, null);
}

public sealed class BgFirePotion : BgPotion
{
    public override int Price => 2;
    public override int Copies => 2;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        CreatureCmd.Damage(choiceContext, target!, 4, ValueProp.Unpowered, Owner.Creature, null, null);
}

// Gambler's Brew: before the die locks in, change it to any number.
public sealed class BgGamblersBrew : BgPotion
{
    public override int Price => 3;

    public override bool PassesCustomUsabilityCheck =>
        CombatManager.Instance.IsInProgress && BgRules.For(Owner.RunState) is { Roll: > 0 } rules && !rules.IsLockedIn(Owner);

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (BgRules.For(Owner.RunState) is not { } rules || Owner.Creature.CombatState is not { } combat)
        {
            return;
        }
        var faces = Enumerable.Range(1, 6).ToList();
        var index = await Choice.Choose(choiceContext, Owner, "Gambler's Brew: change the die",
            faces.Select(f => new Choice.Option($"Roll a {f}", f == rules.Roll ? "Keep the current roll." : $"Change the die to {f}.")).ToList());
        if (index >= 0)
        {
            await rules.SetRoll(combat, faces[index]);
        }
    }
}

public sealed class BgGhostInAJar : BgPotion
{
    public override int Price => 4;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        PowerCmd.Apply<BgGhostInAJarPower>(choiceContext, Owner.Creature, 1, Owner.Creature, null);
}

// Liquid Memories: put a card from your discard pile into your hand. It costs 0 this turn.
public sealed class BgLiquidMemories : BgPotion
{
    public override int Price => 3;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var prefs = new CardSelectorPrefs(Choice.PromptText("Liquid Memories: choose a card"), 1);
        var card = (await CardSelectCmd.FromCombatPile(choiceContext, PileType.Discard.GetPile(Owner), Owner, prefs)).FirstOrDefault();
        if (card != null)
        {
            card.SetToFreeThisTurn();
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}

// Snecko Oil: draw 5 cards, then shuffle 2 Dazed into your draw pile.
public sealed class BgSneckoOil : BgPotion
{
    public override int Price => 3;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CardPileCmd.Draw(choiceContext, 5, Owner);
        if (Owner.Creature.CombatState is { } combat)
        {
            for (var i = 0; i < 2; i++)
            {
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<BgDazed>(Owner), PileType.Draw, Owner, CardPilePosition.Random);
            }
        }
    }
}

public sealed class BgSteroidPotion : BgPotion
{
    public override string BorrowedEntry => "FLEX_POTION";
    public override int Price => 2;
    public override int Copies => 2;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) =>
        RelicEffects.TemporaryStrength(choiceContext, Owner, 1);
}

public sealed class BgSwiftPotion : BgPotion
{
    public override int Price => 2;
    public override int Copies => 2;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target) => CardPileCmd.Draw(choiceContext, 3, Owner);
}
