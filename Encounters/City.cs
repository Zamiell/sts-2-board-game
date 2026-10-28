using BoardGame.Acts;
using BoardGame.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace BoardGame.Encounters;

// Act II encounter, elite, and boss decks.

public sealed class BgSneckoEncounter() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSnecko>()];
}

public sealed class BgSnakePlantEncounter() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSnakePlant>()];
}

public sealed class BgThreeByrds() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgByrd>("1S3", difficulty: 1), Make<BgByrd>(), Make<BgByrd>()];
}

public sealed class BgThreeCultists() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCultist>(), Make<BgCultist>(), Make<BgCultist>()];
}

public sealed class BgChosenAndCultist() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgChosen>(), Make<BgCultist>()];
}

public sealed class BgChosenAndByrd() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgChosen>(difficulty: 1), Make<BgByrd>()];
}

public sealed class BgLooterAndMugger() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgLooter>(difficulty: 1), Make<BgMugger>()];
}

// The City's encounter deck has two Looter and Mugger cards.
public sealed class BgLooterAndMuggerAgain() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgLooter>(difficulty: 1), Make<BgMugger>()];
}

public sealed class BgCenturionA() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCenturion>("B3"), Make<BgMystic>()];
}

public sealed class BgCenturionB() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCenturion>("3B"), Make<BgMystic>()];
}

// Ascension 7 replaces this card with Shelled Parasite and Fungi Beast.
public sealed class BgShelledParasiteEncounter() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => Ascension < 7
        ? [Make<BgShelledParasite>()]
        : [Make<BgShelledParasite>(), Make<BgFungiBeast>("s2!", difficulty: 1)];
}

// Ascension 7 replaces this card with Spheric Guardian and Sentry.
public sealed class BgSphericGuardianEncounter() : BgEncounter<BgCity>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => Ascension < 7
        ? [Make<BgSphericGuardian>()]
        : [Make<BgSphericGuardian>(), Make<BgSentry>("D3")];
}

public sealed class BgBookOfStabbingElite() : BgEncounter<BgCity>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgBookOfStabbing>()];
}

public sealed class BgGremlinLeaderElite() : BgEncounter<BgCity>(RoomType.Elite)
{
    protected override IEnumerable<Type> Summons => [typeof(BgGremlinAngry), typeof(BgGremlinSneaky), typeof(BgGremlinFat), typeof(BgGremlinWizard)];
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgGremlinLeader>()];
}

public sealed class BgTaskmasterElite() : BgEncounter<BgCity>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() =>
        [Make<BgBlueSlaver>(difficulty: 1), Make<BgTaskmaster>(), Make<BgRedSlaver>(difficulty: 1)];
}

public sealed class BgBronzeAutomatonBoss() : BgEncounter<BgCity>(RoomType.Boss)
{
    public override string BorrowedEntry => "KNOWLEDGE_DEMON_BOSS";
    protected override IEnumerable<Type> Summons => [typeof(BgBronzeOrb)];
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgBronzeAutomaton>()];
}

public sealed class BgTheCollectorBoss() : BgEncounter<BgCity>(RoomType.Boss)
{
    public override string BorrowedEntry => "QUEEN_BOSS";
    protected override IEnumerable<Type> Summons => [typeof(BgTorchHead)];
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgTheCollector>()];
}

public sealed class BgChampBoss() : BgEncounter<BgCity>(RoomType.Boss)
{
    public override string BorrowedEntry => "CEREMONIAL_BEAST_BOSS";
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgChamp>()];
}
