using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace BoardGame.Core;

// Resting heals 3 HP in the board game instead of 30% of max HP.
[HarmonyPatch(typeof(HealRestSiteOption), nameof(HealRestSiteOption.GetBaseHealAmount))]
internal static class RestSiteHealPatch
{
    public const int Heal = 3;

    private static void Postfix(Creature creature, ref decimal __result)
    {
        if (BoardGameRun.IsActive(creature.Player?.RunState))
        {
            __result = Heal;
        }
    }
}
