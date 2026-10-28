using BoardGame.Core;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards.Ironclad;

// Card values are the board game's, taken from LuaViper/StSBoardGameMod.

public sealed class BgAnger() : BgIroncladCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);

    // Goes on top of your draw pile instead of the discard pile.
    protected override CardLocation GetResultLocationForCardPlay()
    {
        var location = base.GetResultLocationForCardPlay();
        if (location.pileType == PileType.Discard)
        {
            location.pileType = PileType.Draw;
            location.position = CardPilePosition.Top;
        }
        return location;
    }
}

public sealed class BgBarricade() : BgIroncladCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Me.HasPower<BgBarricadePower>())
        {
            await Apply<BgBarricadePower>(choiceContext, Me, 1m);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgBattleTrance() : BgIroncladCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        await Apply<BgNoDrawPower>(choiceContext, Me, 1m);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgBerserk() : BgIroncladCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgBerserkPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Costs 1 (0 upgraded) if you have lost HP this combat.
public sealed class BgBloodForBlood() : BgIroncladCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay, vfx: "vfx/vfx_bloody_impact");
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(-1m);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || BgRules.For(Owner.RunState)?.LostHpThisCombat(Owner) != true)
        {
            return false;
        }
        modifiedCost = Math.Min(originalCost, DynamicVars["Magic"].BaseValue);
        return true;
    }
}

public sealed class BgBludgeon() : BgIroncladCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay, vfx: "vfx/vfx_heavy_blunt");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

// Deals damage equal to your Block.
public sealed class BgBodySlam() : BgIroncladCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Attack(choiceContext, cardPlay, Me.Block, vfx: "vfx/vfx_heavy_blunt");

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgBurningPact() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ExhaustFromHand(choiceContext, 1, 1);
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgCarnage() : BgIroncladCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay, vfx: "vfx/vfx_heavy_blunt");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

// Can only be played if every card in your hand is an Attack.
public sealed class BgClash() : BgIroncladCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];
    protected override bool IsPlayable => Hand.All(card => card.Type == CardType.Attack);
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgCleave() : BgIroncladCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_giant_horizontal_slash");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgClothesline() : BgIroncladCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new PowerVar<WeakPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
        await Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgCombust() : BgIroncladCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgCombustPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgCorruption() : BgIroncladCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Me.HasPower<BgCorruptionPower>())
        {
            await Apply<BgCorruptionPower>(choiceContext, Me, 1m);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgDarkEmbrace() : BgIroncladCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgDarkEmbracePower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgDemonForm() : BgIroncladCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgDemonFormPower>(choiceContext, Me, DynamicVars.Strength.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgDisarm() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);
}

public sealed class BgDoubleTap() : BgIroncladCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgDoubleAttackPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Double your Block (still at most 20).
public sealed class BgEntrench() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => CreatureCmd.GainBlock(Me, Me.Block, ValueProp.Unpowered, cardPlay);
    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

public sealed class BgEvolve() : BgIroncladCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgEvolvePower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Put a card from your exhaust pile into your hand.
public sealed class BgExhume() : BgIroncladCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = await ChooseFromPile(choiceContext, PileType.Exhaust, "Choose a card to return to your hand", c => c is not BgExhume);
        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// If this kills the enemy, gain Strength.
