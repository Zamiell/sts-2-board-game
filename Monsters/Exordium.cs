using BoardGame.Cards;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace BoardGame.Monsters;

// Act I enemies. Values are the board game's, taken from LuaViper/StSBoardGameMod.

public sealed class BgCultist : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<DampCultist>();
    public override int MinInitialHp => 9;
    public override int MaxInitialHp => 9;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<RitualPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("INCANTATION", async () =>
        {
            await Hit(1);
            await Buff<StrengthPower>(1);
        }, Attacks(1), new BuffIntent());
        Move("DARK_STRIKE", () => Hit(1), Attacks(1));
    }

    protected override string ChooseMove() => Turn == 0 ? "INCANTATION" : "DARK_STRIKE";
}

// Jaw Worm. Difficulty 0: Act I first encounter; 1: Act I; 2: Act III (behavior from the encounter); 3: Ascension 7.
public sealed class BgJawWorm : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Chomper>();

    public override int MinInitialHp => Difficulty switch { 0 => 8, 3 => 7, _ => 10 };
    public override int MaxInitialHp => MinInitialHp;

    private int ChompDamage => Difficulty is 1 or 2 ? 4 : 3;
    private int ThrashDamage => Difficulty is 1 or 2 ? 3 : 2;
    private int BellowBlock => Difficulty == 2 ? 3 : 2;

    protected override void DefineMoves()
    {
        Move("CHOMP", () => Hit(ChompDamage), Attacks(ChompDamage));
        Move("THRASH", async () =>
        {
            await Hit(ThrashDamage);
            await Block(1);
        }, Attacks(ThrashDamage), new DefendIntent());
        Move("BELLOW", async () =>
        {
            await Cast();
            await Buff<StrengthPower>(1);
            await Block(BellowBlock);
        }, new BuffIntent(), new DefendIntent());
    }

    protected override string ChooseMove()
    {
        var behavior = Difficulty switch { 0 => "sda", 1 => "DAs", 3 => "dsa", _ => Behavior };
        return char.ToLowerInvariant(DieMove(behavior)) switch
        {
            's' => "BELLOW",
            'd' => "THRASH",
            _ => "CHOMP",
        };
    }
}

// Acid Slime (M). Summoned slimes draw their behavior from the Acid Slime summon cards.
public sealed class BgAcidSlimeM : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<LeafSlimeM>();
    public override int MinInitialHp => 5;
    public override int MaxInitialHp => 5;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Behavior == "")
        {
            Behavior = FromPool("CAL", "LCA", "ALC", "LAC");
        }
    }

    protected override void DefineMoves()
    {
        Move("CORROSIVE_SPIT", async () =>
        {
            await Hit(2);
            await GiveStatus<BgDazed>(1);
        }, Attacks(2), new StatusIntent(1));
        Move("LICK", () => Debuff<WeakPower>(1), new DebuffIntent());
        Move("TACKLE", () => Hit(2), Attacks(2));
    }

    protected override string ChooseMove() => DieMove() switch
    {
        'C' => "CORROSIVE_SPIT",
        'L' => "LICK",
        _ => "TACKLE",
    };
}

