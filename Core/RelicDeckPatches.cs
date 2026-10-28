using BoardGame.Characters;
using BoardGame.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// Board game runs draw relics only from the board game's relic deck, which has no rarities.

[HarmonyPatch(typeof(RelicGrabBag), nameof(RelicGrabBag.Populate), [typeof(Player), typeof(Rng)])]
internal static class PlayerRelicDeckPatch
{
    private static bool Prefix(RelicGrabBag __instance, Player player, Rng rng)
    {
        if (player.Character is not BgCharacter)
        {
            return true;
        }
        __instance.Populate(ModelDb.RelicPool<BgRelicPool>().GetUnlockedRelics(player.UnlockState), rng);
        return false;
    }
}

// The shared relic bag (used in multiplayer treasure rooms) is filled when a new run starts.
[HarmonyPatch(typeof(RunManager), "InitializeNewRun")]
internal static class SharedRelicDeckPatch
{
    [ThreadStatic] internal static bool BoardGameRunStarting;

    private static void Prefix(RunManager __instance) =>
        BoardGameRunStarting = BoardGameRun.IsActive(__instance.DebugOnlyGetState());

    private static void Finalizer() => BoardGameRunStarting = false;
}

[HarmonyPatch(typeof(RelicGrabBag), nameof(RelicGrabBag.Populate), [typeof(IEnumerable<RelicModel>), typeof(Rng)])]
internal static class SharedRelicDeckPopulatePatch
{
    private static void Prefix(ref IEnumerable<RelicModel> relics)
    {
        if (SharedRelicDeckPatch.BoardGameRunStarting)
        {
            relics = ModelDb.RelicPool<BgRelicPool>().AllRelics;
        }
    }
}

// Every board game relic is Common, so any requested rarity draws from the one deck.
[HarmonyPatch(typeof(RelicGrabBag), "GetAvailableDeque")]
internal static class RelicDeckRarityPatch
{
    private static readonly AccessTools.FieldRef<RelicGrabBag, Dictionary<RelicRarity, List<RelicModel>>> Deques =
        AccessTools.FieldRefAccess<RelicGrabBag, Dictionary<RelicRarity, List<RelicModel>>>("_deques");

    private static void Prefix(RelicGrabBag __instance, ref RelicRarity rarity)
    {
        var deques = Deques(__instance);
        if (deques.Count > 0 && deques.Keys.All(r => r == RelicRarity.Common))
        {
            rarity = RelicRarity.Common;
        }
    }
}
