using BoardGame.Core;
using BoardGame.Encounters;
using BoardGame.Events;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace BoardGame.Acts;

// A board game act. It borrows an STS2 act's backgrounds, music, and treasure chest, and fills its rooms with the
// board game's encounters and events. Board game acts are never chosen for STS2 runs.
public abstract class BgAct : ActModel, IBorrowsAssets
{
    protected abstract ActModel Borrowed { get; }

    public string BorrowedEntry => Borrowed.Id.Entry;

    public override bool IsDefault => false;

    public override bool IsUnlocked(UnlockState unlockState) => false;

    public override Color MapTraveledColor => Borrowed.MapTraveledColor;
    public override Color MapUntraveledColor => Borrowed.MapUntraveledColor;
    public override Color MapBgColor => Borrowed.MapBgColor;
    public override string[] BgMusicOptions => Borrowed.BgMusicOptions;
    public override string[] MusicBankPaths => Borrowed.MusicBankPaths;
    public override string AmbientSfx => Borrowed.AmbientSfx;
    public override string ChestSpineResourcePath => Borrowed.ChestSpineResourcePath;
    public override string ChestSpineSkinNameNormal => Borrowed.ChestSpineSkinNameNormal;
    public override string ChestSpineSkinNameStroke => Borrowed.ChestSpineSkinNameStroke;
    public override string ChestOpenSfx => Borrowed.ChestOpenSfx;

    // Only Act I starts with a "1st Encounter" fight.
    protected override int NumberOfWeakEncounters => Index == 0 ? 1 : 0;

    protected override int BaseNumberOfRooms => 15;

    public override IEnumerable<EncounterModel> BossDiscoveryOrder => [];

    // Neow blesses the party at the start of the run, and restores their health at the start of each later act.
    // STS2's other Ancients don't exist in the board game.
    public override IEnumerable<AncientEventModel> AllAncients => [ModelDb.Event<Neow>()];

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState) => AllAncients;

    public override IEnumerable<EventModel> AllEvents =>
        Content.Models<BgEvent, EventModel>().Where(e => ((BgEvent)e).Acts.Contains(GetType()));

    public override IEnumerable<EncounterModel> GenerateAllEncounters() =>
        Content.Models<BgEncounter, EncounterModel>().Where(e => ((BgEncounter)e).Act == GetType());

    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
    {
    }

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) => Borrowed.GetMapPointTypes(mapRng);

    // The act's printed map layouts; one is chosen at random.
    protected virtual BgMapData.Layout[] Layouts => [];

    public virtual ActMap CreateBoardGameMap(IRunState runState)
    {
        var rng = new Rng(runState.Rng.Seed, $"bg_act_{Index + 1}_map");
        return new BgActMap(rng.NextItem(Layouts)!, rng, runState.Act.HasSecondBoss,
            burningElite: !Core.Keys.PartyHas<Core.BgEmeraldKey>(runState));
    }
}

public sealed class BgExordium : BgAct
{
    protected override ActModel Borrowed => ModelDb.Act<Overgrowth>();
    public override int Index => 0;
    protected override BgMapData.Layout[] Layouts => BgMapData.Exordium;
}

public sealed class BgCity : BgAct
{
    protected override ActModel Borrowed => ModelDb.Act<Hive>();
    public override int Index => 1;
    protected override BgMapData.Layout[] Layouts => BgMapData.City;
}

public sealed class BgBeyond : BgAct
{
    protected override ActModel Borrowed => ModelDb.Act<Glory>();
    public override int Index => 2;
    protected override BgMapData.Layout[] Layouts => BgMapData.Beyond;
}

// Act IV: the Spire Shield and Spear, then the Corrupt Heart.
public sealed class BgEnding : BgAct
{
    protected override ActModel Borrowed => ModelDb.Act<Glory>();
    public override int Index => 3;

    // The Spire Shield and Spear only guard the Heart from board game Ascension 11.
    public override ActMap CreateBoardGameMap(IRunState runState) => new BgActMap(elite: runState.AscensionLevel >= 11);
}

// STS2 spreads its shared Ancients over the later acts. Board game acts have none.
[HarmonyLib.HarmonyPatch(typeof(ActModel), nameof(ActModel.SetSharedAncientSubset))]
internal static class BgActSharedAncientsPatch
{
    private static void Prefix(ActModel __instance, ref List<AncientEventModel> sharedAncientSubset)
    {
        if (__instance is BgAct)
        {
            sharedAncientSubset = [];
        }
    }
}
