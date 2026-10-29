using BoardGame.Core;
using BoardGame.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BoardGame.Orbs;

public abstract class BgOrb : OrbModel, IBorrowsAssets
{
    public abstract string BorrowedEntry { get; }

    public abstract string Name { get; }

    public abstract string EvokeText { get; }

    // Damage or Block from orbs isn't an attack, so Strength, Weak, and Vulnerable don't change it.
    protected async Task DamageChosenEnemy(PlayerChoiceContext choiceContext, decimal amount, bool hitAll)
    {
        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0 || amount <= 0)
        {
            return;
        }
        var targets = hitAll
            ? enemies
            : [await BgOrbs.ChooseEnemy(choiceContext, Owner, enemies, $"{Name}: deal {amount} damage to")];
        foreach (var target in targets)
        {
            VfxCmd.PlayOnCreature(target, "vfx/vfx_attack_lightning");
        }
        PlayEvokeSfx();
        await CreatureCmd.Damage(choiceContext, targets, amount, ValueProp.Unpowered, Owner.Creature);
    }
}

// Passive: at the end of turn, deal 1 damage. Evoke: deal 2 damage. Electrodynamics makes both hit every enemy.
public sealed class BgLightningOrb : BgOrb
{
    public override string BorrowedEntry => "LIGHTNING_ORB";
    public override string Name => "Lightning";
    public override string EvokeText => $"Evoke: deal {EvokeVal} damage.";
    protected override string PassiveSfx => "event:/sfx/characters/defect/defect_lightning_passive";
    protected override string EvokeSfx => "event:/sfx/characters/defect/defect_lightning_evoke";
    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_lightning_channel";
    public override Color DarkenedColor => new("796606");

    public override decimal PassiveVal =>
        1m + Owner.Creature.GetPowerAmount<BgOrbEndOfTurnPower>() + Owner.Creature.GetPowerAmount<BgStaticDischargePower>();

    public override decimal EvokeVal => Math.Max(0m, 2m + Owner.Creature.GetPowerAmount<BgOrbEvokePower>());

    private bool HitsAll => Owner.Creature.HasPower<BgElectrodynamicsPower>();

    public override Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext) => TriggerPassive(choiceContext, null);

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        ActivatePassive();
        await DamageChosenEnemy(choiceContext, PassiveVal, HitsAll);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        await DamageChosenEnemy(choiceContext, EvokeVal, HitsAll);
        return [];
    }
}

// Passive: at the end of turn, gain 1 Block. Evoke: gain 1 Block.
public sealed class BgFrostOrb : BgOrb
{
    public override string BorrowedEntry => "FROST_ORB";
    public override string Name => "Frost";
    public override string EvokeText => $"Evoke: gain {EvokeVal} Block.";
    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_frost_channel";
    public override Color DarkenedColor => new("7860a7");

    public override decimal PassiveVal => Math.Max(0m, 1m + Owner.Creature.GetPowerAmount<BgOrbEndOfTurnPower>());

    public override decimal EvokeVal => Math.Max(0m, 1m + Owner.Creature.GetPowerAmount<BgOrbEvokePower>());

    public override Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext) => TriggerPassive(choiceContext, null);

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        ActivatePassive();
        PlayPassiveSfx();
        await CreatureCmd.GainBlock(Owner.Creature, PassiveVal, ValueProp.Unpowered, null);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        PlayEvokeSfx();
        await CreatureCmd.GainBlock(Owner.Creature, EvokeVal, ValueProp.Unpowered, null);
        return [];
    }
}

// No passive. Evoke: deal 3 damage, plus 1 for each Power played this combat. You can have at most 3 Dark orbs.
public sealed class BgDarkOrb : BgOrb
{
    public override string BorrowedEntry => "DARK_ORB";
    public override string Name => "Dark";
    public override string EvokeText => $"Evoke: deal {EvokeVal} damage.";
    public override Color DarkenedColor => new("2a1340");

    public override decimal PassiveVal => 0m;