public sealed class BgFeed() : BgIroncladCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new PowerVar<StrengthPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_bite").Execute(choiceContext);
        if (attack.Results.SelectMany(hit => hit).Any(result => result.WasTargetKilled))
        {
            await Apply<StrengthPower>(choiceContext, Me, DynamicVars.Strength.BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Strength.UpgradeValueBy(1m);
}

public sealed class BgFeelNoPain() : BgIroncladCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgFeelNoPainPower>(choiceContext, Me, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Exhaust your hand. Deal damage once for each card Exhausted this way.
public sealed class BgFiendFire() : BgIroncladCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cards = Hand.Where(card => card != this).ToList();
        foreach (var card in cards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        if (cards.Count > 0)
        {
            await Attack(choiceContext, cardPlay, cards.Count, "vfx/vfx_attack_fire");
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgFireBreathing() : BgIroncladCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgFireBreathingPower>(choiceContext, Me, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Gain Block, then deal damage to every enemy that intends to attack you, once for each hit in its next action.
public sealed class BgFlameBarrier() : BgIroncladCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Move), new DamageVar(1m, ValueProp.Unpowered)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        foreach (var enemy in LivingEnemies.ToList())
        {
            if (enemy.Monster is not { } monster || !Rows.IsInRow(enemy, Owner))
            {
                continue;
            }
            var hits = monster.NextMove.Intents.OfType<AttackIntent>().Sum(intent => intent.Repeats);
            for (var i = 0; i < hits && enemy.IsAlive; i++)
            {
                await CreatureCmd.Damage(choiceContext, enemy, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Me, this, null);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Gain Strength that you lose at the end of the turn (only what fits under the Strength limit).
public sealed class BgFlex() : BgIroncladCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var gained = (int)Rules.ClampGain(DynamicVars.Strength.BaseValue, Me.GetPowerAmount<StrengthPower>(), Rules.MaxStrength);
        if (gained > 0)
        {
            await Apply<StrengthPower>(choiceContext, Me, gained);
            await Apply<BgFlexPower>(choiceContext, Me, gained);
        }
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

public sealed class BgGhostlyArmor() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Draw a card and play it for free. Exhaust it unless it's a Power.
public sealed class BgHavoc() : BgIroncladCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        CardPileCmd.AutoPlayFromDrawPile(choiceContext, Owner, 1, CardPilePosition.Top, forceExhaust: true);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Put a card from your discard pile on top of your draw pile.
public sealed class BgHeadbutt() : BgIroncladCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
        var card = await ChooseFromPile(choiceContext, PileType.Discard, "Choose a card to put on top of your draw pile");
        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Each Strength adds Magic damage to this card instead of 1.
public sealed class BgHeavyBlade() : BgIroncladCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new DynamicVar("Magic", 3m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var extra = (DynamicVars["Magic"].BaseValue - 1m) * Math.Max(0, Me.GetPowerAmount<StrengthPower>());
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + extra, vfx: "vfx/vfx_heavy_blunt");
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(2m);
}

public sealed class BgImmolate() : BgIroncladCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_attack_fire");
        await AddToDrawPile<BgDazed>(2);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class BgImpervious() : BgIroncladCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class BgInflame() : BgIroncladCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<StrengthPower>(choiceContext, Me, DynamicVars.Strength.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Upgraded: choose 1 damage and 2 Block, or 2 damage and 1 Block.
public sealed class BgIronWave() : BgIroncladCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var (damage, block) = (1m, 1m);
        if (IsUpgraded)
        {
            var choice = await Choice.Choose(choiceContext, Owner, "Iron Wave",
                [new Choice.Option("Shield", "Deal 1 damage. Gain 2 Block."), new Choice.Option("Spear", "Deal 2 damage. Gain 1 Block.")]);
            (damage, block) = choice == 1 ? (2m, 1m) : (1m, 2m);
        }
        await Attack(choiceContext, cardPlay, damage);
        await CreatureCmd.GainBlock(Me, block, ValueProp.Move, cardPlay);
    }
}

public sealed class BgJuggernaut() : BgIroncladCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgJuggernautPower>(choiceContext, Me, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Double your Strength (still at most 8).
public sealed class BgLimitBreak() : BgIroncladCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var strength = Me.GetPowerAmount<StrengthPower>();
        if (strength > 0)
        {
            await Apply<StrengthPower>(choiceContext, Me, strength);
        }
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

public sealed class BgMetallicize() : BgIroncladCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgMetallicizePower>(choiceContext, Me, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgOffering() : BgIroncladCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new EnergyVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Me, 1m, ValueProp.Unblockable | ValueProp.Unpowered, this, cardPlay);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}

// Deals 1 more damage for each other card in your hand containing "Strike".
public sealed class BgPerfectedStrike() : BgIroncladCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var strikes = Hand.Count(card => card != this && card.Tags.Contains(CardTag.Strike));
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + DynamicVars["Magic"].BaseValue * strikes);
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgPommelStrike() : BgIroncladCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgPowerThrough() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await AddToDrawPile<BgDazed>(1);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Gain Block for each Attack in your hand.
public sealed class BgRage() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        GainBlock(cardPlay, DynamicVars.Block.BaseValue * Hand.Count(card => card.Type == CardType.Attack));

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Deals damage equal to the number of cards in your exhaust pile. Upgraded: Exhaust a card first.
public sealed class BgRampage() : BgIroncladCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            await ExhaustFromHand(choiceContext, 1, 1);
        }
        await Attack(choiceContext, cardPlay, PileType.Exhaust.GetPile(Owner).Cards.Count, vfx: "vfx/vfx_heavy_blunt");
    }
}

