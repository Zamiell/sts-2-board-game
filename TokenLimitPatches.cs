using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BoardGame;

// The board game has a limited number of each token. Gaining past the limit is ignored.
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountReceived))]
internal static class PowerLimitPatch
{
    private static void Postfix(ICombatState combatState, PowerModel canonicalPower, Creature target, ref decimal __result)
    {
        __result = canonicalPower switch
        {
            VulnerablePower => Rules.ClampGain(__result, target.GetPowerAmount<VulnerablePower>(), Rules.MaxVulnerable),
            WeakPower => Rules.ClampGain(__result, target.GetPowerAmount<WeakPower>(), Rules.MaxWeak),
            StrengthPower => Rules.ClampGain(__result, target.GetPowerAmount<StrengthPower>(), Rules.MaxStrength),
            // The Poison supply is shared by every enemy in the fight.
            PoisonPower when target.Side == CombatSide.Enemy => Rules.ClampGain(__result,
                combatState.Enemies.Sum(enemy => enemy.GetPowerAmount<PoisonPower>()), Rules.MaxEnemyPoison),
            _ => __result,
        };
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.GainBlockInternal))]
internal static class BlockLimitPatch
{
    private static void Prefix(Creature __instance, ref decimal amount)
    {
        if (__instance.IsPlayer)
        {
            amount = Rules.ClampGain(amount, __instance.Block, Rules.MaxPlayerBlock);
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyEnergyGain))]
internal static class EnergyLimitPatch
{
    private static void Postfix(Player player, ref decimal __result)
    {
        __result = Rules.ClampGain(__result, player.PlayerCombatState?.Energy ?? 0, Rules.MaxEnergy);
    }
}
