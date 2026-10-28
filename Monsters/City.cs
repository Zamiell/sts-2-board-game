using BoardGame.Cards;
using BoardGame.Core;
using BoardGame.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Monsters;

// Act II enemies. Values are the board game's, taken from LuaViper/StSBoardGameMod.

// Byrd: flies (attacks deal at most 1 damage to it). 1 pecks twice, 3 swoops, S grows. Difficulty 1: the 5-HP Byrd.
public sealed class BgByrd : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Byrdpip>();
    public override int MinInitialHp => Difficulty == 1 ? 5 : 4;
    public override int MaxInitialHp => MinInitialHp;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Behavior == "")
        {
            Behavior = FromPool("S13", "S31", "31S");
        }
        await Buff<BgFlightPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("PECK", () => Hit(1, 2), Attacks(1, 2));
        Move("SWOOP", () => Hit(3), Attacks(3));
        Move("CAW", () => Buff<StrengthPower>(1), new BuffIntent());
    }

    protected override string ChooseMove() => DieMove() switch { '1' => "PECK", '3' => "SWOOP", _ => "CAW" };
}

// Centurion: halves of the die. 3 slashes, B shields its Mystic. When the Mystic dies it gains Strength and only attacks.
public sealed class BgCenturion : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<FlailKnight>();
    public override int MinInitialHp => 15;
    public override int MaxInitialHp => 15;

    private bool _fury;

    protected override void DefineMoves()
    {
        Move("SLASH", () => Hit(3), Attacks(3));
        Move("PROTECT", async () =>
        {
            var ally = CombatState.Enemies.FirstOrDefault(e => e.IsAlive && e.Monster is BgMystic) ?? Creature;
            await CreatureCmd.GainBlock(ally, 3, ValueProp.Move, null);
        }, new DefendIntent());
    }

    protected override string ChooseMove() => _fury || Behavior[(Die - 1) / 3] == '3' ? "SLASH" : "PROTECT";

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature.Monster is BgMystic && !_fury && Creature.IsAlive)
        {
            _fury = true;
            await Buff<StrengthPower>(1);
            RefreshMoveForDie();
        }
    }
}

// Mystic: 2 attacks and Weakens, H heals every enemy, S gives every enemy Strength.
public sealed class BgMystic : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<KinPriest>();
    public override int MinInitialHp => 12;
    public override int MaxInitialHp => 12;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Behavior == "")
        {
            Behavior = FromPool("H2S", "2SH");
        }
    }

    protected override void DefineMoves()
    {
        Move("ATTACK", async () =>
        {
            await Hit(2);
            await Debuff<WeakPower>(1);
        }, Attacks(2), new DebuffIntent());
        Move("HEAL", async () =>
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            {
                await CreatureCmd.Heal(enemy, 3);
            }
        }, new HealIntent());
        Move("BUFF", async () =>
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Creature, null);
            }
        }, new BuffIntent());
    }

    protected override string ChooseMove() => DieMove() switch { '2' => "ATTACK", 'H' => "HEAL", _ => "BUFF" };
}

// Chosen: Poke (Dazed), then alternates Zap (2 Dazed) and Drain (Strength). HP from its encounter card.
public sealed class BgChosen : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<KinFollower>();
    public override int MinInitialHp => Difficulty == 1 ? 16 : 14;
    public override int MaxInitialHp => MinInitialHp;

    protected override void DefineMoves()
    {
        Move("POKE", async () =>
        {
            await Hit(1);
            await GiveStatus<BgDazed>(1);
        }, Attacks(1), new StatusIntent(1));
        Move("ZAP", async () =>
        {
            await Hit(3);
            await GiveStatus<BgDazed>(2);
        }, Attacks(3), new StatusIntent(2));
        Move("DRAIN", async () =>
        {
            await Hit(5);
            await Buff<StrengthPower>(1);
        }, Attacks(5), new BuffIntent());
    }

    protected override string ChooseMove() => Turn == 0 ? "POKE" : Turn % 2 == 1 ? "ZAP" : "DRAIN";
}

// Mugger: slash with Block, slash, big slash with Block, then escape with 2 Gold.
public sealed class BgMugger : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<AssassinRubyRaider>();
    public override int MinInitialHp => 12;
    public override int MaxInitialHp => 12;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgThieveryPower>(2);
    }

    protected override void DefineMoves()
    {
        Move("MUG", async () =>
        {
            await Hit(2);
            await Block(1);
        }, Attacks(2), new DefendIntent());
        Move("SLASH", () => Hit(2), Attacks(2));
        Move("BIG_SLASH", async () =>
        {
            await Hit(4);
            await Block(1);
        }, Attacks(4), new DefendIntent());
        Move("ESCAPE", () => Escape(2), new EscapeIntent());
    }

    protected override string ChooseMove() => Turn switch { 0 => "MUG", 1 => "SLASH", 2 => "BIG_SLASH", _ => "ESCAPE" };
}

