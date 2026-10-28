namespace BoardGame;

// Rules from Slay the Spire: The Board Game, kept free of game types so they can be tested directly.
internal static class Rules
{
    public const int MaxVulnerable = 3;
    public const int MaxWeak = 3;
    public const int MaxStrength = 8;
    public const int MaxEnemyPoison = 30;
    public const int MaxPlayerBlock = 20;
    public const int MaxEnergy = 6;

    public const decimal VulnerableMultiplier = 2m;
    public const decimal WeakPenalty = 1m;

    // The video game's Weak multiplier; modifiers that strengthen it scale the board game penalty instead.
    public const decimal VanillaWeakMultiplier = 0.75m;

    // The board game takes tokens from a limited supply: a gain stops at the cap, losses are untouched.
    public static decimal ClampGain(decimal gain, decimal current, decimal cap) =>
        gain <= 0m ? gain : Math.Min(gain, Math.Max(0m, cap - current));

    // A Weak attacker hitting a Vulnerable target is affected by neither.
    public static bool WeakApplies(bool attackerWeak, bool targetVulnerable) => attackerWeak && !targetVulnerable;

    public static bool VulnerableApplies(bool targetVulnerable, bool attackerWeak) => targetVulnerable && !attackerWeak;

    // Converts a modified vanilla Weak multiplier (e.g. Debilitate's 0.5) to a flat per-hit penalty.
    public static decimal WeakPenaltyFor(decimal modifiedMultiplier) =>
        Math.Max(WeakPenalty, Math.Round((1m - modifiedMultiplier) / (1m - VanillaWeakMultiplier) * WeakPenalty));
}
