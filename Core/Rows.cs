using BoardGame.Monsters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Core;

// The board game seats each player in a row with their own enemies; bosses are in every row. In single player every
// enemy is in the one row, so "any row" means every enemy.
public static class Rows
{
    public static Player RowOf(Creature enemy)
    {
        var combat = enemy.CombatState!;
        var players = combat.Players.Where(p => p.Creature.IsAlive).ToList();
        if (players.Count == 0)
        {
            return combat.Players[0];
        }
        var index = combat.Enemies.ToList().IndexOf(enemy);
        return players[Math.Max(0, index) % players.Count];
    }

    public static bool IsInRow(Creature enemy, Player player) =>
        enemy.Monster is BgMonster { IsBoss: true } || RowOf(enemy) == player;

    public static IReadOnlyList<Creature> EnemiesInRow(ICombatState combat, Player player) =>
        combat.HittableEnemies.Where(enemy => IsInRow(enemy, player)).ToList();

    // Lets the player pick a row (their own in single player) and returns its living enemies.
    public static async Task<IReadOnlyList<Creature>> ChooseRow(PlayerChoiceContext choiceContext, Player player, string prompt)
    {
        var combat = player.Creature.CombatState!;
        var rows = combat.Players.Where(p => EnemiesInRow(combat, p).Count > 0).ToList();
        if (rows.Count <= 1)
        {
            return combat.HittableEnemies.ToList();
        }
        var row = await Choice.Choose(choiceContext, player, prompt, rows,
            p => new Choice.Option($"{p.Creature.Name}'s row", string.Join(", ", EnemiesInRow(combat, p).Select(e => e.Name))));
        return EnemiesInRow(combat, row ?? player);
    }

    // "Deal X damage to any row": damage that isn't an attack, so Strength, Weak, and Vulnerable don't change it.
    public static async Task DamageAnyRow(PlayerChoiceContext choiceContext, Player player, decimal amount, string source)
    {
        var targets = await ChooseRow(choiceContext, player, $"{source}: deal {amount} damage to a row");
        if (targets.Count > 0 && amount > 0)
        {
            await CreatureCmd.Damage(choiceContext, targets, amount, ValueProp.Unpowered, player.Creature);
        }
    }

    // Lets the player pick any enemy.
    public static async Task<Creature?> ChooseEnemy(PlayerChoiceContext choiceContext, Player player, string prompt)
    {
        var enemies = player.Creature.CombatState?.HittableEnemies.ToList() ?? [];
        return enemies.Count == 0
            ? null
            : await Choice.Choose(choiceContext, player, prompt, enemies, e => new Choice.Option(e.Name, $"{e.CurrentHp}/{e.MaxHp} HP")) ?? enemies[0];
    }
}