// Acid Slime (L). From the Large Slime encounter it follows a fixed cycle; split from the Slime Boss it uses the die.
public sealed class BgAcidSlimeL : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<LeafSlimeM>();

    // Difficulty 1: split from the Slime Boss.
    private bool FromBoss => Difficulty == 1;

    public override int MinInitialHp => FromBoss ? 12 : Ascension >= 7 ? 9 : 8;
    public override int MaxInitialHp => MinInitialHp;

    private int SplashDamage => Ascension >= 7 ? 2 : 1;
    private int DazeCount => Ascension >= 7 ? 2 : 1;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (FromBoss)
        {
            Behavior = FromPool("4SW", "SW4", "W4S", "W4S");
            if (Ascension >= 10)
            {
                await Buff<StrengthPower>(1);
            }
        }
    }

    protected override void DefineMoves()
    {
        Move("SPLASH", () => HitAll(SplashDamage), Attacks(SplashDamage));
        Move("SLAM", async () =>
        {
            await Hit(4);
            await GiveStatus<BgDazed>(DazeCount);
        }, Attacks(4), new StatusIntent(DazeCount));
        Move("SUMMON", () => Summon<BgAcidSlimeM>(), new SummonIntent());
        Move("TACKLE", () => Hit(4), Attacks(4));
        Move("CORROSIVE_SPIT", async () =>
        {
            await Hit(3);
            await GiveStatus<BgSlimed>(2);
        }, Attacks(3), new StatusIntent(2));
        Move("SPIT", async () =>
        {
            await Hit(2);
            await GiveStatus<BgDazed>(2);
        }, Attacks(2), new StatusIntent(2));
        Move("LICK", async () =>
        {
            await Hit(1);
            await Debuff<WeakPower>(2);
        }, Attacks(1), new DebuffIntent());
    }

    protected override string ChooseMove()
    {
        if (!FromBoss)
        {
            return (Turn % 3) switch { 0 => "SPLASH", 1 => "SLAM", _ => "SUMMON" };
        }
        return DieMove() switch
        {
            '4' => "TACKLE",
            'X' => "SUMMON",
            'S' => "CORROSIVE_SPIT",
            'D' => "SPIT",
            _ => "LICK",
        };
    }
}

public sealed class BgSpikeSlimeS : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<TwigSlimeS>();
    public override int MinInitialHp => 3;
    public override int MaxInitialHp => 3;
    protected override void DefineMoves() => Move("TACKLE", () => Hit(1), Attacks(1));
    protected override string ChooseMove() => "TACKLE";
}

// Spike Slime (M). Summoned slimes draw their behavior from the Spike Slime summon cards.
public sealed class BgSpikeSlimeM : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<TwigSlimeM>();
    public override int MinInitialHp => 5;
    public override int MaxInitialHp => 5;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Behavior == "")
        {
            Behavior = FromPool("2DV", "V2D", "DV2", "VD2");
        }
    }

    protected override void DefineMoves()
    {
        Move("LICK", async () =>
        {
            await Hit(1);
            await Debuff<VulnerablePower>(1);
        }, Attacks(1), new DebuffIntent());
        Move("CORROSIVE_SPIT", async () =>
        {
            await Hit(1);
            await GiveStatus<BgDazed>(1);
        }, Attacks(1), new StatusIntent(1));
        Move("TACKLE", () => Hit(2), Attacks(2));
    }

    protected override string ChooseMove() => DieMove() switch
    {
        'V' => "LICK",
        'D' => "CORROSIVE_SPIT",
        _ => "TACKLE",
    };
}

public abstract class BgLouse : BgMonster
{
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgCurlUpPower>(2);
    }
}

// Red Louse. Difficulty 1: the harder 3 Louse card (4 HP).
public sealed class BgRedLouse : BgLouse
{
    protected override MonsterModel Look => ModelDb.Monster<ShrinkerBeetle>();
    public override int MinInitialHp => Difficulty == 1 ? 4 : 3;
    public override int MaxInitialHp => MinInitialHp;

    protected override void DefineMoves()
    {
        Move("GROW", () => Buff<StrengthPower>(1), new BuffIntent());
        Move("BITE", () => Hit(1), Attacks(1));
        Move("BIG_BITE", () => Hit(2), Attacks(2));
    }

    protected override string ChooseMove() => DieMove() switch
    {
        'S' => "GROW",
        '1' => "BITE",
        _ => "BIG_BITE",
    };
}

public sealed class BgGreenLouse : BgLouse
{
    protected override MonsterModel Look => ModelDb.Monster<Myte>();
    public override int MinInitialHp => 3;
    public override int MaxInitialHp => 3;

    public override async Task AfterAddedToRoom()
    {
        if (Behavior == "")
        {
            Behavior = FromPool("1W2", "21W");
        }
        await base.AfterAddedToRoom();
    }

    protected override void DefineMoves()
    {
        Move("SPIT_WEB", () => Debuff<WeakPower>(1), new DebuffIntent());
        Move("BITE", () => Hit(1), Attacks(1));
        Move("BIG_BITE", () => Hit(2), Attacks(2));
    }

    protected override string ChooseMove() => DieMove() switch
    {
        'W' => "SPIT_WEB",
        '1' => "BITE",
        _ => "BIG_BITE",
    };
}

