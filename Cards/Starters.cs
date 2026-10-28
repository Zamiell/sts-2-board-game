using BoardGame.Orbs;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards.Ironclad
{
    public sealed class BgStrikeIronclad() : BgIroncladCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
    }

    public sealed class BgDefendIronclad() : BgIroncladCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        public override bool GainsBlock => true;
        protected override bool TargetsAnyPlayer => IsUpgraded;
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
    }

    public sealed class BgBash() : BgIroncladCard(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new PowerVar<VulnerablePower>(1m)];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
            await Apply<VulnerablePower>(choiceContext, cardPlay.Target!, DynamicVars.Vulnerable.BaseValue);
        }

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

namespace BoardGame.Cards.Silent
{
    public sealed class BgStrikeSilent() : BgSilentCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
    }

    public sealed class BgDefendSilent() : BgSilentCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        public override bool GainsBlock => true;
        protected override bool TargetsAnyPlayer => IsUpgraded;
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
    }

    public sealed class BgNeutralize() : BgSilentCard(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move), new PowerVar<WeakPower>(1m)];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
            await Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
        }

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
    }

    public sealed class BgSurvivor() : BgSilentCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        public override bool GainsBlock => true;
        protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await GainBlock(cardPlay);
            await DiscardFromHand(choiceContext, 1);
        }

        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
    }
}

namespace BoardGame.Cards.Defect
{
    public sealed class BgStrikeDefect() : BgDefectCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class BgDefendDefect() : BgDefectCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        public override bool GainsBlock => true;
        protected override bool TargetsAnyPlayer => IsUpgraded;
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
    }

    public sealed class BgZap() : BgDefectCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
            BgOrbs.Channel<BgLightningOrb>(choiceContext, Owner);

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class BgDualcast() : BgDefectCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
            BgOrbs.EvokeChosen(choiceContext, Owner, times: 2);

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }
}

namespace BoardGame.Cards.Watcher
{
    public sealed class BgStrikeWatcher() : BgWatcherCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        public override string PortraitPath => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeNecrobinder>().PortraitPath;
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
    }

    public sealed class BgDefendWatcher() : BgWatcherCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        public override string PortraitPath => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendNecrobinder>().PortraitPath;
        public override bool GainsBlock => true;
        protected override bool TargetsAnyPlayer => IsUpgraded;
        protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
        protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];
        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
    }

    public sealed class BgEruption() : BgWatcherCard(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await Attack(choiceContext, cardPlay, vfx: "vfx/vfx_attack_blunt");
            await Stance.Enter<BgWrathStance>(choiceContext, Owner);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class BgVigilance() : BgWatcherCard(2, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        public override bool GainsBlock => true;
        protected override bool TargetsAnyPlayer => true;
        protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await GainBlock(cardPlay);
            await Stance.Enter<BgCalmStance>(choiceContext, Owner);
        }

        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
    }
}
