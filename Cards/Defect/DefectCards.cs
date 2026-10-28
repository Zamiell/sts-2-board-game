using BoardGame.Core;
using BoardGame.Orbs;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards.Defect;

// Card values are the board game's, taken from LuaViper/StSBoardGameMod.

// Cards that cost 1 less for each Power you have in play (Powers played this combat).
public abstract class BgPowerDiscountCard(int cost, CardType type, CardRarity rarity, TargetType target) : BgDefectCard(cost, type, rarity, target)
{
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        var powers = BgRules.For(Owner.RunState)?.PowersPlayed(Owner) ?? 0;
        if (card != this || powers == 0)
        {
            return false;
        }
        modifiedCost = Math.Max(0m, originalCost - powers);
        return true;
    }
}

internal static class DefectHelpers
{
    public static int OrbCount(MegaCrit.Sts2.Core.Entities.Players.Player player) => BgOrbs.Orbs(player).Count;

    public static int OrbCount<TOrb>(MegaCrit.Sts2.Core.Entities.Players.Player player) => BgOrbs.Orbs(player).Count(o => o is TOrb);

    public static bool TopDiscardCostsZero(MegaCrit.Sts2.Core.Entities.Players.Player player) =>
        PileType.Discard.GetPile(player).Cards.LastOrDefault() is { } card && card.EnergyCost.GetResolved() == 0 && !card.EnergyCost.CostsX;
}

// Put all 0-cost cards from your discard pile into your hand.
public sealed class BgAllForOne() : BgDefectCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_heavy_blunt");
        foreach (var card in PileType.Discard.GetPile(Owner).Cards.Where(c => c.EnergyCost.GetResolved() == 0 && !c.EnergyCost.CostsX).ToList())
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgAmplify() : BgDefectCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 3m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgAmplifyPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(2m);
}

public sealed class BgBallLightning() : BgDefectCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
        await BgOrbs.Channel<BgLightningOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Deal damage once for each orb you have (+1 upgraded).
public sealed class BgBarrage() : BgDefectCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hits = DefectHelpers.OrbCount(Owner) + (int)DynamicVars["Magic"].BaseValue;
        if (hits > 0)
        {
            await Attack(choiceContext, cardPlay, hits);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Deal damage; apply Vulnerable if the die shows 1-3 (always upgraded).
public sealed class BgBeamCell() : BgDefectCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new PowerVar<VulnerablePower>(1m)];
    protected override bool ShouldGlowGoldInternal => !IsUpgraded && BgRules.For(Owner?.RunState)?.Roll is >= 1 and <= 3;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
        if (IsUpgraded || BgRules.For(Owner.RunState)?.Roll is >= 1 and <= 3)
        {
            await Apply<VulnerablePower>(choiceContext, cardPlay.Target!, DynamicVars.Vulnerable.BaseValue);
        }
    }
}

// Deal damage to ALL enemies once for each Frost orb you have.
public sealed class BgBlizzard() : BgDefectCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hits = DefectHelpers.OrbCount<BgFrostOrb>(Owner);
        if (hits > 0)
        {
            await AttackAll(choiceContext, cardPlay, hits, "vfx/vfx_attack_blunt");
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// The next time you would lose HP, prevent it.
public sealed class BgBuffer() : BgDefectCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BufferPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgCapacitor() : BgDefectCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => OrbCmd.AddSlots(Owner, (int)DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Channel the orb the die is on: 1-2 Lightning, 3-4 Frost, 5-6 Dark.
public sealed class BgChaos() : BgDefectCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => (BgRules.For(Owner.RunState)?.Roll ?? 1) switch
    {
        <= 2 => BgOrbs.Channel<BgLightningOrb>(choiceContext, Owner),
        <= 4 => BgOrbs.Channel<BgFrostOrb>(choiceContext, Owner),
        _ => BgOrbs.Channel<BgDarkOrb>(choiceContext, Owner),
    };

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Gain Block. Gain 1 Energy if you have 3 or more orbs.
public sealed class BgChargeBattery() : BgDefectCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move), new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        if (DefectHelpers.OrbCount(Owner) >= 3)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// More damage if the top card of your discard pile costs 0.
public sealed class BgClaw() : BgDefectCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new DynamicVar("Magic", 1m)];
    protected override bool ShouldGlowGoldInternal => IsMutable && DefectHelpers.TopDiscardCostsZero(Owner);

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var bonus = DefectHelpers.TopDiscardCostsZero(Owner) ? DynamicVars["Magic"].BaseValue : 0m;
        return Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + bonus);
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(2m);
}

