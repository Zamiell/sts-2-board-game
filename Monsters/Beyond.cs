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

// Act III enemies. Values are the board game's, taken from LuaViper/StSBoardGameMod.

// Darkling: S hardens (Block, Strength), 2 nips twice, 3 chomps and heals. While another Darkling lives, a slain
// Darkling regrows at half HP at the start of the next player turn.
public sealed class BgDarkling : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Exoskeleton>();
    public override int MinInitialHp => 8;
    public override int MaxInitialHp => 8;

    private bool _regrowing;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgRegrowPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("HARDEN", async () =>
        {
            await Block(3);
            await Buff<StrengthPower>(1);
        }, new DefendIntent(), new BuffIntent());
        Move("NIP", () => Hit(2, 2), Attacks(2, 2));
        Move("CHOMP", async () =>
        {
            await Hit(3);
            await CreatureCmd.Heal(Creature, 2);
        }, Attacks(3), new HealIntent());
        Move("REGROW", () => Task.CompletedTask, new UnknownIntent());
    }

    protected override string ChooseMove() => _regrowing ? "REGROW" : DieMove() switch { 'S' => "HARDEN", '2' => "NIP", _ => "CHOMP" };

    private bool OtherDarklingsAlive => CombatState.Enemies.Any(e => e != Creature && e.IsAlive && e.Monster is BgDarkling { _regrowing: false });

    public override bool ShouldDie(Creature creature) => creature != Creature || _regrowing || !OtherDarklingsAlive;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Creature)
        {
            return;
        }
        _regrowing = true;
        await CreatureCmd.SetCurrentHp(Creature, 1);
        await CreatureCmd.LoseBlock(new ThrowingPlayerChoiceContext(), Creature, Creature.Block, null);
        SetMoveImmediate(SelectMove(), forceTransition: true);
    }

    // A regrowing Darkling can't be hit or targeted.
    public override bool ShouldAllowHitting(Creature creature) => creature != Creature || !_regrowing;

    public override bool ShouldAllowTargeting(Creature target) => target != Creature || !_regrowing;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (_regrowing && player == CombatState.Players[0])
        {
            _regrowing = false;
            await CreatureCmd.SetCurrentHp(Creature, MaxInitialHp / 2);
            RefreshMoveForDie();
        }
    }

    // If every other Darkling falls, a regrowing one dies for good.
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (_regrowing && creature != Creature && creature.Monster is BgDarkling && !OtherDarklingsAlive)
        {
            _regrowing = false;
            await CreatureCmd.Kill(Creature, force: true);
        }
    }
}

// Orb Walker: halves of the die. 2 claws and gains Strength, 3 lasers and Dazes. Also gains Strength every round.
public sealed class BgOrbWalker : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<GlobeHead>();
    public override int MinInitialHp => 22;
    public override int MaxInitialHp => 22;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgOrbWalkerPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("CLAW", async () =>
        {
            await Hit(2);
            await Buff<StrengthPower>(1);
        }, Attacks(2), new BuffIntent());
        Move("LASER", async () =>
        {
            await Hit(3);
            await GiveStatus<BgDazed>(1);
        }, Attacks(3), new StatusIntent(1));
    }

    protected override string ChooseMove() => Behavior[(Die - 1) / 3] == '2' ? "CLAW" : "LASER";
}

// Spiker: halves of the die. 2 attacks, T grows its spikes. Spikes damage whoever attacks it.
public sealed class BgSpiker : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Stabbot>();
    public override int MinInitialHp => 10;
    public override int MaxInitialHp => 10;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Behavior == "")
        {
            Behavior = FromPool("2T", "T2");
        }
        await Buff<BgSpikerPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("CUT", () => Hit(2), Attacks(2));
        Move("SPIKE", () => Buff<BgSpikerPower>(1), new BuffIntent());
    }

    protected override string ChooseMove() => Behavior[(Die - 1) / 3] == '2' ? "CUT" : "SPIKE";
}

// Repulsor: halves of the die. D bashes and Dazes twice, 3 attacks.
public sealed class BgRepulsor : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Noisebot>();
    public override int MinInitialHp => 7;
    public override int MaxInitialHp => 7;

    protected override void DefineMoves()
    {
        Move("REPULSE", async () =>
        {
            await Hit(1);
            await GiveStatus<BgDazed>(2);
        }, Attacks(1), new StatusIntent(2));
        Move("BASH", () => Hit(3), Attacks(3));
    }

    protected override string ChooseMove() => Behavior[(Die - 1) / 3] == 'D' ? "REPULSE" : "BASH";
}

