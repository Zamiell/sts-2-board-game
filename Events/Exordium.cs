using BoardGame.Core;
using BoardGame.Acts;
using BoardGame.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace BoardGame.Events;

public sealed class BgLivingWall : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "DOORS_OF_LIGHT_AND_DARK";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("FORGET", HasRemovableCards, async () => { await Remove(); Finish("RESULT"); }),
        Option("CHANGE", HasRemovableCards, async () => { await Transform(); Finish("RESULT"); }),
        Option("GROW", HasUpgradableCards, async () => { await Upgrade(); Finish("RESULT"); }),
    ];
}

public sealed class BgTheLibrary : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "WATERLOGGED_SCRIPTORIUM";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("READ", async () => { await OfferCards(5); Finish("READ"); }),
        Option("SLEEP", async () => { await Heal(3); Finish("SLEEP"); }),
        Leave(),
    ];
}

public sealed class BgGoldenIdol : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "SUNKEN_TREASURY";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("TAKE", async () =>
        {
            await GainRelic();
            await LoseHp(1);
            Finish("TAKE");
        }),
        Leave(),
    ];
}

public sealed class BgGoldenWing : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "BYRDONIS_NEST";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("PRAY", HasRemovableCards, async () =>
        {
            await LoseHp(2);
            await Remove();
            Finish("PRAY");
        }),
        Option("DESTROY", async () => { await GainGold(2); Finish("DESTROY"); }),
        Leave(),
    ];
}

// Offer a card to the spirits: a Rare heals you to full, an Uncommon heals 3, and a Curse burns you for 1.
public sealed class BgBonfireSpirits : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium), typeof(BgBeyond)];
    public override string BorrowedEntry => "UNREST_SITE";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("OFFER", HasRemovableCards, Offer),
        Leave(),
    ];

    private async Task Offer()
    {
        var prefs = new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs.RemoveSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForRemoval(Player, prefs)).FirstOrDefault();
        if (card == null)
        {
            Finish("LEAVE");
            return;
        }
        var rarity = card.Rarity;
        await CardPileCmd.RemoveFromDeck(card);
        switch (rarity)
        {
            case CardRarity.Curse:
                await LoseHp(1);
                Finish("CURSE");
                break;
            case CardRarity.Uncommon:
                await Heal(3);
                Finish("UNCOMMON");
                break;
            case CardRarity.Rare:
                await HealToFull();
                Finish("RARE");
                break;
            default:
                Finish("COMMON");
                break;
        }
    }
}

public sealed class BgTransmogrifier : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "MORPHIC_GROVE";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("PRAY", HasRemovableCards, async () => { await Transform(); Finish("PRAY"); }),
        Option("SACRIFICE", Player.Deck.Cards.Count(c => c.IsRemovable) >= 2, async () =>
        {
            await Transform(2);
            await GainCurse();
            Finish("SACRIFICE");
        }),
        Leave(),
    ];
}

public sealed class BgTheCleric : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "RANWID_THE_ELDER";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("HEAL", Player.Gold >= 1, async () =>
        {
            await LoseGold(1);
            await Heal(3);
            Finish("HEAL");
        }),
        Option("PRAYER", Player.Gold >= 2 && HasUpgradableCards, async () =>
        {
            await LoseGold(2);
            await Upgrade();
            Finish("PRAYER");
        }),
        Option("PURIFY", Player.Gold >= 3 && HasRemovableCards, async () =>
        {
            await LoseGold(3);
            await Remove();
            Finish("PURIFY");
        }),
        Leave(),
    ];
}

public sealed class BgUpgradeShrine : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "TINKER_TIME";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("PRAY", HasUpgradableCards, async () => { await Upgrade(); Finish("PRAY"); }),
        Option("SACRIFICE", HasUpgradableCards, async () =>
        {
            await LoseHp(2);
            UpgradeRandom(2);
            Finish("SACRIFICE");
        }),
        Leave(),
    ];
}

// Spin the wheel: 1 gains 4 Gold, 2 a relic, 3 heals to full, 4 a Curse, 5 removes a card, 6 loses 2 HP.
public sealed class BgWheelOfChange : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium), typeof(BgCity), typeof(BgBeyond)];
    public override string BorrowedEntry => "WELCOME_TO_WONGOS";

    private static readonly string[] Prizes =
        ["Gain 4 Gold.", "Obtain a relic.", "Heal to full HP.", "Gain a Curse.", "Remove a card from your deck.", "Lose 2 HP."];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [Option("PLAY", Play)];

    private async Task Play()
    {
        var roll = await RollDie(face => Prizes[face - 1]);
        switch (roll)
        {
            case 1:
                await GainGold(4);
                break;
            case 2:
                await GainRelic();
                break;
            case 3:
                await HealToFull();
                break;
            case 4:
                await GainCurse();
                break;
            case 5:
                await Remove();
                break;
            default:
                await LoseHp(2);
                break;
        }
        Finish($"PRIZE_{roll}");
    }
}

