using BoardGame.Core;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards.Silent;

// Card values are the board game's, taken from LuaViper/StSBoardGameMod.

public sealed class BgAThousandCuts() : BgSilentCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgAThousandCutsPower>(choiceContext, Me, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class BgAccuracy() : BgSilentCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgAccuracyPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgAcrobatics() : BgSilentCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        await DiscardFromHand(choiceContext, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgAdrenaline() : BgSilentCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

public sealed class BgAfterImage() : BgSilentCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgAfterImagePower>(choiceContext, Me, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgAllOutAttack() : BgSilentCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AttackAll(choiceContext, cardPlay);
        await DiscardFromHand(choiceContext, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgBackflip() : BgSilentCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Deals more damage if the enemy is at full HP.
public sealed class BgBackstab() : BgSilentCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 2m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var bonus = target.CurrentHp >= target.MaxHp ? DynamicVars["Magic"].BaseValue : 0m;
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + bonus);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

// Deals more damage if the target is Poisoned.
public sealed class BgBane() : BgSilentCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 2m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var bonus = cardPlay.Target!.HasPower<PoisonPower>() ? DynamicVars["Magic"].BaseValue : 0m;
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + bonus);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgBladeDance() : BgSilentCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Shivs", 2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Tokens.AddShivs(Owner, (int)DynamicVars["Shivs"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Shivs"].UpgradeValueBy(1m);
}

