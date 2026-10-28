using BoardGame.Core;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Relics;

// The board game has one relic deck shared by every character, plus each character's starting relics.
public sealed class BgRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
        Content.Models<BgRelic, RelicModel>().Where(relic => relic.Rarity != RelicRarity.Starter);
}
