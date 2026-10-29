using BoardGame.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BoardGame.Characters;

// Board game characters borrow STS2 portraits, so on the character select screen their portraits carry the die
// (The Die's icon) in the corner, and their names say "(Board Game)". Every board game character starts with The Die,
// so the screen shows their own starting relic instead.
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
    private static void ShowBoardGameInfo(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
    {
        if (characterModel is not BgCharacter character || charSelectButton.IsLocked)
        {
            return;
        }
        var title = new LocString("characters", characterModel.Id.Entry + ".selectTitle").GetFormattedText();
        NameLabel(__instance).SetTextAutoSize(title);
        var relic = character.SignatureRelic;
        RelicTitle(__instance).Text = relic.Title.GetFormattedText();
        RelicDescription(__instance).Text = relic.DynamicDescription.GetFormattedText();
        RelicIcon(__instance).Texture = relic.Icon;
        RelicIconOutline(__instance).Texture = relic.IconOutline;
    }

    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, MegaLabel> NameLabel =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, MegaLabel>("_name");
    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, MegaRichTextLabel> RelicTitle =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, MegaRichTextLabel>("_relicTitle");
    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, MegaRichTextLabel> RelicDescription =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, MegaRichTextLabel>("_relicDescription");
    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, TextureRect> RelicIcon =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, TextureRect>("_relicIcon");
    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, TextureRect> RelicIconOutline =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, TextureRect>("_relicIconOutline");
}
