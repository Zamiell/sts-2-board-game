using BoardGame.Core;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Relics;

// Every board game relic: the relic deck, boss relics, keys, and each character's starting relics. The game looks up
// a relic's pool to show its description, so every relic must be in one. Relic decks and rewards pick by rarity.
public sealed class BgRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics() => Content.Models<BgRelic, RelicModel>();
}
