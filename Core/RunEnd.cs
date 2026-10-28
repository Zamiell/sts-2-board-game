using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// After the last board game boss the run is won. STS2's final Architect event has no dialogue for board game
// characters, so it's skipped.
[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
internal static class RunEndPatch
{
    private static bool Prefix(RunManager __instance, ref Task __result)
    {
        var state = __instance.DebugOnlyGetState();
        if (state == null || !BoardGameRun.IsActive(state) || state.CurrentActIndex < state.Acts.Count - 1)
        {
            return true;
        }
        __result = __instance.WinRun();
        return false;
    }
}