// More Block if you discarded a card this turn.
public sealed class BgBlur() : BgSilentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move), new DynamicVar("Magic", 1m)];
    protected override bool ShouldGlowGoldInternal => BgRules.For(Owner?.RunState)?.DiscardedThisTurn(Owner!) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        if (BgRules.For(Owner.RunState)?.DiscardedThisTurn(Owner) > 0)
        {
            await GainBlock(cardPlay, DynamicVars["Magic"].BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Apply 1 Poison several times; each may go to a different enemy.
public sealed class BgBouncingFlask() : BgSilentCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < (int)DynamicVars["Magic"].BaseValue; i++)
        {
            var target = await Rows.ChooseEnemy(choiceContext, Owner, $"Bouncing Flask: apply 1 Poison ({i + 1}/{DynamicVars["Magic"].BaseValue})");
            if (target == null)
            {
                return;
            }
            await Apply<PoisonPower>(choiceContext, target, 1m);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Cards in your hand cost 0 this turn. You can't draw more cards this turn.
public sealed class BgBulletTime() : BgSilentCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Apply<BgNoDrawPower>(choiceContext, Me, 1m);
        await Apply<BgBulletTimePower>(choiceContext, Me, 1m);
        Me.GetPower<BgBulletTimePower>()?.Cover(Hand.ToList());
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgBurst() : BgSilentCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool IsPlayable => !Me.HasPower<BurstPower>();
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BurstPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Discard your hand, then draw that many cards.
public sealed class BgCalculatedGamble() : BgSilentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cards = Hand.Where(card => card != this).ToList();
        await CardCmd.DiscardAndDraw(choiceContext, cards, cards.Count);
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

// Double (triple upgraded) the enemy's Poison.
public sealed class BgCatalyst() : BgSilentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var poison = cardPlay.Target!.GetPowerAmount<PoisonPower>();
        if (poison > 0)
        {
            await Apply<PoisonPower>(choiceContext, cardPlay.Target!, poison * (DynamicVars["Magic"].BaseValue - 1m));
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Deals 1 more damage for each Weak and Poison on the target.
public sealed class BgChoke() : BgSilentCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var debuffs = target.GetPowerAmount<WeakPower>() + target.GetPowerAmount<PoisonPower>();
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + DynamicVars["Magic"].BaseValue * debuffs);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgCloakAndDagger() : BgSilentCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new DynamicVar("Shivs", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Tokens.AddShivs(Owner, (int)DynamicVars["Shivs"].BaseValue);
        await GainBlock(cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["Shivs"].UpgradeValueBy(1m);
}

// Discard any number of cards. Gain 1 Energy for each (+1 upgraded).
public sealed class BgConcentrate() : BgSilentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = await DiscardAnyNumber(choiceContext);
        var energy = count + DynamicVars["Magic"].BaseValue;
        if (energy > 0)
        {
            await PlayerCmd.GainEnergy(energy, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);

    private async Task<int> DiscardAnyNumber(PlayerChoiceContext choiceContext)
    {
        var cards = (await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, 99), null, this)).ToList();
        if (cards.Count > 0)
        {
            await CardCmd.Discard(choiceContext, cards);
        }
        return cards.Count;
    }
}

// Apply Poison. When the enemy dies, deal damage to its row.
public sealed class BgCorpseExplosion() : BgSilentCard(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(2m), new DamageVar(6m, ValueProp.Unpowered)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Apply<PoisonPower>(choiceContext, cardPlay.Target!, DynamicVars.Poison.BaseValue);
        if (!LivingEnemies.Any(enemy => enemy.HasPower<BgCorpseExplosionPower>()))
        {
            await Apply<BgCorpseExplosionPower>(choiceContext, cardPlay.Target!, DynamicVars.Damage.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Poison.UpgradeValueBy(1m);
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

public sealed class BgCripplingCloud() : BgSilentCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(1m), new PowerVar<WeakPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ApplyToAllEnemies<PoisonPower>(choiceContext, DynamicVars.Poison.BaseValue);
        await ApplyToAllEnemies<WeakPower>(choiceContext, DynamicVars.Weak.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Poison.UpgradeValueBy(1m);
}

// Each repetition is a separate attack on every enemy.
public sealed class BgDaggerSpray() : BgSilentCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < (int)DynamicVars["Magic"].BaseValue; i++)
        {
            await AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_dagger_spray");
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgDaggerThrow() : BgSilentCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_dagger_throw");
        await Draw(choiceContext, 1m);
        await DiscardFromHand(choiceContext, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgDash() : BgSilentCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new BlockVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        await CreatureCmd.GainBlock(Me, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}

public sealed class BgDeadlyPoison() : BgSilentCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<PoisonPower>(choiceContext, cardPlay.Target!, DynamicVars.Poison.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// More Block if you have a Shiv.
public sealed class BgDeflect() : BgSilentCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new DynamicVar("Magic", 1m)];
    protected override bool ShouldGlowGoldInternal => IsMutable && Tokens.Count<BgShiv>(Owner) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        if (Tokens.Count<BgShiv>(Owner) > 0)
        {
            await GainBlock(cardPlay, DynamicVars["Magic"].BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgDieDieDie() : BgSilentCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_dagger_spray");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgDistraction() : BgSilentCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgDistractionPower>(choiceContext, Me, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Gain Block several times; each can go to any player.
public sealed class BgDodgeAndRoll() : BgSilentCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new DynamicVar("Magic", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var players = CombatState!.Players.Where(p => p.Creature.IsAlive).ToList();
        for (var i = 0; i < (int)DynamicVars["Magic"].BaseValue; i++)
        {
            var target = await Targeting.ChoosePlayer(Owner, $"Dodge and Roll: give {DynamicVars.Block.BaseValue} Block to", players) ?? Owner;
            await CreatureCmd.GainBlock(target.Creature, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, cardPlay);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Play a copy of the last Attack or Skill any player played this turn, paying its cost with X.
public sealed class BgDoppelganger() : BgSilentCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool IsPlayable => BgRules.For(Owner?.RunState)?.LastAttackOrSkillThisTurn is { } card
        && Owner?.PlayerCombatState?.Energy >= Math.Max(0, card.EnergyCost.GetResolved());

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        if (BgRules.For(Owner.RunState)?.LastAttackOrSkillThisTurn is not { } last || x < last.EnergyCost.GetResolved())
        {
            return;
        }
        var copy = CombatState!.CreateCard(last, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
        var target = copy.TargetType == TargetType.AnyEnemy ? await Rows.ChooseEnemy(choiceContext, Owner, $"Choose a target for {copy.Title}") : null;
        await CardCmd.AutoPlay(choiceContext, copy, target);
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

public sealed class BgEnvenom() : BgSilentCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgEnvenomPower>(choiceContext, Me, DynamicVars.Poison.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Draw 1 card; if it's a Skill, gain Block. Upgraded: gain Block, then draw 1 card.
public sealed class BgEscapePlan() : BgSilentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            await GainBlock(cardPlay);
            await Draw(choiceContext, 1m);
            return;
        }
        var drawn = await CardPileCmd.Draw(choiceContext, Owner);
        if (drawn?.Type == CardType.Skill)
        {
            await GainBlock(cardPlay);
        }
    }
}

// Draw cards until you have this many in hand.
public sealed class BgExpertise() : BgSilentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(6)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = DynamicVars.Cards.BaseValue - Hand.Count(card => card != this);
        if (count > 0)
        {
            await Draw(choiceContext, count);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// Deals damage once for each other Attack you played this turn (including Shivs).
public sealed class BgFinisher() : BgSilentCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // This card was counted when it was played, so the others are one fewer.
        var hits = Math.Max(0, (BgRules.For(Owner.RunState)?.AttacksPlayedThisTurn(Owner) ?? 0) - 1);
        if (hits > 0)
        {
            await Attack(choiceContext, cardPlay, hits);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Deals damage once for each Skill in your hand (+1 upgraded).
public sealed class BgFlechettes() : BgSilentCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hits = Hand.Count(card => card.Type == CardType.Skill) + (int)DynamicVars["Magic"].BaseValue;
        if (hits > 0)
        {
            await Attack(choiceContext, cardPlay, hits, "vfx/vfx_dagger_throw");
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Each Block on your cards gets +1.
public sealed class BgFootwork() : BgSilentCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DexterityPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<DexterityPower>(choiceContext, Me, DynamicVars.Dexterity.BaseValue);
    protected override void OnUpgrade() => CardCmd.ApplyKeyword(this, CardKeyword.Retain);
}

// Can only be played if your draw pile is empty.
public sealed class BgGrandFinale() : BgSilentCard(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];
    protected override bool IsPlayable => PileType.Draw.GetPile(Owner).Cards.Count == 0;
    protected override bool ShouldGlowGoldInternal => IsMutable && IsPlayable;
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_giant_horizontal_slash");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class BgInfiniteBlades() : BgSilentCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Shivs", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgInfiniteBladesPower>(choiceContext, Me, DynamicVars["Shivs"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Shivs"].UpgradeValueBy(1m);
}

public sealed class BgLegSweep() : BgSilentCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Move), new PowerVar<WeakPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Me, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, cardPlay);
        await Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Apply X Weak and X Poison (X+1 upgraded).
public sealed class BgMalaise() : BgSilentCard(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var times = ResolveEnergyXValue() + (int)DynamicVars["Magic"].BaseValue;
        if (times > 0)
        {
            await Apply<WeakPower>(choiceContext, cardPlay.Target!, times);
            await Apply<PoisonPower>(choiceContext, cardPlay.Target!, times);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Costs 2 (1 upgraded) if you have lost HP this combat.
public sealed class BgMasterfulStab() : BgSilentCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Magic"].UpgradeValueBy(-1m);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || BgRules.For(Owner.RunState)?.LostHpThisCombat(Owner) != true)
        {
            return false;
        }
        modifiedCost = Math.Max(originalCost, DynamicVars["Magic"].BaseValue);
        return true;
    }
}

// Start of turn: apply Poison to an enemy (every enemy upgraded).
public sealed class BgNoxiousFumes() : BgSilentCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => IsUpgraded
        ? Apply<BgNoxiousFumesAllPower>(choiceContext, Me, DynamicVars.Poison.BaseValue)
        : Apply<BgNoxiousFumesPower>(choiceContext, Me, DynamicVars.Poison.BaseValue);
}

// Retain. If you Retained this last turn, gain 2 Energy (3 upgraded).
public sealed class BgOutmaneuver() : BgSilentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    protected override bool ShouldGlowGoldInternal => WasRetainedLastTurn;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (WasRetainedLastTurn)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

public sealed class BgPiercingWail() : BgSilentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new PowerVar<WeakPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Me, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, cardPlay);
        await ApplyToAllEnemies<WeakPower>(choiceContext, DynamicVars.Weak.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class BgPoisonedStab() : BgSilentCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new PowerVar<PoisonPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        await Apply<PoisonPower>(choiceContext, cardPlay.Target!, DynamicVars.Poison.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Poison.UpgradeValueBy(1m);
}

// Deal damage. Any player draws cards.
public sealed class BgPredator() : BgSilentCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        var players = CombatState!.Players.Where(p => p.Creature.IsAlive).ToList();
        var drawer = await Targeting.ChoosePlayer(Owner, $"Predator: who draws {DynamicVars.Cards.BaseValue} cards?", players) ?? Owner;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, drawer);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgPrepared() : BgSilentCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        await DiscardFromHand(choiceContext, (int)DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// Unplayable. If a card's effect discards this, draw cards.
public sealed class BgReflex() : BgSilentCard(-1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card == this)
        {
            await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgRiddleWithHoles() : BgSilentCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Shivs", 4m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Tokens.AddShivs(Owner, (int)DynamicVars["Shivs"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Shivs"].UpgradeValueBy(1m);
}

// Give Energy to any player.
public sealed class BgSetup() : BgSilentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool TargetsAnyPlayer => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, cardPlay.Target?.Player ?? Owner);

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

// Deal damage X+1 times (upgraded: more damage, X times).
public sealed class BgSkewer() : BgSilentCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hits = ResolveEnergyXValue() + (int)DynamicVars["Magic"].BaseValue;
        if (hits > 0)
        {
            await Attack(choiceContext, cardPlay, hits);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Magic"].UpgradeValueBy(-1m);
    }
}

// More damage if you have a Shiv.
public sealed class BgSlice() : BgSilentCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var bonus = Tokens.Count<BgShiv>(Owner) > 0 ? DynamicVars["Magic"].BaseValue : 0m;
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + bonus);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// If you discarded a card this turn, gain 2 Energy.
public sealed class BgSneakyStrike() : BgSilentCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new EnergyVar(2)];
    protected override bool ShouldGlowGoldInternal => BgRules.For(Owner?.RunState)?.DiscardedThisTurn(Owner!) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        if (BgRules.For(Owner.RunState)?.DiscardedThisTurn(Owner) > 0)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Discard any number of cards. Gain a Shiv for each (+1 upgraded).
public sealed class BgStormOfSteel() : BgSilentCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cards = (await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, 99), null, this)).ToList();
        if (cards.Count > 0)
        {
            await CardCmd.Discard(choiceContext, cards);
        }
        await Tokens.AddShivs(Owner, cards.Count + (int)DynamicVars["Magic"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Unplayable. If a card's effect discards this, gain Energy and Exhaust it.
public sealed class BgTactician() : BgSilentCard(-1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card == this)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            await CardCmd.Exhaust(choiceContext, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

public sealed class BgTerror() : BgSilentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<VulnerablePower>(choiceContext, cardPlay.Target!, DynamicVars.Vulnerable.BaseValue);
    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

// Start of turn: draw 1 card, then discard 1 card.
public sealed class BgToolsOfTheTrade() : BgSilentCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<ToolsOfTheTradePower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Deal damage, then use all your Shivs on the target; each deals extra damage.
public sealed class BgUnload() : BgSilentCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        foreach (var shiv in Hand.OfType<BgShiv>().ToList())
        {
            if (cardPlay.Target is not { IsAlive: true } target)
            {
                target = await Rows.ChooseEnemy(choiceContext, Owner, "Choose a target for Shiv");
                if (target == null)
                {
                    return;
                }
            }
            shiv.BonusDamage = (int)DynamicVars["Magic"].BaseValue;
            await CardCmd.AutoPlay(choiceContext, shiv, target);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// End of turn: you may Retain cards.
public sealed class BgWellLaidPlans() : BgSilentCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgWellLaidPlansPower>(choiceContext, Me, DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// You can't lose more than 1 HP per round, for 2 rounds (3 upgraded).
public sealed class BgWraithForm() : BgSilentCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgWraithFormPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}
