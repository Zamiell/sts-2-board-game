using BoardGame.Cards;
using BoardGame.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Powers;

// Barricade: Block is not removed at the start of your turn (still at most 20).
public sealed class BgBarricadePower : BgPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldClearBlock(Creature creature) => creature != Owner;
}

// Berserk: whenever you Exhaust a card, deal this much damage to any row.
public sealed class BgBerserkPower : BgPower
{
    public override string BorrowedEntry => "RAGE_POWER";

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner?.Creature == Owner && Owner.Player is { } player)
        {
            Flash();
            await Rows.DamageAnyRow(choiceContext, player, Amount, "Berserk");
        }
    }
}

// Once per turn: deal this much damage to any row. Board game "once per turn" abilities are used at the end of the turn
// if the player hasn't used them earlier.
public sealed class BgCombustPower : BgPower
{
    public override string BorrowedEntry => "INFERNO_POWER";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner) && Owner.Player is { } player)
        {
            Flash();
            await Rows.DamageAnyRow(choiceContext, player, Amount, "Combust");
        }
    }
}

// Corruption: Skills cost 0. Whenever you play a Skill, Exhaust it.
public sealed class BgCorruptionPower : BgPower
{
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner || card.Type != CardType.Skill)
        {
            return false;
        }
        modifiedCost = 0m;
        return true;
    }

    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, MegaCrit.Sts2.Core.Entities.Cards.ResourceInfo resources, CardLocation location)
    {
        if (card.Owner?.Creature == Owner && card.Type == CardType.Skill)
        {
            location.pileType = PileType.Exhaust;
        }
        return location;
    }
}

// Dark Embrace: whenever you Exhaust a card, draw 1 card.
public sealed class BgDarkEmbracePower : BgPower
{
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner?.Creature == Owner && Owner.Player is { } player && Owner.CombatState?.HittableEnemies.Any() == true)
        {
            await CardPileCmd.Draw(choiceContext, Amount, player);
        }
    }
}

// Demon Form: at the start of your turn, gain Strength.
public sealed class BgDemonFormPower : BgPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
        }
    }
}

// Evolve: whenever you draw Dazed, Burn, or Slimed, draw 1 card.
public sealed class BgEvolvePower : BgPower
{
    public override string BorrowedEntry => "ADAPTABLE_POWER";

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner?.Creature == Owner && card is BgDazed or BgBurn or BgSlimed && Owner.Player is { } player)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, player);
        }
    }
}

// Feel No Pain: whenever you Exhaust a card, gain Block.
public sealed class BgFeelNoPainPower : BgPower
{
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner?.Creature == Owner)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        }
    }
}

// Fire Breathing: whenever you draw Dazed, Burn, Slimed, or a Curse, deal this much damage to any row.
public sealed class BgFireBreathingPower : BgPower
{
    public override string BorrowedEntry => "INFERNO_POWER";

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner?.Creature == Owner && (card is BgDazed or BgBurn or BgSlimed || card.Type == CardType.Curse) && Owner.Player is { } player)
        {
            Flash();
            await Rows.DamageAnyRow(choiceContext, player, Amount, "Fire Breathing");
        }
    }
}

// Juggernaut: whenever you gain Block (while below 20), deal this much damage to an enemy of your choice.
public sealed class BgJuggernautPower : BgPower
{
    private int _blockBefore;

    public override Task BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature == Owner)
        {
            _blockBefore = Owner.Block;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || amount <= 0 || _blockBefore >= Rules.MaxPlayerBlock || Owner.Player is not { } player)
        {
            return;
        }
        var context = new ThrowingPlayerChoiceContext();
        var target = await Rows.ChooseEnemy(context, player, $"Juggernaut: deal {Amount} damage to");
        if (target != null)
        {
            Flash();
            await CreatureCmd.Damage(context, target, Amount, ValueProp.Unpowered, Owner, null, null);
        }
    }
}

// Metallicize: at the end of your turn, gain Block.
public sealed class BgMetallicizePower : BgPower
{
    public override string BorrowedEntry => "PLATING_POWER";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner))
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        }
    }
}

// No Draw: you can't draw cards this turn.
public sealed class BgNoDrawPower : BgPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldDraw(Player player, bool fromHandDraw) => fromHandDraw || player.Creature != Owner;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Double Tap: this turn, your next Attack is played twice.
public sealed class BgDoubleAttackPower : BgPower
{
    public override string BorrowedEntry => "DUPLICATION_POWER";

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card.Owner?.Creature == Owner && card.Type == CardType.Attack && card is not BgShiv ? playCount + 1 : playCount;

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Flex: Strength gained this turn that is lost at the end of the turn.
public sealed class BgFlexPower : BgPower
{
    public override string BorrowedEntry => "FLEX_POTION_POWER";

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -Amount, Owner, null);
            await PowerCmd.Remove(this);
        }
    }
}
