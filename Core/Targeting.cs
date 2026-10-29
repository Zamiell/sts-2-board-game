using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// Board game effects often ask a player to pick an enemy or a player in the middle of resolving. In combat that pick
// uses the game's own targeting arrow, starting from the choosing player, with the question shown in a thought bubble.
// The choice is synced to the other players like any other.
public static class Targeting
{
    // Returns null when there's nothing to pick from, or when an optional pick is cancelled.
    public static async Task<Creature?> Choose(Player player, IReadOnlyList<Creature> candidates, string prompt, bool optional = false)
    {
        if (candidates.Count == 0)
        {
            return null;
        }
        if (candidates.Count == 1 && !optional)
        {
            return candidates[0];
        }
        if (!CombatManager.Instance.IsInProgress)
        {
            // No arrow outside combat: the pick is offered as option cards.
            return await Choice.Choose(new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(), player, prompt, candidates,
                c => new Choice.Option(c.Name, $"{c.CurrentHp}/{c.MaxHp} HP"), canSkip: optional);
        }
        var choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        if (!LocalContext.IsMe(player))
        {
            var index = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndexOrNull();
            return index is { } i && i >= 0 && i < candidates.Count ? candidates[i] : optional ? null : candidates[0];
        }
        var chosen = await PickLocally(player, candidates, prompt, optional) ?? (optional ? null : candidates[0]);
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId,
            PlayerChoiceResult.FromIndex(chosen == null ? null : candidates.ToList().IndexOf(chosen)));
        return chosen;
    }

    public static Task<Creature?> ChooseEnemy(Player player, IReadOnlyList<Creature> enemies, string prompt, bool optional = false) =>
        Choose(player, enemies, prompt, optional);

    public static async Task<Player?> ChoosePlayer(Player chooser, string prompt, IReadOnlyList<Player>? players = null)
    {
        var options = players ?? chooser.Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList()
            ?? chooser.RunState.Players.Where(p => p.Creature.IsAlive).ToList();
        return (await Choose(chooser, options.Select(p => p.Creature).ToList(), prompt))?.Player;
    }

    private static async Task<Creature?> PickLocally(Player player, IReadOnlyList<Creature> candidates, string prompt, bool optional)
    {
        var room = NCombatRoom.Instance;
        var manager = NTargetManager.Instance;
        if (room == null || manager == null)
        {
            return null;
        }
        var type = candidates.All(c => c.IsPlayer) ? TargetType.AnyPlayer : TargetType.AnyEnemy;
        var from = room.GetCreatureNode(player.Creature) is { } node ? node.GlobalPosition + node.Size / 2f : Vector2.Zero;
        var prompted = new LocString("cards", "BG_OPTION_CARD.prompt");
        prompted.Add("Text", prompt);
        ThinkCmd.Play(prompted, player.Creature);
        // A cancelled pick (right-click) asks again, unless the choice is optional.
        for (var attempt = 0; attempt < (optional ? 1 : 10); attempt++)
        {
            manager.StartTargeting(type, from, TargetMode.ClickMouseToTarget, null,
                n => Creature(n) is { } c && candidates.Contains(c));
            if (Creature(await manager.SelectionFinished()) is { } picked && candidates.Contains(picked))
            {
                return picked;
            }
        }
        return null;
    }

    private static Creature? Creature(Node? node) => node switch
    {
        NCreature creature => creature.Entity,
        NMultiplayerPlayerState state => state.Player.Creature,
        _ => null,
    };
}
