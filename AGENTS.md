# `sts-2-board-game` Agent Instructions

A Slay the Spire 2 mod (C#, Harmony, `net9.0`) that recreates *Slay the Spire:
The Board Game*. The goal is to match the board game exactly, so check every
number and rule against the reference before changing it. `README.md` lists
every feature and every remaining difference from the board game. Keep it up to
date.

## References

- **Board game data:** [LuaViper/StSBoardGameMod](https://github.com/LuaViper/StSBoardGameMod)
  (MIT) is the source of truth for cards, enemies, relics, events, maps, and
  text. Clone it to read it. It is not part of this repository.
- **Game source:** decompiled STS2 code is in
  `../slay-the-spire-decomp/MegaCrit/Sts2/Core`. Read the real method before
  patching it.
- **Game install:** `/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2`
  (WSL). Scenes are packed in the `.pck` file and are not decompiled.

## How the mod is built

- **Assets:** board game models have no art or sound of their own. They
  implement `IBorrowsAssets` (`Core/BorrowedAssets.cs`) and name an STS2 model
  whose assets they use.
- **Model IDs:** a model's ID is its slugified class name, so `BgANoteForYourself`
  becomes `BG_A_NOTE_FOR_YOURSELF`. Renaming a class changes its ID, its
  localization keys, and saves that refer to it.
- **Text:** every model needs entries in `Localization/*.json`. The
  compatibility check fails when one is missing.
- **Board game runs:** a run is a board game run when every player picks a
  board game character (`BoardGameRun.IsActive`). Gate board-game-only
  behavior on that, so runs with STS2 characters keep STS2's rules. The token
  rules in `Rules.cs` are the exception: they apply in every run.
- **Run rules and state:** `Core/BgRules.cs` is a modifier that is added to
  every board game run and receives every hook. Run state that must survive a
  save and load goes in its `[SavedProperty]` fields, and it must be copied in
  `DeepCloneFields`.
- **Registration:** STS2 lists characters, shared card pools, acts, and orbs
  by hand. `Core/Registration.cs` appends the board game's.
- **Multiplayer:** every player runs the same code. Any choice must go through
  `PlayerChoiceSynchronizer` (see `Core/Targeting.cs` and `Core/Choice.cs`),
  and randomness must use the run's or player's seeded RNG, never
  `System.Random`.
- **Inlining:** small game getters can be inlined by the JIT, which skips a
  Harmony patch. Patch their callers with a transpiler instead (see
  `Core/HandSize.cs`).

## Checks

Run `./check.sh`. It builds the mod and runs:

- `tests/Behavior`: the rule math in `Rules.cs` and the act maps.
- `tests/Compatibility`: loads the installed game's assemblies and checks that
  every Harmony patch installs, every private binding resolves, and every
  content model has text.

The compatibility check needs the .NET 9 runtime, because the game's Harmony
rejects .NET 10. If the default `dotnet` is not .NET 9, set `DOTNET9` to a .NET
9 `dotnet` and `DOTNET_ROOT` to its directory.

The checks cannot play the game. Say so when reporting a change to screens or
gameplay that hasn't been tried in the game.

## Version control

After every user prompt that changes this repository:

1. Run `./check.sh` and make sure it passes.
2. If the mod's code or text changed, run `./install.sh` to copy the build into
   the game. The game loads the mod at startup, so tell the user to restart it.
3. Commit all in-scope changes with a descriptive commit message.
4. Push the commit to the current branch's configured upstream.

Do not create empty commits for prompts that make no repository changes. Do not
include unrelated pre-existing worktree changes in the commit. If the checks,
the install, the commit, or the push fails, report the failure instead of claiming the task
is complete.