// Exploder: attacks, waits, then explodes for 10 damage and dies.
public sealed class BgExploder : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<GasBomb>();
    public override int MinInitialHp => 8;
    public override int MaxInitialHp => 8;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgExplosivePower>(3);
    }

    protected override void DefineMoves()
    {
        Move("SLAM", async () =>
        {
            await Hit(3);
            await PowerCmd.Decrement(Creature.GetPower<BgExplosivePower>()!);
        }, Attacks(3));
        Move("PRIMED", async () => await PowerCmd.Decrement(Creature.GetPower<BgExplosivePower>()!), new StunIntent());
        Move("EXPLODE", async () =>
        {
            await HitAll(10);
            await CreatureCmd.Kill(Creature, force: true);
        }, Attacks(10));
    }

    protected override string ChooseMove() => Turn switch { 0 => "SLAM", 1 => "PRIMED", _ => "EXPLODE" };
}

// The Maw: Roar (Vulnerable), then Nom (2 hits), Drool (Strength), Slam, repeating.
public sealed class BgMaw : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<TerrorEel>();
    public override int MinInitialHp => 28;
    public override int MaxInitialHp => 28;

    private int SlamDamage => Ascension < 7 ? 6 : 8;

    protected override void DefineMoves()
    {
        Move("ROAR", async () =>
        {
            if (Ascension >= 7)
            {
                await Hit(2);
            }
            await Debuff<VulnerablePower>(1);
        }, Ascension >= 7 ? [Attacks(2), new DebuffIntent()] : [new DebuffIntent(strong: true)]);
        Move("NOM", () => Hit(2, 2), Attacks(2, 2));
        Move("DROOL", () => Buff<StrengthPower>(2), new BuffIntent());
        Move("SLAM", () => Hit(SlamDamage), Attacks(SlamDamage));
    }

    protected override string ChooseMove() => Turn == 0 ? "ROAR" : ((Turn - 1) % 3) switch { 0 => "NOM", 1 => "DROOL", _ => "SLAM" };
}

// Spire Growth: stunned on its first turn, then the die halves: 1-3 hits 2 and 3, 4-6 hits 4 and 2.
public sealed class BgSpireGrowth : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<SlitheringStrangler>();
    public override int MinInitialHp => 28;
    public override int MaxInitialHp => 28;

    protected override void DefineMoves()
    {
        Move("WAKING", () => Task.CompletedTask, new StunIntent());
        Move("QUICK_TACKLE", async () =>
        {
            await Hit(2);
            await Hit(3);
        }, Attacks(2), Attacks(3));
        Move("SMASH", async () =>
        {
            await Hit(4);
            await Hit(2);
        }, Attacks(4), Attacks(2));
    }

    protected override string ChooseMove() => Turn == 0 ? "WAKING" : Die <= 3 ? "QUICK_TACKLE" : "SMASH";
}

// Transient: attacks for 3 more each turn and fades after 4 turns. When it loses HP, its attacker gains that much Block.
public sealed class BgTransient : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<LivingFog>();
    public override int MinInitialHp => 99;
    public override int MaxInitialHp => 99;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgFadingPower>(4);
        await Buff<BgShiftingPower>(1);
    }

    private int Damage => 6 + 3 * Turn;

    protected override void DefineMoves() => Move("ATTACK", async () =>
    {
        await Hit(Damage);
        var fading = Creature.GetPower<BgFadingPower>();
        if (fading is { Amount: <= 1 })
        {
            await CreatureCmd.Kill(Creature, force: true);
        }
        else if (fading != null)
        {
            await PowerCmd.Decrement(fading);
        }
    }, new SingleAttackIntent(() => Damage));

    protected override string ChooseMove() => "ATTACK";
}

