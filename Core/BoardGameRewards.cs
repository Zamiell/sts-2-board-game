using BoardGame.Encounters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// Combat rewards in a board game run. Each encounter card lists its gold and whether it drops a potion; every fight
// offers 3 cards from the reward deck (upgraded after Act I elites, Rares after a boss, none after the Act III boss),
// and elites drop a relic.
[HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
internal static class BoardGameRewards
{
    private static readonly Dictionary<Type, (int Gold, bool Potion)> Table = new()
    {
        [typeof(BgSmallSlimes)] = (0, true),
        [typeof(BgJawWormEasy)] = (1, true),
        [typeof(BgCultistEncounter)] = (1, false),
        [typeof(BgTwoLouse)] = (1, false),
        [typeof(BgCultistAndSpikeSlime)] = (1, true),
        [typeof(BgCultistAndLouse)] = (1, false),
        [typeof(BgFungiBeasts)] = (1, true),
        [typeof(BgSlimeTrio)] = (1, false),
        [typeof(BgThreeLouse)] = (1, false),
        [typeof(BgLargeSlime)] = (1, true),
        [typeof(BgSneakyGremlinTeam)] = (1, true),
        [typeof(BgAngryGremlinTeam)] = (2, false),
        [typeof(BgBlueSlaverEncounter)] = (2, false),
        [typeof(BgRedSlaverEncounter)] = (1, false),
        [typeof(BgJawWormMedium)] = (1, false),
        [typeof(BgLooterEncounter)] = (0, true),
        [typeof(BgGremlinNobElite)] = (2, false),
        [typeof(BgLagavulinElite)] = (2, false),
        [typeof(BgThreeSentries)] = (2, false),
        [typeof(BgHexaghostBoss)] = (3, false),
        [typeof(BgSlimeBossBoss)] = (3, false),
        [typeof(BgTheGuardianBoss)] = (3, false),

        [typeof(BgCenturionA)] = (1, false),
        [typeof(BgCenturionB)] = (1, true),
        [typeof(BgLooterAndMugger)] = (0, true),
        [typeof(BgLooterAndMuggerAgain)] = (0, true),
        [typeof(BgChosenAndCultist)] = (1, true),
        [typeof(BgChosenAndByrd)] = (2, false),
        [typeof(BgThreeCultists)] = (2, false),
        [typeof(BgThreeByrds)] = (1, false),
        [typeof(BgSphericGuardianEncounter)] = (1, false),
        [typeof(BgSnakePlantEncounter)] = (1, false),
        [typeof(BgSneckoEncounter)] = (1, false),
        [typeof(BgShelledParasiteEncounter)] = (0, true),
        [typeof(BgBookOfStabbingElite)] = (2, false),
        [typeof(BgGremlinLeaderElite)] = (2, false),
        [typeof(BgTaskmasterElite)] = (2, false),
        [typeof(BgBronzeAutomatonBoss)] = (3, false),
        [typeof(BgChampBoss)] = (3, false),
        [typeof(BgTheCollectorBoss)] = (3, false),

        [typeof(BgJawWormsHard)] = (2, false),
        [typeof(BgSpireGrowthEncounter)] = (1, true),
        [typeof(BgExploderAndFriends)] = (1, false),
        [typeof(BgRepulsorAndFriends)] = (0, true),
        [typeof(BgOrbWalker23)] = (1, false),
        [typeof(BgOrbWalker32)] = (1, false),
        [typeof(BgTransientEncounter)] = (2, false),
        [typeof(BgMawEncounter)] = (1, false),
        [typeof(BgWrithingMassEncounter)] = (0, true),
        [typeof(BgThreeDarklings)] = (0, true),
        [typeof(BgGiantHeadElite)] = (3, false),
        [typeof(BgNemesisElite)] = (3, false),
        [typeof(BgReptomancerElite)] = (3, false),
    };

    public static (int Gold, bool Potion) For(Type encounter) => Table.GetValueOrDefault(encounter);

    private static bool Prefix(Player player, AbstractRoom room, ref List<Reward> __result)
    {
        if (!BoardGameRun.IsActive(player.RunState) || room is not CombatRoom combat)
        {
            return true;
        }
        __result = ForEncounter(player, combat.Encounter.GetType(), room.RoomType, combat.GoldProportion);
        return false;
    }

    // The rewards for beating an encounter, also used by events that start fights.
    public static List<Reward> ForEncounter(Player player, Type encounter, RoomType roomType, float goldProportion = 1f)
    {
        var runState = player.RunState;
        var (gold, potion) = For(encounter);
        if (roomType == RoomType.Boss && runState.AscensionLevel >= 10)
        {
            gold--;
        }
        gold = (int)Math.Round(gold * goldProportion);
        var rewards = new List<Reward>();
        if (gold > 0)
        {
            rewards.Add(new GoldReward(gold, player));
        }
        if (potion)
        {
            rewards.Add(new PotionReward(player));
        }
        if (Hook.ShouldForcePotionReward(runState, player, roomType))
        {
            rewards.Add(new PotionReward(player));
        }
        var actIndex = runState.CurrentActIndex;
        if (!(roomType == RoomType.Boss && actIndex >= 2))
        {
            var cards = RewardDeck.Draw(player, 3, rare: roomType == RoomType.Boss,
                upgraded: roomType == RoomType.Elite && actIndex >= 1);
            if (cards.Count > 0)
            {
                rewards.Add(new CardReward(cards, CardCreationSource.Encounter, player,
                    CardCreationOptions.ForRoom(player, roomType)));
            }
        }
        if (roomType == RoomType.Elite)
        {
            rewards.Add(new RelicReward(player));
            Keys.ModifyEliteRewards(player, rewards);
        }
        if (roomType == RoomType.Boss && Relics.BossRelicReward.For(player) is { } bossRelics)
        {
            rewards.Add(bossRelics);
        }
        return rewards;
    }
}
