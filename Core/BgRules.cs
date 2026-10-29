using BoardGame.Monsters;
using BoardGame.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// Added to every board game run. It owns the die and the other run-wide board game rules, and receives every run and
// combat hook because modifiers are hook listeners.
public sealed class BgRules : ModifierModel, IBorrowsAssets
{
    public string BorrowedEntry => "SEALED_DECK";

    // The die is rolled once per round, before enemies choose their moves, and is shared by every player.
    public int Roll { get; private set; }

    // The roll before any relic or potion changed it this round.
    public int InitialRoll { get; private set; }

    private HashSet<Player> _lockedIn = [];

    private Dictionary<Player, int> _powersPlayed = [];

    private HashSet<Player> _lostHp = [];

    private Dictionary<Player, int> _discardedThisTurn = [];

    private Dictionary<Player, int> _attacksThisTurn = [];

    private Dictionary<Player, int> _cardsThisTurn = [];

    private HashSet<Player> _powerThisTurn = [];

    // The party's reward decks (see RewardDeck), saved with the run as "deck=card,card;deck=card".
    internal Dictionary<string, List<string>> RewardDeckState { get; private set; } = [];

    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public string RewardDecks
    {
        get => string.Join(";", RewardDeckState.Select(d => d.Key + "=" + string.Join(",", d.Value)));
        set => RewardDeckState = (value ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Split('='))
            .ToDictionary(d => d[0], d => d.Length > 1 ? d[1].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() : []);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (oldPileType == PileType.None && card.Pile?.Type == PileType.Deck)
        {
            RewardDeck.Obtained(card);
        }
        return Task.CompletedTask;
    }

    // Mummified Hand needs a Power played this turn.
    public bool PlayedPowerThisTurn(Player player) => _powerThisTurn.Contains(player);

    // Relic abilities (Quest cards) are not cards: they don't lock in the die or count as cards played.
    public static bool IsRelicAbility(CardModel card) => card.Type == CardType.Quest;

    private HashSet<Player> _stanceChangedThisTurn = [];

    // Flurry of Blows deals more damage if you switched Stances this turn.
    public bool StanceChangedThisTurn(Player player) => _stanceChangedThisTurn.Contains(player);

    public void MarkStanceChanged(Player player) => _stanceChangedThisTurn.Add(player);

    // Cards played this turn, counting the one being played (FTL).
    public int CardsPlayedThisTurn(Player player) => _cardsThisTurn.GetValueOrDefault(player);

    // Cards discarded by card effects this turn (Blur, Sneaky Strike).
    public int DiscardedThisTurn(Player player) => _discardedThisTurn.GetValueOrDefault(player);

    // Attacks played this turn, including Shivs (Finisher).
    public int AttacksPlayedThisTurn(Player player) => _attacksThisTurn.GetValueOrDefault(player);

    // The last Attack or Skill any player played this turn (Doppelganger).
    public CardModel? LastAttackOrSkillThisTurn { get; private set; }

