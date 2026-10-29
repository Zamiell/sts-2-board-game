using BoardGame.Acts;
using BoardGame.Cards;
using BoardGame.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// STS2 lists characters, shared card pools, and acts by hand, so the board game's are appended to those lists.
[HarmonyPatch]
internal static class Registration
{
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
    [HarmonyPostfix]
    private static void AllCharacters(ref IEnumerable<CharacterModel> __result) =>
        __result = __result.Concat(BgCharacter.InBoxOrder).ToArray();

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedCardPools), MethodType.Getter)]
    [HarmonyPostfix]
    private static void AllSharedCardPools(ref IEnumerable<CardPoolModel> __result) =>
        __result = __result.Concat([
            ModelDb.CardPool<BgColorlessCardPool>(), ModelDb.CardPool<BgCurseCardPool>(),
            ModelDb.CardPool<BgStatusCardPool>(), ModelDb.CardPool<BgTokenCardPool>(),
        ]).ToArray();

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Acts), MethodType.Getter)]
    [HarmonyPostfix]
    private static void Acts(ref IEnumerable<ActModel> __result) =>
        __result = __result.Concat(Content.Models<BgAct, ActModel>()).ToList();

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Orbs), MethodType.Getter)]
    [HarmonyPostfix]
    private static void Orbs(ref IEnumerable<OrbModel> __result) =>
        __result = __result.Concat(Content.Models<Orbs.BgOrb, OrbModel>()).ToArray();

    // A party of board game characters plays the board game's acts with the board game rules.
    [HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
    [HarmonyPrefix]
    private static void CreateForNewRun(IReadOnlyList<Player> players, ref IReadOnlyList<ActModel> acts, ref IReadOnlyList<ModifierModel> modifiers)
    {
        if (!BoardGameRun.IsBoardGameParty(players))
        {
            return;
        }
        acts = Content.Models<BgAct, ActModel>().OrderBy(act => act.Index).Select(act => act.ToMutable()).ToList();
        if (!modifiers.OfType<BgRules>().Any())
        {
            modifiers = [.. modifiers, ModelDb.Modifier<BgRules>().ToMutable()];
        }
    }

    // Achievements are looked up by the character's and act's names, which board game models don't have.
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RunWonAchievement), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool RunWonAchievement(CharacterModel __instance, ref Achievement __result)
    {
        if (__instance is not BgCharacter character)
        {
            return true;
        }
        __result = ModelDb.GetById<CharacterModel>(new ModelId(character.Id.Category, character.BorrowedEntry)).RunWonAchievement;
        return false;
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.DefeatedAllEnemiesAchievement), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DefeatedAllEnemiesAchievement(ActModel __instance, ref Achievement __result)
    {
        if (__instance is not BgAct act)
        {
            return true;
        }
        __result = ModelDb.GetById<ActModel>(new ModelId(act.Id.Category, act.BorrowedEntry)).DefeatedAllEnemiesAchievement;
        return false;
    }
}
