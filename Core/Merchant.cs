using BoardGame.Cards;
using BoardGame.Potions;
using BoardGame.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BoardGame.Core;

// The board game's Merchant sells 3 cards from your reward deck, 3 colorless cards, 3 relics (the first one 1 gold
// cheaper), 3 potions, and one card removal. Nothing is restocked after it is bought.
[HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
internal static class BgMerchantInventoryPatch
{
    private static readonly AccessTools.FieldRef<MerchantCardEntry, CardCreationResult?> CreationResult =
        AccessTools.FieldRefAccess<MerchantCardEntry, CardCreationResult?>("<CreationResult>k__BackingField");

    private static readonly AccessTools.FieldRef<MerchantCardEntry, bool> IsOnSale =
        AccessTools.FieldRefAccess<MerchantCardEntry, bool>("<IsOnSale>k__BackingField");

    private static readonly AccessTools.FieldRef<MerchantInventory, List<MerchantCardEntry>> CharacterEntries =
        AccessTools.FieldRefAccess<MerchantInventory, List<MerchantCardEntry>>("_characterCardEntries");

    private static readonly AccessTools.FieldRef<MerchantInventory, List<MerchantCardEntry>> ColorlessEntries =
        AccessTools.FieldRefAccess<MerchantInventory, List<MerchantCardEntry>>("_colorlessCardEntries");

    public const int CardsPerRow = 3;

    // STS2's shop has 5 character card entries and 2 colorless ones; the board game's has 3 and 3. The screen is
    // rearranged to match (BgMerchantLayoutPatch).
    private static void Postfix(MerchantInventory __result, Player player)
    {
        if (!BoardGameRun.IsActive(player.RunState))
        {
            return;
        }
        var character = CharacterEntries(__result);
        var colorless = ColorlessEntries(__result);
        colorless.Add(character[CardsPerRow]);
        character.RemoveRange(CardsPerRow, character.Count - CardsPerRow);
        Fill(character, RewardDeck.Draw(player, CardsPerRow));
        Fill(colorless, RewardDeck.DrawColorless(player, CardsPerRow));
    }

    private static void Fill(List<MerchantCardEntry> entries, List<CardModel> cards)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            CreationResult(entries[i]) = i < cards.Count ? new CardCreationResult(cards[i]) : null;
            IsOnSale(entries[i]) = false;
            if (i < cards.Count)
            {
                entries[i].CalcCost();
            }
        }
    }

    public static decimal Price(MerchantEntry entry, Player player, decimal cost) => entry switch
    {
        MerchantCardEntry { CreationResult: { } result } => result.Card.Rarity switch
        {
            CardRarity.Rare => 6,
            CardRarity.Uncommon => 3,
            _ => 2,
        },
        MerchantRelicEntry { Model: BgRelic relic } relicEntry => relic.Price - (IsFirstRelic(relicEntry, player) ? 1 : 0),
        MerchantPotionEntry { Model: BgPotion potion } => potion.Price,
        MerchantCardRemovalEntry => player.RunState.AscensionLevel >= 8 ? 4 : 3,
        _ => cost,
    };

    private static bool IsFirstRelic(MerchantRelicEntry entry, Player player) =>
        player.RunState.CurrentRoom is MegaCrit.Sts2.Core.Rooms.MerchantRoom room
        && room.Inventories.FirstOrDefault(i => i.Player == player) is { RelicEntries.Count: > 0 } inventory
        && inventory.RelicEntries[0] == entry;
}

// The board game's shop: 3 character cards on the top row with the relics and potions beside them, and 3 colorless
// cards on the bottom row. STS2's screen has 5 and 2 card slots, so one slot moves down and one is removed.
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
internal static class BgMerchantLayoutPatch
{
    private static void Prefix(NMerchantInventory __instance, MerchantInventory inventory)
    {
        if (!BoardGameRun.IsActive(inventory.Player.RunState)
            || Field(__instance, "_characterCardContainer") is not { } character
            || Field(__instance, "_colorlessCardContainer") is not { } colorless
            || character.GetChildCount() <= BgMerchantInventoryPatch.CardsPerRow)
        {
            return;
        }
        var moved = character.GetChild<Godot.Control>(BgMerchantInventoryPatch.CardsPerRow);
        var spacing = colorless.GetChild<Godot.Control>(1).Position.X - colorless.GetChild<Godot.Control>(0).Position.X;
        var last = colorless.GetChild<Godot.Control>(colorless.GetChildCount() - 1).Position;
        character.RemoveChild(moved);
        colorless.AddChild(moved);
        moved.Position = last + new Godot.Vector2(spacing, 0);
        while (character.GetChildCount() > BgMerchantInventoryPatch.CardsPerRow)
        {
            var extra = character.GetChild(character.GetChildCount() - 1);
            character.RemoveChild(extra);
            extra.QueueFree();
        }
        // The relics and potions move up beside the 3 character cards, leaving the bottom row to the colorless cards.
        var thirdCard = character.GetChild<Godot.Control>(BgMerchantInventoryPatch.CardsPerRow - 1);
        var right = character.Position.X + thirdCard.Position.X + thirdCard.Size.X + 40f;
        if (Field(__instance, "_relicContainer") is { } relics && Field(__instance, "_potionContainer") is { } potions)
        {
            var gap = potions.Position.Y - relics.Position.Y;
            relics.Position = new Godot.Vector2(right, character.Position.Y + 60f);
            potions.Position = new Godot.Vector2(right, character.Position.Y + 60f + gap);
        }
    }

    private static Godot.Control? Field(NMerchantInventory inventory, string name) =>
        AccessTools.Field(typeof(NMerchantInventory), name).GetValue(inventory) as Godot.Control;
}
