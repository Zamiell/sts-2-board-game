using BoardGame.Acts;
using BoardGame.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace BoardGame.Encounters;

// Act III encounter, elite, and boss decks.

public sealed class BgJawWormsHard() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() =>
        [Make<BgJawWorm>("SDA", difficulty: 2), Make<BgJawWorm>("DAS", difficulty: 2), Make<BgJawWorm>("ASD", difficulty: 2)];
}

public sealed class BgSpireGrowthEncounter() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSpireGrowth>()];
}

public sealed class BgOrbWalker23() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgOrbWalker>("23")];
}

public sealed class BgOrbWalker32() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgOrbWalker>("32")];
}

public sealed class BgTransientEncounter() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgTransient>()];
}

public sealed class BgMawEncounter() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgMaw>()];
}

public sealed class BgWrithingMassEncounter() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgWrithingMass>()];
}

public sealed class BgThreeDarklings() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgDarkling>("2S3"), Make<BgDarkling>("32S"), Make<BgDarkling>("S32")];
}

// Ascension 7 adds a second Spiker.
public sealed class BgExploderAndFriends() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => Ascension < 7
        ? [Make<BgExploder>(), Make<BgRepulsor>("3D"), Make<BgSpiker>()]
        : [Make<BgExploder>(), Make<BgRepulsor>("3D"), Make<BgSpiker>(), Make<BgSpiker>()];
}

// Ascension 7 swaps the Spiker for a Spheric Guardian.
public sealed class BgRepulsorAndFriends() : BgEncounter<BgBeyond>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => Ascension < 7
        ? [Make<BgRepulsor>("D3"), Make<BgExploder>(), Make<BgSpiker>()]
        : [Make<BgRepulsor>("D3"), Make<BgExploder>(), Make<BgSphericGuardian>()];
}

public sealed class BgGiantHeadElite() : BgEncounter<BgBeyond>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgGiantHead>()];
}

public sealed class BgNemesisElite() : BgEncounter<BgBeyond>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgNemesis>()];
}

public sealed class BgReptomancerElite() : BgEncounter<BgBeyond>(RoomType.Elite)
{
    protected override IEnumerable<Type> Summons => [typeof(BgSnakeDagger)];
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgReptomancer>()];
}

public sealed class BgAwakenedOneBoss() : BgEncounter<BgBeyond>(RoomType.Boss)
{
    public override string BorrowedEntry => "THE_KIN_BOSS";
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCultist>(), Make<BgCultist>(), Make<BgAwakenedOne>()];
}

public sealed class BgTimeEaterBoss() : BgEncounter<BgBeyond>(RoomType.Boss)
{
    public override string BorrowedEntry => "AEONGLASS_BOSS";
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgTimeEater>()];
}

public sealed class BgDonuAndDecaBoss() : BgEncounter<BgBeyond>(RoomType.Boss)
{
    public override string BorrowedEntry => "VANTOM_BOSS";
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgDeca>(), Make<BgDonu>()];
}
