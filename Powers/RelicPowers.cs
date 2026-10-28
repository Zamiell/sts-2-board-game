using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Powers;

// Incense Burner: you can't lose more than 1 HP this round.
public sealed class BgIncenseBurnerPower : BgHpLossLimitPower;

// Ghost in a Jar: you can't lose more than 1 HP this turn.
public sealed class BgGhostInAJarPower : BgHpLossLimitPower;

public abstract class BgHpLossLimitPower : BgPower
{
    public override string BorrowedEntry => "INTANGIBLE_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    private decimal _lost;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
        {
            return amount;
        }
        var allowed = Math.Max(0m, 1m - _lost);
        var result = Math.Min(amount, allowed);
        _lost += result;
        return result;
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Akabeko: your next Attack deals this much more damage per hit.
public sealed class BgAkabekoPower : BgPower
{
    public override string BorrowedEntry => "VIGOR_POWER";

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() && cardSource?.Type == MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack ? Amount : 0m;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Cards.CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner && cardPlay.Card.Type == MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack)
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Calipers: your Block isn't removed at the start of your next turn.
public sealed class BgCalipersPower : BgPower
{
    public override string BorrowedEntry => "BLUR_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldClearBlock(Creature creature) => creature != Owner;

    public override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (preventer == this)
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Skill Potion: your next Skill this turn is played twice.
public sealed class BgDoubleSkillPower : BgPower
{
    public override string BorrowedEntry => "BURST_POWER";

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card.Owner?.Creature == Owner && card.Type == MegaCrit.Sts2.Core.Entities.Cards.CardType.Skill ? playCount + 1 : playCount;

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
