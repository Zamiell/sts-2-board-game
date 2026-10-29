using BoardGame.Cards;
using BoardGame.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// The three keys open Act IV, as in Slay the Spire:
// - Ruby: "Recall" at a rest site instead of resting or smithing.
// - Emerald: beat the burning elite, hidden under one of the map's dark tokens. It adds 2 Burns to your draw pile.
// - Sapphire: offered with an elite's relic; you take one or the other.
// Without all three, the Act III boss is the last fight of the run. The keys belong to the party.
internal static class Keys
{
    public const int BurningElites = 1;

    public static bool PartyHas<TKey>(IRunState runState) where TKey : BgKey =>
        runState.Players.Any(p => p.Relics.OfType<TKey>().Any());

    public static bool PartyHasAll(IRunState runState) =>
        PartyHas<BgRubyKey>(runState) && PartyHas<BgEmeraldKey>(runState) && PartyHas<BgSapphireKey>(runState);

    // Whether the party is fighting the burning elite.
    public static bool AtBurningElite(IRunState runState) =>
        BgRules.For(runState) is { } rules && runState.CurrentMapCoord is { } coord
        && rules.EmeraldElite == Mark(runState.CurrentActIndex, coord);

    public static string Mark(int actIndex, MapCoord coord) => $"{actIndex}:{coord.col}:{coord.row}";

    public static void MarkBurningElite(IRunState runState, ActMap map)
    {
        if (BgRules.For(runState)?.EmeraldElite is not { } mark || !mark.StartsWith($"{runState.CurrentActIndex}:"))
        {
            return;
        }
        var parts = mark.Split(':');
        if (map.GetPoint(int.Parse(parts[1]), int.Parse(parts[2])) is { } point && point.Quests.Count == 0)
        {
            point.AddQuest(ModelDb.Relic<BgEmeraldKey>());
        }
    }

    public static async Task AddBurns(IRunState runState)
    {
        foreach (var player in runState.Players)
        {
            if (player.Creature.CombatState is { } combat && player.Creature.IsAlive)
            {
                for (var i = 0; i < 2; i++)
                {
                    await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<BgBurn>(player), PileType.Draw, player, CardPilePosition.Random);
                }
            }
        }
    }

    // Elite rewards: the emerald key at the burning elite; the sapphire key, linked with the relic, at map elites.
    public static void ModifyEliteRewards(Player player, List<Reward> rewards)
    {
        var runState = player.RunState;
        if (runState.CurrentMapPoint?.PointType != MapPointType.Elite || runState.CurrentActIndex >= 3)
        {
            return;
        }
        if (AtBurningElite(runState) && !PartyHas<BgEmeraldKey>(runState))
        {
            rewards.Add(new RelicReward(ModelDb.Relic<BgEmeraldKey>().ToMutable(), player));
        }
        if (!PartyHas<BgSapphireKey>(runState) && rewards.OfType<RelicReward>().FirstOrDefault() is { } relic)
        {
            rewards.Remove(relic);
            rewards.Add(new LinkedRewardSet([relic, new RelicReward(ModelDb.Relic<BgSapphireKey>().ToMutable(), player)], player));
        }
    }

    // Before the Act III boss: without all three keys, the run ends there.
    public static void CloseActFourWithoutKeys(RunState runState, AbstractRoom room)
    {
        if (room.RoomType != RoomType.Boss || runState.CurrentActIndex != 2 || runState.Acts.Count <= 3 || PartyHasAll(runState))
        {
            return;
        }
        ActsSetter(runState, runState.Acts.Take(3).ToList());
    }

    private static readonly Action<RunState, IReadOnlyList<ActModel>> ActsSetter =
        AccessTools.MethodDelegate<Action<RunState, IReadOnlyList<ActModel>>>(AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Acts)));
}

public abstract class BgKey : BgRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsAllowedInShops => false;
}

public sealed class BgRubyKey : BgKey
{
    public override string BorrowedEntry => "BRIMSTONE";
}

public sealed class BgEmeraldKey : BgKey
{
    public override string BorrowedEntry => "JEWELRY_BOX";
}

public sealed class BgSapphireKey : BgKey
{
    public override string BorrowedEntry => "DARKSTONE_PERIAPT";
}

// Recall: take the Ruby Key instead of resting or smithing.
public sealed class BgRecallRestSiteOption(Player owner) : RestSiteOption(owner)
{
    public override string OptionId => "BG_RECALL";

    public override async Task<bool> OnSelect()
    {
        await RelicCmd.Obtain<BgRubyKey>(Owner);
        return true;
    }
}

// Rest site options take their icon from their ID; Recall uses the Dig icon.
[HarmonyPatch(typeof(RestSiteOption), "IconPath", MethodType.Getter)]
internal static class RecallIconPatch
{
    private static void Postfix(RestSiteOption __instance, ref string __result)
    {
        if (__instance is BgRecallRestSiteOption)
        {
            __result = __result.Replace("option_bg_recall", "option_dig");
        }
    }
}
