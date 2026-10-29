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

// Every printed map: tokens fit the token pools, every room is reachable, and every room leads on toward the boss.
foreach (var (act, layouts) in new[] { ("Exordium", BoardGame.Acts.BgMapData.Exordium), ("City", BoardGame.Acts.BgMapData.City), ("Beyond", BoardGame.Acts.BgMapData.Beyond) })
{
    foreach (var layout in layouts)
    {
        CheckMap(act, layout);
    }
}

Console.WriteLine("PASS: Token limits, hit order, Weak and Vulnerable interaction, Weak modifiers, and act maps.");

static void CheckMap(string act, BoardGame.Acts.BgMapData.Layout layout)
{
    var rows = layout.Rows.Length - 1;
    var rooms = new HashSet<(int, int)>();
    for (var r = 0; r < rows; r++)
    {
        for (var c = 0; c < 7; c++)
        {
            if (layout.Rows[r][c] != '.')
            {
                rooms.Add((r, c));
            }
        }
    }
    Expect(layout.Rows.Sum(row => row.Count(ch => ch == 'D')), 8, $"{act} dark tokens");
    Expect(layout.Rows.Sum(row => row.Count(ch => ch == 'L')), 7, $"{act} light tokens");
    var edges = layout.Edges
        .Select(e => (From: Parse(e.From), To: Parse(e.To)))
        .Where(e => e.From.Item1 < rows)
        .ToList();
    foreach (var (from, to) in edges)
    {
        Expect(rooms.Contains(from) ? 1 : 0, 1, $"{act} edge from a room at {from}");
        Expect(to.Item1 == rows || rooms.Contains(to) ? 1 : 0, 1, $"{act} edge to a room at {to}");
        Expect(to.Item1, from.Item1 + 1, $"{act} edge {from} climbs one row");
    }
    foreach (var room in rooms)
    {
        Expect(room.Item1 == 0 || edges.Any(e => e.To == room) ? 1 : 0, 1, $"{act} room {room} is reachable");
        Expect(edges.Any(e => e.From == room) ? 1 : 0, 1, $"{act} room {room} leads on");
    }

    static (int, int) Parse(string point) => (int.Parse(point.Split('-')[0]), int.Parse(point.Split('-')[1]));
}

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
