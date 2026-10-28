using BoardGame.Core;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace BoardGame.Powers;

public abstract class BgPower : PowerModel, IBorrowsAssets
{
    // The STS2 power whose icon this power uses, by default the one with the same name.
    public virtual string BorrowedEntry => Borrow.EntryOrFallback<PowerModel>(this, "STRENGTH_POWER");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