// Fungi Beast. Difficulty 1: summoned at Ascension 7 (6 HP, grows by 1).
public sealed class BgFungiBeast : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Flyconid>();
    public override int MinInitialHp => Difficulty == 1 ? 6 : 5;
    public override int MaxInitialHp => MinInitialHp;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgSporeCloudPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("BITE", () => Hit(2), Attacks(2));
        Move("GROW", () => Buff<StrengthPower>(Difficulty == 1 ? 1 : 2), new BuffIntent());
        Move("POWER_UP", async () =>
        {
            await Hit(1);
            await Buff<StrengthPower>(1);
        }, Attacks(1), new BuffIntent());
    }

    protected override string ChooseMove() => DieMove() switch
    {
        '2' => "BITE",
        'S' or 's' => "GROW",
        _ => "POWER_UP",
    };
}

public abstract class BgGremlin : BgMonster
{
    // A gremlin team's leader brings two random gremlins with it.
    public bool Leader { get; set; }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Leader)
        {
            Leader = false;
            for (var i = 0; i < 2; i++)
            {
                switch (CombatState.RunState.Rng.MonsterAi.NextInt(4))
                {
                    case 0: await Summon<BgGremlinAngry>(); break;
                    case 1: await Summon<BgGremlinSneaky>(); break;
                    case 2: await Summon<BgGremlinFat>(); break;
                    default: await Summon<BgGremlinWizard>(); break;
                }
            }
        }
    }
}

public sealed class BgGremlinAngry : BgGremlin
{
    protected override MonsterModel Look => ModelDb.Monster<GremlinMerc>();
    public override int MinInitialHp => 4;
    public override int MaxInitialHp => 4;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgAngryPower>(1);
    }

    protected override void DefineMoves() => Move("SCRATCH", () => Hit(1), Attacks(1));
    protected override string ChooseMove() => "SCRATCH";
}

public sealed class BgGremlinSneaky : BgGremlin
{
    protected override MonsterModel Look => ModelDb.Monster<SneakyGremlin>();
    public override int MinInitialHp => 2;
    public override int MaxInitialHp => 2;
    protected override void DefineMoves() => Move("PUNCTURE", () => Hit(2), Attacks(2));
    protected override string ChooseMove() => "PUNCTURE";
}

public sealed class BgGremlinFat : BgGremlin
{
    protected override MonsterModel Look => ModelDb.Monster<FatGremlin>();
    public override int MinInitialHp => 3;
    public override int MaxInitialHp => 3;
    protected override void DefineMoves() => Move("SMASH", () => Hit(1), Attacks(1));
    protected override string ChooseMove() => "SMASH";
}

// Gremlin Wizard: charges for a turn, then casts 3 damage every turn.
public sealed class BgGremlinWizard : BgGremlin
{
    protected override MonsterModel Look => ModelDb.Monster<Inklet>();
    public override int MinInitialHp => 4;
    public override int MaxInitialHp => 4;

    protected override void DefineMoves()
    {
        Move("CHARGING", () => Cast(), new UnknownIntent());
        Move("ULTIMATE_BLAST", () => Hit(3), Attacks(3));
    }

    protected override string ChooseMove() => Turn == 0 ? "CHARGING" : "ULTIMATE_BLAST";
}

// Blue Slaver: d/D stab and Dazed, W rake and Weak, 2/3 attack. Difficulty 1: the Taskmaster's slaver (City pool).
public sealed class BgBlueSlaver : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<AxeRubyRaider>();
    public override int MinInitialHp => 10;
    public override int MaxInitialHp => 10;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Difficulty == 1)
        {
            Behavior = FromPool("Wd3", "W3d", "dW3", "3Wd");
        }
    }

    protected override void DefineMoves()
    {
        Move("STAB", async () =>
        {
            await Hit(2);
            await GiveStatus<BgDazed>(1);
        }, Attacks(2), new StatusIntent(1));
        Move("STAB_HARD", async () =>
        {
            await Hit(3);
            await GiveStatus<BgDazed>(1);
        }, Attacks(3), new StatusIntent(1));
        Move("RAKE", async () =>
        {
            await Hit(2);
            await Debuff<WeakPower>(1);
        }, Attacks(2), new DebuffIntent());
        Move("SWEEP", () => Hit(2), Attacks(2));
        Move("SWEEP_HARD", () => Hit(3), Attacks(3));
    }

    protected override string ChooseMove() => DieMove(Behavior == "" ? "W2d" : Behavior) switch
    {
        'd' => "STAB",
        'D' => "STAB_HARD",
        'W' => "RAKE",
        '2' => "SWEEP",
        _ => "SWEEP_HARD",
    };
}

