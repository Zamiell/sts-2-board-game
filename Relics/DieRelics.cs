using BoardGame.Cards;
using BoardGame.Core;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Relics;

// One face (or set of faces) of a die relic. Some abilities can't be chosen by The Die's 6 or Dolly's Mirror because
// they are drawbacks (Snecko Eye's Dazed) or choices themselves (The Die's 6).
public sealed record DieAbility(int[] Faces, string Text, Func<PlayerChoiceContext, Task> Run, bool Choosable = true);

// A relic that does something when the die locks in on one of its faces.
public abstract class BgDieRelic : BgDeckRelic, IDieRelic
{
    public abstract IReadOnlyList<DieAbility> Abilities { get; }

    public virtual async Task AfterDieLockedIn(PlayerChoiceContext choiceContext, int roll)
    {
        foreach (var ability in Abilities.Where(a => a.Faces.Contains(roll)))
        {
            Flash();
            await ability.Run(choiceContext);
        }
    }

    protected Creature Me => Owner.Creature;

    protected Task Block(decimal amount) => Block(Me, amount);

    protected static Task Block(Creature creature, decimal amount) =>
        CreatureCmd.GainBlock(creature, amount, ValueProp.Unpowered, null);

    protected Task Draw(PlayerChoiceContext choiceContext, int count) => CardPileCmd.Draw(choiceContext, count, Owner);

    protected Task Energy(int amount) => PlayerCmd.GainEnergy(amount, Owner);

    protected Task TemporaryStrength(PlayerChoiceContext choiceContext, int amount) =>
        RelicEffects.TemporaryStrength(choiceContext, Owner, amount);

    // "Deal X damage" from a relic: chosen enemy, not an attack.
    protected async Task DamageEnemy(PlayerChoiceContext choiceContext, decimal amount)
    {
        if (await Rows.ChooseEnemy(choiceContext, Owner, $"{Title.GetFormattedText()}: deal {amount} damage") is { } target)
        {
            await CreatureCmd.Damage(choiceContext, target, amount, ValueProp.Unpowered, Me, null, null);
        }
    }

    protected async Task Debuff<TPower>(PlayerChoiceContext choiceContext, decimal amount, string name) where TPower : MegaCrit.Sts2.Core.Models.PowerModel
    {
        if (await Rows.ChooseEnemy(choiceContext, Owner, $"{Title.GetFormattedText()}: apply {amount} {name}") is { } target)
        {
            await PowerCmd.Apply<TPower>(choiceContext, target, amount, Me, null);
        }
    }

    protected static DieAbility On(int face, string text, Func<PlayerChoiceContext, Task> run) => new([face], text, run);

    protected static DieAbility On(int[] faces, string text, Func<PlayerChoiceContext, Task> run) => new(faces, text, run);
}

public static class RelicEffects
{
    // Strength that is lost at the end of the turn.
    public static async Task TemporaryStrength(PlayerChoiceContext choiceContext, Player player, int amount)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext, player.Creature, amount, player.Creature, null);
        await PowerCmd.Apply<BgFlexPower>(choiceContext, player.Creature, amount, player.Creature, null);
    }

    public static async Task<Player?> ChoosePlayer(PlayerChoiceContext choiceContext, Player chooser, string prompt)
    {
        var players = chooser.RunState.Players.Where(p => p.Creature.IsAlive).ToList();
        return await Choice.Choose(choiceContext, chooser, prompt, players,
            p => new Choice.Option(p.Creature.Name, $"{p.Creature.CurrentHp}/{p.Creature.MaxHp} HP, {p.Creature.Block} Block"));
    }

    // Lets a player trigger one die relic ability. Its owner gains the effect (The Die's 6, Dolly's Mirror, Nilry's
    // Codex).
    public static async Task ChooseAbility(PlayerChoiceContext choiceContext, Player chooser, string prompt,
        IEnumerable<(BgDieRelic Relic, DieAbility Ability)> options)
    {
        var list = options.Where(o => o.Ability.Choosable).ToList();
        var index = await Choice.Choose(choiceContext, chooser, prompt,
            list.Select(o => new Choice.Option(o.Relic.Title.GetFormattedText(),
                $"[{string.Join("/", o.Ability.Faces)}] {o.Ability.Text}" +
                (o.Relic.Owner == chooser ? "" : $" ({o.Relic.Owner.Creature.Name})"))).ToList());
        if (index >= 0)
        {
            list[index].Relic.Flash();
            await list[index].Ability.Run(choiceContext);
        }
    }

    public static IEnumerable<(BgDieRelic, DieAbility)> AbilitiesOf(IEnumerable<Player> players, Func<BgDieRelic, bool> relicFilter,
        Func<DieAbility, bool>? abilityFilter = null) =>
        players.SelectMany(p => p.Relics.OfType<BgDieRelic>())
            .Where(relicFilter)
            .SelectMany(r => r.Abilities.Where(a => abilityFilter?.Invoke(a) ?? true).Select(a => (r, a)));
}

