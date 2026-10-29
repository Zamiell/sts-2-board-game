using BoardGame.Acts;
using BoardGame.Cards;
using BoardGame.Characters;
using BoardGame.Core;
using BoardGame.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Events;

public sealed class BgGoldenShrine : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "SUNKEN_STATUE";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("PRAY", async () => { await GainGold(2); Finish("PRAY"); }),
        Option("DESECRATE", async () =>
        {
            await GainGold(7);
            await GainCurse();
            Finish("DESECRATE");
        }),
        Leave(),
    ];
}

public sealed class BgCursedTome : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "SELF_HELP_BOOK";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("TAKE", async () =>
        {
            await GainCurse();
            await OfferCards(rare: true);
            Finish("TAKE");
        }),
        Option("READ", async () =>
        {
            await LoseHp(2);
            await GainRandomCards(1, rare: true);
            Finish("READ");
        }),
        Option("SKIM", async () =>
        {
            await LoseHp(1);
            await OfferCards();
            Finish("SKIM");
        }),
        Leave(),
    ];
}

public sealed class BgAncientWriting : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "TABLET_OF_TRUTH";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("ELEGANCE", HasRemovableCards, async () => { await Remove(); Finish("ELEGANCE"); }),
        Option("SIMPLICITY", () =>
        {
            foreach (var tag in new[] { CardTag.Strike, CardTag.Defend })
            {
                if (Player.Deck.Cards.FirstOrDefault(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(tag) && c.IsUpgradable) is { } card)
                {
                    CardCmd.Upgrade(card);
                }
            }
            Finish("SIMPLICITY");
            return Task.CompletedTask;
        }),
    ];
}

public sealed class BgAugmenter : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "SPIRIT_GRAFTER";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("DRINK", HasUpgradableCards, async () =>
        {
            await LoseHp(1);
            await Upgrade();
            Finish("DRINK");
        }),
        Option("TEST_SUBJECT", Player.Deck.Cards.Count(c => c.IsRemovable) >= 2, async () =>
        {
            await LoseHp(2);
            await Transform(2);
            Finish("TEST_SUBJECT");
        }),
        Leave(),
    ];
}

public sealed class BgDesigner : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "ZEN_WEAVER";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("ADJUSTMENTS", Player.Gold >= 2 && HasUpgradableCards, async () =>
        {
            await LoseGold(2);
            await Upgrade();
            Finish("SERVICED");
        }),
        Option("CLEAN_UP", Player.Gold >= 3 && HasRemovableCards, async () =>
        {
            await LoseGold(3);
            await Remove();
            Finish("SERVICED");
        }),
        Option("FULL_SERVICE", Player.Gold >= 5 && HasRemovableCards, async () =>
        {
            await LoseGold(5);
            await Remove();
            await Upgrade();
            Finish("SERVICED");
        }),
        Option("PUNCH", async () => { await LoseHp(1); Finish("PUNCH"); }),
    ];
}

public sealed class BgWomanInBlue : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "POTION_COURIER";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("BUY_1", Player.Gold >= 2, async () =>
        {
            await LoseGold(2);
            await OfferPotions(1);
            Finish("BUY");
        }),
        Option("BUY_2", Player.Gold >= 3, async () =>
        {
            await LoseGold(3);
            await OfferPotions(2);
            Finish("BUY");
        }),
        Option("LEAVE", async () => { await LoseHp(1); Finish("LEAVE"); }),
    ];
}

public sealed class BgOldBeggar : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "LOST_WISP";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("OFFER", Player.Gold >= 2 && HasRemovableCards, async () =>
        {
            await LoseGold(2);
            await Remove();
            Finish("OFFER");
        }),
        Leave(),
    ];
}

// Ask the skull up to two questions. Each costs 1 HP the first time and 1 more each time it is asked again.
public sealed class BgKnowingSkull : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "WHISPERING_HOLLOW";

    private int _asked;
    private bool _potion;
    private bool _gold;
    private bool _card;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => Questions("INITIAL");

    private EventOption[] Questions(string page) =>
    [
        Option("POTION", !_potion, () => Ask(() => { _potion = true; return OfferPotions(1); }), page),
        Option("GOLD", !_gold, () => Ask(() => { _gold = true; return GainGold(3); }), page),
        Option("CARD", !_card, () => Ask(() => { _card = true; return OfferCards(); }), page),
        Leave(page),
    ];

    private async Task Ask(Func<Task> reward)
    {
        await LoseHp(1);
        if (Player.Creature.IsDead)
        {
            return;
        }
        await reward();
        _asked++;
        if (_asked >= 2)
        {
            Finish("DONE");
        }
        else
        {
            Show("ASK", Questions("ASK"));
        }
    }
}

