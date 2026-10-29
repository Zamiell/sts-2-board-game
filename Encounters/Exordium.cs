using BoardGame.Acts;
using BoardGame.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace BoardGame.Encounters;

// Act I encounter, elite, and boss decks.

public sealed class BgSmallSlimes() : BgEncounter<BgExordium>(RoomType.Monster, weak: true)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSpikeSlimeS>(), Make<BgAcidSlimeM>()];
}

public sealed class BgJawWormEasy() : BgEncounter<BgExordium>(RoomType.Monster, weak: true)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgJawWorm>(difficulty: 0)];
}

public sealed class BgCultistEncounter() : BgEncounter<BgExordium>(RoomType.Monster, weak: true)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCultist>()];
}

public sealed class BgTwoLouse() : BgEncounter<BgExordium>(RoomType.Monster, weak: true)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgRedLouse>("S21"), Make<BgGreenLouse>()];
}

public sealed class BgCultistAndSpikeSlime() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCultist>(), Make<BgSpikeSlimeM>()];
}

public sealed class BgCultistAndLouse() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCultist>(), Make<BgGreenLouse>()];
}

public sealed class BgFungiBeasts() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgFungiBeast>("21S"), Make<BgFungiBeast>("S21")];
}

public sealed class BgSlimeTrio() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSpikeSlimeS>(), Make<BgAcidSlimeM>(), Make<BgSpikeSlimeM>()];
}

public sealed class BgThreeLouse() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() =>
        [Make<BgRedLouse>("S22", difficulty: 1), Make<BgGreenLouse>(), Make<BgRedLouse>("21S")];
}

public sealed class BgLargeSlime() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<Type> Summons => [typeof(BgAcidSlimeM)];
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgAcidSlimeL>()];
}

public sealed class BgSneakyGremlinTeam() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<Type> Summons => [typeof(BgGremlinAngry), typeof(BgGremlinSneaky), typeof(BgGremlinFat), typeof(BgGremlinWizard)];
    protected override IEnumerable<BgMonster> Monsters()
    {
        var leader = Make<BgGremlinSneaky>();
        leader.Leader = true;
        return [leader];
    }
}

public sealed class BgAngryGremlinTeam() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<Type> Summons => [typeof(BgGremlinAngry), typeof(BgGremlinSneaky), typeof(BgGremlinFat), typeof(BgGremlinWizard)];
    protected override IEnumerable<BgMonster> Monsters()
    {
        var leader = Make<BgGremlinAngry>();
        leader.Leader = true;
        return [leader];
    }
}

public sealed class BgBlueSlaverEncounter() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgBlueSlaver>("W3D")];
}

public sealed class BgRedSlaverEncounter() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgRedSlaver>("DV3")];
}

// Ascension 7 replaces this card with Jaw Worm and Spike Slime.
public sealed class BgJawWormMedium() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => Ascension < 7
        ? [Make<BgJawWorm>(difficulty: 1)]
        : [Make<BgJawWorm>(difficulty: 3), Make<BgSpikeSlimeM>()];
}

// Ascension 7 replaces this card with Looter and Acid Slime.
public sealed class BgLooterEncounter() : BgEncounter<BgExordium>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => Ascension < 7
        ? [Make<BgLooter>()]
        : [Make<BgLooter>(), Make<BgAcidSlimeM>()];
}

public sealed class BgGremlinNobElite() : BgEncounter<BgExordium>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgGremlinNob>()];
}

public sealed class BgLagavulinElite() : BgEncounter<BgExordium>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgLagavulin>()];
}

public sealed class BgThreeSentries() : BgEncounter<BgExordium>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSentry>("3D"), Make<BgSentry>("D3"), Make<BgSentry>("2D")];
}

public sealed class BgHexaghostBoss() : BgEncounter<BgExordium>(RoomType.Boss)
{
    public override string BorrowedEntry => "SOUL_FYSH_BOSS";
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgHexaghost>()];
}

public sealed class BgSlimeBossBoss() : BgEncounter<BgExordium>(RoomType.Boss)
{
    public override string BorrowedEntry => "THE_INSATIABLE_BOSS";
    protected override IEnumerable<Type> Summons => [typeof(BgAcidSlimeL), typeof(BgAcidSlimeM), typeof(BgSpikeSlimeM)];
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSlimeBoss>()];
}

public sealed class BgTheGuardianBoss() : BgEncounter<BgExordium>(RoomType.Boss)
{
    public override string BorrowedEntry => "KAISER_CRAB_BOSS";
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgTheGuardian>()];
}

// Mind Bloom's "I am War" fights an Act I boss inside an event in The Beyond. These copies are normal fights, so
// winning doesn't end the act; they belong to no act's encounter deck.
public sealed class BgMindBloomHexaghost() : BgEncounter<Events.BgMindBloom>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgHexaghost>()];
}

public sealed class BgMindBloomSlimeBoss() : BgEncounter<Events.BgMindBloom>(RoomType.Monster)
{
    protected override IEnumerable<Type> Summons => [typeof(BgAcidSlimeL), typeof(BgAcidSlimeM), typeof(BgSpikeSlimeM)];
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSlimeBoss>()];
}

public sealed class BgMindBloomTheGuardian() : BgEncounter<Events.BgMindBloom>(RoomType.Monster)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgTheGuardian>()];
}