public sealed class BgColdSnap() : BgDefectCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        await BgOrbs.Channel<BgFrostOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Draw a card for each kind of orb you have.
public sealed class BgCompileDriver() : BgDefectCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        var kinds = BgOrbs.Orbs(Owner).Select(o => o.GetType()).Distinct().Count();
        if (kinds > 0)
        {
            await Draw(choiceContext, kinds);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Orb Evokes get +1.
public sealed class BgConsume() : BgDefectCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgOrbEvokePower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgCoolheaded() : BgDefectCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await BgOrbs.Channel<BgFrostOrb>(choiceContext, Owner);
        if (IsUpgraded)
        {
            await Draw(choiceContext, 1m);
        }
    }
}

// Retain. Remove all Weak and Vulnerable from any player (every player upgraded), then deal damage.
public sealed class BgCoreSurge() : BgDefectCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var players = CombatState!.Players.Where(p => p.Creature.IsAlive).ToList();
        var cleansed = IsUpgraded
            ? players
            : [await Choice.Choose(choiceContext, Owner, "Core Surge: remove Weak and Vulnerable from", players,
                p => new Choice.Option(p.Creature.Name, "")) ?? Owner];
        foreach (var player in cleansed)
        {
            await PowerCmd.Remove<WeakPower>(player.Creature);
            await PowerCmd.Remove<VulnerablePower>(player.Creature);
        }
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgDarkness() : BgDefectCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => BgOrbs.Channel<BgDarkOrb>(choiceContext, Owner);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Orb end-of-turn effects get +1.
public sealed class BgDefragment() : BgDefectCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgOrbEndOfTurnPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Ethereal);
}

public sealed class BgDoomAndGloom() : BgDefectCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_attack_fire");
        await BgOrbs.Channel<BgDarkOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Double your Energy (6 at most).
public sealed class BgDoubleEnergy() : BgDefectCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var energy = Owner.PlayerCombatState?.Energy ?? 0;
        if (energy > 0)
        {
            await PlayerCmd.GainEnergy(energy, Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Start of turn: the first Attack or Skill you play is played twice.
public sealed class BgEchoForm() : BgDefectCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgEchoFormPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Ethereal);
}

// Lightning hits every enemy. Channel Lightning.
public sealed class BgElectrodynamics() : BgDefectCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Me.HasPower<BgElectrodynamicsPower>())
        {
            await Apply<BgElectrodynamicsPower>(choiceContext, Me, 1m);
        }
        for (var i = 0; i < (int)DynamicVars["Magic"].BaseValue; i++)
        {
            await BgOrbs.Channel<BgLightningOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Gain Block. You may Retain cards this turn.
public sealed class BgEquilibrium() : BgDefectCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Apply<BgTemporaryRetainPower>(choiceContext, Me, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// If this is the first card you played this turn, draw 1 card.
public sealed class BgFtl() : BgDefectCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new CardsVar(1)];
    protected override bool ShouldGlowGoldInternal => IsMutable && BgRules.For(Owner.RunState)?.CardsPlayedThisTurn(Owner) == 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var first = BgRules.For(Owner.RunState)?.CardsPlayedThisTurn(Owner) <= 1;
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
        if (first)
        {
            await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Remove (upgraded: Evoke) all your orbs. For each, gain 1 Energy and draw 1 card.
public sealed class BgFission() : BgDefectCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var orbs = BgOrbs.Orbs(Owner).ToList();
        if (IsUpgraded)
        {
            foreach (var orb in orbs)
            {
                await BgOrbs.Evoke(choiceContext, Owner, orb);
            }
        }
        else
        {
            BgOrbs.RemoveAll(Owner);
        }
        if (orbs.Count > 0)
        {
            await PlayerCmd.GainEnergy(orbs.Count, Owner);
            await Draw(choiceContext, orbs.Count);
        }
    }
}

