using BoardGame.Cards;
using BoardGame.Core;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Monsters;

// Act IV enemies. Values are the board game's, taken from LuaViper/StSBoardGameMod.

// Spire Shield (elite, with the Spear): Fortify, Smash, then Strength for both.
// Surrounded: at the start of each player turn, each player faces the Shield or the Spear and suffers its special ability.
public sealed class BgSpireShield : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<LivingShield>();
    public override int MinInitialHp => 30;
    public override int MaxInitialHp => 30;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgPincerMovementPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("FORTIFY", () => Block(20), new DefendIntent());
        Move("SMASH", () => Hit(8), Attacks(8));
        Move("BASH", async () =>
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Creature, null);
            }
        }, new BuffIntent());
    }

    protected override string ChooseMove() => (Turn % 3) switch { 0 => "FORTIFY", 1 => "SMASH", _ => "BASH" };

    // Facing the Shield: lose 1 Energy, can't draw cards, or deal no damage this turn, depending on its next move.
    internal async Task Face(PlayerChoiceContext choiceContext, Player player)
    {
        switch (NextMove.Id)
        {
            case "SMASH":
                await PowerCmd.Apply<BgNoDrawPower>(choiceContext, player.Creature, 1, Creature, null);
                break;
            case "BASH":
                await PowerCmd.Apply<BgZeroDamagePower>(choiceContext, player.Creature, 1, Creature, null);
                break;
            default:
                await PlayerCmd.LoseEnergy(1, player);
                break;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!Creature.IsAlive)
        {
            return;
        }
        var spear = CombatState.Enemies.FirstOrDefault(e => e.IsAlive && e.Monster is BgSpireSpear)?.Monster as BgSpireSpear;
        var faced = spear == null
            ? Creature
            : await Targeting.ChooseEnemy(player, [Creature, spear.Creature],
                $"Surrounded: face the Shield ({ShieldText}) or the Spear (shuffle 2 Burns into your discard pile)") ?? Creature;
        var choice = faced == spear?.Creature ? 1 : 0;
        if (choice == 1 && spear != null)
        {
            await spear.Face(player);
        }
        else
        {
            await Face(choiceContext, player);
        }
    }

    private string ShieldText => NextMove.Id switch
    {
        "SMASH" => "You can't draw cards this turn.",
        "BASH" => "Damage you deal this turn is reduced to 0.",
        _ => "Lose 1 Energy.",
    };
}

// Spire Spear (elite, with the Shield): Burn Strike (2 hits), Piercer (2 Dazed), Skewer.
public sealed class BgSpireSpear : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<HunterKiller>();
    public override int MinInitialHp => 42;
    public override int MaxInitialHp => 42;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgPincerMovementPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("BURN_STRIKE", () => Hit(2, 2), Attacks(2, 2));
        Move("PIERCER", () => GiveStatus<BgDazed>(2), new StatusIntent(2));
        Move("SKEWER", () => Hit(9), Attacks(9));
    }

    protected override string ChooseMove() => (Turn % 3) switch { 0 => "BURN_STRIKE", 1 => "PIERCER", _ => "SKEWER" };

    // Facing the Spear: 2 Burns into your discard pile.
    internal async Task Face(Player player)
    {
        for (var i = 0; i < 2; i++)
        {
            await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<BgBurn>(player), MegaCrit.Sts2.Core.Entities.Cards.PileType.Discard, player);
        }
    }
}

// The Corrupt Heart (final boss): Invincible and Beat of Death. Debilitate, then Echo, Blood Shots, and Buff (which
// removes Invincible and strengthens Beat of Death).
public sealed class BgCorruptHeart : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<SoulFysh>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 11 ? 100 : 120;
    public override int MaxInitialHp => MinInitialHp;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgInvinciblePower>(Ascension < 11 ? 50 : 60);
        await Buff<BgBeatOfDeathPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("DEBILITATE", async () =>
        {
            await Debuff<WeakPower>(1, all: true);
            await Debuff<VulnerablePower>(1, all: true);
            foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
            {
                for (var i = 0; i < 5; i++)
                {
                    var card = Ascension < 11 ? (MegaCrit.Sts2.Core.Models.CardModel)CombatState.CreateCard<BgSlimed>(player) : CombatState.CreateCard<BgBurn>(player);
                    await CardPileCmd.AddGeneratedCardToCombat(card, MegaCrit.Sts2.Core.Entities.Cards.PileType.Draw, player,
                        MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Random);
                }
            }
        }, new DebuffIntent(strong: true), new StatusIntent(5));
        Move("ECHO", () => HitAll(5), Attacks(5));
        Move("BLOOD_SHOTS", () => HitAll(2, 3), Attacks(2, 3));
        Move("BUFF", async () =>
        {
            await Buff<StrengthPower>(2);
            var beat = Creature.GetPower<BgBeatOfDeathPower>();
            var max = Ascension < 11 ? 3 : 5;
            var gain = Math.Min(Ascension < 11 ? 1 : 2, max - (beat?.Amount ?? 0));
            if (gain > 0)
            {
                await Buff<BgBeatOfDeathPower>(gain);
            }
            await PowerCmd.Remove<BgInvinciblePower>(Creature);
        }, new BuffIntent());
    }

    protected override string ChooseMove() => Turn == 0 ? "DEBILITATE" : ((Turn - 1) % 3) switch { 0 => "ECHO", 1 => "BLOOD_SHOTS", _ => "BUFF" };
}
