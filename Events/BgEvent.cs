using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Events;

public abstract class BgEvent : EventModel
{
    // The acts whose event deck includes this event.
    public abstract IReadOnlyList<Type> Acts { get; }
}
