using BoardGame.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Powers;

// Apotheosis: your starter Strikes deal and starter Defends block this much more.
public sealed class BgApotheosisPower : BgPower
{
    public override string BorrowedEntry => "IMPROVEMENT_POWER";

    private static bool IsStarter(CardModel? card, CardTag tag) => card is { Rarity: CardRarity.Basic } && card.Tags.Contains(tag);

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && IsStarter(cardSource, CardTag.Strike) && props.IsPoweredAttack() ? Amount : 0m;

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay) =>
        cardSource?.Owner?.Creature == Owner && IsStarter(cardSource, CardTag.Defend) ? Amount : 0m;
}

// Apparition: you can't lose more than 1 HP this turn.
public sealed class BgApparitionPower : BgPower
{
    public override string BorrowedEntry => "INTANGIBLE_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    private int _lost;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner ? Math.Max(0, Math.Min(amount, 1 - _lost)) : amount;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner && delta < 0)
        {
            _lost += (int)-delta;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Madness: the next card you play this turn costs 0.
public sealed class BgFreeCardPower : BgPower
{
    public override string BorrowedEntry => "FREE_SKILL_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner)
        {
            return false;
        }
        modifiedCost = 0m;
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner && !cardPlay.IsAutoPlay && !BgRules.IsRelicAbility(cardPlay.Card))
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Mayhem: at the start of your turn, play the top card of your draw pile for free.
public sealed class BgMayhemPower : BgPower
{
    public override string BorrowedEntry => "MAYHEM_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            Flash();
            await CardPileCmd.AutoPlayFromDrawPile(choiceContext, player, Amount, CardPilePosition.Top, forceExhaust: false);
        }
    }
}

// Panache: at the end of your turn, if your hand is empty, deal this much damage to any row.
public sealed class BgPanachePower : BgPower
{
    public override string BorrowedEntry => "PANACHE_POWER";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner) && Owner.Player is { } player && player.PlayerCombatState?.Hand.Cards.All(c => c is BoardGame.Cards.BgTokenCard) == true)
        {
            Flash();
            await Rows.DamageAnyRow(choiceContext, player, Amount, "Panache");
        }
    }
}

// Sadistic Nature: whenever you apply Weak, Vulnerable, or Poison to an enemy, deal this much damage to it for each.
public sealed class BgSadisticNaturePower : BgPower
{
    public override string BorrowedEntry => "SADISTIC_POWER";

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (applier != Owner || amount <= 0 || power.Owner == Owner || power is not (WeakPower or VulnerablePower or PoisonPower) || !power.Owner.IsAlive)
        {
            return;
        }
        Flash();
        await CreatureCmd.Damage(choiceContext, power.Owner, Amount * amount, ValueProp.Unpowered, Owner, null, null);
    }
}

// The Bomb: at the end of 3 turns, deal damage to ALL enemies. Each Bomb counts down on its own.
public sealed class BgTheBombPower : BgPower
{
    public override string BorrowedEntry => "THE_BOMB_POWER";
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public int Damage { get; set; }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || Owner.CombatState is not { } combat)
        {
            return;
        }
        if (Amount <= 1)
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, combat.HittableEnemies.ToList(), Damage, ValueProp.Unpowered, Owner, null, null);
            await PowerCmd.Remove(this);
        }
        else
        {
            await PowerCmd.Decrement(this);
        }
    }
}