// Shelled Parasite: Double Strike with Block, then alternates Suck (Vulnerable, Block) and Fell (Block).
public sealed class BgShelledParasite : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<PhrogParasite>();
    public override int MinInitialHp => Ascension < 7 ? 18 : 16;
    public override int MaxInitialHp => MinInitialHp;

    protected override void DefineMoves()
    {
        Move("STRIKE", async () =>
        {
            await Hit(4);
            await Block(2);
        }, Attacks(4), new DefendIntent());
        Move("SUCK", async () =>
        {
            await Hit(3);
            await Debuff<VulnerablePower>(1);
            await Block(2);
        }, Attacks(3), new DebuffIntent(), new DefendIntent());
        Move("FELL", async () =>
        {
            await Hit(2);
            await Block(2);
        }, Attacks(2), new DefendIntent());
    }

    protected override string ChooseMove() => Turn == 0 ? "STRIKE" : Turn % 2 == 1 ? "SUCK" : "FELL";
}

// Snake Plant: M chomps twice (3 and 2), W spores (2 Weak).
public sealed class BgSnakePlant : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<VineShambler>();
    public override int MinInitialHp => 17;
    public override int MaxInitialHp => 17;

    protected override void DefineMoves()
    {
        Move("CHOMP", async () =>
        {
            await Hit(3);
            await Hit(2);
        }, Attacks(3), Attacks(2));
        Move("SPORES", () => Debuff<WeakPower>(2), new DebuffIntent(strong: true));
    }

    protected override string ChooseMove() => DieMove("MMW") == 'M' ? "CHOMP" : "SPORES";
}

// Snecko: the die sets both its bite and the Confusion cost of your first card this turn.
public sealed class BgSnecko : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<SpinyToad>();
    public override int MinInitialHp => 23;
    public override int MaxInitialHp => 23;

    protected override void DefineMoves()
    {
        Move("BITE", () => Hit(3), Attacks(3));
        Move("BIG_BITE", () => Hit(5), Attacks(5));
        Move("NIBBLE", () => Hit(2), Attacks(2));
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Debuff<BgConfusionPower>(1, all: true);
    }

    protected override string ChooseMove() => Die switch { <= 2 => "BITE", <= 4 => "BIG_BITE", _ => "NIBBLE" };
}

// Spheric Guardian: Barricade and 10 starting Block; alternates Activate (Block) and Harden.
public sealed class BgSphericGuardian : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<CubexConstruct>();
    public override int MinInitialHp => 5;
    public override int MaxInitialHp => 5;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgBarricadePower>(1);
        await Block(10);
    }

    protected override void DefineMoves()
    {
        Move("ACTIVATE", async () =>
        {
            await Hit(2);
            await Block(5);
        }, Attacks(2), new DefendIntent());
        Move("SLAM", () => Hit(5), Attacks(5));
    }

    protected override string ChooseMove() => Turn % 2 == 0 ? "ACTIVATE" : "SLAM";
}

// Book of Stabbing (elite): Painful Stabs. Alternates Multi-Stab (2 hits, Strength) and Single Stab (Strength).
public sealed class BgBookOfStabbing : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<ScrollOfBiting>();
    public override int MinInitialHp => Ascension >= 12 ? 36 : Ascension >= 1 ? 35 : 30;
    public override int MaxInitialHp => MinInitialHp;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Buff<BgPainfulStabsPower>(1);
    }

    protected override void DefineMoves()
    {
        Move("MULTI_STAB", async () =>
        {
            await Hit(1, 2);
            await Buff<StrengthPower>(1);
        }, Attacks(1, 2), new BuffIntent());
        Move("SINGLE_STAB", async () =>
        {
            await Hit(Ascension < 12 ? 3 : 4);
            await Buff<StrengthPower>(1);
        }, Attacks(Ascension < 12 ? 3 : 4), new BuffIntent());
    }

    // From Ascension 1 it opens with the Single Stab.
    protected override string ChooseMove() => (Turn + (Ascension >= 1 ? 1 : 0)) % 2 == 0 ? "MULTI_STAB" : "SINGLE_STAB";
}

