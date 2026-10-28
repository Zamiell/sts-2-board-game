using BoardGame.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Cards.Watcher;

// Scry X: look at the top X cards of your draw pile and discard any of them. You can't Scry more cards than your draw
// pile holds. STS2 has no Scry, so it's built from a card selection.
public static class Scry
{
    public static async Task Run(PlayerChoiceContext choiceContext, Player player, int count)
    {
        var top = PileType.Draw.GetPile(player).Cards.Take(count).ToList();
        if (top.Count > 0)
        {
            var prefs = new CardSelectorPrefs(Choice.PromptText($"Scry {top.Count}: choose cards to discard"), 0, top.Count);
            foreach (var card in (await CardSelectCmd.FromSimpleGrid(choiceContext, top, player, prefs)).ToList())
            {
                // Weave is played instead of discarded.
                if (card is BgWeave weave)
                {
                    await weave.PlayFromScry(choiceContext);
                }
                else
                {
                    await CardPileCmd.Add(card, PileType.Discard);
                }
            }
        }
        foreach (var listener in player.Creature.Powers.OfType<IScryListener>().ToList())
        {
            await listener.AfterScry(choiceContext);
        }
    }
}

public interface IScryListener
{
    Task AfterScry(PlayerChoiceContext choiceContext);
}
