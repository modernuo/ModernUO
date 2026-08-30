using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Pins the reacquire gate: an empty scan retries quickly, a successful acquire holds its
// target for the full ReacquireDelay (stickiness). Re-arming the long delay on failure
// left creatures blind for the whole delay to a player walking up.
[Collection("Sequential Pathfinding Tests")]
public class AcquisitionTests : IDisposable
{
    private readonly List<Mobile> _created = new();

    public void Dispose()
    {
        foreach (var m in _created)
        {
            m?.Delete();
        }

        _created.Clear();
    }

    private sealed class WildStub : BaseCreature
    {
        public WildStub() : base(AIType.AI_Melee, FightMode.Closest, 16, 1) => Body = 0xC9;

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.3;
            passiveSpeed = 0.6;
        }
    }

    private sealed class TargetStub : Mobile
    {
        public TargetStub() => Body = 0x190;
    }

    private WildStub Spawn(Map map, Point3D loc)
    {
        var bc = new WildStub();
        bc.MoveToWorld(loc, map);
        bc.AIObject.AITimer?.Stop();
        _created.Add(bc);
        return bc;
    }

    [Fact]
    public void EmptyScan_RetriesQuickly()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = Spawn(map, new Point3D(1500, 1600, (sbyte)z));
        bc.NextReacquireTime = Core.TickCount;

        Assert.False(bc.AIObject.AcquireFocusMob(bc.RangePerception, FightMode.Closest, false, false, true));
        Assert.InRange(bc.NextReacquireTime - Core.TickCount, 1, 5000);
    }

    [Fact]
    public void SuccessfulAcquire_HoldsFullDelay()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = Spawn(map, new Point3D(1500, 1600, (sbyte)z));

        var target = new TargetStub();
        target.DefaultMobileInit();
        target.MoveToWorld(new Point3D(1497, 1600, (sbyte)z), map);
        _created.Add(target);

        bc.NextReacquireTime = Core.TickCount;

        Assert.True(bc.AIObject.AcquireFocusMob(bc.RangePerception, FightMode.Closest, false, false, true));
        Assert.Equal(target, bc.FocusMob);
        Assert.True(bc.NextReacquireTime - Core.TickCount > 5000);
    }
}