// Gremlin Leader (elite): brings two gremlins; Encourage, Stab, Rally (two more gremlins).
public sealed class BgGremlinLeader : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<FrogKnight>();
    public override int MinInitialHp => Ascension < 1 ? 30 : Ascension < 12 ? 35 : 40;
    public override int MaxInitialHp => MinInitialHp;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await SummonGremlins();
    }

    private async Task SummonGremlins()
    {
        var gremlins = CombatState.Enemies.Count(e => e.IsAlive && e.Monster is BgGremlin);
        for (var i = gremlins; i < 2; i++)
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

    protected override void DefineMoves()
    {
        var encourage = Ascension < 1 ? 2 : 3;
        Move("ENCOURAGE", async () =>
        {
            await Hit(encourage);
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Creature, null);
                await CreatureCmd.GainBlock(enemy, 1, ValueProp.Move, null);
            }
        }, Attacks(encourage), new BuffIntent());
        Move("STAB", () => Hit(5), Attacks(5));
        Move("RALLY", async () =>
        {
            await SummonGremlins();
            if (Ascension >= 12)
            {
                foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
                {
                    await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Creature, null);
                }
            }
        }, new SummonIntent());
    }

    protected override string ChooseMove() => (Turn % 3) switch { 0 => "ENCOURAGE", 1 => "STAB", _ => "RALLY" };
}

// Taskmaster (elite), with a Blue and a Red Slaver.
public sealed class BgTaskmaster : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<CrossbowRubyRaider>();
    public override int MinInitialHp => Ascension >= 12 ? 16 : Ascension > 1 ? 15 : 13;
    public override int MaxInitialHp => MinInitialHp;

    protected override void DefineMoves()
    {
        Move("WHIP", async () =>
        {
            await Hit(1);
            await Buff<StrengthPower>(1);
        }, Attacks(1), new BuffIntent());
        Move("SCOURING_WHIP", () => Hit(1), Attacks(1));
        Move("SCOURING_WHIP_HARD", async () =>
        {
            await Hit(2);
            await GiveStatus<BgDazed>(1);
            await Buff<StrengthPower>(1);
        }, Attacks(2), new StatusIntent(1), new BuffIntent());
        Move("SCOURING_WHIP_A12", async () =>
        {
            await Hit(1);
            await GiveStatus<BgDazed>(1);
            await Buff<StrengthPower>(1);
        }, Attacks(1), new StatusIntent(1), new BuffIntent());
    }

    protected override string ChooseMove() => Ascension switch
    {
        >= 12 => "SCOURING_WHIP_A12",
        >= 1 => Turn == 0 ? "SCOURING_WHIP" : "SCOURING_WHIP_HARD",
        _ => "WHIP",
    };
}

// Bronze Automaton (boss): spawns a Bronze Orb; Spinning Up, Flail, Boost, Hyper Beam.
public sealed class BgBronzeAutomaton : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Fabricator>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 55 : 60;
    public override int MaxInitialHp => MinInitialHp;

    private int BeamDamage => Ascension < 10 ? 7 : 9;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await Summon<BgBronzeOrb>();
    }

    private async Task Cleanse()
    {
        await PowerCmd.Remove<WeakPower>(Creature);
        await PowerCmd.Remove<VulnerablePower>(Creature);
    }

    protected override void DefineMoves()
    {
        Move("SPIN_UP", async () =>
        {
            await Buff<StrengthPower>(1);
            if (Ascension >= 10)
            {
                await Cleanse();
            }
        }, new BuffIntent());
        Move("FLAIL", () => Hit(1, 2), Attacks(1, 2));
        Move("BOOST", async () =>
        {
            await Buff<StrengthPower>(1);
            await Cleanse();
        }, new BuffIntent());
        Move("HYPER_BEAM", () => Hit(BeamDamage), Attacks(BeamDamage));
    }

    protected override string ChooseMove() => (Turn % 4) switch { 0 => "SPIN_UP", 1 => "FLAIL", 2 => "BOOST", _ => "HYPER_BEAM" };
}

// Bronze Orb: 3 beams, D beams with Dazed, B shields the Automaton.
public sealed class BgBronzeOrb : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Rocket>();
    public override int MinInitialHp => 19;
    public override int MaxInitialHp => 19;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Behavior == "")
        {
            Behavior = FromPool("3DB", "3BD", "DB3", "B3D");
        }
    }

    protected override void DefineMoves()
    {
        Move("BEAM", () => Hit(3), Attacks(3));
        Move("STASIS", async () =>
        {
            await Hit(2);
            await GiveStatus<BgDazed>(1);
        }, Attacks(2), new StatusIntent(1));
        Move("SUPPORT", async () =>
        {
            var automaton = CombatState.Enemies.FirstOrDefault(e => e.IsAlive && e.Monster is BgBronzeAutomaton) ?? Creature;
            await CreatureCmd.GainBlock(automaton, 3, ValueProp.Move, null);
        }, new DefendIntent());
    }

    protected override string ChooseMove() => DieMove() switch { '3' => "BEAM", 'D' => "STASIS", _ => "SUPPORT" };
}