// Writhing Mass: one action for each face of the die. Taking attack damage rerolls the die.
public sealed class BgWrithingMass : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<InfestedPrism>();
    public override int MinInitialHp => 27;
    public override int MaxInitialHp => 27;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgReactivePower>(1);
    }

    protected override void DefineMoves()
    {
        Move("IMPLANT", () => Debuff<VulnerablePower>(1), new DebuffIntent(strong: true));
        Move("WITHER", () => GiveStatus<BgDazed>(2), new StatusIntent(2));
        Move("MULTI_STRIKE", () => Hit(3, 2), Attacks(3, 2));
        Move("FLAIL", async () =>
        {
            await Hit(5);
            await Block(5);
        }, Attacks(5), new DefendIntent());
        Move("WALLOP", () => Hit(7), Attacks(7));
        Move("STRIKE", async () =>
        {
            await Hit(4);
            await Debuff<WeakPower>(1);
        }, Attacks(4), new DebuffIntent());
    }

    protected override string ChooseMove() => Die switch
    {
        1 => "IMPLANT",
        2 => "WITHER",
        3 => "MULTI_STRIKE",
        4 => "FLAIL",
        5 => "WALLOP",
        _ => "STRIKE",
    };
}

// Giant Head (elite): Slow. Counts down for three turns (attacking on the second from Ascension 1), then "It Is
// Time" every turn, gaining Strength.
public sealed class BgGiantHead : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<TheObscura>();
    public override int MinInitialHp => Ascension < 1 ? 80 : Ascension < 12 ? 85 : 90;
    public override int MaxInitialHp => MinInitialHp;

    private int DeathDamage => Ascension < 1 ? 7 : Ascension < 12 ? 8 : 9;
    private int GlareDamage => Ascension < 12 ? 5 : 6;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgSlowPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("COUNT", () => Task.CompletedTask, new UnknownIntent());
        Move("GLARE", () => Hit(GlareDamage), Attacks(GlareDamage));
        Move("IT_IS_TIME", async () =>
        {
            await Hit(DeathDamage);
            await Buff<StrengthPower>(1);
        }, Attacks(DeathDamage), new BuffIntent());
    }

    protected override string ChooseMove() => Turn switch
    {
        0 or 2 => "COUNT",
        1 => Ascension >= 1 ? "GLARE" : "COUNT",
        _ => "IT_IS_TIME",
    };
}

// Nemesis (elite): Intangible every other turn. Debilitate (Burns), then Attack, then alternates Scythe-like
// multi-hits (with a Burn) and heavy attacks.
public sealed class BgNemesis : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Vantom>();
    public override int MinInitialHp => Ascension < 1 ? 30 : Ascension < 12 ? 35 : 36;
    public override int MaxInitialHp => MinInitialHp;

    private int Burns => Ascension < 1 ? 4 : 5;
    private int SecondaryBurns => Ascension < 12 ? 1 : 2;
    private int Hits => Ascension < 12 ? 2 : 3;
    private int AttackDamage => Ascension < 1 ? 4 : Ascension < 12 ? 5 : 6;
    private int HeavyDamage => Ascension < 1 ? 7 : 8;

    private bool _intangibleNext = true;

    private async Task ToggleIntangible()
    {
        if (_intangibleNext)
        {
            await Buff<BgIntangiblePower>(1);
        }
        else
        {
            await PowerCmd.Remove<BgIntangiblePower>(Creature);
        }
        _intangibleNext = !_intangibleNext;
    }

    protected override void DefineMoves()
    {
        Move("DEBILITATE", async () =>
        {
            await GiveStatus<BgBurn>(Burns);
            await ToggleIntangible();
        }, new StatusIntent(Burns));
        Move("ATTACK", async () =>
        {
            await Hit(AttackDamage);
            await ToggleIntangible();
        }, Attacks(AttackDamage));
        Move("SCYTHE", async () =>
        {
            await Hit(2, Hits);
            await GiveStatus<BgBurn>(SecondaryBurns);
            await ToggleIntangible();
        }, Attacks(2, Hits), new StatusIntent(SecondaryBurns));
        Move("HEAVY", async () =>
        {
            await Hit(HeavyDamage);
            await ToggleIntangible();
        }, Attacks(HeavyDamage));
    }

    protected override string ChooseMove() => Turn switch
    {
        0 => "DEBILITATE",
        1 => "ATTACK",
        _ => Turn % 2 == 0 ? "SCYTHE" : "HEAVY",
    };
}

