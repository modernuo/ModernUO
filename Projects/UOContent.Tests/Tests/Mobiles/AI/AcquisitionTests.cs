using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Pins the reacquire gate and the AcquireOnApproachDelay intelligence gradient: every scan
// re-arms the full ReacquireDelay; enemy movement clamps the deadline to the approach
// delay (Zero = instant paragon engage); an illegal deadline self-heals.
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
    public void EmptyScan_HonorsReacquireDelay()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = Spawn(map, new Point3D(1500, 1600, (sbyte)z));
        bc.NextReacquireTime = Core.TickCount;

        Assert.False(bc.AIObject.AcquireFocusMob(bc.RangePerception, FightMode.Closest, false, false, true));
        Assert.InRange(bc.NextReacquireTime - Core.TickCount, 5000, 10000);
    }

    [Fact]
    public void WedgedGate_SelfHeals()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = Spawn(map, new Point3D(1500, 1600, (sbyte)z));

        var target = new TargetStub();
        target.DefaultMobileInit();
        target.MoveToWorld(new Point3D(1497, 1600, (sbyte)z), map);
        _created.Add(target);

        // Illegal deadline (beyond ReacquireDelay): must read as open, not block forever.
        bc.NextReacquireTime = Core.TickCount + 60000;

        Assert.True(bc.AIObject.AcquireFocusMob(bc.RangePerception, FightMode.Closest, false, false, true));
        Assert.Equal(target, bc.FocusMob);
    }

    [Theory]
    [InlineData(false, 5, true)]  // an enemy moving inside notice range clamps the deadline
    [InlineData(true, 5, false)]  // a same-team wild creature is not an enemy — ignored
    [InlineData(false, 20, false)] // outside notice range (16) is ignored
    public void MovementClampsScanDeadlineOnlyForEnemiesInRange(bool wildMover, int distance, bool notices)
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = Spawn(map, new Point3D(1500, 1600, (sbyte)z));
        bc.NextReacquireTime = Core.TickCount + 8000;

        Mobile mover;
        if (wildMover)
        {
            mover = Spawn(map, new Point3D(1500 - distance, 1600, (sbyte)z));
        }
        else
        {
            mover = new TargetStub { Player = true };
            mover.DefaultMobileInit();
            mover.MoveToWorld(new Point3D(1500 - distance, 1600, (sbyte)z), map);
            _created.Add(mover);
        }

        bc.OnMovement(mover, new Point3D(1400, 1600, (sbyte)z));

        var remaining = bc.NextReacquireTime - Core.TickCount;

        if (notices)
        {
            // Clamped to the notice delay (2s), never opened outright.
            Assert.InRange(remaining, 1, (long)bc.AcquireOnApproachDelay.TotalMilliseconds);
        }
        else
        {
            Assert.True(remaining > 5000);
        }
    }

    private sealed class InstantStub : BaseCreature
    {
        public InstantStub() : base(AIType.AI_Melee, FightMode.Closest, 16, 1) => Body = 0xC9;

        public override TimeSpan AcquireOnApproachDelay => TimeSpan.Zero;

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.3;
            passiveSpeed = 0.6;
        }
    }

    [Fact]
    public void ZeroApproachDelay_EngagesInstantly()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = new InstantStub();
        bc.MoveToWorld(new Point3D(1500, 1600, (sbyte)z), map);
        bc.AIObject.AITimer?.Stop();
        _created.Add(bc);
        bc.NextReacquireTime = Core.TickCount + 8000;

        var mover = new TargetStub { Player = true };
        mover.DefaultMobileInit();
        mover.MoveToWorld(new Point3D(1495, 1600, (sbyte)z), map);
        _created.Add(mover);

        bc.OnMovement(mover, new Point3D(1400, 1600, (sbyte)z));

        Assert.Equal(mover, bc.Combatant);
    }

    [Fact]
    public void RepeatedMovement_DoesNotShortenBelowApproachDelay()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var bc = Spawn(map, new Point3D(1500, 1600, (sbyte)z));
        bc.NextReacquireTime = Core.TickCount + 8000;

        var mover = new TargetStub { Player = true };
        mover.DefaultMobileInit();
        mover.MoveToWorld(new Point3D(1495, 1600, (sbyte)z), map);
        _created.Add(mover);

        bc.OnMovement(mover, new Point3D(1400, 1600, (sbyte)z));
        var afterFirst = bc.NextReacquireTime;

        bc.OnMovement(mover, new Point3D(1496, 1600, (sbyte)z));

        Assert.Equal(afterFirst, bc.NextReacquireTime);
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
