# Board Game Rules

A Slay the Spire 2 mod that plays by the rules of *Slay the Spire: The Board Game*.

This is the rules layer: the core mechanics that work the same no matter which cards, enemies, or relics are in play. The cards, enemies, and relics themselves are still Slay the Spire 2's (see [Not yet included](#not-yet-included)).

## Rules

| Rule | Video game | With this mod |
| --- | --- | --- |
| Vulnerable | 50% more damage from Attacks, lasts a number of turns | Double damage from the next Attack. Each Attack removes 1 Vulnerable. Maximum 3. |
| Weak | 25% less damage with Attacks, lasts a number of turns | 1 less damage per hit on the next Attack. Each Attack removes 1 Weak. Maximum 3. |
| Weak attacker vs. Vulnerable target | Both apply | Neither applies, and both lose 1 |
| Multi-hit Attacks | Vulnerable and Weak affect every hit | Same, but only 1 is removed after the whole Attack resolves |
| Poison on enemies | Deals damage, then goes down by 1 each turn | Deals damage every turn and never goes down. Maximum 30 across all enemies. |
| Strength | No maximum | Maximum 8 |
| Player Block | No maximum | Maximum 20 |
| Energy | No maximum | Maximum 6 |

Hits are calculated in the board game's order: Strength, then Weak's −1, then Vulnerable's doubling. Damage never drops below 0.

Gaining past a maximum is ignored, like taking a token from an empty supply. Losses are never blocked, so negative Strength still works.

Vulnerable, Weak, and Poison tooltips describe these rules. The new text is English in every language, because translations would describe the old rules.

### Items that change these rules

- **Paper Phrog, Cruelty, Debilitate** still increase Vulnerable damage on top of the doubling.
- **Paper Krane, Debilitate** make Weak take 2 damage per hit instead of 1.

### Already the same in Slay the Spire 2

These board game rules need no changes:

- 3 Energy and 5 cards drawn per turn
- Block removed at the start of your turn
- 3 potion slots
- 3 cards per card reward
- Resting heals about a third of max HP (3 of the board game's ~9)
- Healing to full at the start of each new act

## Not yet included

To play *exactly* like the board game, the mod would also need the board game's content. That content can't be inferred from the rulebook, so these parts aren't included:

- **Board game numbers.** Card damage, Block, HP, and gold are about 1/5 to 1/10 of the video game's (for example, starting HP is around 9). The maximums above are the board game's literal numbers, so Block (20), Strength (8), and Poison (30) are tighter than in the board game until the numbers are scaled down too.
- **The die.** In the board game, a die rolled each round decides many enemies' actions and triggers some relics. Slay the Spire 2 enemies use their own move patterns, so a die would have nothing to control.
- **Card, enemy, relic, potion, and event lists.** These are the board game's own versions of Slay the Spire 1 content, including the Watcher. Slay the Spire 2's characters (such as the Regent and Necrobinder) aren't in the board game.
- **Unlimited hand size.** The board game has no hand limit. Slay the Spire 2's limit of 10 is built into the hand layout, so it stays.
- **Map, rows, and shared multiplayer rules.** The board game's maps, enemy rows, shared gold, and Ascension list.
- **Poison on players.** The board game has none, so player Poison keeps the video game's rules.

## Build and install

Requires the .NET SDK with `net9.0` support and a local game installation.

```sh
dotnet build -c Release
./install.sh
```

Default Steam paths are supported on Windows, macOS, and WSL (using the Windows installation). Override a non-default location with `-p:GameDir="/path/to/Slay the Spire 2"` or `-p:GameDataDir="/path/to/data_sts2_platform"`.

Build output is in `dist/BoardGame/`. To install manually, copy `BoardGame.dll` and `BoardGame.json` into the game's `mods/BoardGame/` directory. On macOS, `mods/` is inside `SlayTheSpire2.app/Contents/MacOS/`.

Restart the game after installation. To uninstall, remove `mods/BoardGame/`. The mod changes gameplay, so all players in a multiplayer game need it.

## Development checks

The compatibility check needs the .NET 9 runtime: the game's Harmony library does not support .NET 10.

```sh
dotnet run --project tests/Behavior
dotnet run --project tests/Compatibility -- \
  "/path/to/data_sts2_platform" \
  "dist/BoardGame/BoardGame.dll"
```

Behavior checks cover the rule math in `Rules.cs`: maximums, hit order, and how Weak and Vulnerable interact. The compatibility check loads the real game assemblies and verifies that private bindings resolve and every Harmony patch installs. Combat behavior and tooltips still need an in-game check.
