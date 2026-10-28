using BoardGame.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Monsters;

// A board game enemy. It looks and sounds like an STS2 monster (Look) and chooses its move each round from the die,
// a fixed sequence, or a single repeated action, as printed on its board game card.
public abstract class BgMonster : MonsterModel, IBorrowsAssets
{
    private static readonly Func<MonsterModel, string> LookVisualsPath =
        AccessTools.MethodDelegate<Func<MonsterModel, string>>(AccessTools.PropertyGetter(typeof(MonsterModel), "VisualsPath"));

    private Dictionary<string, MoveState> _moves = [];

    // The STS2 monster this enemy borrows its body, animations, and sounds from.
    protected abstract MonsterModel Look { get; }

    public string BorrowedEntry => Look.Id.Entry;

    // The number of moves this enemy has performed this combat. Sequence enemies advance with it.
    protected int Turn { get; private set; }

    // The die-based behavior printed on the card variant: the move for rolls 1-2, 3-4, and 5-6.
    public string Behavior { get; set; } = "";

    // Which printed version of the enemy this is, e.g. Jaw Worm's Act I easy, medium, and Ascension 7 cards.
    public int Difficulty { get; set; }

    // Bosses are in every row, so every player's row effects reach them.
    public virtual bool IsBoss => false;

    protected int Ascension => CombatState?.RunState.AscensionLevel ?? 0;

    protected int Die => BgRules.For(CombatState?.RunState)?.Roll is > 0 and var roll ? roll : 1;

    protected override string VisualsPath => LookVisualsPath(Look);

    public override string? HurtSfx => Look.HurtSfx;
    public override DamageSfxType TakeDamageSfxType => Look.TakeDamageSfxType;
    public override string DeathSfx => Look.DeathSfx;
    public override bool HasDeathSfx => Look.HasDeathSfx;

    public override CreatureAnimator GenerateAnimator(MegaSprite controller) => Look.GenerateAnimator(controller);

    public override void SetupSkins(MegaSprite spine, MegaSkeleton skeleton) => Look.SetupSkins(spine, skeleton);

