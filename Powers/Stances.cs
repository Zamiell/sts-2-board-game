using BoardGame.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Powers;

// The Watcher's stances are powers: having neither stance power is Neutral.
public abstract class BgStance : BgPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public virtual Task OnExit(PlayerChoiceContext choiceContext) => Task.CompletedTask;
}

public static class Stance
{
    public static BgStance? Current(Player player) => player.Creature.Powers.OfType<BgStance>().FirstOrDefault();

    public static bool IsIn<TStance>(Player player) where TStance : BgStance => Current(player) is TStance;

    public static Task Enter<TStance>(PlayerChoiceContext choiceContext, Player player) where TStance : BgStance =>
        Enter(choiceContext, player, typeof(TStance));

    // Entering the stance you are already in does nothing. Leaving a stance triggers its exit effect, e.g. Calm's Energy.
    public static async Task Enter(PlayerChoiceContext choiceContext, Player player, Type? stance)
    {
        var current = Current(player);
        if (current?.GetType() == stance)
        {
            return;
        }
        BgRules.For(player.RunState)?.MarkStanceChanged(player);
        if (current != null)
        {
            await PowerCmd.Remove(current);
            await current.OnExit(choiceContext);
        }
        if (stance != null)
        {
            await PowerCmd.Apply(choiceContext, ((PowerModel)Content.Get(stance)).ToMutable(), player.Creature, 1m, player.Creature, null);
            foreach (var listener in player.Creature.Powers.OfType<IStanceListener>().ToList())
            {
                await listener.AfterStanceChanged(choiceContext, stance);
            }
        }
        else
        {
            foreach (var listener in player.Creature.Powers.OfType<IStanceListener>().ToList())
            {
                await listener.AfterStanceChanged(choiceContext, null);
            }
        }
    }

    public static Task ExitToNeutral(PlayerChoiceContext choiceContext, Player player) => Enter(choiceContext, player, null);
}

// Powers like Mental Fortress and Rushdown react to stance changes.
public interface IStanceListener
{
    Task AfterStanceChanged(PlayerChoiceContext choiceContext, Type? newStance);
}

// Wrath: +1 damage on every hit of your attacks. If you end your turn in Wrath, take 1 damage.
public sealed class BgWrathStance : BgStance
{
    public override string BorrowedEntry => "RAGE_POWER";

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || !props.IsPoweredAttack())
        {
            return 0m;
        }
        return 1m + Owner.GetPowerAmount<BgSimmeringFuryPower>();
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner))
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, Owner, 1m, ValueProp.Unpowered, null, null);
        }
    }
}

// Calm: when you leave Calm, gain 2 Energy.
public sealed class BgCalmStance : BgStance
{
    public override string BorrowedEntry => "CLARITY_POWER";

    public override async Task OnExit(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player is { } player)
        {
            await PlayerCmd.GainEnergy(2m, player);
        }
    }
}

// Simmering Fury: Wrath deals this much more damage per hit.
public sealed class BgSimmeringFuryPower : BgPower
{
    public override string BorrowedEntry => "RAGE_POWER";
}
