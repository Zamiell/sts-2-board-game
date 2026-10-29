using System.Reflection;
using BoardGame.Cards;
using BoardGame.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace BoardGame.Core;

// The board game's own 13 Ascension levels replace Slay the Spire 2's 10 for board game characters:
//  1 elites deadlier, 2 lower max HP, 3 unfavorable events, 4 fewer potion slots, 5 start cursed, 6 heal less after
//  bosses, 7 normal enemies deadlier, 8 card removal costs more, 9 start damaged, 10 bosses deadlier, 11 Act IV
//  elites, 12 elites even deadlier, 13 double boss.
// Enemy changes live with each enemy; the rest are applied here and where noted.
public static class BgAscension
{
    public const int Max = 13;

    public const int LowerMaxHp = 2;
    public const int UnfavorableEvents = 3;
    public const int FewerPotionSlots = 4;
    public const int StartCursed = 5;
    public const int HealLessAfterBoss = 6;
    public const int StartDamaged = 9;
    public const int DoubleBoss = 13;

    // Between acts you heal to full, or only this much from Ascension 6.
    public const int ActHealAtA6 = 4;

    public static int Level(IRunState? runState) => runState?.AscensionLevel ?? 0;

    internal static IRunState? CurrentRun => RunManager.Instance?.DebugOnlyGetState();

    // Whether ascension text should describe the board game's levels: in a board game run, or when a board game
    // character is selected.
    internal static bool BoardGameContext =>
        CurrentRun is { } run ? BoardGameRun.IsActive(run) : LobbyTracker.LocalCharacterIsBoardGame;
}

// Slay the Spire 2's own ascension effects don't apply to board game runs.
[HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.HasLevel))]
internal static class Sts2AscensionOffPatch
{
    private static void Postfix(ref bool __result)
    {
        if (__result && BoardGameRun.IsActive(BgAscension.CurrentRun))
        {
            __result = false;
        }
    }
}

// Start-of-run effects: Ascension 2, 4, and 5. (Ascension 9 is applied after Neow fills your HP.)
[HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.ApplyEffectsTo))]
internal static class BgAscensionStartPatch
{
    private static readonly AccessTools.FieldRef<AscensionManager, int> LevelField =
        AccessTools.FieldRefAccess<AscensionManager, int>("_level");

    private static bool Prefix(AscensionManager __instance, Player player)
    {
        if (player.Character is not BgCharacter)
        {
            return true;
        }
        var level = LevelField(__instance);
        if (level >= BgAscension.LowerMaxHp)
        {
            player.Creature.SetMaxHpInternal(player.Creature.MaxHp - 1);
            player.Creature.SetCurrentHpInternal(Math.Min(player.Creature.CurrentHp, player.Creature.MaxHp));
        }
        if (level >= BgAscension.FewerPotionSlots)
        {
            player.SubtractFromMaxPotionCount(1);
        }
        if (level >= BgAscension.StartCursed)
        {
            var bane = player.RunState.CreateCard<BgAscendersBane>(player);
            bane.FloorAddedToDeck = 1;
            player.Deck.AddInternal(bane, -1, silent: true);
        }
        return false;
    }
}

// Ascension 13: The Beyond ends with two bosses.
[HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
internal static class BgDoubleBossPatch
{
    private static void Postfix(RunManager __instance)
    {
        var state = __instance.DebugOnlyGetState();
        if (state == null || !BoardGameRun.IsActive(state) || BgAscension.Level(state) < BgAscension.DoubleBoss)
        {
            return;
        }
        var act = state.Acts.FirstOrDefault(a => a is Acts.BgBeyond);
        if (act != null && !act.HasSecondBoss)
        {
            act.SetSecondBossEncounter(state.Rng.UpFront.NextItem(act.AllBossEncounters.Where(e => e.Id != act.BossEncounter.Id)));
        }
    }
}

// Ascension text for board game characters, including levels 11-13.
[HarmonyPatch]
internal static class BgAscensionTextPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(AscensionHelper), nameof(AscensionHelper.GetTitle)),
        AccessTools.Method(typeof(AscensionHelper), nameof(AscensionHelper.GetDescription)),
    ];

    private static void Postfix(int level, MethodBase __originalMethod, ref LocString __result)
    {
        if (level > 0 && (level > AscensionManager.maxAscensionAllowed || BgAscension.BoardGameContext))
        {
            var part = __originalMethod.Name == nameof(AscensionHelper.GetTitle) ? "title" : "description";
            __result = new LocString("ascension", $"BG_LEVEL_{level:D2}.{part}");
        }
    }
}

