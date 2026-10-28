using BoardGame.Cards;
using BoardGame.Cards.Watcher;
using BoardGame.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Powers;

// Battle Hymn: once per turn (at the end of your turn), deal this much damage, twice as much in Wrath.
public sealed class BgBattleHymnPower : BgPower
{
    public override string BorrowedEntry => "MONOLOGUE_POWER";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || Owner.Player is not { } player)
        {
            return;
        }
        var damage = Amount * (Stance.IsIn<BgWrathStance>(player) ? 2 : 1);
        var target = await Rows.ChooseEnemy(choiceContext, player, $"Battle Hymn: deal {damage} damage to");
        if (target != null)
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered, Owner, null, null);
        }
    }
}

// You can't play additional cards this turn (Conclude, Meditate).
public sealed class BgConclusionPower : BgPower
{
    public override string BorrowedEntry => "NO_DRAW_POWER";
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        autoPlayType != AutoPlayType.None || card.Owner?.Creature != Owner;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Conjure Blade: your starter Strikes deal this much more damage.
public sealed class BgConjureBladePower : BgPower
{
    public override string BorrowedEntry => "SWORD_SAGE_POWER";

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && cardSource is { Rarity: CardRarity.Basic } && cardSource.Tags.Contains(CardTag.Strike) && props.IsPoweredAttack()
            ? Amount
            : 0m;
}

// Deva Form: at the start of your turn, gain Miracles.
public sealed class BgDevaFormPower : BgPower
{
    public override string BorrowedEntry => "PILLAR_OF_CREATION_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            Flash();
            await Tokens.AddMiracles(player, Amount);
        }
    }
}

// Devotion: at the start of your turn, gain a Miracle and draw 1 card. Lasts this many turns.
public sealed class BgDevotionPower : BgPower
{
    public override string BorrowedEntry => "PILLAR_OF_CREATION_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        await Tokens.AddMiracles(player, 1);
        await CardPileCmd.Draw(choiceContext, 1m, player);
        await PowerCmd.Decrement(this);
    }
}

// Establishment: cards you Retained last turn cost this much less this turn.
public sealed class BgEstablishmentPower : BgPower
{
    public override string BorrowedEntry => "ESTABLISHMENT_POWER";

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner || card is not BgCard { WasRetainedLastTurn: true })
        {
            return false;
        }
        modifiedCost = Math.Max(0m, originalCost - Amount);
        return true;
    }
}

// Foresight: at the start of your turn, before you draw, Scry this many.
public sealed class BgForesightPower : BgPower
{
    public override string BorrowedEntry => "FORESIGHT_POWER";

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player.Creature == Owner)
        {
            Flash();
            await Scry.Run(choiceContext, player, Amount);
        }
    }
}

// The next Attack you play this turn costs 0 (Swivel).
public sealed class BgFreeAttackPower : BgPower
{
    public override string BorrowedEntry => "FREE_ATTACK_POWER";

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner || card.Type != CardType.Attack)
        {
            return false;
        }
        modifiedCost = 0m;
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner && cardPlay.Card.Type == CardType.Attack && !cardPlay.IsAutoPlay)
        {
            await PowerCmd.Decrement(this);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Like Water: at the end of your turn, if you're in Calm, gain Block.
public sealed class BgLikeWaterPower : BgPower
{
    public override string BorrowedEntry => "PLATING_POWER";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner) && Owner.Player is { } player && Stance.IsIn<BgCalmStance>(player))
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        }
    }
}

// Mental Fortress: whenever you switch Stances, gain Block.
public sealed class BgMentalFortressPower : BgPower, IStanceListener
{
    public override string BorrowedEntry => "PLATING_POWER";

    public async Task AfterStanceChanged(PlayerChoiceContext choiceContext, Type? newStance)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

// Nirvana: whenever you Scry, gain Block.
public sealed class BgNirvanaPower : BgPower, IScryListener
{
    public override string BorrowedEntry => "CLARITY_POWER";

    public async Task AfterScry(PlayerChoiceContext choiceContext)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

// Omega: at the end of your turn, deal this much damage to any row.
public sealed class BgOmegaPower : BgPower
{
    public override string BorrowedEntry => "DYING_STAR_POWER";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner) && Owner.Player is { } player)
        {
            Flash();
            await Rows.DamageAnyRow(choiceContext, player, Amount, "Omega");
        }
    }
}

// Rushdown: the first time you enter Wrath each turn, draw cards.
public sealed class BgRushdownPower : BgPower, IStanceListener
{
    public override string BorrowedEntry => "RAGE_POWER";

    private bool _usedThisTurn;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            _usedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public async Task AfterStanceChanged(PlayerChoiceContext choiceContext, Type? newStance)
    {
        if (newStance == typeof(BgWrathStance) && !_usedThisTurn && Owner.Player is { } player)
        {
            _usedThisTurn = true;
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, player);
        }
    }
}

// Study: at the start of your turn, if you're in Calm, draw cards.
public sealed class BgStudyPower : BgPower
{
    public override string BorrowedEntry => "CLARITY_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner && Stance.IsIn<BgCalmStance>(player))
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, player);
        }
    }
}

// Blasphemy: your next Attack this turn is played 3 times.
public sealed class BgTripleAttackPower : BgPower
{
    public override string BorrowedEntry => "DUPLICATION_POWER";

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card.Owner?.Creature == Owner && card.Type == CardType.Attack && card is not BgShiv ? playCount + 2 : playCount;

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
