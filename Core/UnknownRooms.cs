using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// A "?" room draws a card from the act's event deck, which also holds two Hallway Encounters (a normal fight) and a
// Merchant Encounter (from Ascension 3). The first "?" room of Act I is always an event, as the board game discards the others there.
[HarmonyPatch(typeof(UnknownMapPointOdds), nameof(UnknownMapPointOdds.Roll))]
internal static class UnknownRoomPatch
{
    private const int HallwayCards = 2;
    private const int MerchantCards = 1;

    private static readonly AccessTools.FieldRef<AbstractOdds, Rng> OddsRng = AccessTools.FieldRefAccess<AbstractOdds, Rng>("_rng");

    private static bool Prefix(UnknownMapPointOdds __instance, IEnumerable<RoomType> blacklist, IRunState runState, ref RoomType __result)
    {
        if (!BoardGameRun.IsActive(runState))
        {
            return true;
        }
        var banned = blacklist.ToHashSet();
        var actHistory = runState.MapPointHistory.Count > runState.CurrentActIndex
            ? runState.MapPointHistory[runState.CurrentActIndex]
            : [];
        var firstOfAct = !actHistory.Any(p => p.MapPointType == MapPointType.Unknown);
        var events = runState.Act.AllEvents.Count(e => e.IsAllowed(runState) && !(runState is RunState state && state.VisitedEventIds.Contains(e.Id)));
        var deck = new List<RoomType>();
        if (!banned.Contains(RoomType.Event))
        {
            deck.AddRange(Enumerable.Repeat(RoomType.Event, Math.Max(1, events)));
        }
        // The Hallway and Merchant cards join the event decks from Ascension 3.
        if (!(firstOfAct && runState.CurrentActIndex == 0) && BgAscension.Level(runState) >= BgAscension.UnfavorableEvents)
        {
            if (!banned.Contains(RoomType.Monster))
            {
                deck.AddRange(Enumerable.Repeat(RoomType.Monster, HallwayCards));
            }
            if (!banned.Contains(RoomType.Shop))
            {
                deck.AddRange(Enumerable.Repeat(RoomType.Shop, MerchantCards));
            }
        }
        __result = deck.Count == 0 ? RoomType.Event : deck[OddsRng(__instance).NextInt(deck.Count)];
        return false;
    }
}
