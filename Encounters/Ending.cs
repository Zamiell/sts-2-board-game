using BoardGame.Acts;
using BoardGame.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace BoardGame.Encounters;

public sealed class BgShieldAndSpearElite() : BgEncounter<BgEnding>(RoomType.Elite)
{
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgSpireSpear>(), Make<BgSpireShield>()];
}

public sealed class BgCorruptHeartBoss() : BgEncounter<BgEnding>(RoomType.Boss)
{
    public override string BorrowedEntry => "WATERFALL_GIANT_BOSS";
    protected override IEnumerable<BgMonster> Monsters() => [Make<BgCorruptHeart>()];
}
