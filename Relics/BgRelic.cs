using BoardGame.Core;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Relics;

public abstract class BgRelic : RelicModel, IBorrowsAssets
{
    // The STS2 relic whose icon this relic uses, by default the one with the same name.
    public virtual string BorrowedEntry => Borrow.EntryOrFallback<RelicModel>(this, "CIRCLET");

    protected override string IconBaseName => BorrowedEntry.ToLowerInvariant();

    // The price printed on the relic card, paid at the Merchant and to The Courier.
    public virtual int Price => 7;

    public override int MerchantCost => Rarity == RelicRarity.Common ? Price : base.MerchantCost;
}

// A relic from the board game's relic deck. The deck isn't split by rarity, so every relic in it is Common.
public abstract class BgDeckRelic : BgRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
}

// A boss relic. They are only offered after a boss, so they are Ancient (never in treasure, shops, or rewards).
public abstract class BgBossRelic : BgRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsAllowedInShops => false;
}

// A relic that triggers on the locked-in die roll, like the board game's die relics.
public interface IDieRelic
{
    Task AfterDieLockedIn(PlayerChoiceContext choiceContext, int roll);
}
