using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Powers;

// Curl Up: the first time this enemy takes attack damage that doesn't kill it, it gains Block (once per combat).
public sealed class BgCurlUpPower : BgPower
{
    public override string BorrowedEntry => "CURL_UP_POWER";

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0 || !Owner.IsAlive)
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        await PowerCmd.Remove(this);
    }
}

// Spore Cloud: when this enemy dies, the player in its row gains Vulnerable.
public sealed class BgSporeCloudPower : BgPower
{
    public override string BorrowedEntry => "SMOGGY_POWER";

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Owner || wasRemovalPrevented || Owner.CombatState is not { } combat || combat.HittableEnemies.Count == 0)
        {
            return;
        }
        var row = Core.Rows.RowOf(Owner);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, row.Creature, Amount, null, null);
    }
}

// Angry: whenever this enemy takes attack damage, it gains Strength.
public sealed class BgAngryPower : BgPower
{
    public override string BorrowedEntry => "ENRAGE_POWER";

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && props.IsPoweredAttack() && Owner.IsAlive)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
        }
    }
}

// Enrage (Gremlin Nob): whenever a player plays a Skill, they take this much damage.
public sealed class BgEnragePower : BgPower
{
    public override string BorrowedEntry => "ENRAGE_POWER";

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type == CardType.Skill && cardPlay.Card.Owner is { } player && Owner.IsAlive && Core.Rows.IsInRow(Owner, player))
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, player.Creature, Amount, ValueProp.Unpowered, Owner, null, null);
        }
    }
}

// Mode Shift (The Guardian): when its Block is broken, it changes to Defensive Mode.
public sealed class BgModeShiftPower : BgPower
{
    public override string BorrowedEntry => "HARDENED_SHELL_POWER";
    public override PowerStackType StackType => PowerStackType.Single;
}

// Split (Slime Boss): when it would die, it splits into three slimes.
public sealed class BgSplitPower : BgPower
{
    public override string BorrowedEntry => "INFESTED_POWER";
    public override PowerStackType StackType => PowerStackType.Single;
}

// Thievery (Looter, Mugger): steals this much Gold if it escapes.
public sealed class BgThieveryPower : BgPower
{
    public override string BorrowedEntry => "THIEVERY_POWER";
}

// Flight (Byrd): attacks deal at most 1 damage to it.
public sealed class BgFlightPower : BgPower
{
    public override string BorrowedEntry => "FLUTTER_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        target == Owner && props.IsPoweredAttack() ? 1m : decimal.MaxValue;
}

// Painful Stabs (Book of Stabbing): when you lose HP from its attacks, put a Dazed on top of your draw pile.
public sealed class BgPainfulStabsPower : BgPower
{
    public override string BorrowedEntry => "PAINFUL_STABS_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner && result.UnblockedDamage > 0 && props.IsPoweredAttack() && target.Player is { } player)
        {
            await CardPileCmd.AddGeneratedCardToCombat(Owner.CombatState!.CreateCard<BoardGame.Cards.BgDazed>(player), PileType.Draw, player, CardPilePosition.Top);
        }
    }
}

// Confusion (Snecko): while a Snecko lives, the first card you play each turn costs 2 (die 1-2), 1 (die 3-4), or
// 3 (die 5-6).
public sealed class BgConfusionPower : BgPower
{
    public override string BorrowedEntry => "CONFUSED_POWER";
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public static int CostFor(int die) => die switch { <= 2 => 2, <= 4 => 1, _ => 3 };

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        var combat = Owner.CombatState;
        var rules = Core.BgRules.For(combat?.RunState);
        if (card.Owner?.Creature != Owner || originalCost < 0 || rules == null || rules.Roll == 0
            || rules.CardsPlayedThisTurn(card.Owner) > 0 || combat?.Enemies.Any(e => e.IsAlive && e.Monster is Monsters.BgSnecko) != true)
        {
            return false;
        }
        modifiedCost = CostFor(rules.Roll);
        return true;
    }
}

// Curiosity (Awakened One): whenever a player plays a Power, it gains Strength (ignoring the Strength limit).
public sealed class BgCuriosityPower : BgPower
{
    public override string BorrowedEntry => "CURIOUS_POWER";

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type == CardType.Power && Owner.IsAlive)
        {
            Flash();
            await PowerCmd.Apply<BgUncappedStrengthPower>(choiceContext, Owner, Amount, Owner, null);
        }
    }
}

// Strength that isn't limited to 8.
public sealed class BgUncappedStrengthPower : BgPower
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() ? Amount : 0m;
}

// Explosive (Exploder): explodes after its third turn.
public sealed class BgExplosivePower : BgPower
{
    public override string BorrowedEntry => "THE_BOMB_POWER";
}

// Fading (Transient): dies after this many more of its turns.
public sealed class BgFadingPower : BgPower
{
    public override string BorrowedEntry => "FADE_POWER";
}

