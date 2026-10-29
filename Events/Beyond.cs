using BoardGame.Acts;
using BoardGame.Core;
using BoardGame.Encounters;
using BoardGame.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Events;

public sealed class BgPurifier : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "WELLSPRING";

    private static bool Cleansable(CardModel card) => card.Type == CardType.Curse && card.IsRemovable && card is not Cards.BgAscendersBane;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("PRAY", HasRemovableCards, async () => { await Remove(); Finish("PRAY"); }),
        Option("CLEANSE", Player.Deck.Cards.Any(Cleansable), async () =>
        {
            await CardPileCmd.RemoveFromDeck(Player.Deck.Cards.Where(Cleansable).ToList());
            Finish("CLEANSE");
        }),
        Leave(),
    ];
}

public sealed class BgMindBloom : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "COLOSSAL_FLOWER";

    public override bool IsShared => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("WAR", () =>
        {
            var bosses = Content.Models<BgEncounter, EncounterModel>().Where(e => ((BgEncounter)e).Act == typeof(BgMindBloom));
            EnterCombatWithoutExitingEvent(Rng.NextItem(bosses)!, [new RelicReward(Player)], shouldResumeAfterCombat: false);
            return Task.CompletedTask;
        }),
        Option("RICH", async () => { await GainGold(5); Finish("RICH"); }),
        Option("HEALTHY", async () =>
        {
            await HealToFull();
            await GainCurse();
            Finish("HEALTHY");
        }),
        Option("AWAKE", async () =>
        {
            await LoseHp(3);
            if (!Player.Creature.IsDead)
            {
                await Upgrade(2);
            }
            Finish("AWAKE");
        }),
    ];
}

public sealed class BgTombOfLordRedMask : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "THE_LEGENDS_WERE_TRUE";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("DON_MASK", Player.Relics.OfType<BgRedMask>().Any(), async () => { await GainGold(6); Finish("DON_MASK"); }),
        Option("OFFER_GOLD", async () =>
        {
            await LoseGold(Player.Gold);
            await GainRelic();
            Finish("OFFER_GOLD");
        }),
        Leave(),
    ];
}

// Three random cards from your deck are in your hands as you fall. Choose one to let go of.
public sealed class BgFalling : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "FIELD_OF_MAN_SIZED_HOLES";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        Player.Deck.Cards.Any(c => c.IsRemovable) ? [Option("LAND", Land)] : [Leave()];

    private async Task Land()
    {
        var cards = Player.Deck.Cards.Where(c => c.IsRemovable).ToList().UnstableShuffle(Rng).Take(3).ToList();
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var lost = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), cards, Player, prefs)).FirstOrDefault()
            ?? cards[0];
        await CardPileCmd.RemoveFromDeck(lost);
        Finish("LAND");
    }
}

public sealed class BgSecretPortal : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "SAPPHIRE_SEED";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("ENTER", async () =>
        {
            await RelicCmd.Obtain<BgSecretPortalRelic>(Player);
            Finish("ENTER");
        }),
        Leave(),
    ];
}

// Marks that the party took the Secret Portal: their next move on the map is straight to the boss. It goes away
// when the boss is reached.
public sealed class BgSecretPortalRelic : BgRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override string BorrowedEntry => "WINGED_BOOTS";

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType == RoomType.Boss)
        {
            await RelicCmd.Remove(this);
        }
    }
}

[HarmonyPatch(typeof(MapTravel), nameof(MapTravel.GetTravelablePointsFrom))]
internal static class SecretPortalTravelPatch
{
    private static void Postfix(IRunState runState, ref IEnumerable<MapPoint> __result)
    {
        if (runState.Players.Any(p => p.Relics.OfType<BgSecretPortalRelic>().Any()))
        {
            __result = [runState.Map.BossMapPoint];
        }
    }
}

public sealed class BgWindingHalls : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "JUNGLE_MAZE_ADVENTURE";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("EMBRACE", async () => { await GainRandomCards(2); Finish("EMBRACE"); }),
        Option("FOCUS", async () =>
        {
            await Heal(3);
            await GainCurse();
            Finish("FOCUS");
        }),
        Option("RETRACE", async () => { await LoseHp(2); Finish("RETRACE"); }),
    ];
}

public sealed class BgSensoryStone : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "CRYSTAL_SPHERE";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("RECALL_1", async () => { await OfferColorless(1); Finish("RECALL"); }),
        Option("RECALL_2", async () =>
        {
            await LoseHp(2);
            await OfferColorless(2);
            Finish("RECALL");
        }),
    ];

    private Task OfferColorless(int rewards) =>
        Offer(Enumerable.Range(0, rewards).Select(_ => (Reward)new CardReward(RewardDeck.DrawColorless(Player, 3),
            MegaCrit.Sts2.Core.Runs.CardCreationSource.Encounter, Player,
            CardCreationOptions.ForNonCombatWithDefaultOdds([Player.Character.CardPool]))).ToArray());
}

public sealed class BgTheMoaiHead : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "WOOD_CARVINGS";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("JUMP", async () =>
        {
            await HealToFull();
            await LoseHp(2);
            Finish("JUMP");
        }),
        Option("OFFER", PayableRelics.Count > 0, async () =>
        {
            if (await ChooseRelicToGive() is BgRelic relic)
            {
                await GainGold(relic.Price);
            }
            Finish("OFFER");
        }),
        Leave(),
    ];
}

// Take a relic from the Face Trader, then give him one of your relics (possibly the one you just took).
public sealed class BgFaceTrader : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgBeyond)];
    public override string BorrowedEntry => "SYMBIOTE";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("TRADE", async () =>
        {
            await GainRelic();
            await ChooseRelicToGive();
            Finish("TRADE");
        }),
        Leave(),
    ];
}