// Every character starts with the die. Its counter shows this round's roll. On a 4 or 5 it gives 1 Block; on a 6 you
// trigger one of your die relics' abilities.
public sealed class BgTheDie : BgDieRelic
{
    public override string BorrowedEntry => "GAME_PIECE";

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool IsAllowedInShops => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Unpowered)];

    public override bool ShowCounter => DisplayAmount > 0;

    public override int DisplayAmount => BgRules.For(Owner.RunState)?.Roll ?? 0;

    public void RefreshDisplay() => InvokeDisplayAmountChanged();

    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On([4, 5], "Gain 1 Block.", _ => Block(1)),
        new([6], "Trigger a die relic ability.", ctx => RelicEffects.ChooseAbility(ctx, Owner,
            "The Die: trigger a die relic ability", RelicEffects.AbilitiesOf([Owner], r => r is not BgTheDie)), Choosable: false),
    ];
}

public sealed class BgCaptainsWheel : BgDieRelic
{
    public override int Price => 8;
    public override IReadOnlyList<DieAbility> Abilities => [On(3, "Gain 3 Block.", _ => Block(3))];
}

public sealed class BgDuality : BgDieRelic
{
    public override string BorrowedEntry => "YIN_YANG";
    public override int Price => 8;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On(2, "Gain 2 Block.", _ => Block(2)),
        On(4, "Deal 2 damage.", ctx => DamageEnemy(ctx, 2)),
    ];
}

public sealed class BgHappyFlower : BgDieRelic
{
    public override int Price => 6;
    public override IReadOnlyList<DieAbility> Abilities => [On([3, 4], "Gain 1 Energy.", _ => Energy(1))];
}

public sealed class BgHornCleat : BgDieRelic
{
    public override int Price => 6;
    public override IReadOnlyList<DieAbility> Abilities => [On([1, 2], "Gain 1 Block.", _ => Block(1))];
}

public sealed class BgInkBottle : BgDieRelic
{
    public override string BorrowedEntry => "BOOKMARK";
    public override int Price => 6;
    public override IReadOnlyList<DieAbility> Abilities => [On([5, 6], "Draw a card.", ctx => Draw(ctx, 1))];
}

public sealed class BgMercuryHourglass : BgDieRelic
{
    public override int Price => 6;
    public override IReadOnlyList<DieAbility> Abilities =>
        [On([1, 2], "Deal 1 damage to any row.", ctx => Rows.DamageAnyRow(ctx, Owner, 1, Title.GetFormattedText()))];
}

public sealed class BgNecronomicon : BgDieRelic
{
    public override string BorrowedEntry => "BOOK_OF_FIVE_RINGS";
    public override int Price => 9;
    public override IReadOnlyList<DieAbility> Abilities =>
        [On(1, "Your next Attack this turn is played twice.", ctx => PowerCmd.Apply<BgDoubleAttackPower>(ctx, Me, 1, Me, null))];
}

public sealed class BgNilrysCodex : BgDieRelic
{
    public override string BorrowedEntry => "DUSTY_TOME";
    public override int Price => 7;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On(2, "Draw a card.", ctx => Draw(ctx, 1)),
        On(4, "Trigger a [2] ability. Its owner gains the effect.", ctx => RelicEffects.ChooseAbility(ctx, Owner,
            "Nilry's Codex: trigger a [2] ability", RelicEffects.AbilitiesOf(Owner.RunState.Players, _ => true,
                a => a.Faces.Contains(2)))),
    ];
}

public sealed class BgOddlySmoothStone : BgDieRelic
{
    public override int Price => 7;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On(4, "Gain 2 Block (any player).", async ctx =>
        {
            if (await RelicEffects.ChoosePlayer(ctx, Owner, "Oddly Smooth Stone: give 2 Block") is { } player)
            {
                await Block(player.Creature, 2);
            }
        }),
    ];
}

public sealed class BgPocketwatch : BgDieRelic
{
    public override int Price => 9;
    public override IReadOnlyList<DieAbility> Abilities => [On(3, "Draw 3 cards.", ctx => Draw(ctx, 3))];
}

