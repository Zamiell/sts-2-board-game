# Slay the Spire: The Board Game, in Slay the Spire 2

A Slay the Spire 2 mod that recreates *Slay the Spire: The Board Game*: its four characters, cards, enemies, die, relics, potions, events, maps, and economy.

The board game's content and numbers come from [LuaViper/StSBoardGameMod](https://github.com/LuaViper/StSBoardGameMod) (MIT), a faithful Slay the Spire 1 port of the board game.

## How to play

Start a run with one of the board game characters: **Ironclad**, **Silent**, **Defect**, or **Watcher**. They're listed after the Slay the Spire 2 characters, with a die in the corner of their portraits and "(Board Game)" after their names. A run is a board game run when every player picks a board game character. Board game runs use the board game's acts, maps, rewards, and economy. Runs with Slay the Spire 2 characters are unchanged, except for the rule changes under [Rules for every run](#rules-for-every-run).

Slay the Spire 2 has no Watcher, so she uses the Necrobinder's body and voice. Other board game content uses the art of the Slay the Spire 2 card, relic, enemy, or event with the same name or a similar theme.

## What's included

### Characters and cards

- The four characters with their board game HP (9 or 10), 5 starting gold, starting decks, and starting relics.
- Every card in each character's set, plus the colorless cards, curses, statuses, and tokens (Shivs, Miracles, and others).
- Watcher stances and Miracles, Defect orbs, and Silent Shivs work as in the board game.

### Combat

- **The die.** It's rolled once per round, before enemies choose their moves, and all players share it. It shows on screen. Enemy moves and many relics follow the roll. The die locks in when you play your first card or use a potion. Gambling Chip, The Abacus, Toolbox, and Gambler's Brew can change it before then.
- **Enemies.** All of them are here, with board game HP and move patterns. That covers Act I, The City, The Beyond, and Act IV's Spire Shield, Spire Spear, and Corrupt Heart. A few encounters have harder versions from Ascension 7.
- **Token rules.** Vulnerable, Weak, Poison, Strength, Block, and Energy follow the board game (see the table below).

### Relics and potions

- **Relic deck.** All of its relics: die relics, passive relics, relics with an effect when you pick them up, and once-per-combat relics.
  - Slay the Spire 2 relics can't be clicked. Instead, each once-per-combat relic puts a free, retained card in your hand at the start of combat. Playing it doesn't lock in the die or count as playing a card.
- **Boss relics.** After the Act I and Act II bosses, each player draws 3 boss relics and keeps 1.
- **Potions.** All 21 potions. A potion that the board game deck has two copies of is twice as likely to drop.

### Rewards and economy

- **Reward decks.** Each color has a reward deck: 2 copies of each Common, 1 of each Uncommon, and 2 Golden Tickets that draw from that color's Rare deck. There are also colorless and curse decks.
  - The decks are shuffled once and saved with the run. The party shares them.
  - Drawn cards go to the bottom of their deck. A card taken into anyone's deck leaves the reward deck.
- **Fight rewards.** Each encounter gives the board game's gold (0–3), and some also give a potion.
  - Every fight offers 3 cards from your reward deck.
  - Elites give a relic. From Act II on, their cards are upgraded.
  - Bosses offer Rare cards. The Act III boss offers none.
  - After the Act I and Act II bosses, the rewards include 3 boss relics. Taking one discards the others.
- **Merchant.**
  - The shop shows the board game's layout: 3 cards from your reward deck on top, with the relics and potions beside them, and 3 colorless cards below.
  - Commons cost 2, Uncommons 3, Rares 6. Relics cost their board game price (the first is 1 gold cheaper). Potions cost their board game price.
  - Card removal costs 3 (4 from Ascension 8).
  - Bought items aren't restocked.
- **Rest sites.** Resting heals 3 HP.
- **Between acts.** You heal to full, or only 4 HP from Ascension 6.
- **Hand size.** No hand limit. Past 10 cards, the hand fans out wider and the cards shrink.

### Events and Neow

- **Neow.** Draws a random Neow card with two free blessings and a third that has a drawback. Slay the Spire 2's other Ancients don't appear.
- **Events.** All 39 board game events, each in its own act's event deck, with the board game's intro text.
  - Die events (Scrap Ooze, Wheel of Change, The Joust, and others) roll the die. Your die relics and Gambler's Brew can change the roll.
  - In shared multiplayer events, the party shares one roll, and any player's relics and potions can change it.
  - Some events start fights: Dead Adventurer, The Colosseum, and Mind Bloom.
  - The Secret Portal sends you straight to the boss.
- **"?" rooms.** A "?" room draws from the act's event deck.
  - From Ascension 3, harsher events, two Hallway fights, and a Merchant join the event decks.
  - The first "?" room of Act I is always an event.

### Maps, keys, and Act IV

- **Maps.** Acts I–III use the board game's printed maps, with their dark and light tokens shuffled face-down. Act I has two maps to choose from at random.
- **Keys.** Act IV is reached only with all three keys:
  - **Ruby Key:** choose Recall at a rest site.
  - **Emerald Key:** beat the burning elite. It's hidden under one of the dark tokens, marked on the map, and adds 2 Burns to your draw pile.
  - **Sapphire Key:** offered with an elite's relic. You take one or the other.
- **Act IV.** A rest site, a Merchant, and the Corrupt Heart. From Ascension 11, the Spire Shield and Spear guard the Heart. They stand in different rows (Pincer Movement), so area attacks hit only one of them.

### Ascension

Board game characters use the board game's 13 Ascension levels instead of Slay the Spire 2's 10. Winning on your highest level unlocks the next, up to 13. The Ascension panel describes the board game's levels.

| Level | Effect |
| --- | --- |
| 1 | Elites are deadlier |
| 2 | 1 less max HP |
| 3 | Harsher events, Hallway fights, and Merchants in "?" rooms |
| 4 | 1 less potion slot |
| 5 | Start with Ascender's Bane |
| 6 | Heal only 4 HP between acts |
| 7 | Normal enemies are deadlier |
| 8 | Card removal costs 4 |
| 9 | Start 1 HP down |
| 10 | Bosses are deadlier, and give 1 less gold |
| 11 | The Spire Shield and Spear guard the Heart |
| 12 | Elites are even deadlier |
| 13 | Two bosses at the end of The Beyond |

Slay the Spire 2's own Ascension effects don't apply in board game runs.

### Choices

When an effect asks you to pick an enemy or a player, you pick with the targeting arrow, and a thought bubble shows the question. Relics you give up in events are picked on the relic selection screen. Choices between effects, die faces, orbs, or relic abilities are offered as option cards.

## Rules for every run

These rules apply in every run, including runs with Slay the Spire 2 characters.

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

Hits are calculated in the board game's order: Strength, then Weak's −1, then Vulnerable's doubling. Damage never drops below 0. Gaining past a maximum is ignored, like taking a token from an empty supply. Losses are never blocked, so negative Strength still works.

The Vulnerable, Weak, and Poison tooltips describe these rules. That text is English in every language, because the translations would describe the old rules.

Some Slay the Spire 2 items change these rules. Paper Phrog, Cruelty, and Debilitate still increase Vulnerable damage on top of the doubling. Paper Krane and Debilitate make Weak remove 2 damage per hit instead of 1.

## Differences from the board game

- **Option cards.** Choices that don't point at a creature, like die faces, relic abilities, orbs, and modal cards such as Wish, appear as option cards. Slay the Spire 2 has nothing on screen to point at for these.
- **Art and result text.** Board game content uses Slay the Spire 2 art. Event intros use the board game's text. The text after an event choice, and all of Living Wall's text, is written for this mod.

## Status

The build, rule-math checks, and compatibility check pass against the installed game. All content is implemented, but the mod hasn't been played through in the game yet. Expect balance and presentation issues.

## Build and install

Requires the .NET SDK with `net9.0` support and a local game installation.

```sh
dotnet build -c Release
./install.sh
```

Default Steam paths are supported on Windows, macOS, and WSL (using the Windows installation). To use a different location, pass `-p:GameDir="/path/to/Slay the Spire 2"` or `-p:GameDataDir="/path/to/data_sts2_platform"`.

Build output is in `dist/BoardGame/`. To install manually, copy `BoardGame.dll` and `BoardGame.json` into the game's `mods/BoardGame/` directory. On macOS, `mods/` is inside `SlayTheSpire2.app/Contents/MacOS/`.

Restart the game after installing. To uninstall, remove `mods/BoardGame/`. The mod changes gameplay, so every player in a multiplayer game needs it.

## Development checks

```sh
./check.sh
```

This builds the mod and runs two checks:

- **Behavior checks** cover the rule math in `Rules.cs`: maximums, hit order, and how Weak and Vulnerable interact. They also check that every printed act map is well formed.
- **The compatibility check** loads the real game assemblies. It verifies that every Harmony patch installs, that private bindings resolve, and that every content model has text.

The compatibility check needs the .NET 9 runtime, because the game's Harmony library doesn't support .NET 10. Point `DOTNET9` at a .NET 9 `dotnet` if it isn't your default.
