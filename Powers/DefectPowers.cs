using BoardGame.Core;
using BoardGame.Orbs;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Powers;

// Echo Form: the first Attack or Skill you play each turn is played twice.
public sealed class BgEchoFormPower : BgPower
{
    public override string BorrowedEntry => "ECHO_FORM_POWER";

    private int _usedThisTurn;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            _usedThisTurn = 0;
        }
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card.Owner?.Creature == Owner && card.Type is CardType.Attack or CardType.Skill && card is not Cards.BgShiv && _usedThisTurn < Amount
            ? playCount + 1
            : playCount;

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        _usedThisTurn++;
        return Task.CompletedTask;
    }
}

// Fusion: at the start of your turn, gain Energy.
public sealed class BgEnergyUpPower : BgPower
{
    public override string BorrowedEntry => "ENERGY_NEXT_TURN_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            Flash();
            await PlayerCmd.GainEnergy(Amount, player);
        }
    }
}

// Heatsinks: whenever you play a Power, draw cards.
public sealed class BgHeatsinksPower : BgPower
{
    public override string BorrowedEntry => "COOLANT_POWER";

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == Owner && cardPlay.Card.Type == CardType.Power && Owner.Player is { } player)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, player);
        }
    }
}

// Loop: at the end of your turn, trigger one orb's end-of-turn ability this many times.
public sealed class BgLoopPower : BgPower
{
    public override string BorrowedEntry => "LOOP_POWER";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || Owner.Player is not { } player)
        {
            return;
        }
        var orbs = BgOrbs.Orbs(player).Where(o => o.PassiveVal > 0).ToList();
        var orb = await Choice.Choose(choiceContext, player, "Loop: trigger an orb's end-of-turn ability", orbs,
            o => new Choice.Option(o is BgOrb bg ? bg.Name : o.Title.GetFormattedText(), $"Passive: {o.PassiveVal}"));
        if (orb == null)
        {
            return;
        }
        Flash();
        for (var i = 0; i < Amount; i++)
        {
            await OrbCmd.Passive(choiceContext, orb, null);
        }
    }
}

// Machine Learning: at the start of your turn, draw additional cards.
public sealed class BgMachineLearningPower : BgPower
{
    public override string BorrowedEntry => "MACHINE_LEARNING_POWER";

    public override decimal ModifyHandDraw(Player player, decimal count) => player.Creature == Owner ? count + Amount : count;
}

// Storm: at the start of your turn, Channel Lightning.
public sealed class BgStormPower : BgPower
{
    public override string BorrowedEntry => "STORM_POWER";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        for (var i = 0; i < Amount; i++)
        {
            await BgOrbs.Channel<BgLightningOrb>(choiceContext, player);
        }
    }
}

// Equilibrium: you may Retain cards this turn.
public sealed class BgTemporaryRetainPower : BgPower
{
    public override string BorrowedEntry => "RETAIN_HAND_POWER";

    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player.PlayerCombatState is not { } state)
        {
            return;
        }
        if (state.Hand.Cards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(Choice.PromptText($"Choose up to {Amount} {(Amount == 1 ? "card" : "cards")} to Retain"), 0, Amount);
            foreach (var card in await CardSelectCmd.FromHand(choiceContext, player, prefs, null, this))
            {
                CardCmd.ApplySingleTurnRetain(card);
            }
        }
        await PowerCmd.Remove(this);
    }
}