public sealed class BgSundial : BgDieRelic
{
    public override string BorrowedEntry => "PENDULUM";
    public override int Price => 8;
    public override IReadOnlyList<DieAbility> Abilities => [On(2, "Gain 2 Energy.", _ => Energy(2))];
}

public sealed class BgTungstenRod : BgDieRelic
{
    public override int Price => 8;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On(5, "1 Block to all players (3 Block if solo).", async _ =>
        {
            var players = Owner.RunState.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players.Count == 1)
            {
                await Block(3);
                return;
            }
            foreach (var player in players)
            {
                await Block(player.Creature, 1);
            }
        }),
    ];
}

public sealed class BgVajra : BgDieRelic
{
    public override int Price => 7;
    public override IReadOnlyList<DieAbility> Abilities =>
        [On(2, "Gain 1 Strength this turn.", ctx => TemporaryStrength(ctx, 1))];
}

public sealed class BgDollysMirror : BgDieRelic
{
    public override int Price => 7;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        new([1], "Trigger a die relic ability. Its owner gains the effect.", ctx => RelicEffects.ChooseAbility(ctx, Owner,
            "Dolly's Mirror: trigger a die relic ability", RelicEffects.AbilitiesOf(Owner.RunState.Players, r => r is not BgDollysMirror)),
            Choosable: false),
    ];
}

public sealed class BgIncenseBurner : BgDieRelic
{
    public override int Price => 11;
    public override IReadOnlyList<DieAbility> Abilities =>
        [On(6, "You can't lose more than 1 HP this round.", ctx => PowerCmd.Apply<BgIncenseBurnerPower>(ctx, Me, 1, Me, null))];
}

public sealed class BgPenNib : BgDieRelic
{
    public override int Price => 8;
    public override IReadOnlyList<DieAbility> Abilities =>
        [On(5, "Apply 1 Vulnerable.", ctx => Debuff<VulnerablePower>(ctx, 1, "Vulnerable"))];
}

public sealed class BgRedMask : BgDieRelic
{
    public override int Price => 7;
    public override IReadOnlyList<DieAbility> Abilities =>
        [On([5, 6], "Apply 1 Weak.", ctx => Debuff<WeakPower>(ctx, 1, "Weak"))];
}

public sealed class BgStoneCalendar : BgDieRelic
{
    public override int Price => 7;
    public override IReadOnlyList<DieAbility> Abilities => [On(4, "Deal 4 damage.", ctx => DamageEnemy(ctx, 4))];
}

public sealed class BgTheBoot : BgDieRelic
{
    public override int Price => 5;
    public override IReadOnlyList<DieAbility> Abilities => [On([4, 5, 6], "Deal 1 damage.", ctx => DamageEnemy(ctx, 1))];
}

public sealed class BgCharonsAshes : BgDieRelic
{
    public override int Price => 7;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On([1, 2], "You may Exhaust a card in your hand to deal 2 damage.", async ctx =>
        {
            if (Owner.PlayerCombatState?.Hand.Cards.Any(c => c is not BgTokenCard) != true)
            {
                return;
            }
            var prefs = new CardSelectorPrefs(Choice.PromptText("Charon's Ashes: Exhaust a card to deal 2 damage"), 0, 1);
            var chosen = (await CardSelectCmd.FromHand(ctx, Owner, prefs, c => c is not BgTokenCard, this)).FirstOrDefault();
            if (chosen != null)
            {
                await CardCmd.Exhaust(ctx, chosen);
                await DamageEnemy(ctx, 2);
            }
        }),
    ];
}

public sealed class BgGremlinHorn : BgDieRelic
{
    public override int Price => 8;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On(4, "Draw a card.", ctx => Draw(ctx, 1)),
        On(5, "Gain 1 Energy.", _ => Energy(1)),
    ];
}

// Boss relic: the die decides what Snecko Eye gives you.
public sealed class BgSneckoEye : BgDieRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool IsAllowedInShops => false;
    public override IReadOnlyList<DieAbility> Abilities =>
    [
        On([1, 2], "Draw 2 cards.", ctx => Draw(ctx, 2)),
        On([3, 4], "Gain 1 Energy.", _ => Energy(1)),
        new([5, 6], "Gain a Dazed.", async _ =>
        {
            if (Owner.Creature.CombatState is { } combat)
            {
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<BgDazed>(Owner), MegaCrit.Sts2.Core.Entities.Cards.PileType.Draw,
                    Owner, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top);
            }
        }, Choosable: false),
    ];
}
