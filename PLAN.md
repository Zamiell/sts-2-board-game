# Port plan

Goal: play *Slay the Spire: The Board Game* inside Slay the Spire 2. Values and behavior come from
[LuaViper/StSBoardGameMod](https://github.com/LuaViper/StSBoardGameMod) (MIT), a faithful StS1 port of the board game.

## Architecture

- **Board game run**: a run where every player is a board game character. Board game acts, rewards, and economy apply only
  to these runs. The rule changes (Vulnerable, Weak, Poison, limits) apply to every run.
- **Content** is new STS2 models in this assembly (`ModelDb` registers mod subtypes automatically):
  - Characters `BgIronclad`, `BgSilent`, `BgDefect`, `BgWatcher`, which borrow the visuals and sounds of an STS2 character
    by redirecting asset paths (`CharacterAssetPatch`). The Watcher borrows the Necrobinder.
  - Cards, powers, relics, and potions with `Bg` class prefixes (IDs `BG_*`, no clashes with STS2).
  - Monsters that borrow an STS2 monster's visuals and animator (`BgMonster.Look`) and pick their move from the die.
  - Acts `BgExordium`, `BgCity`, `BgBeyond`, `BgEnding`, swapped in by `RunState.CreateForNewRun` for board game runs.
- **Die**: rolled once per round, before enemies choose moves (`Hook.BeforeSideTurnStart`, player side).
- **Text**: embedded JSON (`Localization/eng/*.json`) merged into the game's tables as they load.
- **Art**: a card borrows the portrait of the STS2 card with the same name, else the game's placeholder portrait.

## Layers

1. [x] Rules: Vulnerable, Weak, Poison, token limits
2. [x] Foundation: text loading, die + display, board game run detection, character/card/monster/act bases
3. [x] Characters and starting decks
4. [x] Cards: Ironclad, Silent, Defect, Watcher, colorless, curses, statuses
5. [x] Powers
6. [x] Monsters, encounters, acts (Exordium, City, Beyond, Ending)
7. [ ] Relics, potions
8. [ ] Events, Neow, rewards, merchant, gold economy, rest sites
9. [ ] Maps
10. [ ] Character mechanics: Watcher stances and Miracles, Defect orbs, Silent Shivs as tokens

## Progress note
- Layer 7 done: all relic deck relics (die, passive, once-per-combat ability cards, on-pickup), boss relics (offered 3-choose-1
  after the Act I and II bosses), Prismatic Shard, and all 21 potions (drawn by their copies in the potion deck). Board game runs
  only find board game relics and potions.
- Next: layer 8 (events, Neow, rewards, merchant, gold, rest sites), layer 9 (maps), README.
