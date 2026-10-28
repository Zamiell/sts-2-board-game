using System.Runtime.CompilerServices;
using BoardGame.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace BoardGame;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        // Resolve the private game members up front so a game update fails at load, not mid-combat.
        foreach (var type in new[] { typeof(PoisonTriggerPatch), typeof(Text), typeof(Orbs.BgOrbs), typeof(Monsters.BgMonster), typeof(Monsters.BgMonsterMovePatch) })
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }
        new Harmony("james.sts2.board-game").PatchAll(typeof(ModEntry).Assembly);
        Text.MergeIntoLoadedTables();
    }
}
