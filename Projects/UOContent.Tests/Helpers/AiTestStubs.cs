using Server.Mobiles;

namespace Server.Tests;

/// <summary>
/// A plain <see cref="Mobile"/>: walker, target or bystander. Deriving straight from Mobile rather
/// than BaseCreature is the point: MovementImpl sees no creature capabilities (no swim, no fly, no
/// door-opening), which is exactly the walker the step cache bakes for.
/// </summary>
public sealed class WalkerStub : Mobile
{
    public WalkerStub() => Body = 0xC9;
}

/// <summary>
/// A BaseCreature built through the Serial constructor, which skips the NPCSpeeds lookup and
/// leaves AIObject unset: call <see cref="Mobile.DefaultMobileInit"/> and attach an AI by hand.
/// </summary>
public sealed class BareCreatureStub : BaseCreature
{
    public BareCreatureStub(Serial serial) : base(serial) => Body = 0xC9;
}