// Reptomancer (elite): summons Snake Daggers, then strikes; strikes harder while a dagger lives, else summons again.
public sealed class BgReptomancer : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Entomancer>();
    public override int MinInitialHp => Ascension < 1 ? 35 : Ascension < 12 ? 40 : 42;
    public override int MaxInitialHp => MinInitialHp;

    private int DazeCount => Ascension < 12 ? 1 : 2;

    // 0: summon; 1: first strike after summoning; 2: second strike.
    private int _step;

    private bool DaggersAlive => CombatState.Enemies.Any(e => e.IsAlive && e.Monster is BgSnakeDagger);

    protected override void DefineMoves()
    {
        Move("SUMMON", async () =>
        {
            var daggers = CombatState.Enemies.Count(e => e.IsAlive && e.Monster is BgSnakeDagger);
            for (var i = daggers; i < daggers + 2 && i < 4; i++)
            {
                await Summon<BgSnakeDagger>();
            }
            _step = 1;
        }, new SummonIntent());
        Move("SNAKE_STRIKE", async () =>
        {
            await Hit(3, 2);
            _step = DaggersAlive ? 2 : 0;
        }, Attacks(3, 2));
        Move("BIG_BITE", async () =>
        {
            await Hit(7);
            _step = 0;
        }, Attacks(7));
        Move("VENOM_STRIKE", async () =>
        {
            await Hit(7);
            await GiveStatus<BgDazed>(DazeCount);
            _step = DaggersAlive ? 2 : 0;
        }, Attacks(7), new StatusIntent(DazeCount));
        Move("DOUBLE_STRIKE", async () =>
        {
            await Hit(4, 2);
            await Buff<StrengthPower>(1);
            _step = 0;
        }, Attacks(4, 2), new BuffIntent());
    }

    protected override string ChooseMove() => (_step, Ascension >= 1) switch
    {
        (0, _) => "SUMMON",
        (1, false) => "SNAKE_STRIKE",
        (1, true) => "VENOM_STRIKE",
        (_, false) => "BIG_BITE",
        _ => "DOUBLE_STRIKE",
    };
}

// Snake Dagger: stabs, then strikes for 5 and dies.
public sealed class BgSnakeDagger : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Parafright>();
    public override int MinInitialHp => 5;
    public override int MaxInitialHp => 5;

    protected override void DefineMoves()
    {
        Move("STAB", () => Hit(2), Attacks(2));
        Move("SACRIFICE", async () =>
        {
            await Hit(5);
            await CreatureCmd.Kill(Creature, force: true);
        }, Attacks(5));
    }

    protected override string ChooseMove() => Turn == 0 ? "STAB" : "SACRIFICE";
}

// The Awakened One (boss), with two Cultists. Phase 1 (Curiosity): Slash, Soul Strike, double hit. When first slain it
// is reborn at full HP: Rebirth, then Dark Echo, Sludge (Voids), and a Strength-gaining double hit.
public sealed class BgAwakenedOne : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<TheForgotten>();
    public override bool IsBoss => true;
    public override int MinInitialHp => 50;
    public override int MaxInitialHp => 50;

    private bool _awakened;
    private int _phase2Turn;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgCuriosityPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("SLASH", () => Hit(3), Attacks(3));
        var soulStrike = Ascension < 10 ? 5 : 6;
        Move("SOUL_STRIKE", () => Hit(soulStrike), Attacks(soulStrike));
        Move("DOUBLE_SLASH", () => Hit(2, 2), Attacks(2, 2));
        Move("REBIRTH", async () =>
        {
            if (Ascension >= 10 && BgRules.For(CombatState.RunState) is { } rules)
            {
                var powers = CombatState.Players.Sum(rules.PowersPlayed);
                if (powers > 0)
                {
                    await Buff<BgUncappedStrengthPower>(powers);
                }
            }
            _phase2Turn = 1;
        }, new BuffIntent());
        var darkEcho = Ascension < 10 ? 7 : 6;
        Move("DARK_ECHO", async () =>
        {
            await Hit(darkEcho);
            _phase2Turn++;
        }, Attacks(darkEcho));
        Move("SLUDGE", async () =>
        {
            await Hit(4);
            await GiveStatus<BgVoid>(2);
            _phase2Turn++;
        }, Attacks(4), new StatusIntent(2));
        Move("TACKLE", async () =>
        {
            await Hit(3, 2);
            await Buff<StrengthPower>(1);
            _phase2Turn++;
        }, Attacks(3, 2), new BuffIntent());
    }

    protected override string ChooseMove()
    {
        if (!_awakened)
        {
            return (Turn % 3) switch { 0 => "SLASH", 1 => "SOUL_STRIKE", _ => "DOUBLE_SLASH" };
        }
        return _phase2Turn == 0 ? "REBIRTH" : ((_phase2Turn - 1) % 3) switch { 0 => "DARK_ECHO", 1 => "SLUDGE", _ => "TACKLE" };
    }

    public override bool ShouldDie(Creature creature) => creature != Creature || _awakened;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Creature || _awakened)
        {
            return;
        }
        _awakened = true;
        foreach (var power in Creature.Powers.ToList())
        {
            await PowerCmd.Remove(power);
        }
        await CreatureCmd.Heal(Creature, Creature.MaxHp);
        SetMoveImmediate(SelectMove(), forceTransition: true);
    }
}

