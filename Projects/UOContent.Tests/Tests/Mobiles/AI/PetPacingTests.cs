using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Pet order handlers own the speed clocks; combat chases and herding keep their own pacing.
[Collection("Sequential UOContent Tests")]
public class PetPacingTests : IDisposable
{
    private readonly List<Mobile> _created = new();

    private (PlayerMobile master, PetTestStub pet) Spawn(Point3D masterLoc, Point3D petLoc)
    {
        var pair = PetTestSetup.SpawnControlledPet(masterLoc, petLoc);
        _created.Add(pair.master);
        _created.Add(pair.pet);
        return pair;
    }

    public void Dispose()
    {
        foreach (var m in _created)
        {
            m?.Delete();
        }

        _created.Clear();
    }

    // Movement orders run active, resting orders run passive; the move clock follows.
    [Fact]
    public void OrderIssue_SetsThinkClock()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);
        pet.SetCurrentSpeedToPassive();

        pet.ControlOrder = OrderType.Come;
        Assert.Equal(0.2, pet.CurrentSpeed);
        Assert.Equal(0.3, pet.CurrentMoveSpeed); // verbatim active -> activeMove

        pet.ControlOrder = OrderType.Stay;
        Assert.Equal(0.4, pet.CurrentSpeed);
        Assert.Equal(0.9, pet.CurrentMoveSpeed);

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow;
        Assert.Equal(0.2, pet.CurrentSpeed);

        pet.ControlOrder = OrderType.Guard;
        Assert.Equal(0.2, pet.CurrentSpeed);
    }

    // The follow pace caps the step delay and leaves the think clock on the active value.
    [Fact]
    public void FollowMaster_PacesStepsWithoutInflatingTheThinkClock()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow; // fixture era is EJ

        Assert.Equal(0.2, pet.CurrentSpeed);     // active think, not the follow pace
        Assert.Equal(0.1, pet.CurrentMoveSpeed); // capped at the follow pace
    }

    // A creature configured faster than the follow pace keeps its own.
    [Fact]
    public void FollowMaster_KeepsAFasterConfiguredPace()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.05, 0.9);

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow;

        Assert.Equal(0.05, pet.CurrentMoveSpeed);
    }

    // The move-clock override survives the order: it is capped while following, not overwritten.
    [Fact]
    public void FollowMaster_LeavesTheConfiguredMoveClockAlone()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow;
        pet.AIObject.Obey();
        pet.ControlOrder = OrderType.Stay;

        Assert.Equal(0.3, pet.ActiveMoveSpeed);
        Assert.Equal(0.9, pet.PassiveMoveSpeed);
        Assert.Equal(0.9, pet.CurrentMoveSpeed); // resting on its own passive pace again
    }

    // Pre-AOS pets follow at their own pace; nothing caps them.
    [Fact]
    public void FollowMaster_PreAOS_KeepsItsOwnPace()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.UOR;
            var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
            pet.SetMoveSpeed(0.3, 0.9);

            pet.ControlTarget = master;
            pet.ControlOrder = OrderType.Follow;

            Assert.Equal(0.2, pet.CurrentSpeed);
            Assert.Equal(0.3, pet.CurrentMoveSpeed);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    private sealed class SprintingPet : PetTestStub
    {
        public override double FollowMoveSpeed => 0.125;
    }

    // A shard paces follows in any era by overriding the property, not by patching the AI.
    [Fact]
    public void FollowMoveSpeedOverride_PacesFollowsInAnyEra()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.UOR;
            var master = new PlayerMobile(World.NewMobile);
            master.DefaultMobileInit();
            master.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
            _created.Add(master);

            var pet = new SprintingPet();
            pet.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
            pet.SetControlMaster(master);
            _created.Add(pet);
            pet.SetMoveSpeed(0.3, 0.9);

            pet.ControlTarget = master;
            pet.ControlOrder = OrderType.Follow;

            Assert.Equal(0.125, pet.CurrentMoveSpeed);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    // A guarding pet outside guard range closes at the follow pace, thinking on its active clock.
    [Fact]
    public void GuardReturn_PacesStepsWithoutInflatingTheThinkClock()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1006, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);

        pet.ControlOrder = OrderType.Guard;

        Assert.Equal(0.2, pet.CurrentSpeed);
        Assert.Equal(0.1, pet.CurrentMoveSpeed);
    }

    // Obeying the follow order must not write the pace into either clock.
    [Fact]
    public void FollowMaster_ObeyKeepsTheThinkClockActive()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);
        pet.AIObject.AITimer?.Stop();

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow; // fixture era is EJ
        pet.AIObject.Obey();

        Assert.Equal(0.2, pet.CurrentSpeed);
        Assert.Equal(0.1, pet.CurrentMoveSpeed);
    }

    // At the master's side a guarding pet stays active: no stale-warmode passive, no sprint.
    [Fact]
    public void GuardAtMastersSide_IsActive()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);
        pet.AIObject.AITimer?.Stop();
        pet.SetCurrentSpeedToPassive();

        pet.ControlOrder = OrderType.Guard;
        pet.AIObject.Obey(); // nothing to guard against, master adjacent

        Assert.Equal(0.2, pet.CurrentSpeed);
        Assert.Equal(0.3, pet.CurrentMoveSpeed);
    }

    // A pet chasing a combatant keeps the move table.
    [Fact]
    public void CombatChasingPet_KeepsMoveTable()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        var target = new PetTestStub();
        target.MoveToWorld(new Point3D(1003, 1000, 0), Map.Felucca);
        _created.Add(target);

        pet.SetMoveSpeed(0.3, 0.9);
        pet.ControlOrder = OrderType.Guard;
        pet.Combatant = target;
        pet.SetCurrentSpeedToActive();

        Assert.Equal(0.3, pet.CurrentMoveSpeed);
    }

    // Herding overrides order pacing.
    [Fact]
    public void HerdedObeyingPet_KeepsHerdingPace()
    {
        var (_, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.45, 0.9);
        pet.SetCurrentSpeedToPassive();

        pet.TargetLocation = new Point2D(1010, 1010);

        Assert.Equal(0.3, pet.CurrentMoveSpeed); // fixed herding pace
    }

    private sealed class ThinkProbe : PetTestStub
    {
        public int Thinks;

        public override void OnThink()
        {
            Thinks++;
            base.OnThink();
        }
    }

    private (PlayerMobile master, ThinkProbe pet) SpawnProbe()
    {
        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        master.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        _created.Add(master);

        var pet = new ThinkProbe();
        pet.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
        pet.SetControlMaster(master);
        _created.Add(pet);

        return (master, pet);
    }

    // Advances time in 8ms lockstep so the wheel and Core.TickCount stay in sync.
    private static void RunFor(long ms)
    {
        var deadline = Core._tickCount + ms;

        while (Core._tickCount < deadline)
        {
            Core._tickCount += 8;
            Timer.Slice(Core._tickCount);
        }
    }

    private static bool RunUntil(Func<bool> condition, long maxMs)
    {
        var deadline = Core._tickCount + maxMs;

        while (Core._tickCount < deadline)
        {
            if (condition())
            {
                return true;
            }

            Core._tickCount += 8;
            Timer.Slice(Core._tickCount);
        }

        return condition();
    }

    // Runs past the spawn stagger; returns right after a think with the next 0.4s away.
    private ThinkProbe SettledProbe(out PlayerMobile master)
    {
        Core._tickCount = 0;
        Timer.Init(0);

        var (m, pet) = SpawnProbe();
        master = m;
        pet.ForceIdle = true; // no wandering; pure cadence
        pet.ControlOrder = OrderType.Stay;

        var settled = RunUntil(() => pet.Thinks >= 2, 8000);
        Assert.True(settled, "the AI must reach a steady think cadence");

        return pet;
    }

    [Fact]
    public void OrderChange_WakesStaleThinkTimer()
    {
        var pet = SettledProbe(out var master);
        var thinksBefore = pet.Thinks;

        RunFor(200); // mid-wait, next think ~200ms out
        Assert.Equal(thinksBefore, pet.Thinks);

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow;

        RunFor(80);
        Assert.True(pet.Thinks > thinksBefore, "a fresh order must wake the AI promptly");
    }

    [Fact]
    public void SpeedUp_ReschedulesPendingWake()
    {
        var pet = SettledProbe(out _);
        var thinksBefore = pet.Thinks;

        RunFor(200); // mid-wait, next think ~200ms out
        Assert.Equal(thinksBefore, pet.Thinks);

        pet.CurrentSpeed = 0.1;

        RunFor(120);
        Assert.True(pet.Thinks > thinksBefore, "a speed-up must reschedule the pending wake");
    }
}
