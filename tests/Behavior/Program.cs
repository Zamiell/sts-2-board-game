using BoardGame;

// Token limits: gains stop at the cap, losses always apply.
Expect(Rules.ClampGain(2, 0, Rules.MaxVulnerable), 2, "Vulnerable under the cap");
Expect(Rules.ClampGain(2, 2, Rules.MaxVulnerable), 1, "Vulnerable reaching the cap");
Expect(Rules.ClampGain(2, 3, Rules.MaxWeak), 0, "Weak at the cap");
Expect(Rules.ClampGain(5, 6, Rules.MaxStrength), 2, "Strength reaching the cap");
Expect(Rules.ClampGain(-2, 8, Rules.MaxStrength), -2, "Strength loss at the cap");
Expect(Rules.ClampGain(3, 10, Rules.MaxStrength), 0, "Strength already over the cap");
Expect(Rules.ClampGain(12, 25, Rules.MaxEnemyPoison), 5, "Shared Poison supply");
Expect(Rules.ClampGain(5, 18, Rules.MaxPlayerBlock), 2, "Block reaching 20");
Expect(Rules.ClampGain(3, 5, Rules.MaxEnergy), 1, "Energy reaching 6");

// Hits follow the board game order: Strength, then Weak's -1, then Vulnerable doubling.
Expect(Hit(6, strength: 2, weak: false, vulnerable: false), 8, "Plain hit with Strength");
Expect(Hit(6, strength: 2, weak: false, vulnerable: true), 16, "Strength added before Vulnerable doubles");
Expect(Hit(6, strength: 2, weak: true, vulnerable: false), 7, "Weak removes 1 per hit");
Expect(Hit(6, strength: 2, weak: true, vulnerable: true), 8, "Weak against Vulnerable cancels both");
Expect(Hit(1, strength: 0, weak: true, vulnerable: false), 0, "Weak can reduce a hit to 0");
Expect(Hit(0, strength: 0, weak: true, vulnerable: false), 0, "Weak never makes damage negative");

// Modifiers that strengthen the video game's Weak become a bigger flat penalty.
Expect(Rules.WeakPenaltyFor(Rules.VanillaWeakMultiplier), 1, "Plain Weak");
Expect(Rules.WeakPenaltyFor(0.6m), 2, "Paper Krane");
Expect(Rules.WeakPenaltyFor(0.5m), 2, "Debilitate");
Expect(Rules.WeakPenaltyFor(0.9m), 1, "Weakened modifiers never drop below 1");

Console.WriteLine("PASS: Token limits, hit order, Weak and Vulnerable interaction, and Weak modifiers.");

// Mirrors the game's damage pipeline: additive modifiers, multiplicative modifiers, then a floor at 0.
static decimal Hit(decimal damage, decimal strength, bool weak, bool vulnerable)
{
    var additive = damage + strength;
    if (Rules.WeakApplies(weak, vulnerable))
    {
        additive -= Rules.WeakPenalty;
    }
    var multiplier = Rules.VulnerableApplies(vulnerable, weak) ? Rules.VulnerableMultiplier : 1m;
    return Math.Max(0m, additive * multiplier);
}

static void Expect(decimal actual, decimal expected, string name)
{
    if (actual != expected)
    {
        throw new Exception($"{name}: expected {expected}, got {actual}.");
    }
}
