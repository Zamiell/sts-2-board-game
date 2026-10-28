using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards;

// Statuses are removed from the deck at the end of combat. Dazed goes on top of the draw pile; others into the discard pile.

public sealed class BgDazed() : BgStatusCard(-1)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];
}

public sealed class BgBurn() : BgStatusCard(-1)
{
    public override bool HasTurnEndInHandEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Unpowered)];

    protected override Task OnTurnEndInHand(PlayerChoiceContext choiceContext) =>
        CreatureCmd.Damage(choiceContext, Me, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, this, null);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgSlimed() : BgStatusCard(1)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
}

// When drawn, you must immediately pay 1 Energy to Exhaust it, if able.
public sealed class BgVoid() : BgStatusCard(1)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Models.CardModel card, bool fromHandDraw)
    {
        if (card == this && Owner.PlayerCombatState is { Energy: >= 1 })
        {
            await PlayerCmd.LoseEnergy(1m, Owner);
            await CardCmd.Exhaust(choiceContext, this);
        }
    }
}

public sealed class BgAscendersBane() : BgCurseCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal, CardKeyword.Eternal];
}

public sealed class BgClumsy() : BgCurseCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];
}

public sealed class BgInjury() : BgCurseCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}

public sealed class BgRegret() : BgCurseCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Retain];
}

// End of turn: if this is in your hand, take 1 damage.
public sealed class BgDecay() : BgCurseCard
{
    public override bool HasTurnEndInHandEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override Task OnTurnEndInHand(PlayerChoiceContext choiceContext) =>
        CreatureCmd.Damage(choiceContext, Me, 1m, ValueProp.Unpowered, this, null);
}

// End of turn: if this is in your hand, gain 1 Weak.
public sealed class BgDoubt() : BgCurseCard
{
    public override bool HasTurnEndInHandEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override Task OnTurnEndInHand(PlayerChoiceContext choiceContext) => Apply<WeakPower>(choiceContext, Me, 1m);
}

// End of turn: if this is in your hand with 2 or fewer cards, lose 1 HP.
public sealed class BgPain() : BgCurseCard
{
    public override bool HasTurnEndInHandEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        if (Owner.PlayerCombatState?.Hand.Cards.Count <= 2)
        {
            await CreatureCmd.Damage(choiceContext, Me, 1m, ValueProp.Unblockable | ValueProp.Unpowered, this, null);
        }
    }
}

// If this is removed from your deck, lose 2 HP.
public sealed class BgParasite() : BgCurseCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override async Task BeforeCardRemoved(MegaCrit.Sts2.Core.Models.CardModel card)
    {
        if (card == this && !Me.IsDead)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Me, 2m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        }
    }
}

// End of turn: if this is in your hand, lose 1 Block.
public sealed class BgShame() : BgCurseCard
{
    public override bool HasTurnEndInHandEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override Task OnTurnEndInHand(PlayerChoiceContext choiceContext) =>
        CreatureCmd.LoseBlock(choiceContext, Me, 1m, null);
}

// Costs 1. When played, Exhaust.
public sealed class BgWrithe() : BgCurseCard(1)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
}
