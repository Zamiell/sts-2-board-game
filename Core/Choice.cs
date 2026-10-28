using BoardGame.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// The board game often lets a player choose an enemy, an orb, or a relic in the middle of an effect. STS2 only has
// card selection screens (which are synced in multiplayer), so each option is shown as a temporary card.
public static class Choice
{
    public sealed record Option(string Title, string Description);

    // Returns the index of the chosen option, or -1 if skipped. A single option is chosen without asking.
    public static async Task<int> Choose(PlayerChoiceContext choiceContext, Player player, string prompt,
        IReadOnlyList<Option> options, bool canSkip = false)
    {
        if (options.Count == 0)
        {
            return -1;
        }
        if (options.Count == 1 && !canSkip)
        {
            return 0;
        }
        var cards = options.Select(option => Create(player, option)).ToList();
        CardModel? chosen;
        if (cards.Count <= 3)
        {
            chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, player, canSkip);
        }
        else
        {
            var prefs = new CardSelectorPrefs(PromptText(prompt), canSkip ? 0 : 1, 1);
            chosen = (await CardSelectCmd.FromSimpleGrid(choiceContext, cards, player, prefs)).FirstOrDefault();
        }
        return chosen is BgOptionCard option ? cards.IndexOf(option) : -1;
    }

    public static async Task<T?> Choose<T>(PlayerChoiceContext choiceContext, Player player, string prompt,
        IReadOnlyList<T> items, Func<T, Option> describe, bool canSkip = false) where T : class
    {
        var index = await Choose(choiceContext, player, prompt, items.Select(describe).ToList(), canSkip);
        return index < 0 ? null : items[index];
    }

    public static LocString PromptText(string text)
    {
        var prompt = new LocString("cards", "BG_OPTION_CARD.prompt");
        prompt.Add("Text", text);
        return prompt;
    }

    private static BgOptionCard Create(Player player, Option option)
    {
        var card = (BgOptionCard)(CombatManager.Instance.IsInProgress && player.Creature.CombatState is { } combat
            ? combat.CreateCard(MegaCrit.Sts2.Core.Models.ModelDb.Card<BgOptionCard>(), player)
            : ((RunState)player.RunState).CreateCard(MegaCrit.Sts2.Core.Models.ModelDb.Card<BgOptionCard>(), player));
        card.Set(option);
        return card;
    }
}

// A card that stands for one option of a board game choice. Its text is set when it is created.
public sealed class BgOptionCard() : BgTokenCard(-1, CardType.Skill, TargetType.None)
{
    private Choice.Option _option = new("", "");

    internal void Set(Choice.Option option) => _option = option;

    public override string Title => _option.Title;

    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Text", _option.Description);
}
