namespace BoardGame.Powers;

// Orbs read these powers directly; each adds its amount to an orb value.

// Every orb's Evoke is this much stronger.
public sealed class BgOrbEvokePower : BgPower
{
    public override string BorrowedEntry => "FOCUS_POWER";
}

// Every orb's end-of-turn Passive is this much stronger.
public sealed class BgOrbEndOfTurnPower : BgPower
{
    public override string BorrowedEntry => "FOCUS_POWER";
}

// Lightning's Passive deals this much more damage.
public sealed class BgStaticDischargePower : BgPower
{
    public override string BorrowedEntry => "LIGHTNING_ROD_POWER";
}

// Dark orbs Evoke for this much more damage.
public sealed class BgAmplifyPower : BgPower
{
    public override string BorrowedEntry => "FOCUS_POWER";
}

// Lightning hits every enemy.
public sealed class BgElectrodynamicsPower : BgPower
{
    public override string BorrowedEntry => "HIGH_VOLTAGE_POWER";
    public override MegaCrit.Sts2.Core.Entities.Powers.PowerStackType StackType => MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Single;
}

// Shivs deal this much more damage.
public sealed class BgAccuracyPower : BgPower
{
}
