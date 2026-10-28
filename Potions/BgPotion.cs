using BoardGame.Characters;
using BoardGame.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace BoardGame.Potions;

public abstract class BgPotion : PotionModel, IBorrowsAssets
{
    // The STS2 potion whose bottle this potion uses, by default the one with the same name.
    public virtual string BorrowedEntry => Borrow.EntryOrFallback<PotionModel>(this, "BLOCK_POTION");

    // The price printed on the potion card, paid at the Merchant and to The Courier.
    public virtual int Price => 3;

    // How many copies of this potion are in the board game's potion deck.
    public virtual int Copies => 1;

    // The potion deck has no rarities.
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;
}

// The board game's potion deck, shared by every character.
public sealed class BgPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<PotionModel> GenerateAllPotions() => Content.Models<BgPotion, PotionModel>();
}

// Board game characters only find board game potions.
[HarmonyPatch(typeof(PotionFactory), nameof(PotionFactory.GetPotionOptions))]
internal static class BgPotionOptionsPatch
{
    private static void Postfix(Player player, ref IEnumerable<PotionModel> __result)
    {
        if (player.Character is BgCharacter)
        {
            __result = player.Character.PotionPool.GetUnlockedPotions(player.UnlockState);
        }
    }
}

// Board game potions are drawn by their number of copies in the deck rather than by rarity.
[HarmonyPatch(typeof(PotionFactory), "CreateRandomPotions")]
internal static class BgPotionDrawPatch
{
    private static bool Prefix(IEnumerable<PotionModel> options, int count, Rng rng, ref IEnumerable<PotionModel> __result)
    {
        var list = options.ToList();
        if (list.Count == 0 || !list.All(p => p is BgPotion))
        {
            return true;
        }
        var drawn = new List<PotionModel>();
        for (var i = 0; i < count && list.Count > 0; i++)
        {
            var weighted = list.SelectMany(p => Enumerable.Repeat(p, ((BgPotion)p).Copies)).ToList();
            var potion = rng.NextItem(weighted)!;
            drawn.Add(potion);
            list.Remove(potion);
        }
        __result = drawn;
        return false;
    }
}