// Rummaging finds a relic, but on a 1-3 also a Curse.
public sealed class BgOminousForge : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "STONE_OF_ALL_TIME";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("FORGE", HasUpgradableCards, async () =>
        {
            await LoseHp(2);
            await Upgrade();
            Finish("FORGE");
        }),
        Option("RUMMAGE", async () =>
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

// Each player gains a potion; on a 4-6 they find a second one.
public sealed class BgLab : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "THE_FUTURE_OF_POTIONS";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("SEARCH", async () =>
        {
            var roll = await RollDie(face => face >= 4 ? "Gain 2 potions." : "Gain 1 potion.");
            await OfferPotions(roll >= 4 ? 2 : 1);
            Finish("SEARCH");
        }),
    ];
}

public sealed class BgBigFish : BgEvent
{
    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "SPIRALING_WHIRLPOOL";

    private CardModel? StarterStrike(bool upgraded) =>
        Player.Deck.Cards.FirstOrDefault(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Strike) && c.IsUpgraded == upgraded);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("BANANA", async () => { await Heal(2); Finish("BANANA"); }),
        Option("DONUT", StarterStrike(false) is { IsUpgradable: true }, () =>
        {
            CardCmd.Upgrade(StarterStrike(false)!);
            Finish("DONUT");
            return Task.CompletedTask;
        }),
        Option("BOX", async () =>
        {
            await GainRelic();
            await GainCurse();
            Finish("BOX");
        }),
        Option("RESTRAINT", (StarterStrike(false) ?? StarterStrike(true)) != null, async () =>
        {
            await CardPileCmd.RemoveFromDeck((StarterStrike(false) ?? StarterStrike(true))!);
            Finish("RESTRAINT");
        }),
    ];
}

// Reach inside for 1 HP: on a 1-2 you find nothing and may reach again, on a 3-4 2 Gold, on a 5-6 a relic.
public sealed class BgScrapOoze : BgEvent
{
    public override int MinAscension => BgAscension.UnfavorableEvents;

    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "TRASH_HEAP";

    private static string Describe(int face) => face switch
    {
        <= 2 => "Find nothing.",
        <= 4 => "Find 2 Gold.",
        _ => "Find a relic.",
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [Option("REACH", Reach), Leave()];

    private async Task Reach()
    {
        await LoseHp(1);
        if (Player.Creature.IsDead)
        {
            return;
        }
        var roll = await RollDie(Describe);
        if (roll <= 2)
        {
            Show("NOTHING", Option("REACH", Reach, "NOTHING"), Leave("NOTHING"));
            return;
        }
        if (roll <= 4)
        {
            await GainGold(2);
            Finish("GOLD");
        }
        else
        {
            await GainRelic();
            Finish("RELIC");
        }
    }
}

public sealed class BgWorldOfGoop : BgEvent
{
    public override int MinAscension => BgAscension.UnfavorableEvents;

    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "ABYSSAL_BATHS";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option("GATHER", async () =>
        {
            await LoseHp(2);
            await GainGold(3);
            Finish("GATHER");
        }),
        Option("DEEPER", async () =>
        {
            await GainRelic();
            await GainCurse();
            Finish("DEEPER");
        }),
        Option("LEAVE_IT", async () =>
        {
            await LoseGold(1);
            Finish("LEAVE_IT");
        }),
    ];
}

// Search the body: on a 1-2 the elite that killed the adventurer returns, on a 3-4 you find 2 Gold, on a 5-6 a relic.
public sealed class BgDeadAdventurer : BgEvent
{
    public override int MinAscension => BgAscension.UnfavorableEvents;

    public override IReadOnlyList<Type> Acts => [typeof(BgExordium)];
    public override string BorrowedEntry => "GRAVE_OF_THE_FORGOTTEN";

    public override bool IsShared => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [Option("SEARCH", Search), Leave()];

    private async Task Search()
    {
        var roll = await RollDie(face => face switch
        {
            <= 2 => "Fight an Elite.",
            <= 4 => "Find 2 Gold.",
            _ => "Find a relic.",
        });
        if (roll <= 2)
        {
            Show("FIGHT", Option("FIGHT", () =>
            {
                var elite = Rng.NextItem(Player.RunState.Act.AllEliteEncounters)!;
                EnterCombatWithoutExitingEvent(elite, [], shouldResumeAfterCombat: false);
                return Task.CompletedTask;
            }, "FIGHT"));
            return;
        }
        if (roll <= 4)
        {
            await GainGold(2);
            Finish("GOLD");
        }
        else
        {
            await GainRelic();
            Finish("RELIC");
        }
    }
}