// Remembers whether the local player picked a board game character, and refreshes the ascension text when that
// changes.
[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.SetLocalCharacter))]
internal static class LobbyTracker
{
    internal static bool LocalCharacterIsBoardGame;

    private static void Postfix(StartRunLobby __instance, CharacterModel character)
    {
        LocalCharacterIsBoardGame = character is BgCharacter;
        if (__instance.LobbyListener is NCharacterSelectScreen screen
            && AccessTools.Field(typeof(NCharacterSelectScreen), "_ascensionPanel").GetValue(screen) is NAscensionPanel panel)
        {
            AccessTools.Method(typeof(NAscensionPanel), "RefreshAscensionText").Invoke(panel, []);
        }
    }
}

// Winning on your highest Ascension unlocks the next, up to the board game's 13.
[HarmonyPatch(typeof(ProgressSaveManager), "IncrementSingleplayerAscension")]
internal static class BgSingleplayerAscensionUnlockPatch
{
    private static void Prefix(SerializableRun run, CharacterStats charStats, out bool __state) =>
        __state = run.Ascension == charStats.MaxAscension && charStats.MaxAscension >= AscensionManager.maxAscensionAllowed;

    private static void Postfix(CharacterStats charStats, bool __state)
    {
        if (__state && BgAscensionUnlocks.IsBoardGame(charStats.Id) && charStats.MaxAscension < BgAscension.Max)
        {
            charStats.MaxAscension++;
            charStats.PreferredAscension = charStats.MaxAscension;
        }
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), "IncrementMultiplayerAscension")]
internal static class BgMultiplayerAscensionUnlockPatch
{
    private static void Prefix(ProgressSaveManager __instance, SerializableRun run, out bool __state) =>
        __state = run.Ascension == __instance.Progress.MaxMultiplayerAscension
            && run.Ascension >= AscensionManager.maxAscensionAllowed
            && run.Players.Count > 0 && run.Players.All(p => BgAscensionUnlocks.IsBoardGame(p.CharacterId));

    private static void Postfix(ProgressSaveManager __instance, bool __state)
    {
        if (__state && __instance.Progress.MaxMultiplayerAscension < BgAscension.Max)
        {
            __instance.Progress.MaxMultiplayerAscension++;
            __instance.Progress.PreferredMultiplayerAscension = __instance.Progress.MaxMultiplayerAscension;
        }
    }
}

// The progress file clamps ascension to 10 when it loads; board game characters keep up to 13.
[HarmonyPatch(typeof(ProgressState), "ClampCharacterStatsFields")]
internal static class BgCharacterAscensionClampPatch
{
    private static void Prefix(CharacterStats stats, out (int Max, int Preferred) __state) =>
        __state = (stats.MaxAscension, stats.PreferredAscension);

    private static void Postfix(CharacterStats stats, (int Max, int Preferred) __state)
    {
        if (BgAscensionUnlocks.IsBoardGame(stats.Id))
        {
            stats.MaxAscension = Math.Clamp(__state.Max, 0, BgAscension.Max);
            stats.PreferredAscension = Math.Clamp(__state.Preferred, 0, BgAscension.Max);
        }
    }
}

[HarmonyPatch(typeof(ProgressState), "ClampAscension")]
internal static class BgMultiplayerAscensionClampPatch
{
    private static void Postfix(int value, ref int __result)
    {
        if (value > __result)
        {
            __result = Math.Min(value, BgAscension.Max);
        }
    }
}

internal static class BgAscensionUnlocks
{
    public static bool IsBoardGame(ModelId? id) =>
        id != null && ModelDb.GetByIdOrNull<CharacterModel>(id) is BgCharacter;
}
