using BoardGame.Cards;
using BoardGame.Core;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Relics;

// Relics from the relic deck that trigger by themselves.

public sealed class BgAnchor : BgDeckRelic
{
    public override int Price => 6;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 2, ValueProp.Unpowered, null);
    }
}

public sealed class BgBagOfPreparation : BgDeckRelic
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && player.PlayerCombatState?.TurnNumber == 1)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, 2, Owner);
        }
    }
}

public sealed class BgBloodVial : BgDeckRelic
{
    public override int Price => 6;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CreatureCmd.Heal(Owner.Creature, 1);
    }
}

public sealed class BgLantern : BgDeckRelic
{
    public override int Price => 6;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && player.PlayerCombatState?.TurnNumber == 1)
        {
            Flash();
            await PlayerCmd.GainEnergy(1, Owner);
        }
    }
}

public sealed class BgMutagenicStrength : BgDeckRelic
{
    public override string BorrowedEntry => "VAJRA";
    public override int Price => 6;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && player.PlayerCombatState?.TurnNumber == 1)
        {
            Flash();
            await RelicEffects.TemporaryStrength(choiceContext, Owner, 1);
        }
    }
}

public sealed class BgBirdFacedUrn : BgDeckRelic
{
    public override string BorrowedEntry => "BELLOWS";
    public override int Price => 9;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && cardPlay.Card.Type == CardType.Power)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, 1, ValueProp.Unpowered, null);
        }
    }
}

public sealed class BgGoldenIdol : BgDeckRelic
{
    public override string BorrowedEntry => "WONGO_CUSTOMER_APPRECIATION_BADGE";
    public override int Price => 4;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        Flash();
        await PlayerCmd.GainGold(1, Owner);
    }
}

public sealed class BgIceCream : BgDeckRelic
{
    public override bool ShouldPlayerResetEnergy(Player player) =>
        player != Owner || Owner.PlayerCombatState?.TurnNumber == 1;
}

public sealed class BgMeatOnTheBone : BgDeckRelic
{
    public override int Price => 8;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        var creature = Owner.Creature;
        if (creature.CurrentHp is > 0 and < 4)
        {
            Flash();
            await CreatureCmd.Heal(creature, 4 - creature.CurrentHp);
        }
    }
}

public sealed class BgOrichalcum : BgDeckRelic
{
    public override int Price => 5;

    private bool _shouldTrigger;

    public override Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature))
        {
            _shouldTrigger = Owner.Creature.Block == 0;
        }
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (_shouldTrigger && participants.Contains(Owner.Creature))
        {
            _shouldTrigger = false;
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, 1, ValueProp.Unpowered, null);
        }
    }
}

public sealed class BgRedSkull : BgDeckRelic
{
    public override int Price => 8;

    private bool _triggered;

    public override Task BeforeCombatStart()
    {
        _triggered = false;
        return Task.CompletedTask;
    }

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler == Owner && !_triggered)
        {
            _triggered = true;
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 1, Owner.Creature, null);
        }
    }
}

public sealed class BgStrikeDummy : BgDeckRelic
{
    public override int Price => 8;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay) =>
        props.IsPoweredAttack() && cardSource?.Owner == Owner && cardSource.Rarity == CardRarity.Basic
        && cardSource.Tags.Contains(CardTag.Strike) ? 1m : 0m;
}

public sealed class BgDuVuDoll : BgDeckRelic
{
    public override string BorrowedEntry => "FUNERARY_MASK";

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner == Owner && card.Type == CardType.Curse)
        {
            Flash();
            await RelicEffects.TemporaryStrength(choiceContext, Owner, 1);
        }
    }
}

public sealed class BgSsserpentHead : BgDeckRelic
{
    public override string BorrowedEntry => "SNECKO_SKULL";
    public override int Price => 6;

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner.RunState.BaseRoom == room && Owner.RunState.CurrentMapPoint?.PointType == MapPointType.Unknown)
        {
            Flash();
            await PlayerCmd.GainGold(2, Owner);
        }
    }
}

// A relic with a limited number of uses, shown on its counter. The count is saved with the run.
public abstract class BgLimitedRelic : BgDeckRelic
{
    protected abstract int Uses { get; }

    private int _timesUsed;

    [SavedProperty]
    public int TimesUsed
    {
        get => _timesUsed;
        set
        {
            AssertMutable();
            _timesUsed = value;
            InvokeDisplayAmountChanged();
            if (IsUsedUp)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }

    public override bool IsUsedUp => TimesUsed >= Uses;
    public override bool ShowCounter => !IsUsedUp;
    public override int DisplayAmount => Uses - TimesUsed;
}

// Omamori: the next 2 Curses you would gain are prevented.
public sealed class BgOmamori : BgLimitedRelic
{
    public override string BorrowedEntry => "AMETHYST_AUBERGINE";
    public override int Price => 5;
    protected override int Uses => 2;

    public override bool ShouldAddToDeck(CardModel card) => IsUsedUp || card.Owner != Owner || card.Type != CardType.Curse;

    public override Task AfterAddToDeckPrevented(CardModel card)
    {
        if (card.Owner == Owner && card.Type == CardType.Curse && !IsUsedUp)
        {
            Flash();
            TimesUsed++;
        }
        return Task.CompletedTask;
    }
}

// Molten Egg and Toxic Egg upgrade the next 3 Attacks or Skills added to your deck.
public abstract class BgEggRelic(CardType type) : BgLimitedRelic
{
    public override int Price => 8;
    protected override int Uses => 3;

    public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
    {
        newCard = null;
        if (IsUsedUp || card.Owner != Owner || card.Type != type || !card.IsUpgradable || card.CurrentUpgradeLevel > 0)
        {
            return false;
        }
        newCard = Owner.RunState.CloneCard(card);
        CardCmd.Upgrade(newCard, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        TimesUsed++;
        Flash();
        return true;
    }
}

public sealed class BgMoltenEgg() : BgEggRelic(CardType.Attack);

public sealed class BgToxicEgg() : BgEggRelic(CardType.Skill);

// Wing Boots: up to 3 times, you may ignore paths when moving to the next row of the map.
public sealed class BgWingBoots : BgLimitedRelic
{
    public override string BorrowedEntry => "WINGED_BOOTS";
    protected override int Uses => 3;

    public override bool ShouldAllowFreeTravel() => !IsUsedUp;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (IsUsedUp || Owner.RunState.CurrentRoomCount > 1 || Owner.RunState is not MegaCrit.Sts2.Core.Runs.RunState runState
            || runState.VisitedMapCoords.Count <= 1)
        {
            return Task.CompletedTask;
        }
        var previous = runState.Map.GetPoint(runState.VisitedMapCoords[^2]);
        if (previous != null && Owner.RunState.CurrentMapPoint is { } current && !previous.Children.Contains(current))
        {
            Flash();
            TimesUsed++;
        }
        return Task.CompletedTask;
    }
}

// Regal Pillow: heal 3 more when you Rest.
public sealed class BgRegalPillow : BgDeckRelic
{
    public override int Price => 6;

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount) =>
        creature.Player == Owner ? amount + 3 : amount;
}

// Peace Pipe: when you Rest, you may also remove a card.
public sealed class BgPeacePipe : BgDeckRelic
{
    public override int Price => 8;

    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner || isMimicked)
        {
            return;
        }
        var prefs = new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(
            MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs.RemoveSelectionPrompt, 0, 1);
        foreach (var card in await CardSelectCmd.FromDeckForRemoval(Owner, prefs))
        {
            Flash();
            await CardPileCmd.RemoveFromDeck(card);
        }
    }
}
