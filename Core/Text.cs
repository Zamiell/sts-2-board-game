using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace BoardGame.Core;

// The mod's text lives in embedded Localization/<table>.<part>.json files. They are merged into every language's tables,
// so board game content always has text and the rule overrides replace the vanilla descriptions.
[HarmonyPatch(typeof(LocManager), "LoadTablesFromPath")]
internal static class Text
{
    private static readonly AccessTools.FieldRef<LocManager, Dictionary<string, LocTable>> Tables =
        AccessTools.FieldRefAccess<LocManager, Dictionary<string, LocTable>>("_tables");

    internal static readonly Dictionary<string, Dictionary<string, string>> ModTables = Load();

    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var assembly = typeof(Text).Assembly;
        var tables = new Dictionary<string, Dictionary<string, string>>();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("loc/") && n.EndsWith(".json")))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            var table = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                ?? throw new InvalidDataException($"Empty localization table {name}.");
            var tableName = name["loc/".Length..].Split('.')[0];
            if (!tables.TryGetValue(tableName, out var merged))
            {
                tables[tableName] = merged = [];
            }
            foreach (var (key, value) in table)
            {
                merged[key] = value;
            }
        }
        return tables;
    }

    private static void Postfix(ref (Dictionary<string, LocTable> tables, bool overridesActive, List<LocValidationError> validationErrors) __result)
    {
        Merge(__result.tables);
    }

    // Mods can initialize after the first language load, so the live tables are merged directly too.
    internal static void MergeIntoLoadedTables()
    {
        if (LocManager.Instance != null)
        {
            Merge(Tables(LocManager.Instance));
        }
    }

    // Board game characters copy every key of the STS2 character they borrow (pronouns, quotes, unlock text, ...)
    // unless the mod defines it, so they read naturally in every language.
    private static readonly (string Table, string Own, string Borrowed)[] Aliases =
    [
        ("characters", "BG_IRONCLAD", "IRONCLAD"),
        ("characters", "BG_SILENT", "SILENT"),
        ("characters", "BG_DEFECT", "DEFECT"),
        ("characters", "BG_WATCHER", "NECROBINDER"),
    ];

    private static void Merge(Dictionary<string, LocTable> tables)
    {
        foreach (var (tableName, own, borrowed) in Aliases)
        {
            if (!tables.TryGetValue(tableName, out var table))
            {
                continue;
            }
            var copies = new Dictionary<string, string>();
            foreach (var key in table.Keys.Where(k => k.StartsWith(borrowed + ".")).ToList())
            {
                copies[own + key[borrowed.Length..]] = table.GetRawText(key);
            }
            table.MergeWith(copies);
        }
        foreach (var (name, entries) in ModTables)
        {
            if (tables.TryGetValue(name, out var table))
            {
                table.MergeWith(entries);
            }
        }
    }
}