public sealed class BgForceField() : BgPowerDiscountCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Start of turn: gain 1 Energy.
public sealed class BgFusion() : BgDefectCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgEnergyUpPower>(choiceContext, Me, DynamicVars.Energy.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgGlacier() : BgDefectCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => IsUpgraded;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await BgOrbs.Channel<BgFrostOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Deal damage; apply Weak if the die shows 4-6 (always upgraded).
public sealed class BgGoForTheEyes() : BgDefectCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new PowerVar<WeakPower>(1m)];
    protected override bool ShouldGlowGoldInternal => !IsUpgraded && BgRules.For(Owner?.RunState)?.Roll is >= 4;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
        if (IsUpgraded || BgRules.For(Owner.RunState)?.Roll is >= 4)
        {
            await Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
        }
    }
}

public sealed class BgHeatsinks() : BgDefectCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgHeatsinksPower>(choiceContext, Me, DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// Gain Block. Put a card from your discard pile into your hand.
public sealed class BgHologram() : BgDefectCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        var card = await ChooseFromPile(choiceContext, PileType.Discard, "Choose a card to put into your hand");
        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

// Deal damage to ALL enemies. Remove all your orbs.
public sealed class BgHyperbeam() : BgDefectCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
        BgOrbs.RemoveAll(Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class BgLeap() : BgDefectCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgLoop() : BgDefectCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgLoopPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Start of turn: draw a card.
public sealed class BgMachineLearning() : BgDefectCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgMachineLearningPower>(choiceContext, Me, DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// First remove all Block from the target, then deal damage.
public sealed class BgMelter() : BgDefectCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.LoseBlock(choiceContext, cardPlay.Target!, cardPlay.Target!.Block, Me);
        await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_fire");
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgMeteorStrike() : BgPowerDiscountCard(5, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_fire");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}

// Evoke one orb X times (X+1 upgraded).
public sealed class BgMultiCast() : BgDefectCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var times = ResolveEnergyXValue() + (int)DynamicVars["Magic"].BaseValue;
        if (times > 0)
        {
            await BgOrbs.EvokeChosen(choiceContext, Owner, times);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Draw cards. Put a Dazed into your discard pile.
public sealed class BgOverclock() : BgDefectCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        await CardPileCmd.AddGeneratedCardToCombat(CombatState!.CreateCard<BgDazed>(Owner), PileType.Discard, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgRainbow() : BgDefectCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await BgOrbs.Channel<BgLightningOrb>(choiceContext, Owner);
        await BgOrbs.Channel<BgFrostOrb>(choiceContext, Owner);
        await BgOrbs.Channel<BgDarkOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

// Evoke an orb, then Channel another of the same kind.
public sealed class BgRecursion() : BgDefectCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var orb = await BgOrbs.ChooseOrb(choiceContext, Owner, "Recursion: choose an orb to Evoke and Channel again");
        if (orb == null)
        {
            return;
        }
        var type = orb.GetType();
        await BgOrbs.Evoke(choiceContext, Owner, orb);
        await BgOrbs.Channel(choiceContext, Owner, (BgOrb)((OrbModel)Content.Get(type)).ToMutable());
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Exhaust a card. Gain Energy equal to its cost; if it costs X, double your Energy instead.
public sealed class BgRecycle() : BgDefectCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = (await ExhaustFromHand(choiceContext, 1, 1)).FirstOrDefault();
        if (card == null)
        {
            return;
        }
        var energy = card.EnergyCost.CostsX ? Owner.PlayerCombatState?.Energy ?? 0 : Math.Max(0, card.EnergyCost.GetResolved());
        if (energy > 0)
        {
            await PlayerCmd.GainEnergy(energy, Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Gain X+1 Block (X can't be 0). Upgraded: gain X Block twice.
public sealed class BgReinforcedBody() : BgDefectCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool HasEnergyCostX => true;
    protected override bool IsPlayable => IsUpgraded || Owner?.PlayerCombatState?.Energy > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        if (IsUpgraded)
        {
            if (x > 0)
            {
                await CreatureCmd.GainBlock(Me, x, ValueProp.Move, cardPlay);
                await CreatureCmd.GainBlock(Me, x, ValueProp.Move, cardPlay);
            }
        }
        else if (x > 0)
        {
            await CreatureCmd.GainBlock(Me, x + 1, ValueProp.Move, cardPlay);
        }
    }
}

// Gain Strength. Remove all your orbs.
public sealed class BgReprogram() : BgDefectCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Apply<StrengthPower>(choiceContext, Me, DynamicVars.Strength.BaseValue);
        BgOrbs.RemoveAll(Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// If the top card of your discard pile costs 0, return it to your hand.
public sealed class BgScrape() : BgDefectCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];
    protected override bool ShouldGlowGoldInternal => IsMutable && DefectHelpers.TopDiscardCostsZero(Owner);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        if (DefectHelpers.TopDiscardCostsZero(Owner))
        {
            await CardPileCmd.Add(PileType.Discard.GetPile(Owner).Cards.Last(), PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Search your draw pile for cards, put them into your hand, then shuffle your draw pile.
public sealed class BgSeek() : BgDefectCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < (int)DynamicVars.Cards.BaseValue; i++)
        {
            var card = await ChooseFromPile(choiceContext, PileType.Draw, "Choose a card to put into your hand");
            if (card == null)
            {
                break;
            }
            await CardPileCmd.Add(card, PileType.Hand);
        }
        await CardPileCmd.Shuffle(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgSkim() : BgDefectCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Draw(choiceContext, DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// Gain Block (to any player) equal to the number of orbs you have (+1 upgraded).
public sealed class BgStack() : BgDefectCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 0m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        GainBlock(cardPlay, DefectHelpers.OrbCount(Owner) + DynamicVars["Magic"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgStaticDischarge() : BgDefectCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgStaticDischargePower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// More Block if the top card of your discard pile costs 0.
public sealed class BgSteamBarrier() : BgDefectCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new DynamicVar("Magic", 1m)];
    protected override bool ShouldGlowGoldInternal => IsMutable && DefectHelpers.TopDiscardCostsZero(Owner);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var bonus = DefectHelpers.TopDiscardCostsZero(Owner);
        await GainBlock(cardPlay);
        if (bonus)
        {
            await GainBlock(cardPlay, DynamicVars["Magic"].BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

public sealed class BgStorm() : BgDefectCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgStormPower>(choiceContext, Me, DynamicVars["Magic"].BaseValue);
    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgStreamline() : BgPowerDiscountCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// If this kills the enemy, gain 3 Energy.
public sealed class BgSunder() : BgDefectCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move), new EnergyVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_heavy_blunt").Execute(choiceContext);
        if (attack.Results.SelectMany(hit => hit).Any(result => result.WasTargetKilled))
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class BgSweepingBeam() : BgDefectCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AttackAll(choiceContext, cardPlay, vfx: "vfx/vfx_attack_lightning");
        await Draw(choiceContext, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// Channel X Lightning (X+1 upgraded).
public sealed class BgTempest() : BgDefectCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 0m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = ResolveEnergyXValue() + (int)DynamicVars["Magic"].BaseValue;
        for (var i = 0; i < count; i++)
        {
            await BgOrbs.Channel<BgLightningOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Deal damage to ALL enemies once for each Lightning orb you have.
public sealed class BgThunderStrike() : BgDefectCard(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hits = DefectHelpers.OrbCount<BgLightningOrb>(Owner);
        if (hits > 0)
        {
            await AttackAll(choiceContext, cardPlay, hits, "vfx/vfx_attack_lightning");
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

// Gain Energy. Put a Dazed into your discard pile.
public sealed class BgTurbo() : BgDefectCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(CombatState!.CreateCard<BgDazed>(Owner), PileType.Discard, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