// Red Slaver: D entangle (damage and Dazed), V scrape (damage and Vulnerable), 3 stab. Difficulty 1: City pool.
public sealed class BgRedSlaver : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<BruteRubyRaider>();
    public override int MinInitialHp => 10;
    public override int MaxInitialHp => 10;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Difficulty == 1)
        {
            Behavior = FromPool("3VD", "3DV", "DV3", "V3D");
        }
    }

    protected override void DefineMoves()
    {
        Move("ENTANGLE", async () =>
        {
            await GiveStatus<BgDazed>(1);
            await Hit(2);
        }, Attacks(2), new StatusIntent(1));
        Move("SCRAPE", async () =>
        {
            await Hit(2);
            await Debuff<VulnerablePower>(1);
        }, Attacks(2), new DebuffIntent());
        Move("STAB", () => Hit(3), Attacks(3));
    }

    protected override string ChooseMove() => DieMove(Behavior == "" ? "DV3" : Behavior) switch
    {
        'D' => "ENTANGLE",
        'V' => "SCRAPE",
        _ => "STAB",
    };
}

// Looter: swipe, lunge with Block, then escape with 2 Gold. Difficulty 1: the City's harder Looter (10 HP).
public sealed class BgLooter : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<ThievingHopper>();
    public override int MinInitialHp => Difficulty == 1 ? 10 : Ascension >= 7 ? 7 : 9;
    public override int MaxInitialHp => MinInitialHp;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgThieveryPower>(2);
    }

    protected override void DefineMoves()
    {
        Move("MUG", () => Hit(2), Attacks(2));
        Move("LUNGE", async () =>
        {
            await Hit(3);
            await Block(1);
        }, Attacks(3), new DefendIntent());
        Move("ESCAPE", () => Escape(2), new EscapeIntent());
    }

    protected override string ChooseMove() => Turn switch { 0 => "MUG", 1 => "LUNGE", _ => "ESCAPE" };
}

// Gremlin Nob (elite): bellows to gain Enrage, then Skull Bashes (gaining Strength from Ascension 1).
public sealed class BgGremlinNob : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Mawler>();
    public override int MinInitialHp => Ascension == 0 ? 14 : Ascension < 12 ? 17 : 19;
    public override int MaxInitialHp => MinInitialHp;

    protected override void DefineMoves()
    {
        Move("BELLOW", async () =>
        {
            await Cast();
            await Buff<BgEnragePower>(1);
            if (Ascension >= 12)
            {
                await Hit(1);
            }
        }, Ascension >= 12 ? [Attacks(1), new BuffIntent()] : [new BuffIntent()]);
        Move("SKULL_BASH", async () =>
        {
            await Hit(3);
            if (Ascension >= 1)
            {
                await Buff<StrengthPower>(1);
            }
        }, Ascension >= 1 ? [Attacks(3), new BuffIntent()] : [Attacks(3)]);
    }

    protected override string ChooseMove() => Turn == 0 ? "BELLOW" : "SKULL_BASH";
}