// Choose another character's color and take a card from their reward deck.
public sealed class BgANoteForYourself : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity), typeof(BgBeyond)];
    public override string BorrowedEntry => "REFLECTIONS";

    private static readonly (string Name, Type Pool)[] Colors =
    [
        ("RED", typeof(BgIroncladCardPool)), ("GREEN", typeof(BgSilentCardPool)),
        ("BLUE", typeof(BgDefectCardPool)), ("PURPLE", typeof(BgWatcherCardPool)),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        Colors.Where(c => c.Pool != Player.Character.CardPool.GetType())
            .Select(c => Option(c.Name, async () =>
            {
                var pool = (CardPoolModel)Content.Get(c.Pool);
                // Defect cards need orb slots.
                if (pool is BgDefectCardPool && !Player.Relics.OfType<BgPrismaticShard>().Any())
                {
                    await RelicCmd.Obtain<BgPrismaticShard>(Player);
                }
                await Offer(RewardDeck.Reward(Player, pool));
                Finish("TAKE");
            }))
            .ToList();
}

// Warm up the crowd against a normal encounter, or be the main event against an elite (with a second relic).
public sealed class BgColosseum : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "TRIAL";

    public override bool IsShared => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("WARM_UP", () => Fight(Player.RunState.Act.AllRegularEncounters, [])),
        Option("MAIN_EVENT", () => Fight(Player.RunState.Act.AllEliteEncounters, [new RelicReward(Player)])),
    ];

    private Task Fight(IEnumerable<EncounterModel> encounters, List<Reward> extraRewards)
    {
        EnterCombatWithoutExitingEvent(Rng.NextItem(encounters)!, extraRewards, shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }
}

// Give N'loth a random relic for a Rare card of your choice, or a potion for a random Rare card.
public sealed class BgNloth : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "RELIC_TRADER";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("OFFER_RELIC", PayableRelics.Count > 0, async () =>
        {
            await RelicCmd.Remove(Rng.NextItem(PayableRelics)!);
            await OfferCards(rare: true);
            Finish("OFFER");
        }),
        Option("OFFER_POTION", Player.Potions.Any(), async () =>
        {
            if (await ChoosePotionToGive("Give N'loth a potion") != null)
            {
                await GainRandomCards(1, rare: true);
            }
            Finish("OFFER");
        }),
        Leave(),
    ];
}

// Pay the stranger with 4 Gold, a relic, or an Uncommon or Rare card, and he gives you a relic.
public sealed class BgWeMeetAgain : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "ROUND_TEA_PARTY";

    private static bool Ranwiddable(CardModel card) => card.IsRemovable && card.Rarity is CardRarity.Uncommon or CardRarity.Rare;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("GOLD", Player.Gold >= 4, async () =>
        {
            await LoseGold(4);
            await GainRelic();
            Finish("GIVE");
        }),
        Option("RELIC", PayableRelics.Count > 0, async () =>
        {
            if (await ChooseRelicToGive() != null)
            {
                await GainRelic();
            }
            Finish("GIVE");
        }),
        Option("CARD", Player.Deck.Cards.Any(Ranwiddable), async () =>
        {
            await Remove(1, Ranwiddable);
            await GainRelic();
            Finish("GIVE");
        }),
        Option("ATTACK", () => { Finish("ATTACK"); return Task.CompletedTask; }),
    ];
}

// Bet 2 Gold, a relic, or a potion on a knight. On a 4-6 you win 6 Gold.
public sealed class BgTheJoust : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "BUGSLAYER";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("BET_GOLD", Player.Gold >= 2, async () => { await LoseGold(2); await Joust(); }),
        Option("BET_RELIC", PayableRelics.Count > 0, async () =>
        {
            if (await ChooseRelicToGive() != null)
            {
                await Joust();
            }
            else
            {
                Finish("WATCH");
            }
        }),
        Option("BET_POTION", Player.Potions.Any(), async () =>
        {
            if (await ChoosePotionToGive("Wager a potion") != null)
            {
                await Joust();
            }
            else
            {
                Finish("WATCH");
            }
        }),
        Option("WATCH", () => { Finish("WATCH"); return Task.CompletedTask; }),
    ];

    private async Task Joust()
    {
        var roll = await RollDie(face => face >= 4 ? "Win 6 Gold." : "Lose the bet.");
        if (roll >= 4)
        {
            await GainGold(6);
            Finish("WIN");
        }
        else
        {
            Finish("LOSE");
        }
    }
}

// Opening the coffin gives a relic, but on a 1-3 also a Curse.
public sealed class BgTheMausoleum : BgEvent
{
    public override int MinAscension => BgAscension.UnfavorableEvents;

    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "DOLL_ROOM";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("OPEN", async () =>
        {
            await GainRelic();
            var roll = await RollDie(face => face >= 4 ? "No Curse." : "Gain a Curse.");
            if (roll < 4)
            {
                await GainCurse();
            }
            Finish(roll >= 4 ? "LUCKY" : "UNLUCKY");
        }),
        Leave(),
    ];
}

public sealed class BgForgottenAltar : BgEvent
{
    public override int MinAscension => BgAscension.UnfavorableEvents;

    public override IReadOnlyList<Type> Acts => [typeof(BgCity)];
    public override string BorrowedEntry => "LUMINOUS_CHOIR";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("OFFER", PayableRelics.Count > 0, async () =>
        {
            await RelicCmd.Remove(Rng.NextItem(PayableRelics)!);
            await GainRelic();
            Finish("OFFER");
        }),
        Option("SACRIFICE", async () => { await LoseHp(2); Finish("SACRIFICE"); }),
        Option("DESECRATE", async () => { await GainCurse(); Finish("DESECRATE"); }),
    ];
}
