using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;

if (args.Length != 2)
{
    throw new ArgumentException("Usage: Compatibility <game-data-directory> <BoardGame.dll>");
}

var gameData = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(gameData, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};

var mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[1]));
var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(gameData, "sts2.dll"));
var failures = new List<string>();

// 1. Private bindings resolve and every Harmony patch installs.
foreach (var name in new[] { "BoardGame.PoisonTriggerPatch", "BoardGame.Core.Text", "BoardGame.Orbs.BgOrbs", "BoardGame.Monsters.BgMonster", "BoardGame.Monsters.BgMonsterMovePatch", "BoardGame.Characters.CharacterSelect" })
{
    RuntimeHelpers.RunClassConstructor(mod.GetType(name, throwOnError: true)!.TypeHandle);
}
var harmonyAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(gameData, "0Harmony.dll"));
var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", throwOnError: true)!;
var harmony = Activator.CreateInstance(harmonyType, "board-game.compatibility")!;
harmonyType.GetMethod("PatchAll", [typeof(Assembly)])!.Invoke(harmony, [mod]);
var patched = ((IEnumerable<MethodBase>)harmonyType.GetMethod("GetPatchedMethods")!.Invoke(harmony, null)!).ToList();

var weak = game.GetType("MegaCrit.Sts2.Core.Models.Powers.WeakPower", throwOnError: true)!;
if (weak.GetMethod("ModifyDamageAdditive")!.DeclaringType != game.GetType("MegaCrit.Sts2.Core.Models.AbstractModel"))
{
    failures.Add("WeakPower now overrides ModifyDamageAdditive; the Weak penalty patch would not run.");
}

// 2. Every content model has the text the game will look up.
var text = new Dictionary<string, Dictionary<string, string>>();
foreach (var resource in mod.GetManifestResourceNames().Where(n => n.StartsWith("loc/")))
{
    using var stream = mod.GetManifestResourceStream(resource)!;
    var table = resource["loc/".Length..].Split('.')[0];
    if (!text.TryGetValue(table, out var entries))
    {
        text[table] = entries = [];
    }
    foreach (var (key, value) in JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!)
    {
        entries[key] = value;
    }
}

var getEntry = game.GetType("MegaCrit.Sts2.Core.Models.ModelDb", throwOnError: true)!.GetMethod("GetEntry", [typeof(Type)])!;
string Entry(Type type) => (string)getEntry.Invoke(null, [type])!;
Type Model(string name) => game.GetType("MegaCrit.Sts2.Core.Models." + name, throwOnError: true)!;

var required = new (Type Base, string Table, string[] Keys)[]
{
    (Model("CardModel"), "cards", ["title", "description"]),
    (Model("PowerModel"), "powers", ["title", "description"]),
    (Model("RelicModel"), "relics", ["title", "description", "flavor"]),
    (Model("PotionModel"), "potions", ["title", "description"]),
    (Model("MonsterModel"), "monsters", ["name"]),
    (Model("EncounterModel"), "encounters", ["title", "loss"]),
    (Model("ActModel"), "acts", ["title"]),
    (Model("OrbModel"), "orbs", ["title", "description", "smartDescription"]),
    (Model("CharacterModel"), "characters", ["title", "description"]),
    (Model("ModifierModel"), "modifiers", ["title", "description"]),
    (Model("EventModel"), "events", ["title"]),
};
var contentCount = 0;
foreach (var type in mod.GetTypes().Where(t => !t.IsAbstract && t.IsClass))
{
    foreach (var (baseType, table, keys) in required.Where(r => r.Base.IsAssignableFrom(type)))
    {
        contentCount++;
        if (type.GetConstructor(Type.EmptyTypes) == null)
        {
            failures.Add($"{type.Name} has no parameterless constructor.");
        }
        var entry = Entry(type);
        foreach (var key in keys)
        {
            if (!text.TryGetValue(table, out var entries) || !entries.ContainsKey($"{entry}.{key}"))
            {
                failures.Add($"Missing text {table}/{entry}.{key} for {type.Name}.");
            }
        }
    }
}

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine("FAIL: " + failure);
    }
    Environment.Exit(1);
}

Console.WriteLine($"PASS: {patched.Count} patched game methods, {contentCount} content models with text, against the installed game.");
