using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Core;

// Finds the STS2 model a board game model borrows art from. By convention a board game model "BG_X" uses the STS2
// model "X" of the same kind when one exists, e.g. BG_BASH uses BASH's portrait.
internal static class Borrow
{
    public const string Prefix = "BG_";

    public static string Unprefixed(string entry) => entry.StartsWith(Prefix) ? entry[Prefix.Length..] : entry;

    public static T? Matching<T>(AbstractModel model) where T : AbstractModel =>
        ModelDb.GetByIdOrNull<T>(new ModelId(model.Id.Category, Unprefixed(model.Id.Entry)));

    public static string EntryOrFallback<T>(AbstractModel model, string fallback) where T : AbstractModel =>
        Matching<T>(model)?.Id.Entry ?? fallback;
}
