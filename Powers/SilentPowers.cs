using BoardGame.Cards;
using BoardGame.Core;
using MegaCrit.Sts2.Core.CardSelection;
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

// A Thousand Cuts: whenever you shuffle your draw pile, deal this much damage to any row.
public sealed class BgAThousandCutsPower : BgPower
{
    public override string BorrowedEntry => "PAPER_CUTS_POWER";

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler.Creature == Owner)
        {
            Flash();
            await Rows.DamageAnyRow(choiceContext, shuffler, Amount, "A Thousand Cuts");
        }
    }
}

// After Image: whenever you discard one or more cards during your turn, gain Block (once per card or effect).
public sealed class BgAfterImagePower : BgPower
{
    public override string BorrowedEntry => "AFTERIMAGE_POWER";

    private bool _triggeredThisEffect;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        _triggeredThisEffect = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card.Owner?.Creature != Owner || _triggeredThisEffect)
        {
            return;
        }
        _triggeredThisEffect = true;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _triggeredThisEffect = false;
        return Task.CompletedTask;
    }
}

// Corpse Explosion: when this enemy dies, deal this much damage to its row.
public sealed class BgCorpseExplosionPower : BgPower
{
    public override string BorrowedEntry => "POISON_POWER";
    public override PowerType Type => PowerType.Debuff;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Owner || wasRemovalPrevented || Owner.CombatState is not { } combat)
        {
            return;
        }
        var row = Rows.RowOf(Owner);
        var targets = Rows.EnemiesInRow(combat, row).Where(e => e != Owner && e.IsAlive).ToList();
        if (targets.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, targets, Amount, ValueProp.Unpowered, Applier, null, null);
        }
    }
}

// Distraction: once per turn, when you apply Weak, Vulnerable, or Poison to an enemy, gain Block.
public sealed class BgDistractionPower : BgPower
{
    public override string BorrowedEntry => "SNEAKY_POWER";

    private bool _usedThisTurn;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            _usedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_usedThisTurn || applier != Owner || amount <= 0 || power.Owner == Owner
            || power is not (WeakPower or VulnerablePower or PoisonPower))
        {
            return;
        }
        _usedThisTurn = true;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

// Envenom: every hit of your attacks also applies Poison.
public sealed class BgEnvenomPower : BgPower
{
    public override string BorrowedEntry => "ENVENOM_POWER";

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner && target != Owner && props.IsPoweredAttack() && target.IsAlive)
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Owner, cardSource, silent: true);
        }
    }
}

// Infinite Blades: at the start of your turn, gain Shivs.
public sealed class BgInfiniteBladesPower : BgPower
{
    public override string BorrowedEntry => "INFINITE_BLADES_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            Flash();
            await Tokens.AddShivs(player, Amount);
        }
    }
}

// Noxious Fumes: at the start of your turn, apply Poison to an enemy of your choice (every enemy when upgraded).
public sealed class BgNoxiousFumesPower : BgPower
{
    public override string BorrowedEntry => "NOXIOUS_FUMES_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        var target = await Rows.ChooseEnemy(choiceContext, player, $"Noxious Fumes: apply {Amount} Poison to");
        if (target != null)
        {
            Flash();
            await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Owner, null);
        }
    }
}

public sealed class BgNoxiousFumesAllPower : BgPower
{
    public override string BorrowedEntry => "NOXIOUS_FUMES_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState is not { } combat)
        {
            return;
        }
        Flash();
        foreach (var enemy in combat.HittableEnemies.ToList())
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, Amount, Owner, null);
        }
    }
}

// Well-Laid Plans: at the end of your turn, you may Retain this many cards.
public sealed class BgWellLaidPlansPower : BgPower
{
    public override string BorrowedEntry => "WELL_LAID_PLANS_POWER";

    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player.PlayerCombatState is not { } state || state.Hand.Cards.Count == 0)
        {
            return;
        }
        var prefs = new CardSelectorPrefs(Choice.PromptText($"Choose up to {Amount} {(Amount == 1 ? "card" : "cards")} to Retain"), 0, Amount);
        foreach (var card in await CardSelectCmd.FromHand(choiceContext, player, prefs, null, this))
        {
            CardCmd.ApplySingleTurnRetain(card);
        }
    }
}

// Wraith Form: you can't lose more than 1 HP per round. Lasts this many rounds.
public sealed class BgWraithFormPower : BgPower
{
    public override string BorrowedEntry => "WRAITH_FORM_POWER";

    private int _lostThisRound;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner ? Math.Max(0, Math.Min(amount, 1 - _lostThisRound)) : amount;

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner && delta < 0)
        {
            _lostThisRound += (int)-delta;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        _lostThisRound = 0;
        await PowerCmd.Decrement(this);
    }
}

// Burst-like token powers for Silent's Bullet Time: cards in hand cost 0 this turn.
public sealed class BgBulletTimePower : BgPower
{
    public override string BorrowedEntry => "FREE_SKILL_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    private HashSet<CardModel> _cards = [];

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _cards = [];
    }

    public void Cover(IEnumerable<CardModel> cards) => _cards.UnionWith(cards);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!_cards.Contains(card))
        {
            return false;
        }
        modifiedCost = 0m;
        return true;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
