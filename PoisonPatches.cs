using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame;

// Enemies lose HP equal to their Poison every turn, and their Poison never goes down.
// Poison on players keeps the video game's decay, since the board game has no player Poison.
[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.Trigger))]
internal static class PoisonTriggerPatch
{
    // Accelerant's extra triggers are still capped by the Poison amount, as in the video game.
    internal static readonly Func<PoisonPower, int> TriggerCount =
        AccessTools.MethodDelegate<Func<PoisonPower, int>>(AccessTools.PropertyGetter(typeof(PoisonPower), "TriggerCount"));

    private static bool Prefix(PoisonPower __instance, ref Task __result)
    {
        if (__instance.Owner.Side != CombatSide.Enemy)
        {
            return true;
        }
        __result = Trigger(__instance);
        return false;
    }

    private static async Task Trigger(PoisonPower poison)
    {
        var iterations = TriggerCount(poison);
        for (var i = 0; i < iterations && poison.Owner.IsAlive; i++)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), poison.Owner, poison.Amount,
                ValueProp.Unblockable | ValueProp.Unpowered, (CardModel?)null, null);
            if (!poison.Owner.IsAlive)
            {
                await Cmd.CustomScaledWait(0.1f, 0.25f);
            }
        }
    }
}

// Keeps the incoming-damage preview in line with Poison that doesn't decay.
[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.CalculateTotalDamageNextTurn))]
internal static class PoisonPreviewPatch
{
    private static bool Prefix(PoisonPower __instance, ref int __result)
    {
        var owner = __instance.Owner;
        var combatState = owner.CombatState;
        if (owner.Side != CombatSide.Enemy || combatState == null)
        {
            return true;
        }
        var damage = Hook.ModifyDamage(combatState.RunState, combatState, owner, null, __instance.Amount,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, ModifyDamageHookType.All, CardPreviewMode.None, out _);
        __result = (int)(damage * PoisonTriggerPatch.TriggerCount(__instance));
        return false;
    }
}
