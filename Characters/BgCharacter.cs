using BoardGame.Cards;
using BoardGame.Core;
using BoardGame.Potions;
using BoardGame.Relics;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace BoardGame.Characters;

// A board game character looks, sounds, and animates like an STS2 character, but plays with board game numbers:
// about a tenth of the HP, a handful of gold, and the board game's cards and relics.
public abstract class BgCharacter : CharacterModel, IBorrowsAssets
{
    protected abstract CharacterModel Borrowed { get; }

    public string BorrowedEntry => Borrowed.Id.Entry;

    public override int StartingGold => 5;

    protected override CharacterModel? UnlocksAfterRunAs => null;

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<BgRelicPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<BgPotionPool>();

    public override Color NameColor => Borrowed.NameColor;
    public override CharacterGender Gender => Borrowed.Gender;
    public override float AttackAnimDelay => Borrowed.AttackAnimDelay;
    public override float CastAnimDelay => Borrowed.CastAnimDelay;
    public override Color EnergyLabelOutlineColor => Borrowed.EnergyLabelOutlineColor;
    public override Color DialogueColor => Borrowed.DialogueColor;
    public override VfxColor SpeechBubbleColor => Borrowed.SpeechBubbleColor;
    public override Color MapDrawingColor => Borrowed.MapDrawingColor;
    public override Color RemoteTargetingLineColor => Borrowed.RemoteTargetingLineColor;
    public override Color RemoteTargetingLineOutline => Borrowed.RemoteTargetingLineOutline;
    public override string CharacterTransitionSfx => Borrowed.CharacterTransitionSfx;
    public override string CharacterSelectSfx => Borrowed.CharacterSelectSfx;

    public override List<string> GetArchitectAttackVfx() => Borrowed.GetArchitectAttackVfx();

    public override CreatureAnimator GenerateAnimator(MegaSprite controller, Creature creature) =>
        Borrowed.GenerateAnimator(controller, creature);

    protected static IEnumerable<CardModel> Deck(params (CardModel Card, int Count)[] cards) =>
        cards.SelectMany(entry => Enumerable.Repeat(entry.Card, entry.Count)).ToArray();
}

public sealed class BgIronclad : BgCharacter
{
    protected override CharacterModel Borrowed => ModelDb.Character<Ironclad>();
    public override int StartingHp => 10;
    public override CardPoolModel CardPool => ModelDb.CardPool<BgIroncladCardPool>();

    public override IEnumerable<CardModel> StartingDeck => Deck(
        (ModelDb.Card<Cards.Ironclad.BgStrikeIronclad>(), 5),
        (ModelDb.Card<Cards.Ironclad.BgDefendIronclad>(), 4),
        (ModelDb.Card<Cards.Ironclad.BgBash>(), 1));

    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<BgTheDie>(), ModelDb.Relic<BgBurningBlood>()];
}

public sealed class BgSilent : BgCharacter
{
    protected override CharacterModel Borrowed => ModelDb.Character<Silent>();
    public override int StartingHp => 9;
    public override CardPoolModel CardPool => ModelDb.CardPool<BgSilentCardPool>();

    public override IEnumerable<CardModel> StartingDeck => Deck(
        (ModelDb.Card<Cards.Silent.BgStrikeSilent>(), 5),
        (ModelDb.Card<Cards.Silent.BgDefendSilent>(), 5),
        (ModelDb.Card<Cards.Silent.BgNeutralize>(), 1),
        (ModelDb.Card<Cards.Silent.BgSurvivor>(), 1));

    public override IReadOnlyList<RelicModel> StartingRelics =>
        [ModelDb.Relic<BgTheDie>(), ModelDb.Relic<BgRingOfTheSnake>()];
}

public sealed class BgDefect : BgCharacter
{
    protected override CharacterModel Borrowed => ModelDb.Character<Defect>();
    public override int StartingHp => 9;
    public override int BaseOrbSlotCount => 3;
    public override CardPoolModel CardPool => ModelDb.CardPool<BgDefectCardPool>();

    public override IEnumerable<CardModel> StartingDeck => Deck(
        (ModelDb.Card<Cards.Defect.BgStrikeDefect>(), 4),
        (ModelDb.Card<Cards.Defect.BgDefendDefect>(), 4),
        (ModelDb.Card<Cards.Defect.BgZap>(), 1),
        (ModelDb.Card<Cards.Defect.BgDualcast>(), 1));

    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<BgTheDie>(), ModelDb.Relic<BgCrackedCore>()];
}

// STS2 has no Watcher, so she borrows the Necrobinder's body and voice.
public sealed class BgWatcher : BgCharacter
{
    protected override CharacterModel Borrowed => ModelDb.Character<Necrobinder>();
    public override int StartingHp => 9;
    public override CardPoolModel CardPool => ModelDb.CardPool<BgWatcherCardPool>();

    public override IEnumerable<CardModel> StartingDeck => Deck(
        (ModelDb.Card<Cards.Watcher.BgStrikeWatcher>(), 4),
        (ModelDb.Card<Cards.Watcher.BgDefendWatcher>(), 4),
        (ModelDb.Card<Cards.Watcher.BgEruption>(), 1),
        (ModelDb.Card<Cards.Watcher.BgVigilance>(), 1));

    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<BgTheDie>(), ModelDb.Relic<BgMiracles>()];
}