    public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card.Owner is { } player)
        {
            _discardedThisTurn[player] = DiscardedThisTurn(player) + 1;
        }
        return Task.CompletedTask;
    }

    // Blood for Blood costs less once you've lost HP this combat.
    public bool LostHpThisCombat(Player player) => _lostHp.Contains(player);

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (delta < 0 && creature.Player is { } player && MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress)
        {
            _lostHp.Add(player);
        }
        return Task.CompletedTask;
    }

    // Dark orbs deal 1 more damage for each Power their owner played this combat.
    public int PowersPlayed(Player player) => _powersPlayed.GetValueOrDefault(player);

    // Models are copied with MemberwiseClone, so each run needs its own collections.
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _lockedIn = [];
        _powersPlayed = [];
        _lostHp = [];
        _discardedThisTurn = [];
        _attacksThisTurn = [];
        _cardsThisTurn = [];
        _powerThisTurn = [];
        _stanceChangedThisTurn = [];
        RewardDeckState = RewardDeckState.ToDictionary(d => d.Key, d => d.Value.ToList());
    }

    public static BgRules? For(IRunState? runState) =>
        runState?.Modifiers.OfType<BgRules>().FirstOrDefault();

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        // Extra turns (e.g. Vault) are part of the same round and keep the die.
        if (side != CombatSide.Player || CombatManager.Instance.PlayersTakingExtraTurn.Count > 0)
        {
            return;
        }
        _discardedThisTurn.Clear();
        _attacksThisTurn.Clear();
        _cardsThisTurn.Clear();
        _powerThisTurn.Clear();
        _stanceChangedThisTurn.Clear();
        LastAttackOrSkillThisTurn = null;
        var roll = combatState.RunState.Rng.MonsterAi.NextInt(1, 7);
        InitialRoll = roll;
        _lockedIn.Clear();
        await SetRoll(combatState, roll, refreshMonsters: false);
    }

    // Changes the die before it is locked in (Gambling Chip, The Abacus, Toolbox, Gambler's Brew). Enemies whose moves
    // depend on the die change their intents to match.
    public async Task SetRoll(ICombatState combatState, int roll, bool refreshMonsters = true)
    {
        Roll = Math.Clamp(roll, 1, 6);
        foreach (var player in combatState.Players)
        {
            foreach (var relic in player.Relics.OfType<BgTheDie>())
            {
                relic.RefreshDisplay();
            }
        }
        if (refreshMonsters)
        {
            foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive))
            {
                if (enemy.Monster is BgMonster monster)
                {
                    monster.RefreshMoveForDie();
                }
            }
        }
        await Task.CompletedTask;
    }

    public bool IsLockedIn(Player player) => _lockedIn.Contains(player);

    // The die locks in for a player the first time they play a card, use a potion, or end their turn. Their die relics
    // then trigger on the final roll.
    public async Task LockIn(PlayerChoiceContext choiceContext, Player player)
    {
        if (Roll == 0 || !_lockedIn.Add(player))
        {
            return;
        }
        foreach (var relic in player.Relics.OfType<IDieRelic>().ToList())
        {
            await relic.AfterDieLockedIn(choiceContext, Roll);
        }
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (IsRelicAbility(cardPlay.Card))
        {
            return;
        }
        if (cardPlay.Card.Owner is { } owner && cardPlay.Card is not Cards.BgShiv)
        {
            _cardsThisTurn[owner] = CardsPlayedThisTurn(owner) + 1;
        }
        if (cardPlay.Card.Owner is { } player && !cardPlay.IsAutoPlay)
        {
            await LockIn(new ThrowingPlayerChoiceContext(), player);
        }
    }

    public override async Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner is { } player && CombatManager.Instance.IsInProgress && potion is not Potions.BgGamblersBrew)
        {
            await LockIn(new ThrowingPlayerChoiceContext(), player);
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player)
        {
            return;
        }
        foreach (var creature in participants)
        {
            if (creature.Player is { } player)
            {
                await LockIn(choiceContext, player);
            }
        }
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner is not { } player)
        {
            return Task.CompletedTask;
        }
        if (cardPlay.Card.Type == CardType.Power)
        {
            _powersPlayed[player] = PowersPlayed(player) + 1;
            _powerThisTurn.Add(player);
        }
        if (cardPlay.Card.Type == CardType.Attack)
        {
            _attacksThisTurn[player] = AttacksPlayedThisTurn(player) + 1;
        }
        if (cardPlay.Card.Type is CardType.Attack or CardType.Skill && cardPlay.Card is not Cards.BgShiv)
        {
            LastAttackOrSkillThisTurn = cardPlay.Card;
        }
        return Task.CompletedTask;
    }

    // Where the burning elite is: "act:column:row", or empty.
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public string EmeraldElite { get; set; } = "";

    // Board game acts use the board game's printed maps.
    public override MegaCrit.Sts2.Core.Map.ActMap ModifyGeneratedMap(IRunState runState, MegaCrit.Sts2.Core.Map.ActMap map, int actIndex)
    {
        if (runState.Act is not Acts.BgAct act)
        {
            return map;
        }
        var boardGameMap = act.CreateBoardGameMap(runState);
        if (boardGameMap is Acts.BgActMap { BurningElite: { } burning })
        {
            EmeraldElite = Keys.Mark(actIndex, burning.coord);
        }
        Keys.MarkBurningElite(runState, boardGameMap);
        return boardGameMap;
    }

    public override MegaCrit.Sts2.Core.Map.ActMap ModifyGeneratedMapLate(IRunState runState, MegaCrit.Sts2.Core.Map.ActMap map, int actIndex)
    {
        Keys.MarkBurningElite(runState, map);
        return map;
    }

    public override Task BeforeCombatStart() => Keys.AtBurningElite(RunState) ? Keys.AddBurns(RunState) : Task.CompletedTask;

    public override Task BeforeRoomEntered(MegaCrit.Sts2.Core.Rooms.AbstractRoom room)
    {
        Keys.CloseActFourWithoutKeys(RunState, room);
        return Task.CompletedTask;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<MegaCrit.Sts2.Core.Entities.RestSite.RestSiteOption> options)
    {
        if (RunState.CurrentActIndex >= 3 || Keys.PartyHas<BgRubyKey>(RunState))
        {
            return false;
        }
        options.Add(new BgRecallRestSiteOption(player));
        return true;
    }

    public override decimal ModifyMerchantPrice(Player player, MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry entry, decimal cost) =>
        BgMerchantInventoryPatch.Price(entry, player, cost);

    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        Roll = 0;
        InitialRoll = 0;
        _lockedIn.Clear();
        _powersPlayed.Clear();
        _lostHp.Clear();
        return Task.CompletedTask;
    }
}