// Shifting (Transient): when it loses HP, the player who hurt it gains that much Block.
public sealed class BgShiftingPower : BgPower
{
    public override string BorrowedEntry => "SHROUD_POWER";

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && (dealer?.Player ?? dealer?.PetOwner) is { } player)
        {
            await CreatureCmd.GainBlock(player.Creature, result.UnblockedDamage, ValueProp.Unpowered, null);
        }
    }
}

// Intangible (Nemesis): damage it takes is reduced to 0. Reapplied every other turn.
public sealed class BgIntangiblePower : BgPower
{
    public override string BorrowedEntry => "INTANGIBLE_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        target == Owner ? 0m : decimal.MaxValue;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner ? 0m : amount;
}

// Orb Walker: at the end of each round, it gains Strength.
public sealed class BgOrbWalkerPower : BgPower
{
    public override string BorrowedEntry => "RITUAL_POWER";

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy && Owner.IsAlive)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
        }
    }
}

// Reactive (Writhing Mass): when it takes attack damage and survives, the die is rerolled.
public sealed class BgReactivePower : BgPower
{
    public override string BorrowedEntry => "IMBALANCED_POWER";
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && props.IsPoweredAttack() && Owner.IsAlive
            && Owner.CombatState is { } combat && Core.BgRules.For(combat.RunState) is { } rules)
        {
            Flash();
            await rules.SetRoll(combat, combat.RunState.Rng.MonsterAi.NextInt(1, 7));
        }
    }
}

// Regrow (Darkling): while another Darkling lives, it revives at the start of the players' turn at half HP.
public sealed class BgRegrowPower : BgPower
{
    public override string BorrowedEntry => "REATTACH_POWER";
    public override PowerStackType StackType => PowerStackType.Single;
}

// Slow (Giant Head): Attacks deal 1 more damage to it.
public sealed class BgSlowPower : BgPower
{
    public override string BorrowedEntry => "SLOW_POWER";
    public override PowerType Type => PowerType.Debuff;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        target == Owner && props.IsPoweredAttack() ? Amount : 0m;
}

// Spikes (Spiker): after a card attacks it, the attacker takes this much damage (at most 5).
public sealed class BgSpikerPower : BgPower
{
    public override string BorrowedEntry => "THORNS_POWER";

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Commands.Builders.AttackCommand command)
    {
        if (command.Attacker is { IsPlayer: true } attacker && command.DamageProps.IsPoweredAttack()
            && command.Results.SelectMany(hit => hit).Any(result => result.Receiver == Owner))
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, attacker, Math.Min(5, Amount), ValueProp.Unpowered, Owner, null, null);
        }
    }
}

// Time Warp (Time Eater): you can only play this many cards this turn.
public sealed class BgTimeWarpPower : BgPower
{
    public override string BorrowedEntry => "TIME_WARP_POWER";
    public override PowerType Type => PowerType.Debuff;

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        autoPlayType != AutoPlayType.None || card.Owner?.Creature != Owner || card is BoardGame.Cards.BgShiv || Core.BgRules.IsRelicAbility(card)
        || (Core.BgRules.For(Owner.CombatState?.RunState)?.CardsPlayedThisTurn(card.Owner) ?? 0) < Amount;

    public static async Task Set(Creature player, int limit)
    {
        if (player.GetPower<BgTimeWarpPower>() is { } power)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), power, limit - power.Amount, null, null, silent: true);
        }
        else
        {
            await PowerCmd.Apply<BgTimeWarpPower>(new ThrowingPlayerChoiceContext(), player, limit, null, null);
        }
    }
}

// Beat of Death (Corrupt Heart): at the start of each player turn, players take this much damage.
public sealed class BgBeatOfDeathPower : BgPower
{
    public override string BorrowedEntry => "PAINFUL_STABS_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (Owner.IsAlive)
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, player.Creature, Amount, ValueProp.Unpowered, Owner, null, null);
        }
    }
}

// Invincible (Corrupt Heart): it can only lose this much more HP, until it removes this with its buff.
public sealed class BgInvinciblePower : BgPower
{
    public override string BorrowedEntry => "INVINCIBLE_POWER";

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner ? Math.Min(amount, Amount) : amount;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner && delta < 0)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, Math.Max(delta, -Amount), null, null, silent: true);
        }
    }
}

// Damage you deal this turn is reduced to 0 (Spire Shield).
public sealed class BgZeroDamagePower : BgPower
{
    public override string BorrowedEntry => "SHRINK_POWER";
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner ? 0m : decimal.MaxValue;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

// Pincer Movement: the Spire Shield and Spear stand in different rows, so area attacks hit only one of them.
public sealed class BgPincerMovementPower : BgPower
{
    public override string BorrowedEntry => "SURROUNDED_POWER";
    public override PowerStackType StackType => PowerStackType.Single;
}
