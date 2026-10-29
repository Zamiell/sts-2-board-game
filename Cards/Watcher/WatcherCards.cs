using BoardGame.Core;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards.Watcher;

// Card values are the board game's, taken from LuaViper/StSBoardGameMod.

public abstract class BgWatcherCardBase(int cost, CardType type, CardRarity rarity, TargetType target) : BgWatcherCard(cost, type, rarity, target)
{
    protected bool InWrath => Stance.IsIn<BgWrathStance>(Owner);

    protected bool InCalm => Stance.IsIn<BgCalmStance>(Owner);

    protected int Miracles => Tokens.Count<BgMiracle>(Owner);
}

public sealed class BgBattleHymn() : BgWatcherCardBase(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgBattleHymnPower>(choiceContext, Me, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Your next Attack this turn is played 3 times. Exhaust your draw pile.
public sealed class BgBlasphemy() : BgWatcherCardBase(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var card in PileType.Draw.GetPile(Owner).Cards.ToList())
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        await Apply<BgTripleAttackPower>(choiceContext, Me, 1m);
    }

    protected override void OnUpgrade() => CardCmd.ApplyKeyword(this, CardKeyword.Retain);
}

// Deal damage once for each Miracle you have.
public sealed class BgBrilliance() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Miracles > 0)
        {
            await Attack(choiceContext, cardPlay, Miracles, "vfx/vfx_attack_blunt");
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Deal damage to one or two enemies.
public sealed class BgCarveReality() : BgWatcherCardBase(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        var others = LivingEnemies.Where(e => e != cardPlay.Target).ToList();
        if (others.Count == 0)
        {
            return;
        }
        var second = await Targeting.ChooseEnemy(Owner, others, "Carve Reality: choose a second enemy", optional: true);
        if (second != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(second)
                .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgCollect() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Tokens.AddMiracles(Owner, (int)DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Deal damage to ALL enemies several times. You can't play additional cards this turn.
public sealed class BgConclude() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < (int)DynamicVars["Magic"].BaseValue; i++)
        {
            await AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_giant_horizontal_slash");
        }
        await Apply<BgConclusionPower>(choiceContext, Me, 1m);
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Your starter Strikes deal X+1 more damage (X+2 upgraded).
public sealed class BgConjureBlade() : BgWatcherCardBase(0, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Apply<BgConjureBladePower>(choiceContext, Me, ResolveEnergyXValue() + DynamicVars["Magic"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgConsecrate() : BgWatcherCardBase(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_attack_fire");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgCrescendo() : BgWatcherCardBase(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Stance.Enter<BgWrathStance>(choiceContext, Owner);
        if (IsUpgraded)
        {
            await Draw(choiceContext, 1m);
        }
    }
}

// If you're in Wrath, apply Vulnerable.
public sealed class BgCrushJoints() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new PowerVar<VulnerablePower>(1m)];
    protected override bool ShouldGlowGoldInternal => IsMutable && InWrath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
        if (InWrath)
        {
            await Apply<VulnerablePower>(choiceContext, cardPlay.Target!, DynamicVars.Vulnerable.BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgCutThroughFate() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        await Scry.Run(choiceContext, Owner, (int)DynamicVars["Magic"].BaseValue);
        await Draw(choiceContext, 1m);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }
}

public sealed class BgDeusExMachina() : BgWatcherCardBase(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Tokens.AddMiracles(Owner, (int)DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgDevaForm() : BgWatcherCardBase(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgDevaFormPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgDevotion() : BgWatcherCardBase(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 3m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgDevotionPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgEmptyBody() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Stance.ExitToNeutral(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgEmptyFist() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
        await Stance.ExitToNeutral(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgEmptyMind() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        await Stance.ExitToNeutral(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgEstablishment() : BgWatcherCardBase(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgEstablishmentPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// If you're in Wrath, enter Calm.
public sealed class BgFearNoEvil() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        if (InWrath)
        {
            await Stance.Enter<BgCalmStance>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Deals extra hits if you switched Stances this turn.
public sealed class BgFlurryOfBlows() : BgWatcherCardBase(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
    protected override bool ShouldGlowGoldInternal => IsMutable && BgRules.For(Owner.RunState)?.StanceChangedThisTurn(Owner) == true;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var switched = BgRules.For(Owner.RunState)?.StanceChangedThisTurn(Owner) == true;
        return Attack(choiceContext, cardPlay, switched ? (IsUpgraded ? 3 : 2) : 1);
    }
}

public sealed class BgFlyingSleeves() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay, (int)DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Deal damage, or play a copy of the last Attack another player played this turn.
public sealed class BgForeignInfluence() : BgWatcherCardBase(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var other = BgRules.For(Owner.RunState)?.LastAttackOrSkillThisTurn is { Type: CardType.Attack } card && card.Owner != Owner ? card : null;
        if (other != null)
        {
            var choice = await Choice.Choose(choiceContext, Owner, "Foreign Influence",
                [new Choice.Option("Strike", $"Deal {DynamicVars.Damage.BaseValue} damage."), new Choice.Option(other.Title, "Play a copy of it.")]);
            if (choice == 1)
            {
                var copy = CombatState!.CreateCard(other, Owner);
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
                await CardCmd.AutoPlay(choiceContext, copy, cardPlay.Target);
                return;
            }
        }
        await Attack(choiceContext, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgForesight() : BgWatcherCardBase(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 3m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgForesightPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// More Block if you're in Wrath.
public sealed class BgHalt() : BgWatcherCardBase(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new DynamicVar("Magic", 1m)];
    protected override bool ShouldGlowGoldInternal => IsMutable && InWrath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        if (InWrath)
        {
            await GainBlock(cardPlay, DynamicVars["Magic"].BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// If you're in Wrath, apply Vulnerable (to every enemy upgraded). Otherwise, enter Wrath.
public sealed class BgIndignation() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1m)];

    public override TargetType TargetType => IsMutable && InWrath ? (IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy) : TargetType.Self;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!InWrath)
        {
            await Stance.Enter<BgWrathStance>(choiceContext, Owner);
        }
        else if (IsUpgraded)
        {
            await ApplyToAllEnemies<VulnerablePower>(choiceContext, DynamicVars.Vulnerable.BaseValue);
        }
        else if (cardPlay.Target is { } target)
        {
            await Apply<VulnerablePower>(choiceContext, target, DynamicVars.Vulnerable.BaseValue);
        }
    }
}

// If you're in Calm, draw cards. Otherwise, enter Calm.
public sealed class BgInnerPeace() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (InCalm)
        {
            await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        }
        else
        {
            await Stance.Enter<BgCalmStance>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// If the enemy has this much HP or less, set its HP to 0.
public sealed class BgJudgment() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 7m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is { } target && target.CurrentHp <= DynamicVars["Magic"].BaseValue)
        {
            await CreatureCmd.Kill(target);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Magic"].UpgradeValueBy(1m);
        CardCmd.RemoveKeyword(this, CardKeyword.Ethereal);
        CardCmd.ApplyKeyword(this, CardKeyword.Retain);
    }
}

// Die 1-3: Scry. Die 4-6: gain Block.
public sealed class BgJustLucky() : BgWatcherCardBase(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new BlockVar(1m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        if ((BgRules.For(Owner.RunState)?.Roll ?? 1) <= 3)
        {
            await Scry.Run(choiceContext, Owner, (int)DynamicVars["Magic"].BaseValue);
        }
        else
        {
            await CreatureCmd.GainBlock(Me, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }
}

public sealed class BgLikeWater() : BgWatcherCardBase(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgLikeWaterPower>(choiceContext, Me, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Put cards from your discard pile into your hand and Retain them. Enter Calm. You can't play more cards this turn.
public sealed class BgMeditate() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < (int)DynamicVars.Cards.BaseValue; i++)
        {
            var card = await ChooseFromPile(choiceContext, PileType.Discard, "Choose a card to put into your hand and Retain");
            if (card == null)
            {
                break;
            }
            await CardPileCmd.Add(card, PileType.Hand);
            CardCmd.ApplySingleTurnRetain(card);
        }
        await Stance.Enter<BgCalmStance>(choiceContext, Owner);
        await Apply<BgConclusionPower>(choiceContext, Me, 1m);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgMentalFortress() : BgWatcherCardBase(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgMentalFortressPower>(choiceContext, Me, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgNirvana() : BgWatcherCardBase(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgNirvanaPower>(choiceContext, Me, DynamicVars.Block.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgOmega() : BgWatcherCardBase(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgOmegaPower>(choiceContext, Me, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Choose an Attack or Skill in your draw pile. Play it twice for free and Exhaust it. Shuffle your draw pile.
public sealed class BgOmniscience() : BgWatcherCardBase(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = await ChooseFromPile(choiceContext, PileType.Draw, "Omniscience: choose a card to play twice",
            c => c.Type is CardType.Attack or CardType.Skill);
        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            for (var i = 0; i < 2; i++)
            {
                var target = card.TargetType == TargetType.AnyEnemy ? await Rows.ChooseEnemy(choiceContext, Owner, $"Choose a target for {card.Title}") : null;
                await CardCmd.AutoPlay(choiceContext, card, target);
            }
            if (card.Pile is { Type: not PileType.Exhaust })
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }
        await CardPileCmd.Shuffle(choiceContext, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// More Block if this was Retained last turn.
public sealed class BgPerseverance() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new DynamicVar("Magic", 2m)];
    protected override bool ShouldGlowGoldInternal => WasRetainedLastTurn;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var retained = WasRetainedLastTurn;
        await GainBlock(cardPlay);
        if (retained)
        {
            await GainBlock(cardPlay, DynamicVars["Magic"].BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Gain Miracles. Draw 2 cards. You can't draw additional cards this turn.
public sealed class BgPray() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Tokens.AddMiracles(Owner, (int)DynamicVars["Magic"].BaseValue);
        await Draw(choiceContext, 2m);
        await Apply<BgNoDrawPower>(choiceContext, Me, 1m);
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgProstrate() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Tokens.AddMiracles(Owner, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgProtect() : BgWatcherCardBase(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Several separate hits; each can target a different enemy.
public sealed class BgRagnarok() : BgWatcherCardBase(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
        for (var i = 1; i < (int)DynamicVars["Magic"].BaseValue; i++)
        {
            var target = await Rows.ChooseEnemy(choiceContext, Owner, $"Ragnarok: choose the next target ({i + 1}/{DynamicVars["Magic"].BaseValue})");
            if (target == null)
            {
                return;
            }
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(target)
                .WithHitFx("vfx/vfx_attack_lightning").Execute(choiceContext);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// More damage for each Miracle you have.
public sealed class BgReachHeaven() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + DynamicVars["Magic"].BaseValue * Miracles);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgRushdown() : BgWatcherCardBase(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgRushdownPower>(choiceContext, Me, DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// More damage for each other card with Retain in your hand.
public sealed class BgSandsOfTime() : BgWatcherCardBase(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new DynamicVar("Magic", 2m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var retain = Hand.Count(card => card != this && card.Keywords.Contains(CardKeyword.Retain));
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + DynamicVars["Magic"].BaseValue * retain);
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// If you're in Calm, apply Weak.
public sealed class BgSashWhip() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new PowerVar<WeakPower>(1m)];
    protected override bool ShouldGlowGoldInternal => IsMutable && InCalm;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        if (InCalm)
        {
            await Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);
}

public sealed class BgScrawl() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(5)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Draw(choiceContext, DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Can only be played if this is the only Attack in your hand.
public sealed class BgSignatureMove() : BgWatcherCardBase(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];
    protected override bool IsPlayable => !Hand.Any(card => card != this && card.Type == CardType.Attack);
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay, vfx: "vfx/vfx_heavy_blunt");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class BgSimmeringFury() : BgWatcherCardBase(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgSimmeringFuryPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Gain Block for each other card in your hand.
public sealed class BgSpiritShield() : BgWatcherCardBase(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = Hand.Count(card => card != this);
        for (var i = 0; i < count; i++)
        {
            await GainBlock(cardPlay);
        }
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

// Start of turn: if you're in Calm, draw 2 cards.
public sealed class BgStudy() : BgWatcherCardBase(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgStudyPower>(choiceContext, Me, 2m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Gain Block (to any player upgraded). The next Attack you play this turn costs 0.
public sealed class BgSwivel() : BgWatcherCardBase(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => IsUpgraded;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Apply<BgFreeAttackPower>(choiceContext, Me, 1m);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Deal damage. Gain Block for each Miracle you have.
public sealed class BgTalkToTheHand() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
        for (var i = 0; i < Miracles; i++)
        {
            await CreatureCmd.GainBlock(Me, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, cardPlay);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Deal damage. Enter Wrath. Put this card on top of your draw pile.
public sealed class BgTantrum() : BgWatcherCardBase(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, (int)DynamicVars["Magic"].BaseValue, "vfx/vfx_attack_blunt");
        await Stance.Enter<BgWrathStance>(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(-1m);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }

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

public sealed class BgThirdEye() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move), new DynamicVar("Magic", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Scry.Run(choiceContext, Owner, (int)DynamicVars["Magic"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars["Magic"].UpgradeValueBy(2m);
    }
}

public sealed class BgTranquility() : BgWatcherCardBase(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Stance.Enter<BgCalmStance>(choiceContext, Owner);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Discard all cards without Retain. Draw 5 cards. Gain 3 Energy.
public sealed class BgVault() : BgWatcherCardBase(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(5), new EnergyVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discard = Hand.Where(card => card != this && !card.Keywords.Contains(CardKeyword.Retain)).ToList();
        if (discard.Count > 0)
        {
            await CardCmd.Discard(choiceContext, discard);
        }
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Gain Block equal to the unblocked damage dealt.
public sealed class BgWallop() : BgWatcherCardBase(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_blunt").Execute(choiceContext);
        var unblocked = attack.Results.SelectMany(hit => hit).Sum(result => result.UnblockedDamage);
        if (unblocked > 0)
        {
            await CreatureCmd.GainBlock(Me, unblocked, ValueProp.Unpowered, cardPlay);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// If you would discard this while Scrying, play it instead with extra damage.
public sealed class BgWeave() : BgWatcherCardBase(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 5m)];

    private bool _fromScry;

    internal async Task PlayFromScry(PlayerChoiceContext choiceContext)
    {
        var target = await Rows.ChooseEnemy(choiceContext, Owner, "Weave: choose a target");
        _fromScry = true;
        await CardPileCmd.Add(this, PileType.Hand);
        await CardCmd.AutoPlay(choiceContext, this, target);
        _fromScry = false;
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + (_fromScry ? DynamicVars["Magic"].BaseValue : 0m));

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }
}

// More damage if this was Retained last turn.
public sealed class BgWindmillStrike() : BgWatcherCardBase(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 3m)];
    protected override bool ShouldGlowGoldInternal => WasRetainedLastTurn;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + (WasRetainedLastTurn ? DynamicVars["Magic"].BaseValue : 0m));

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(2m);
}

// Choose one: gain Strength, gain Block, or gain Miracles.
public sealed class BgWish() : BgWatcherCardBase(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(1m), new BlockVar(10m, ValueProp.Move), new DynamicVar("Magic", 4m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var choice = await Choice.Choose(choiceContext, Owner, "Wish",
        [
            new Choice.Option("Become Almighty", $"Gain {DynamicVars.Strength.BaseValue} Strength."),
            new Choice.Option("Live Forever", $"Gain {DynamicVars.Block.BaseValue} Block."),
            new Choice.Option("Fame and Fortune", $"Gain {DynamicVars["Magic"].BaseValue} Miracles."),
        ]);
        switch (choice)
        {
            case 0:
                await Apply<StrengthPower>(choiceContext, Me, DynamicVars.Strength.BaseValue);
                break;
            case 1:
                await GainBlock(cardPlay);
                break;
            default:
                await Tokens.AddMiracles(Owner, (int)DynamicVars["Magic"].BaseValue);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(5m);
        DynamicVars["Magic"].UpgradeValueBy(1m);
    }
}

// Gain X+1 Miracles.
public sealed class BgWorship() : BgWatcherCardBase(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Tokens.AddMiracles(Owner, ResolveEnergyXValue() + 1);
    protected override void OnUpgrade() => CardCmd.ApplyKeyword(this, CardKeyword.Retain);
}

// Gain X Strength. Lose it at the end of your turn.
public sealed class BgWreathOfFlame() : BgWatcherCardBase(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var gained = (int)Rules.ClampGain(ResolveEnergyXValue(), Me.GetPowerAmount<StrengthPower>(), Rules.MaxStrength);
        if (gained > 0)
        {
            await Apply<StrengthPower>(choiceContext, Me, gained);
            await Apply<BgFlexPower>(choiceContext, Me, gained);
        }
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}
