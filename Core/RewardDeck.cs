using BoardGame.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// The board game's reward decks, shared by the party and saved with the run:
// - each color's deck: two copies of every Common, one of every Uncommon, and two Golden Tickets;
// - each color's Rare deck, which a Golden Ticket draws from;
// - the colorless deck and the curse deck.
// Each deck is shuffled once. A drawn card goes to the bottom of its deck, and a card that enters anyone's deck is
// taken out of the reward decks.
internal static class RewardDeck
{
    private const string GoldenTicket = "*";
    private const string Colorless = "COLORLESS";
    private const string Curses = "CURSES";
    private const string RareSuffix = "_RARE";

    private static readonly (Type Card, int Copies)[] CurseDeck =
    [
        (typeof(BgInjury), 2), (typeof(BgClumsy), 2), (typeof(BgParasite), 2), (typeof(BgRegret), 2),
        (typeof(BgWrithe), 1), (typeof(BgDoubt), 1), (typeof(BgPain), 1), (typeof(BgShame), 1), (typeof(BgDecay), 1),
    ];

    // Draws cards for a reward from a color's deck (the player's own by default). Rare rewards draw from the Rare deck.
    public static List<CardModel> Draw(Player player, int count, bool rare = false, bool upgraded = false, CardPoolModel? color = null)
    {
        var pool = color ?? player.Character.CardPool;
        var cards = new List<CardModel>();
        for (var i = 0; i < count; i++)
        {
            var entry = DrawTop(Deck(player, rare ? RareName(pool) : NormalName(pool)));
            if (entry == GoldenTicket)
            {
                entry = DrawTop(Deck(player, RareName(pool)));
            }
            if (entry != null && Find(entry) is { } card)
            {
                cards.Add(Create(player, card, upgraded));
            }
        }
        return cards;
    }

    public static List<CardModel> DrawColorless(Player player, int count) =>
        Enumerable.Range(0, count)
            .Select(_ => DrawTop(Deck(player, Colorless)) is { } entry ? Find(entry) : null)
            .OfType<CardModel>()
            .Select(card => Create(player, card, false))
            .ToList();

    // A canonical curse from the top of the curse deck.
    public static CardModel DrawCurse(Player player) =>
        (DrawTop(Deck(player, Curses)) is { } entry ? Find(entry) : null) ?? (CardModel)Content.Get(typeof(BgInjury));

    public static Task GainCurses(Player player, int count = 1) =>
        CardPileCmd.AddCursesToDeck(Enumerable.Range(0, count).Select(_ => DrawCurse(player)).ToList(), player);

    // A card reward of cards drawn from the reward deck.
    public static CardReward Reward(Player player, int count = 3, bool rare = false) =>
        new(Draw(player, count, rare), CardCreationSource.Encounter, player,
            CardCreationOptions.ForNonCombatWithDefaultOdds([player.Character.CardPool]));

    // A card reward from another character's reward deck (A Note for Yourself).
    public static CardReward Reward(Player player, CardPoolModel color) =>
        new(Draw(player, 3, color: color), CardCreationSource.Encounter, player,
            CardCreationOptions.ForNonCombatWithDefaultOdds([color]));

    public static CardReward ColorlessReward(Player player) =>
        new(DrawColorless(player, 3), CardCreationSource.Encounter, player,
            CardCreationOptions.ForNonCombatWithDefaultOdds([player.Character.CardPool]));

    // Transforming a card replaces it with the top card of the reward deck.
    public static async Task Transform(CardModel original, bool upgrade = false)
    {
        var player = original.Owner!;
        if (Draw(player, 1, upgraded: upgrade).FirstOrDefault() is { } replacement)
        {
            await CardCmd.Transform(original, replacement);
        }
    }

    // A card that enters a deck leaves the reward decks.
    public static void Obtained(CardModel card)
    {
        if (card.Owner is not { } player || BgRules.For(player.RunState) == null)
        {
            return;
        }
        string? deckName = card switch
        {
            BgCurseCard => Curses,
            BgColorlessCard => Colorless,
            _ when !IsColorPool(card.Pool) => null,
            _ when card.Rarity == CardRarity.Rare => RareName(card.Pool),
            _ when card.Rarity is CardRarity.Common or CardRarity.Uncommon => NormalName(card.Pool),
            _ => null,
        };
        if (deckName != null)
        {
            Deck(player, deckName).Remove(card.CanonicalInstance.Id.Entry);
        }
    }

    private static bool IsColorPool(CardPoolModel pool) =>
        pool is BgIroncladCardPool or BgSilentCardPool or BgDefectCardPool or BgWatcherCardPool;

    private static string NormalName(CardPoolModel pool) => pool.Id.Entry;

    private static string RareName(CardPoolModel pool) => pool.Id.Entry + RareSuffix;

    private static List<string> Deck(Player player, string name)
    {
        var decks = BgRules.For(player.RunState)!.RewardDeckState;
        if (!decks.TryGetValue(name, out var deck))
        {
            decks[name] = deck = Build(name).ToList().UnstableShuffle(player.RunState.Rng.Niche);
        }
        return deck;
    }

    private static IEnumerable<string> Build(string name)
    {
        if (name == Curses)
        {
            return CurseDeck.SelectMany(e => Enumerable.Repeat(Content.Get(e.Card).Id.Entry, e.Copies));
        }
        if (name == Colorless)
        {
            return Content.Models<BgColorlessCard, CardModel>()
                .Where(c => c.Rarity is not (CardRarity.Status or CardRarity.Token or CardRarity.Basic))
                .Select(c => c.Id.Entry);
        }
        var rare = name.EndsWith(RareSuffix);
        var poolName = rare ? name[..^RareSuffix.Length] : name;
        var pool = ModelDb.AllCardPools.First(p => p.Id.Entry == poolName);
        if (rare)
        {
            return pool.AllCards.Where(c => c.Rarity == CardRarity.Rare).Select(c => c.Id.Entry);
        }
        return pool.AllCards
            .Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon)
            .SelectMany(c => Enumerable.Repeat(c.Id.Entry, c.Rarity == CardRarity.Common ? 2 : 1))
            .Concat(Enumerable.Repeat(GoldenTicket, 2));
    }

    // Takes the top card and puts it on the bottom.
    private static string? DrawTop(List<string> deck)
    {
        if (deck.Count == 0)
        {
            return null;
        }
        var entry = deck[0];
        deck.RemoveAt(0);
        deck.Add(entry);
        return entry;
    }

    private static CardModel? Find(string entry) => ModelDb.AllCards.FirstOrDefault(c => c.Id.Entry == entry);

    private static CardModel Create(Player player, CardModel canonical, bool upgraded)
    {
        var card = ((RunState)player.RunState).CreateCard(canonical, player);
        if (upgraded && card.IsUpgradable)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
        return card;
    }
}
