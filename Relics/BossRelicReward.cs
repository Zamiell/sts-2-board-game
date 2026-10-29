using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace BoardGame.Relics;

// After the Act I and Act II bosses, each player draws 3 boss relics and keeps 1. They're offered as linked rewards
// on the boss's reward screen: taking one discards the others.
internal static class BossRelicReward
{
    public static Reward? For(Player player)
    {
        if (player.RunState.CurrentActIndex >= 2)
        {
            return null;
        }
        var owned = player.RunState.Players.SelectMany(p => p.Relics).Select(r => r.Id).ToHashSet();
        var deck = ModelDb.RelicPool<BgRelicPool>().AllRelics
            .Where(r => r.Rarity == RelicRarity.Ancient && !owned.Contains(r.Id))
            .ToList();
        var drawn = new List<Reward>();
        for (var i = 0; i < 3 && deck.Count > 0; i++)
        {
            var relic = player.PlayerRng.Rewards.NextItem(deck)!;
            deck.Remove(relic);
            drawn.Add(new RelicReward(relic.ToMutable(), player));
        }
        return drawn.Count == 0 ? null : new LinkedRewardSet(drawn, player);
    }
}
