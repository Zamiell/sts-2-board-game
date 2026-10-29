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

namespace BoardGame.Cards.Colorless;

// Card values are the board game's, taken from LuaViper/StSBoardGameMod.

public sealed class BgApotheosis() : BgColorlessCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgApotheosisPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BgApparition() : BgColorlessCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgApparitionPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Ethereal);
}

public sealed class BgBlind() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<WeakPower>(choiceContext, cardPlay.Target!, DynamicVars.Weak.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);
}

// Gain Block for each enemy that intends to attack you.
public sealed class BgDarkShackles() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attackers = LivingEnemies.Count(e => Rows.IsInRow(e, Owner) && e.Monster?.NextMove.Intents.OfType<AttackIntent>().Any() == true);
        for (var i = 0; i < attackers; i++)
        {
            await GainBlock(cardPlay);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// Deal damage to ALL enemies; more on the first turn of combat.
public sealed class BgDramaticEntrance() : BgColorlessCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new DynamicVar("Magic", 1m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        AttackAll(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + (Owner.PlayerCombatState?.TurnNumber == 1 ? DynamicVars["Magic"].BaseValue : 0m));

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

public sealed class BgFinesse() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GainBlock(cardPlay);
        await Draw(choiceContext, 1m);
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

public sealed class BgFlashOfSteel() : BgColorlessCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Attack(choiceContext, cardPlay);
        await Draw(choiceContext, 1m);
    }

    protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
}

public sealed class BgGoodInstincts() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override bool TargetsAnyPlayer => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainBlock(cardPlay);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}

// More damage if you have 10 or more Gold.
public sealed class BgHandOfGreed() : BgColorlessCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new DynamicVar("Magic", 3m)];
    protected override bool ShouldGlowGoldInternal => IsMutable && Owner.Gold >= 10;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue + (Owner.Gold >= 10 ? DynamicVars["Magic"].BaseValue : 0m));

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(2m);
}

// Draw cards if you have no Attacks in your hand.
public sealed class BgImpatience() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override bool ShouldGlowGoldInternal => IsMutable && !Hand.Any(card => card.Type == CardType.Attack);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Hand.Any(card => card.Type == CardType.Attack))
        {
            await Draw(choiceContext, DynamicVars.Cards.BaseValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

// The next card you play this turn costs 0.
public sealed class BgMadness() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgFreeCardPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => CardCmd.ApplyKeyword(this, CardKeyword.Retain);
}

public sealed class BgMasterOfStrategy() : BgColorlessCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Draw(choiceContext, DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class BgMayhem() : BgColorlessCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgMayhemPower>(choiceContext, Me, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Deals damage equal to the number of other cards in your hand (+1 upgraded).
public sealed class BgMindBlast() : BgColorlessCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Magic", 0m)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Attack(choiceContext, cardPlay, Hand.Count(card => card != this) + DynamicVars["Magic"].BaseValue, vfx: "vfx/vfx_attack_lightning");

    protected override void OnUpgrade() => DynamicVars["Magic"].UpgradeValueBy(1m);
}

// Remove all Weak and Vulnerable from any player (every player upgraded).
public sealed class BgPanacea() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var players = CombatState!.Players.Where(p => p.Creature.IsAlive).ToList();
        var cleansed = IsUpgraded
            ? players
            : [await Targeting.ChoosePlayer(Owner, "Panacea: remove Weak and Vulnerable from", players) ?? Owner];
        foreach (var player in cleansed)
        {
            await PowerCmd.Remove<WeakPower>(player.Creature);
            await PowerCmd.Remove<VulnerablePower>(player.Creature);
        }
    }
}

public sealed class BgPanache() : BgColorlessCard(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgPanachePower>(choiceContext, Me, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

// Exhaust up to this many cards in your hand.
public sealed class BgPurity() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => ExhaustFromHand(choiceContext, 0, (int)DynamicVars.Cards.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}

public sealed class BgSadisticNature() : BgColorlessCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Unpowered)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<BgSadisticNaturePower>(choiceContext, Me, DynamicVars.Damage.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class BgSwiftStrike() : BgColorlessCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Attack(choiceContext, cardPlay);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

// At the end of 3 turns, deal damage to ALL enemies.
public sealed class BgTheBomb() : BgColorlessCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Unpowered)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var bomb = (BgTheBombPower)ModelDb.Power<BgTheBombPower>().ToMutable();
        bomb.Damage = (int)DynamicVars.Damage.BaseValue;
        return PowerCmd.Apply(choiceContext, bomb, Me, 3m, Me, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

// Draw cards, then put a card from your hand on top of your draw pile.
public sealed class BgThinkingAhead() : BgColorlessCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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

public sealed class BgTrip() : BgColorlessCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(2m)];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Apply<VulnerablePower>(choiceContext, cardPlay.Target!, DynamicVars.Vulnerable.BaseValue);
    protected override void OnUpgrade() => DynamicVars.Vulnerable.UpgradeValueBy(1m);
}
