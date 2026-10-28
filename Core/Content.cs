using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Core;

// Finds the board game's content by type, so pools and acts never need hand-maintained lists.
internal static class Content
{
    private static readonly Type[] Types = typeof(Content).Assembly.GetTypes();

    public static IEnumerable<Type> Concrete<TBase>() =>
        Types.Where(type => !type.IsAbstract && typeof(TBase).IsAssignableFrom(type)).OrderBy(type => type.Name, StringComparer.Ordinal);

    public static TModel[] Models<TBase, TModel>() where TModel : AbstractModel =>
        Concrete<TBase>().Select(type => (TModel)Get(type)).ToArray();

    // The canonical model of a type.
    public static AbstractModel Get(Type type) => ModelDb.GetById<AbstractModel>(ModelDb.GetId(type));
}
