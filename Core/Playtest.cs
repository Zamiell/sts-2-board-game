using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using BoardGame.Characters;

namespace BoardGame.Core;

// Automated playtests. Launched with --bg-playtest, the game plays a board game run by itself with STS2's AutoSlay
// (its developers' automatic player, which release builds don't start). See playtest.sh.
//   --bg-playtest              start AutoSlay from the main menu
//   --seed <seed>              the run's seed
//   --log-file <path>          AutoSlay's log
//   --bg-character <ID>        e.g. BG_IRONCLAD; otherwise a random board game character
//   --bg-screenshots <dir>     save a screenshot every few seconds
internal static class Playtest
{
    public static bool Active { get; } = CommandLineHelper.HasArg("bg-playtest");

    private const double ScreenshotInterval = 3.0;
    private static int _screenshots;

    [HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
    internal static class StartPatch
    {
        private static bool _started;

        private static void Postfix(NMainMenu __instance)
        {
            if (!Active || _started)
            {
                return;
            }
            _started = true;
            if (CommandLineHelper.GetValue("bg-screenshots") is { } dir)
            {
                System.IO.Directory.CreateDirectory(dir);
                ScheduleScreenshot(__instance.GetTree(), dir);
            }
            new AutoSlayer().Start(CommandLineHelper.GetValue("seed") ?? SeedHelper.GetRandomSeed(),
                CommandLineHelper.GetValue("log-file"));
        }
    }

    // AutoSlay picks a random unlocked character, so every other character is locked.
    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.UnlockIfPossible))]
    internal static class CharacterPatch
    {
        private static readonly AccessTools.FieldRef<NCharacterSelectButton, bool> IsLocked =
            AccessTools.FieldRefAccess<NCharacterSelectButton, bool>("_isLocked");

        private static void Postfix(NCharacterSelectButton __instance)
        {
            if (!Active || __instance.Character is not { } character)
            {
                return;
            }
            var wanted = CommandLineHelper.GetValue("bg-character");
            if (character is not BgCharacter || (wanted != null && character.Id.Entry != wanted))
            {
                IsLocked(__instance) = true;
            }
        }
    }

    private static void ScheduleScreenshot(SceneTree tree, string dir) =>
        tree.CreateTimer(ScreenshotInterval).Timeout += () =>
        {
            try
            {
                var image = tree.Root.GetViewport().GetTexture().GetImage();
                image.Resize(image.GetWidth() / 2, image.GetHeight() / 2);
                var name = $"{++_screenshots:D4}.jpg";
                image.SaveJpg(System.IO.Path.Combine(dir, name), 0.7f);
                AutoSlayLog.Info($"Screenshot {name}");
            }
            catch (Exception e)
            {
                AutoSlayLog.Warn($"Screenshot failed: {e.Message}");
            }
            ScheduleScreenshot(tree, dir);
        };
}
