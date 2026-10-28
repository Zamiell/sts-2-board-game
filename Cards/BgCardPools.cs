using BoardGame.Core;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Cards;

// Each pool holds every card that derives from its card base class.
public abstract class BgCardPool<TCard> : CardPoolModel where TCard : BgCard
{
    protected override CardModel[] GenerateAllCards() => Content.Models<TCard, CardModel>();
}

public sealed class BgIroncladCardPool : BgCardPool<BgIroncladCard>
{
    public override string Title => "bg_ironclad";
    public override string EnergyColorName => "ironclad";
    public override string CardFrameMaterialPath => "card_frame_red";
    public override Color DeckEntryCardColor => new("D62000");
    public override Color EnergyOutlineColor => new("802020");
    public override bool IsColorless => false;
}

public sealed class BgSilentCardPool : BgCardPool<BgSilentCard>
{
    public override string Title => "bg_silent";
    public override string EnergyColorName => "silent";
    public override string CardFrameMaterialPath => "card_frame_green";
    public override Color DeckEntryCardColor => new("5EBD00");
    public override Color EnergyOutlineColor => new("2E5E00");
    public override bool IsColorless => false;
}

public sealed class BgDefectCardPool : BgCardPool<BgDefectCard>
{
    public override string Title => "bg_defect";
    public override string EnergyColorName => "defect";
    public override string CardFrameMaterialPath => "card_frame_blue";
    public override Color DeckEntryCardColor => new("3EB3ED");
    public override Color EnergyOutlineColor => new("1D5A78");
    public override bool IsColorless => false;
}

// STS2 has no Watcher, so her cards use the Necrobinder's purple-pink frame and energy.
public sealed class BgWatcherCardPool : BgCardPool<BgWatcherCard>
{
    public override string Title => "bg_watcher";
    public override string EnergyColorName => "necrobinder";
    public override string CardFrameMaterialPath => "card_frame_pink";
    public override Color DeckEntryCardColor => new("A35DD8");
    public override Color EnergyOutlineColor => new("4E2A6B");
    public override bool IsColorless => false;
}

public sealed class BgColorlessCardPool : BgCardPool<BgColorlessCard>
{
    public override string Title => "bg_colorless";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => new("A3A3A3FF");
    public override bool IsColorless => true;
}

public sealed class BgCurseCardPool : BgCardPool<BgCurseCard>
{
    public override string Title => "bg_curse";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_curse";
    public override Color DeckEntryCardColor => new("585B61FF");
    public override bool IsColorless => true;
}

public sealed class BgStatusCardPool : BgCardPool<BgStatusCard>
{
    public override string Title => "bg_status";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => Colors.White;
    public override bool IsColorless => true;
}

public sealed class BgTokenCardPool : BgCardPool<BgTokenCard>
{
    public override string Title => "bg_token";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => Colors.White;
    public override bool IsColorless => true;
}
