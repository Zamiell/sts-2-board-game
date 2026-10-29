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
        // The Spire Shield and Spear each stand in their own row.
        var pincered = combat.HittableEnemies.Where(Pincer.InPincer).ToList();
        if (pincered.Count >= 2)
        {
            return [await Targeting.ChooseEnemy(player, pincered, prompt) ?? pincered[0]];
        }
        var rows = combat.Players.Where(p => EnemiesInRow(combat, p).Count > 0).ToList();
        if (rows.Count <= 1)
        {
            return combat.HittableEnemies.ToList();
        }
        // Pick any enemy in the row you want.
        var enemy = await Targeting.ChooseEnemy(player, combat.HittableEnemies.ToList(), prompt + " (pick any enemy in the row)");
        return EnemiesInRow(combat, enemy == null ? player : RowOf(enemy));
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
            : await Targeting.ChooseEnemy(player, enemies, prompt) ?? enemies[0];
    }
}

// Pincer Movement (Spire Shield and Spear): they stand in different rows, so an area attack hits only one of them.
public static class Pincer
{
    private static readonly AsyncLocal<HashSet<Creature>?> Spared = new();

    public static bool InPincer(Creature enemy) => enemy.IsAlive && enemy.HasPower<Powers.BgPincerMovementPower>();

    // Asks which of the pincered enemies an area attack hits; returns the ones it doesn't.
    public static async Task<HashSet<Creature>> ChooseSpared(PlayerChoiceContext choiceContext, Player player)
    {
        var pincered = player.Creature.CombatState?.HittableEnemies.Where(InPincer).ToList() ?? [];
        if (pincered.Count < 2)
        {
            return [];
        }
        var hit = await Targeting.ChooseEnemy(player, pincered, "Pincer Movement: choose which enemy to hit") ?? pincered[0];
        return pincered.Where(e => e != hit).ToHashSet();
    }

    // Runs an area attack with the spared enemies left out of its targets.
    public static async Task WithSpared(HashSet<Creature> spared, Func<Task> attack)
    {
        Spared.Value = spared.Count == 0 ? null : spared;
        try
        {
            await attack();
        }
        finally
        {
            Spared.Value = null;
        }
    }

    internal static IReadOnlyList<Creature> Filter(IReadOnlyList<Creature> opponents) =>
        Spared.Value is { } spared ? opponents.Where(c => !spared.Contains(c)).ToList() : opponents;
}

[HarmonyLib.HarmonyPatch(typeof(CombatState), nameof(CombatState.GetOpponentsOf))]
internal static class PincerTargetsPatch
{
    private static void Postfix(ref IReadOnlyList<Creature> __result) => __result = Pincer.Filter(__result);
}
