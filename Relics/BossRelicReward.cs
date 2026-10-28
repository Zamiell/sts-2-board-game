using BoardGame.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace BoardGame.Relics;

// After the Act I and Act II bosses, each player draws 3 boss relics and keeps 1.
internal static class BossRelicReward
{
    public static async Task Offer(CombatRoom room)
    {
        var runState = room.CombatState.RunState;
        if (room.RoomType != RoomType.Boss || runState.CurrentActIndex >= 2)
        {
            return;
        }
        foreach (var player in runState.Players.Where(p => p.Creature.IsAlive))
        {
            await OfferTo(player);
        }
    }

    private static async Task OfferTo(Player player)
    {
        var owned = player.RunState.Players.SelectMany(p => p.Relics).Select(r => r.Id).ToHashSet();
        var deck = ModelDb.RelicPool<BgRelicPool>().AllRelics
            .Where(r => r.Rarity == RelicRarity.Ancient && !owned.Contains(r.Id))
            .ToList();
        var drawn = new List<RelicModel>();
        for (var i = 0; i < 3 && deck.Count > 0; i++)
        {
            var relic = player.RunState.Rng.Niche.NextItem(deck)!;
            deck.Remove(relic);
            drawn.Add(relic);
        }
        var chosen = await Choice.Choose(new ThrowingPlayerChoiceContext(), player, "Choose a boss relic", drawn,
            r => new Choice.Option(r.Title.GetFormattedText(), r.ToMutable().DynamicDescription.GetFormattedText()));
        if (chosen != null)
        {
            await RelicCmd.Obtain(chosen.ToMutable(), player);
        }
    }
}
