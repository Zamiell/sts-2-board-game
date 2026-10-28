using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Cards;

// The board game's Shiv and Miracle tokens are kept in hand as cards that stay until used. Each is limited to 5.
public static class Tokens
{
    public const int MaxShivs = 5;
    public const int MaxMiracles = 5;

    public static int Count<TToken>(Player player) where TToken : BgTokenCard =>
        player.PlayerCombatState?.Hand.Cards.Count(card => card is TToken) ?? 0;

    public static async Task Add<TToken>(Player player, int count, int max) where TToken : BgTokenCard
    {
        var combat = player.Creature.CombatState;
        if (combat == null)
        {
            return;
        }
        var room = Math.Min(count, max - Count<TToken>(player));
        for (var i = 0; i < room; i++)
        {
            await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<TToken>(player), PileType.Hand, player);
        }
    }

    public static Task AddShivs(Player player, int count) => Add<BgShiv>(player, count, MaxShivs);

    public static Task AddMiracles(Player player, int count) => Add<BgMiracle>(player, count, MaxMiracles);
}

// A Shiv is a separate 1-damage attack. Accuracy makes every Shiv deal more damage. Shivs don't count as cards played.
public sealed class BgShiv() : BgTokenCard(0, CardType.Attack, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1m, ValueProp.Move)];

    // Extra damage for the next use only (Unload).
    public int BonusDamage { get; set; }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = DynamicVars.Damage.BaseValue + Me.GetPowerAmount<Powers.BgAccuracyPower>() + BonusDamage;
        BonusDamage = 0;
        await Attack(choiceContext, cardPlay, damage);
    }
}

// A Miracle gives 1 Energy when used.
public sealed class BgMiracle() : BgTokenCard(0, CardType.Skill, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
}
