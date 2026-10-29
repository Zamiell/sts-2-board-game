using BoardGame.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BoardGame.Characters;

// Board game characters borrow STS2 portraits, so on the character select screen their portraits carry the die
// (The Die's icon) in the corner, and their names say "(Board Game)".
[HarmonyPatch]
internal static class CharacterSelect
{
    private const string BadgeName = "BgBadge";
    private const float BadgeSize = 40f;

    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
    [HarmonyPostfix]
    private static void AddBadge(NCharacterSelectButton __instance, CharacterModel character)
    {
        if (character is not BgCharacter || __instance.HasNode(BadgeName))
        {
            return;
        }
        var badge = new TextureRect
        {
            Name = BadgeName,
            Texture = ModelDb.Relic<BgTheDie>().Icon,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        badge.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        badge.OffsetLeft = -BadgeSize;
        badge.OffsetTop = -BadgeSize;
        badge.OffsetRight = 4f;
        badge.OffsetBottom = 4f;
        __instance.AddChild(badge);
    }

    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPostfix]
    private static void RenameSelected(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
    {
        if (characterModel is not BgCharacter || charSelectButton.IsLocked)
        {
            return;
        }
        var title = new LocString("characters", characterModel.Id.Entry + ".selectTitle").GetFormattedText();
        NameLabel(__instance).SetTextAutoSize(title);
    }

    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, MegaLabel> NameLabel =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, MegaLabel>("_name");
}
