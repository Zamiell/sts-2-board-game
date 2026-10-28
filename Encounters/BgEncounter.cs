using BoardGame.Core;
using BoardGame.Monsters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Encounters;

public abstract class BgEncounter : EncounterModel, IBorrowsAssets
{
    // Board game bosses borrow an STS2 boss's map icon; other encounters have no assets of their own.
    public virtual string BorrowedEntry => Id.Entry;

    // The act this encounter belongs to and its deck: weak (first encounter), normal, elite, or boss.
    public abstract Type Act { get; }

    // The run the monsters are being generated for, so encounters can pick their Ascension 7 version.
    [ThreadStatic] internal static IRunState? GeneratingFor;

    protected static int Ascension => GeneratingFor?.AscensionLevel ?? 0;

    protected abstract IEnumerable<BgMonster> Monsters();

    // Enemies this encounter can summon mid-fight, so their visuals are loaded with the act.
    protected virtual IEnumerable<Type> Summons => [];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        Monsters().Select(m => m.GetType()).Concat(Summons).Distinct().Select(type => (MonsterModel)Content.Get(type));

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        Monsters().Select(m => ((MonsterModel)m, (string?)null)).ToList();

    protected static T Make<T>(string behavior = "", int difficulty = 0) where T : BgMonster
    {
        var monster = (T)ModelDb.Monster<T>().ToMutable();
        monster.Behavior = behavior;
        monster.Difficulty = difficulty;
        return monster;
    }
}

public abstract class BgEncounter<TAct>(RoomType roomType, bool weak = false) : BgEncounter
{
    public override Type Act => typeof(TAct);
    public override RoomType RoomType => roomType;
    public override bool IsWeak => weak;
}

[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.GenerateMonstersWithSlots))]
internal static class EncounterGenerationPatch
{
    private static void Prefix(IRunState runState) => BgEncounter.GeneratingFor = runState;
}
