using BoardGame.Cards;
using BoardGame.Orbs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace BoardGame.Relics;

// Ironclad: at the end of combat, heal 1 HP.
public sealed class BgBurningBlood : BgRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(1m)];

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (!Owner.Creature.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        }
    }
}

// Silent: at the start of combat, draw 2 cards.
public sealed class BgRingOfTheSnake : BgRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner && player.PlayerCombatState?.TurnNumber == 1)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }
}

// Defect: at the start of combat, Channel 1 Lightning.
public sealed class BgCrackedCore : BgRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await BgOrbs.Channel<BgLightningOrb>(new ThrowingPlayerChoiceContext(), Owner);
    }
}

// Watcher: at the start of combat, gain 1 Miracle. Miracles give 1 Energy each and last until used.
public sealed class BgMiracles : BgRelic
{
    public override string BorrowedEntry => "DIVINE_RIGHT";

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner && player.PlayerCombatState?.TurnNumber == 1)
        {
            Flash();
            await Tokens.AddMiracles(Owner, 1);
        }
    }
}