    public override decimal EvokeVal =>
        3m + Owner.Creature.GetPowerAmount<BgOrbEvokePower>() + Owner.Creature.GetPowerAmount<BgAmplifyPower>()
        + (BgRules.For(Owner.RunState)?.PowersPlayed(Owner) ?? 0);

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        await DamageChosenEnemy(choiceContext, EvokeVal, hitAll: false);
        return [];
    }
}

// Board game orb rules: channel into any empty slot; when every slot is full, evoke an orb of your choice first.
// Evoking lets you pick any orb, and orbs never rotate.
public static class BgOrbs
{
    public const int MaxDark = 3;

    private static readonly Func<PlayerChoiceContext, Player, OrbModel, bool, Task> EvokeOrb =
        AccessTools.MethodDelegate<Func<PlayerChoiceContext, Player, OrbModel, bool, Task>>(
            AccessTools.Method(typeof(OrbCmd), "Evoke", [typeof(PlayerChoiceContext), typeof(Player), typeof(OrbModel), typeof(bool)]));

    public static IReadOnlyList<OrbModel> Orbs(Player player) => player.PlayerCombatState?.OrbQueue.Orbs ?? [];

    public static Task Channel<TOrb>(PlayerChoiceContext choiceContext, Player player) where TOrb : BgOrb =>
        Channel(choiceContext, player, (BgOrb)ModelDb.Orb<TOrb>().ToMutable());

    public static async Task Channel(PlayerChoiceContext choiceContext, Player player, BgOrb orb)
    {
        var queue = player.PlayerCombatState?.OrbQueue;
        if (queue == null || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }
        var darkCount = queue.Orbs.Count(o => o is BgDarkOrb);
        if (orb is BgDarkOrb && darkCount >= MaxDark)
        {
            // With 3 Dark orbs, channeling another evokes one of them instead of growing past the limit.
            if (queue.Orbs.Count < queue.Capacity)
            {
                return;
            }
            await Evoke(choiceContext, player, queue.Orbs.First(o => o is BgDarkOrb));
        }
        else if (queue.Capacity > 0 && queue.Orbs.Count >= queue.Capacity)
        {
            await EvokeChosen(choiceContext, player);
        }
        await OrbCmd.Channel(choiceContext, orb, player);
    }

    // Evokes the orb the player picks, `times` times, then removes it.
    public static async Task EvokeChosen(PlayerChoiceContext choiceContext, Player player, int times = 1)
    {
        var orb = await ChooseOrb(choiceContext, player, times == 1 ? "Choose an orb to Evoke" : $"Choose an orb to Evoke {times} times");
        if (orb == null)
        {
            return;
        }
        for (var i = 1; i < times; i++)
        {
            await EvokeOrb(choiceContext, player, orb, false);
        }
        await Evoke(choiceContext, player, orb);
    }

    public static Task Evoke(PlayerChoiceContext choiceContext, Player player, OrbModel orb, bool remove = true) =>
        EvokeOrb(choiceContext, player, orb, remove);

    // Removes every orb without evoking it (Hyperbeam, Reprogram, Fission).
    public static void RemoveAll(Player player)
    {
        var queue = player.PlayerCombatState?.OrbQueue;
        if (queue == null)
        {
            return;
        }
        foreach (var orb in queue.Orbs.ToList())
        {
            queue.Remove(orb);
            MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.EvokeOrbAnim(orb);
            orb.RemoveInternal();
        }
    }

    public static Task<OrbModel?> ChooseOrb(PlayerChoiceContext choiceContext, Player player, string prompt) =>
        Choice.Choose(choiceContext, player, prompt, Orbs(player),
            orb => orb is BgOrb bg ? new Choice.Option(bg.Name, bg.EvokeText) : new Choice.Option(orb.Title.GetFormattedText(), ""));

    public static async Task<Creature> ChooseEnemy(PlayerChoiceContext choiceContext, Player player, IReadOnlyList<Creature> enemies, string prompt) =>
        await Targeting.ChooseEnemy(player, enemies, prompt) ?? enemies[0];
}