// Lose 1 HP. Gain Strength.
public sealed class BgRupture() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Me, 1m, ValueProp.Unblockable | ValueProp.Unpowered, this, cardPlay);
        await Apply<StrengthPower>(choiceContext, Me, DynamicVars.Strength.BaseValue);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Exhaust all non-Attack cards in your hand. Gain Block for each.
public sealed class BgSecondWind() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cards = Hand.Where(card => card != this && card.Type != CardType.Attack).ToList();
        foreach (var card in cards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        await GainBlock(cardPlay, DynamicVars.Block.BaseValue * cards.Count);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgSeeingRed() : BgIroncladCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// If this card is Exhausted, gain 2 Energy (3 upgraded).
public sealed class BgSentinel() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move), new EnergyVar(2)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }
}

// Exhaust 1 card in your hand (1 or 2 upgraded).
public sealed class BgSeverSoul() : BgIroncladCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        await ExhaustFromHand(choiceContext, 1, IsUpgraded ? 2 : 1);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgShockwave() : BgIroncladCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m), new PowerVar<WeakPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ApplyToAllEnemies<VulnerablePower>(choiceContext, DynamicVars.Vulnerable.BaseValue);
        await ApplyToAllEnemies<WeakPower>(choiceContext, DynamicVars.Weak.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);
}

public sealed class BgShrugItOff() : BgIroncladCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Gain Strength (to any player) if the die shows 1-3 (1-4 upgraded).
public sealed class BgSpotWeakness() : BgIroncladCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool TargetsAnyPlayer => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m), new DynamicVar("Magic", 3m)];
    protected override bool ShouldGlowGoldInternal => (BgRules.For(Owner?.RunState)?.Roll ?? 6) <= DynamicVars["Magic"].BaseValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if ((BgRules.For(Owner.RunState)?.Roll ?? 6) <= DynamicVars["Magic"].BaseValue)
        {
            await Apply<StrengthPower>(choiceContext, cardPlay.Target is { IsPlayer: true } target ? target : Me, DynamicVars.Strength.BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Gain Block (to any player). Exhaust a card in your hand.
public sealed class BgTrueGrit() : BgIroncladCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await ExhaustFromHand(choiceContext, 1, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgTwinStrike() : BgIroncladCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay, hits: 2);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgUppercut() : BgIroncladCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3m, ValueProp.Move), new PowerVar<VulnerablePower>(1m), new PowerVar<WeakPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
        await Apply<VulnerablePower>(choiceContext, cardPlay.Target!, DynamicVars.Vulnerable.BaseValue);
        await Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Vulnerable.UpgradeValueBy(1m);
}

// Draw cards, then put a card from your hand on top of your draw pile.
public sealed class BgWarcry() : BgIroncladCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        var card = await ChooseFromPile(choiceContext, PileType.Hand, "Choose a card to put on top of your draw pile", c => c != this);
        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// Deal damage to ALL enemies X times (X+1 upgraded).
public sealed class BgWhirlwind() : BgIroncladCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var times = ResolveEnergyXValue() + (int)DynamicVars["Magic"].BaseValue;
        if (times > 0)
        {
            await AttackAll(choiceContext, cardPlay, times, "vfx/vfx_giant_horizontal_slash");
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgWildStrike() : BgIroncladCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        await AddToDrawPile<BgDazed>(1);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
