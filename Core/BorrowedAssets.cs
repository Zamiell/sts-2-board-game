using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Core;

// Board game models have no art or sounds of their own. A model that borrows assets names an STS2 model, and every
// asset path the game builds from the board game model's ID is redirected to that model's assets.
internal interface IBorrowsAssets
{
    // The ID entry of the STS2 model whose assets are used, e.g. "IRONCLAD".
    string BorrowedEntry { get; }
}

[HarmonyPatch]
internal static class BorrowedAssetsPatch
{
    private static readonly Type[] AssetOwners =
    [
        typeof(CharacterModel), typeof(MonsterModel), typeof(PowerModel), typeof(RelicModel), typeof(PotionModel),
        typeof(EventModel), typeof(ActModel), typeof(OrbModel), typeof(EncounterModel),
    ];

    // Asset paths are string properties named after paths or sounds, e.g. VisualsPath, IconPath, AttackSfx.
    private static IEnumerable<MethodBase> TargetMethods() =>
        AssetOwners
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(property => property.PropertyType == typeof(string)
                && (property.Name.EndsWith("Path") || property.Name.EndsWith("Sfx") || property.Name == "FilePathIdentifier"))
            .Select(property => property.GetMethod)
            .Where(getter => getter != null && !getter.IsAbstract)
            .Cast<MethodBase>();

    private static void Postfix(AbstractModel __instance, ref string? __result)
    {
        if (__result != null && __instance is IBorrowsAssets borrower)
        {
            __result = Redirect(__result, __instance.Id.Entry, borrower.BorrowedEntry);
        }
    }

    internal static string Redirect(string path, string ownEntry, string borrowedEntry) =>
        path.Replace(ownEntry.ToLowerInvariant(), borrowedEntry.ToLowerInvariant());
}