// Lagavulin (elite). Ascension 0: sleeps one turn (or wakes stunned if hurt), then attacks twice and Siphons Soul.
// Ascension 1+: awake from the start, cycling Siphon Soul, attack, attack with Strength (Ascension 12 reorders them).
public sealed class BgLagavulin : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<LagavulinMatriarch>();
    public override int MinInitialHp => Ascension < 12 ? 22 : 24;
    public override int MaxInitialHp => MinInitialHp;

    private bool _woken;
    private int _awakeTurns;

    protected override void DefineMoves()
    {
        Move("SLEEP", () => Task.CompletedTask, new SleepIntent());
        Move("STUNNED", () => Task.CompletedTask, new StunIntent());
        Move("ATTACK", async () =>
        {
            await Hit(4);
            _awakeTurns++;
        }, Attacks(4));
        Move("ATTACK_BUFF", async () =>
        {
            await Hit(4);
            await Buff<StrengthPower>(1);
            _awakeTurns++;
        }, Attacks(4), new BuffIntent());
        Move("SIPHON_SOUL", async () =>
        {
            await Debuff<WeakPower>(2);
            if (Ascension == 0)
            {
                await Buff<StrengthPower>(1);
            }
            _awakeTurns++;
        }, Ascension == 0 ? [new DebuffIntent(strong: true), new BuffIntent()] : [new DebuffIntent(strong: true)]);
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        // Hurting it while it sleeps wakes it up, stunned.
        if (creature == Creature && delta < 0 && Ascension == 0 && Turn == 0 && !_woken)
        {
            _woken = true;
            SetMoveImmediate(SelectMove(), forceTransition: true);
        }
        return Task.CompletedTask;
    }

    protected override string ChooseMove()
    {
        if (Ascension == 0)
        {
            if (Turn == 0)
            {
                return _woken ? "STUNNED" : "SLEEP";
            }
            return (_awakeTurns % 3) == 2 ? "SIPHON_SOUL" : "ATTACK";
        }
        return Ascension < 12
            ? (_awakeTurns % 3) switch { 0 => "SIPHON_SOUL", 1 => "ATTACK", _ => "ATTACK_BUFF" }
            : (_awakeTurns % 3) switch { 0 => "SIPHON_SOUL", 1 => "ATTACK_BUFF", _ => "ATTACK" };
    }
}

// Sentry (elite). Behaviors use halves of the die: 1-3 and 4-6. From Ascension 1 the "2D" Sentry follows a fixed cycle.
public sealed class BgSentry : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Guardbot>();

    private bool Cycles => Behavior == "2D" && Ascension >= 1;

    public override int MinInitialHp => Behavior == "D3" || (Behavior == "2D" && Ascension == 0) ? 7 : Behavior == "2D" && Ascension >= 12 ? 9 : 8;
    public override int MaxInitialHp => MinInitialHp;

    protected override void DefineMoves()
    {
        Move("BOLT", () => GiveStatus<BgDazed>(1), new StatusIntent(1));
        Move("BEAM", () => Hit(2), Attacks(2));
        Move("BEAM_HARD", () => Hit(3), Attacks(3));
        Move("DOUBLE_BOLT", () => GiveStatus<BgDazed>(2), new StatusIntent(2));
    }

    protected override string ChooseMove()
    {
        if (Cycles)
        {
            return Turn == 0 ? "BOLT" : Turn % 2 == 1 ? "BEAM" : "DOUBLE_BOLT";
        }
        var move = Behavior[(Die - 1) / 3];
        return move switch { 'D' => "BOLT", '2' => "BEAM", _ => "BEAM_HARD" };
    }
}

// Hexaghost (boss): a fixed six-turn cycle of Sear, Tackle, burns, Strengthen, Sear, and Inferno.
public sealed class BgHexaghost : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<SoulNexus>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 36 : 38;
    public override int MaxInitialHp => MinInitialHp;

    private int FirstSearBurns => Ascension < 10 ? 1 : 2;
    private int StrengthenDamage => Ascension < 10 ? 3 : 4;
    private int SecondSearDamage => Ascension < 10 ? 2 : 3;

    protected override void DefineMoves()
    {
        Move("SEAR", async () =>
        {
            await Hit(1);
            await GiveStatus<BgBurn>(FirstSearBurns);
        }, Attacks(1), new StatusIntent(FirstSearBurns));
        Move("TACKLE", async () =>
        {
            await Hit(2, 2);
            await GiveStatus<BgBurn>(1);
        }, Attacks(2, 2), new StatusIntent(1));
        Move("BURN", () => GiveStatus<BgBurn>(2), new StatusIntent(2));
        Move("STRENGTHEN", async () =>
        {
            await Block(5);
            await Hit(StrengthenDamage);
        }, Attacks(StrengthenDamage), new DefendIntent());
        Move("SEAR_HARD", async () =>
        {
            await Hit(SecondSearDamage);
            await GiveStatus<BgBurn>(1);
        }, Attacks(SecondSearDamage), new StatusIntent(1));
        Move("INFERNO", async () =>
        {
            await Hit(3, 2);
            await GiveStatus<BgBurn>(2);
            await Buff<StrengthPower>(1);
        }, Attacks(3, 2), new StatusIntent(2), new BuffIntent());
    }

    protected override string ChooseMove() => (Turn % 6) switch
    {
        0 => "SEAR",
        1 => "TACKLE",
        2 => "BURN",
        3 => "STRENGTHEN",
        4 => "SEAR_HARD",
        _ => "INFERNO",
    };
}

