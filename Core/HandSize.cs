using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BoardGame.Core;

// The board game has no hand size limit. In board game runs the hand can hold any number of cards; past 10 the
// cards fan out wider and shrink to fit.
[HarmonyPatch(typeof(CardPile), nameof(CardPile.MaxCardsInHand), MethodType.Getter)]
internal static class UnlimitedHandPatch
{
    public const int Unlimited = 99;

    private static void Postfix(ref int __result)
    {
        if (BoardGameRun.IsActive(BgAscension.CurrentRun))
        {
            __result = Unlimited;
        }
    }
}

// The limit is a tiny property the JIT may inline into its callers, which would skip the patch above. The game's
// methods that read it are rewritten to call it through this method instead.
[HarmonyPatch]
internal static class HandLimitCallersPatch
{
    private static readonly System.Reflection.MethodInfo Getter =
        AccessTools.PropertyGetter(typeof(CardPile), nameof(CardPile.MaxCardsInHand));

    private static IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
    [
        AccessTools.AsyncMoveNext(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardPileCmd), nameof(MegaCrit.Sts2.Core.Commands.CardPileCmd.Add),
            [typeof(IEnumerable<MegaCrit.Sts2.Core.Models.CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(MegaCrit.Sts2.Core.Models.AbstractModel), typeof(bool), typeof(bool)])),
        AccessTools.AsyncMoveNext(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardPileCmd), "DrawInternal")),
        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardPileCmd), "CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot"),
        AccessTools.AsyncMoveNext(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "SetupPlayerTurn")),
    ];

    public static int Limit() => BoardGameRun.IsActive(BgAscension.CurrentRun) ? UnlimitedHandPatch.Unlimited : 10;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        instructions.Select(i => i.Calls(Getter) ? new CodeInstruction(System.Reflection.Emit.OpCodes.Call, AccessTools.Method(typeof(HandLimitCallersPatch), nameof(Limit))).MoveLabelsFrom(i).MoveBlocksFrom(i) : i);
}

[HarmonyPatch]
internal static class LargeHandLayoutPatch
{
    private const int LargestVanillaHand = 10;

    // The vanilla 10-card fan spans x = -610..610, rises 88 from the ends to the middle, and tilts -15..15 degrees.
    [HarmonyPatch(typeof(HandPosHelper), nameof(HandPosHelper.GetPosition))]
    [HarmonyPrefix]
    private static bool Position(int handSize, int cardIndex, ref Vector2 __result)
    {
        if (handSize <= LargestVanillaHand)
        {
            return true;
        }
        var t = Spread(handSize, cardIndex);
        __result = new Vector2(t * 660f, -50f + 88f * t * t);
        return false;
    }

    [HarmonyPatch(typeof(HandPosHelper), nameof(HandPosHelper.GetAngle))]
    [HarmonyPrefix]
    private static bool Angle(int handSize, int cardIndex, ref float __result)
    {
        if (handSize <= LargestVanillaHand)
        {
            return true;
        }
        __result = Spread(handSize, cardIndex) * 15f;
        return false;
    }

    [HarmonyPatch(typeof(HandPosHelper), nameof(HandPosHelper.GetScale))]
    [HarmonyPostfix]
    private static void Scale(int handSize, ref Vector2 __result)
    {
        if (handSize > LargestVanillaHand)
        {
            __result = Vector2.One * 0.8f * Math.Max(0.55f, 0.85f - 0.05f * (handSize - LargestVanillaHand));
        }
    }

    // -1 for the leftmost card, 1 for the rightmost.
    private static float Spread(int handSize, int cardIndex) => cardIndex * 2f / (handSize - 1) - 1f;
}

// Only the first 10 cards have number-key shortcuts. Dragging a card past them uses the 10th card's key to cancel.
[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
internal static class LargeHandShortcutPatch
{
    private static readonly AccessTools.FieldRef<NPlayerHand, StringName[]> Shortcuts =
        AccessTools.FieldRefAccess<NPlayerHand, StringName[]>("_selectCardShortcuts");

    private static void Prefix(NPlayerHand __instance, NHandCardHolder holder, out StringName[]? __state)
    {
        var shortcuts = Shortcuts(__instance);
        __state = null;
        var index = holder.GetIndex();
        if (index >= shortcuts.Length)
        {
            __state = shortcuts;
            Shortcuts(__instance) = shortcuts.Concat(Enumerable.Repeat(shortcuts[^1], index + 1 - shortcuts.Length)).ToArray();
        }
    }

    private static void Postfix(NPlayerHand __instance, StringName[]? __state)
    {
        if (__state != null)
        {
            Shortcuts(__instance) = __state;
        }
    }
}
