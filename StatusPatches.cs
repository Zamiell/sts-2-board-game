using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame;

// Vulnerable: each hit of the next attack deals double damage, unless the attacker is Weak.
[HarmonyPatch(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative))]
internal static class VulnerableDamagePatch
{
    private static bool Prefix(VulnerablePower __instance, Creature? target, ValueProp props, Creature? dealer,
        CardModel? cardSource, ref decimal __result)
    {
        __result = 1m;
        if (target != __instance.Owner
            || !props.IsPoweredAttack()
            || !Rules.VulnerableApplies(true, dealer?.HasPower<WeakPower>() == true))
        {
            return false;
        }

        // Paper Phrog, Cruelty, and Debilitate add to the multiplier, so they still stack on top of double damage.
        var multiplier = Rules.VulnerableMultiplier;
        if (dealer != null)
        {
            var paperPhrog = dealer.Player?.GetRelic<PaperPhrog>();
            if (paperPhrog != null)
            {
                multiplier = paperPhrog.ModifyVulnerableMultiplier(target, multiplier, props, dealer, cardSource);
            }
            var cruelty = dealer.GetPower<CrueltyPower>() ?? dealer.PetOwner?.Creature.GetPower<CrueltyPower>();
            if (cruelty != null)
            {
                multiplier = cruelty.ModifyVulnerableMultiplier(target, multiplier, props, dealer, cardSource);
            }
        }
        var debilitate = target.GetPower<DebilitatePower>();
        if (debilitate != null)
        {
            multiplier = debilitate.ModifyVulnerableMultiplier(target, multiplier, props, dealer, cardSource);
        }
        __result = multiplier;
        return false;
    }
}

// Weak no longer scales damage; its flat penalty is applied with Strength in WeakPenaltyPatch.
[HarmonyPatch(typeof(WeakPower), nameof(WeakPower.ModifyDamageMultiplicative))]
internal static class WeakMultiplierPatch
{
    private static bool Prefix(ref decimal __result)
    {
        __result = 1m;
        return false;
    }
}

// Weak: each hit deals 1 less damage, unless the target is Vulnerable. WeakPower inherits the base
// additive hook, so the penalty is added there, before Vulnerable doubles the hit.
[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageAdditive))]
internal static class WeakPenaltyPatch
{
    private static void Postfix(AbstractModel __instance, Creature? target, ValueProp props, Creature? dealer,
        CardModel? cardSource, ref decimal __result)
    {
        if (__instance is not WeakPower weak
            || dealer != weak.Owner
            || !props.IsPoweredAttack()
            || !Rules.WeakApplies(true, target?.HasPower<VulnerablePower>() == true))
        {
            return;
        }

        // Paper Krane and Debilitate strengthen Weak in the video game; keep that as a bigger flat penalty.
        var multiplier = Rules.VanillaWeakMultiplier;
        var paperKrane = target?.Player?.GetRelic<PaperKrane>();
        if (paperKrane != null && target != null)
        {
            multiplier = paperKrane.ModifyWeakMultiplier(target, multiplier, props, dealer, cardSource);
        }
        var debilitate = dealer.GetPower<DebilitatePower>();
        if (debilitate != null)
        {
            multiplier = debilitate.ModifyWeakMultiplier(dealer, multiplier, props, dealer, cardSource);
        }
        __result -= Rules.WeakPenaltyFor(multiplier);
    }
}

// Vulnerable and Weak are tokens, not durations: they stay until attacks use them up.
[HarmonyPatch]
internal static class StatusDurationPatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(VulnerablePower), nameof(VulnerablePower.AfterSideTurnEnd)),
        AccessTools.Method(typeof(WeakPower), nameof(WeakPower.AfterSideTurnEnd)),
    ];

    private static bool Prefix(ref Task __result)
    {
        __result = Task.CompletedTask;
        return false;
    }
}

// After a whole attack resolves (every hit of a multi-hit), the attacker loses one Weak and each creature
// it hit loses one Vulnerable. This also covers the Weak-versus-Vulnerable case, where both are removed.
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterAttack))]
internal static class StatusRemovalPatch
{
    private static void Postfix(AttackCommand command, ref Task __result)
    {
        __result = RemoveTokens(__result, command);
    }

    private static async Task RemoveTokens(Task afterAttack, AttackCommand command)
    {
        await afterAttack;
        if (!command.DamageProps.IsPoweredAttack())
        {
            return;
        }

        var weak = command.Attacker?.GetPower<WeakPower>();
        if (weak != null && command.Attacker!.IsAlive)
        {
            await PowerCmd.Decrement(weak);
        }

        var receivers = command.Results.SelectMany(hit => hit).Select(result => result.Receiver).Distinct();
        foreach (var receiver in receivers.ToList())
        {
            var vulnerable = receiver.GetPower<VulnerablePower>();
            if (vulnerable != null && receiver.IsAlive)
            {
                await PowerCmd.Decrement(vulnerable);
            }
        }
    }
}
