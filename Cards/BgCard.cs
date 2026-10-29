using BoardGame.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards;

public abstract class BgCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CardModel(cost, type, rarity, target)
{
    // Uses the portrait of the STS2 card with the same name, e.g. BG_BASH uses BASH; otherwise the placeholder.
    public override string PortraitPath => Borrow.Matching<CardModel>(this)?.PortraitPath ?? MissingPortraitPath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath];

    protected bool IsMultiplayer => IsMutable && Owner?.RunState.Players.Count > 1;

    // Board game cards that say "to any player" let you choose another player in multiplayer.
    protected virtual bool TargetsAnyPlayer => false;

    public override TargetType TargetType => TargetsAnyPlayer && IsMultiplayer ? TargetType.AnyPlayer : base.TargetType;

    protected Creature Me => Owner.Creature;

    private bool _inHandAtTurnEnd;

    // True if this card was in your hand at the end of last turn and is still there (it was Retained).
    public bool WasRetainedLastTurn => _inHandAtTurnEnd && Pile?.Type == PileType.Hand;

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Combat.CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == MegaCrit.Sts2.Core.Combat.CombatSide.Player && IsMutable && participants.Contains(Owner.Creature))
        {
            _inHandAtTurnEnd = Pile?.Type == PileType.Hand;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, MegaCrit.Sts2.Core.Models.AbstractModel? clonedBy)
    {
        if (card == this && oldPileType == PileType.Hand)
        {
            _inHandAtTurnEnd = false;
        }
        return Task.CompletedTask;
    }

    protected IEnumerable<Creature> LivingEnemies => CombatState?.HittableEnemies ?? [];

    // Deals this card's Damage to the chosen enemy, once per hit.
    protected Task Attack(PlayerChoiceContext choiceContext, CardPlay cardPlay, int hits = 1, string vfx = "vfx/vfx_attack_slash") =>
        Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue, hits, vfx);

    protected async Task Attack(PlayerChoiceContext choiceContext, CardPlay cardPlay, decimal damage, int hits = 1,
        string vfx = "vfx/vfx_attack_slash")
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(damage).WithHitCount(hits).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(vfx).Execute(choiceContext);
    }

    protected Task AttackAll(PlayerChoiceContext choiceContext, CardPlay cardPlay, int hits = 1, string vfx = "vfx/vfx_attack_slash") =>
        AttackAll(choiceContext, cardPlay, DynamicVars.Damage.BaseValue, hits, vfx);

    protected async Task AttackAll(PlayerChoiceContext choiceContext, CardPlay cardPlay, decimal damage, int hits = 1,
        string vfx = "vfx/vfx_attack_slash")
    {
        var spared = await Pincer.ChooseSpared(choiceContext, Owner);
        await Pincer.WithSpared(spared, () => DamageCmd.Attack(damage).WithHitCount(hits).FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!).WithHitFx(vfx).Execute(choiceContext));
    }

    // Gains this card's Block for the chosen player, or for the card's owner.
    protected Task GainBlock(CardPlay cardPlay) => GainBlock(cardPlay, DynamicVars.Block.BaseValue);

    protected async Task GainBlock(CardPlay cardPlay, decimal amount)
    {
        var target = cardPlay.Target is { IsPlayer: true } player ? player : Me;
        await CreatureCmd.GainBlock(target, amount, DynamicVars.Block.Props, cardPlay);
    }

    protected Task Apply<TPower>(PlayerChoiceContext choiceContext, Creature target, decimal amount) where TPower : PowerModel =>
        PowerCmd.Apply<TPower>(choiceContext, target, amount, Me, this);

    protected async Task ApplyToAllEnemies<TPower>(PlayerChoiceContext choiceContext, decimal amount) where TPower : PowerModel
    {
        foreach (var enemy in LivingEnemies.ToList())
        {
            await Apply<TPower>(choiceContext, enemy, amount);
        }
    }

    protected Task Draw(PlayerChoiceContext choiceContext, decimal count) => CardPileCmd.Draw(choiceContext, count, Owner);

    protected async Task<IReadOnlyList<CardModel>> ExhaustFromHand(PlayerChoiceContext choiceContext, int min, int max,
        Func<CardModel, bool>? filter = null)
    {
        var cards = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs.ExhaustSelectionPrompt, min, max),
            filter, this)).ToList();
        foreach (var card in cards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        return cards;
    }

    // Statuses from cards: Dazed goes on top of the draw pile.
    protected async Task AddToDrawPile<TCard>(int count, CardPilePosition position = CardPilePosition.Top) where TCard : CardModel
    {
        for (var i = 0; i < count; i++)
        {
            await CardPileCmd.AddGeneratedCardToCombat(CombatState!.CreateCard<TCard>(Owner), PileType.Draw, Owner, position);
        }
    }

    protected async Task<CardModel?> ChooseFromPile(PlayerChoiceContext choiceContext, PileType pile, string prompt, Func<CardModel, bool>? filter = null)
    {
        var cards = pile.GetPile(Owner).Cards.Where(c => filter?.Invoke(c) ?? true).ToList();
        if (cards.Count == 0)
        {
            return null;
        }
        var prefs = new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(Choice.PromptText(prompt), 1);
        return (await CardSelectCmd.FromSimpleGrid(choiceContext, cards, Owner, prefs)).FirstOrDefault();
    }

    protected IEnumerable<CardModel> Hand => Owner.PlayerCombatState?.Hand.Cards ?? [];

    protected async Task DiscardFromHand(PlayerChoiceContext choiceContext, int count)
    {
        var cards = await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs.DiscardSelectionPrompt, count),
            null, this);
        foreach (var card in cards.ToList())
        {
            await CardCmd.Discard(choiceContext, card);
        }
    }
}

public abstract class BgIroncladCard(int cost, CardType type, CardRarity rarity, TargetType target) : BgCard(cost, type, rarity, target);

public abstract class BgSilentCard(int cost, CardType type, CardRarity rarity, TargetType target) : BgCard(cost, type, rarity, target);

public abstract class BgDefectCard(int cost, CardType type, CardRarity rarity, TargetType target) : BgCard(cost, type, rarity, target);

public abstract class BgWatcherCard(int cost, CardType type, CardRarity rarity, TargetType target) : BgCard(cost, type, rarity, target);

public abstract class BgColorlessCard(int cost, CardType type, CardRarity rarity, TargetType target) : BgCard(cost, type, rarity, target);

public abstract class BgCurseCard(int cost = -1) : BgCard(cost, CardType.Curse, CardRarity.Curse, TargetType.None);

public abstract class BgStatusCard(int cost, TargetType target = TargetType.None) : BgCard(cost, CardType.Status, CardRarity.Status, target);

// Cards created during combat that never appear in rewards, like Shivs and Miracles.
public abstract class BgTokenCard(int cost, CardType type, TargetType target) : BgCard(cost, type, CardRarity.Token, target);