// The Collector (boss): spawns Torch Heads (replacing dead ones), Fireball with Strength for all, Fireball, and
// once per fight a Mega Debuff.
public sealed class BgTheCollector : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<Queen>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 57 : 60;
    public override int MaxInitialHp => MinInitialHp;

    private bool _megaDebuffUsed;
    private int _step;

    protected override void DefineMoves()
    {
        Move("SPAWN", async () =>
        {
            var torches = CombatState.Enemies.Count(e => e.IsAlive && e.Monster is BgTorchHead);
            for (var i = torches; i < 2; i++)
            {
                await Summon<BgTorchHead>();
            }
            _step = 1;
        }, new SummonIntent());
        Move("BUFF_FIREBALL", async () =>
        {
            await Hit(3);
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Creature, null);
            }
            _step = 2;
        }, Attacks(3), new BuffIntent());
        var fireball = Ascension < 10 ? 5 : 6;
        Move("FIREBALL", async () =>
        {
            await Hit(fireball);
            _step = _megaDebuffUsed ? 0 : 3;
        }, Attacks(fireball));
        var burns = Ascension < 10 ? 2 : 3;
        Move("MEGA_DEBUFF", async () =>
        {
            await Debuff<WeakPower>(2);
            await GiveStatus<BgDazed>(2);
            await GiveStatus<BgBurn>(burns);
            _megaDebuffUsed = true;
            _step = 0;
        }, new DebuffIntent(strong: true), new StatusIntent(2 + burns));
    }

    protected override string ChooseMove() => _step switch { 0 => "SPAWN", 1 => "BUFF_FIREBALL", 2 => "FIREBALL", _ => "MEGA_DEBUFF" };
}

public sealed class BgTorchHead : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<TorchHeadAmalgam>();
    public override int MinInitialHp => 9;
    public override int MaxInitialHp => 9;
    protected override void DefineMoves() => Move("TACKLE", () => Hit(1), Attacks(1));
    protected override string ChooseMove() => "TACKLE";
}

// The Champ (boss): Face Slap, Taunt (Weak, Strength), Defensive Stance; when first reduced to 0 HP it heals to full
// and enters Limit Break: Anger (cleanse), then alternates Execute (2 hits) and gaining Strength.
public sealed class BgChamp : BgMonster
{
    protected override MonsterModel Look => ModelDb.Monster<MechaKnight>();
    public override bool IsBoss => true;
    public override int MinInitialHp => Ascension < 10 ? 40 : 45;
    public override int MaxInitialHp => MinInitialHp;

    private bool _phase2;
    private int _phase2Turn;

    protected override void DefineMoves()
    {
        Move("FACE_SLAP", () => Hit(4), Attacks(4));
        Move("TAUNT", async () =>
        {
            await Debuff<WeakPower>(1);
            await Buff<StrengthPower>(1);
        }, new DebuffIntent(), new BuffIntent());
        var stance = Ascension < 10 ? 5 : 6;
        Move("DEFENSIVE_STANCE", async () =>
        {
            await Hit(stance);
            await Block(3);
        }, Attacks(stance), new DefendIntent());
        Move("ANGER", async () =>
        {
            await PowerCmd.Remove<WeakPower>(Creature);
            await PowerCmd.Remove<VulnerablePower>(Creature);
            _phase2Turn++;
        }, new BuffIntent());
        Move("EXECUTE", async () =>
        {
            await Hit(4, 2);
            _phase2Turn++;
        }, Attacks(4, 2));
        Move("GATHER", async () =>
        {
            await Buff<StrengthPower>(Ascension < 10 ? 1 : 2);
            _phase2Turn++;
        }, new BuffIntent());
    }

    protected override string ChooseMove()
    {
        if (_phase2)
        {
            return _phase2Turn == 0 ? "ANGER" : _phase2Turn % 2 == 1 ? "EXECUTE" : "GATHER";
        }
        return (Turn % 3) switch { 0 => "FACE_SLAP", 1 => "TAUNT", _ => "DEFENSIVE_STANCE" };
    }

    public override bool ShouldDie(Creature creature) => creature != Creature || _phase2;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Creature || _phase2)
        {
            return;
        }
        _phase2 = true;
        await CreatureCmd.Heal(Creature, Creature.MaxHp);
        SetMoveImmediate(SelectMove(), forceTransition: true);
    }
}