// Time Eater (boss): Time Warp limits how many cards you can play each turn. Reverberate, Ripple (Slimed), Head Slam;
// when first slain it heals and gains Strength (Haste).
public sealed class BgTimeEater : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Aeonglass>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 60 : 64;
    public override int MaxInitialHp => MinInitialHp;

    private bool _hasted;

    private async Task Warp(int limit)
    {
        foreach (var player in CombatState.PlayerCreatures.Where(p => p.IsAlive).ToList())
        {
            await BgTimeWarpPower.Set(player, limit);
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Warp(5);
    }

    protected override void DefineMoves()
    {
        var reverb = Ascension < 10 ? 2 : 3;
        Move("REVERBERATE", async () =>
        {
            await Hit(reverb, 2);
            await Warp(4);
        }, Attacks(reverb, 2), new DebuffIntent());
        Move("RIPPLE", async () =>
        {
            await GiveStatus<BgSlimed>(3);
            await Warp(3);
        }, new StatusIntent(3), new DebuffIntent());
        Move("HEAD_SLAM", async () =>
        {
            await Hit(6);
            await GiveStatus<BgDazed>(1);
            await Buff<StrengthPower>(1);
            await Warp(5);
        }, Attacks(6), new StatusIntent(1), new BuffIntent());
    }

    protected override string ChooseMove() => (Turn % 3) switch { 0 => "REVERBERATE", 1 => "RIPPLE", _ => "HEAD_SLAM" };

    public override bool ShouldDie(Creature creature) => creature != Creature || _hasted;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Creature || _hasted)
        {
            return;
        }
        _hasted = true;
        await CreatureCmd.Heal(Creature, Ascension < 10 ? 30 : 32);
        await Buff<StrengthPower>(1);
        await PowerCmd.Remove<VulnerablePower>(Creature);
        await PowerCmd.Remove<WeakPower>(Creature);
    }
}

// Deca (boss, with Donu): alternates a double beam and Dazed plus Slimed.
public sealed class BgDeca : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<PunchConstruct>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 50 : 55;
    public override int MaxInitialHp => MinInitialHp;

    private int SlimedCount => Ascension < 10 ? 1 : 2;

    protected override void DefineMoves()
    {
        Move("BEAM", () => Hit(3, 2), Attacks(3, 2));
        Move("SQUARE", async () =>
        {
            await GiveStatus<BgDazed>(1);
            await GiveStatus<BgSlimed>(SlimedCount);
        }, new StatusIntent(1 + SlimedCount));
    }

    protected override string ChooseMove() => Turn % 2 == 0 ? "BEAM" : "SQUARE";
}

// Donu (boss, with Deca): alternates Circle of Power (Strength for every enemy) and a double beam.
public sealed class BgDonu : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<OwlMagistrate>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 50 : 55;
    public override int MaxInitialHp => MinInitialHp;

    protected override void DefineMoves()
    {
        Move("CIRCLE_OF_POWER", async () =>
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Creature, null);
            }
        }, new BuffIntent());
        Move("BEAM", () => Hit(3, 2), Attacks(3, 2));
    }

    protected override string ChooseMove() => Turn % 2 == 0 ? "CIRCLE_OF_POWER" : "BEAM";
}