    // Models are copied with MemberwiseClone, so each copy needs its own move table.
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _moves = [];
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _moves.Clear();
        DefineMoves();
        var states = _moves.Values.ToList();
        return new MonsterMoveStateMachine(states, states[0]);
    }

    // Registers every move with Move(...).
    protected abstract void DefineMoves();

    // Picks this round's move. Called whenever the die is rolled or changed.
    protected abstract string ChooseMove();

    protected void Move(string id, Func<Task> perform, params AbstractIntent[] intents)
    {
        _moves[id] = new MoveState(id, async _ =>
        {
            await perform();
            Turn++;
        }, intents)
        { MustPerformOnceBeforeTransitioning = false };
    }

    internal MoveState SelectMove() => _moves[ChooseMove()];

    // The die move for this round: the first character of the behavior for rolls 1-2, the second for 3-4, the third for 5-6.
    protected char DieMove(string? behavior = null)
    {
        var b = behavior ?? Behavior;
        return b[Math.Min(b.Length - 1, (Die - 1) / 2)];
    }

    // Changes intents to match a changed die roll (Gambling Chip, The Abacus, Toolbox).
    internal void RefreshMoveForDie()
    {
        if (!IsPerformingMove && Creature.IsAlive && MoveStateMachine != null)
        {
            SetMoveImmediate(SelectMove(), forceTransition: true);
        }
    }

    // The player in this enemy's row. Single-target attacks hit them; in single player that's the only player.
    protected Creature RowTarget
    {
        get
        {
            var players = CombatState.PlayerCreatures.Where(p => p.IsAlive).ToList();
            if (players.Count == 0)
            {
                return CombatState.PlayerCreatures[0];
            }
            var index = CombatState.Enemies.ToList().IndexOf(Creature);
            return players[Math.Max(0, index) % players.Count];
        }
    }

    protected AttackCommand AttackRow(int damage, int hits = 1, string anim = "Attack") =>
        DamageCmd.Attack(damage).WithHitCount(hits).FromMonster(this).Targeting(RowTarget)
            .WithAttackerAnim(anim, 0.15f).WithHitFx("vfx/vfx_attack_blunt");

    protected AttackCommand AttackAll(int damage, int hits = 1, string anim = "Attack") =>
        DamageCmd.Attack(damage).WithHitCount(hits).FromMonster(this).TargetingAllOpponents(CombatState)
            .WithAttackerAnim(anim, 0.15f).WithHitFx("vfx/vfx_attack_blunt");

    protected Task Hit(int damage, int hits = 1) => AttackRow(damage, hits).Execute(null);

    protected Task HitAll(int damage, int hits = 1) => AttackAll(damage, hits).Execute(null);

    protected Task Block(int amount) => CreatureCmd.GainBlock(Creature, amount, ValueProp.Move, null);

    protected Task Buff<TPower>(int amount) where TPower : PowerModel =>
        PowerCmd.Apply<TPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);

    protected Task Debuff<TPower>(int amount, bool all = false) where TPower : PowerModel =>
        PowerCmd.Apply<TPower>(new ThrowingPlayerChoiceContext(), all ? CombatState.PlayerCreatures.Where(p => p.IsAlive) : [RowTarget],
            amount, Creature, null);

    protected Task Cast() => CreatureCmd.TriggerAnim(Creature, "Cast", 0.4f);

    // Gives the targeted player status cards: Dazed on top of their draw pile, others into their discard pile.
    protected async Task GiveStatus<TCard>(int count, bool all = false) where TCard : MegaCrit.Sts2.Core.Models.CardModel
    {
        var players = all ? CombatState.PlayerCreatures.Where(p => p.IsAlive).ToList() : [RowTarget];
        foreach (var player in players.Select(p => p.Player).OfType<MegaCrit.Sts2.Core.Entities.Players.Player>())
        {
            var pile = typeof(TCard) == typeof(Cards.BgDazed) ? MegaCrit.Sts2.Core.Entities.Cards.PileType.Draw : MegaCrit.Sts2.Core.Entities.Cards.PileType.Discard;
            for (var i = 0; i < count; i++)
            {
                await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<TCard>(player), pile, player,
                    MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top);
            }
        }
    }

    // Summons another enemy into the fight.
    protected async Task<TMonster> Summon<TMonster>(string behavior = "", int difficulty = 0) where TMonster : BgMonster
    {
        var monster = (TMonster)ModelDb.Monster<TMonster>().ToMutable();
        monster.Behavior = behavior;
        monster.Difficulty = difficulty;
        await CreatureCmd.Add(monster, CombatState, CombatSide.Enemy, null);
        return monster;
    }

    // Picks a random behavior from a summon pool, e.g. the Acid Slime summon cards.
    protected string FromPool(params string[] behaviors) => CombatState.RunState.Rng.MonsterAi.NextItem(behaviors)!;

    // Escapes from combat, stealing gold from the player in its row.
    protected async Task Escape(int stealGold = 0)
    {
        if (stealGold > 0 && RowTarget.Player is { } player)
        {
            await PlayerCmd.LoseGold(Math.Min(stealGold, player.Gold), player);
        }
        await CreatureCmd.Escape(Creature);
    }

    protected static SingleAttackIntent Attacks(int damage) => new(damage);

    protected static MultiAttackIntent Attacks(int damage, int hits) => new(damage, hits);
}

// Board game enemies choose their move from the die (or their sequence) whenever the game rolls a new move.
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove))]
internal static class BgMonsterMovePatch
{
    private static readonly Action<MonsterModel, MoveState> SetNextMove =
        AccessTools.MethodDelegate<Action<MonsterModel, MoveState>>(AccessTools.PropertySetter(typeof(MonsterModel), nameof(MonsterModel.NextMove)));

    private static bool Prefix(MonsterModel __instance)
    {
        if (__instance is not BgMonster monster || monster.MoveStateMachine == null)
        {
            return true;
        }
        var move = monster.SelectMove();
        SetNextMove(monster, move);
        monster.MoveStateMachine.ForceCurrentState(move);
        return false;
    }
}