// Slime Boss: Goop Spray, Tackle, Slam, repeat. When it would die it splits into three slimes.
public sealed class BgSlimeBoss : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<TheInsatiable>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 22 : 23;
    public override int MaxInitialHp => MinInitialHp;

    private int GoopCount => Ascension < 10 ? 3 : 4;
    private bool _split;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgSplitPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("GOOP_SPRAY", () => GiveStatus<BgSlimed>(GoopCount), new StatusIntent(GoopCount));
        Move("TACKLE", async () =>
        {
            await Hit(3);
            await GiveStatus<BgSlimed>(2);
        }, Attacks(3), new StatusIntent(2));
        Move("SLAM", () => Hit(6), Attacks(6));
    }

    protected override string ChooseMove() => (Turn % 3) switch { 0 => "GOOP_SPRAY", 1 => "TACKLE", _ => "SLAM" };

    public override bool ShouldDie(Creature creature) => creature != Creature || _split;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Creature || _split)
        {
            return;
        }
        _split = true;
        await Summon<BgAcidSlimeL>(difficulty: 1);
        await Summon<BgAcidSlimeM>();
        await Summon<BgSpikeSlimeM>();
        await CreatureCmd.Kill(Creature, force: true);
    }
}

// The Guardian (boss). Offensive Mode: Charge Up (Whirlwind and Block), then Fierce Bash. When its Block is broken it
// shifts to Defensive Mode: Close Up (Sharp Hide), Roll Attack, Twin Slam (Strength), then back to Offensive Mode.
public sealed class BgTheGuardian : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Crusher>();
    public override bool IsBoss => true;
    public override int MinInitialHp => 40;
    public override int MaxInitialHp => 40;

    private int FierceBashDamage => Ascension < 10 ? 6 : 7;
    private int RollDamage => Ascension < 10 ? 2 : 3;
    private int ChargeBlock => Ascension < 10 ? 5 : 6;

    // Position in the current mode's cycle; defensive mode is a separate cycle.
    private int _step;
    private bool _defensive;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgModeShiftPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("WHIRLWIND", async () =>
        {
            await Hit(2);
            await Block(ChargeBlock);
            _step++;
        }, Attacks(2), new DefendIntent());
        Move("FIERCE_BASH", async () =>
        {
            await Hit(FierceBashDamage);
            _step++;
        }, Attacks(FierceBashDamage));
        Move("CLOSE_UP", async () =>
        {
            await Buff<ThornsPower>(1);
            _step++;
        }, new BuffIntent());
        Move("ROLL_ATTACK", async () =>
        {
            await Hit(RollDamage);
            _step++;
        }, Attacks(RollDamage));
        Move("TWIN_SLAM", async () =>
        {
            await Hit(4);
            await Buff<StrengthPower>(1);
            await PowerCmd.Remove<ThornsPower>(Creature);
            await Buff<BgModeShiftPower>(1);
            _defensive = false;
            _step = 0;
        }, Attacks(4), new BuffIntent());
    }

    protected override string ChooseMove() => _defensive
        ? _step switch { 0 => "CLOSE_UP", 1 => "ROLL_ATTACK", _ => "TWIN_SLAM" }
        : _step % 2 == 0 ? "WHIRLWIND" : "FIERCE_BASH";

    public override async Task AfterBlockBroken(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
    {
        if (target != Creature || _defensive || !Creature.HasPower<BgModeShiftPower>())
        {
            return;
        }
        await PowerCmd.Remove<BgModeShiftPower>(Creature);
        _defensive = true;
        _step = 0;
        SetMoveImmediate(SelectMove(), forceTransition: true);
    }
}
