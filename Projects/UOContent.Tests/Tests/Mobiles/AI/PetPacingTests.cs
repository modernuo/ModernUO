using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Pins the pet-obedience pacing policy (issue #2593): a controlled pet executing a
// master's movement order paces its steps on the think clock, not the wild-creature
// move table; combat chases and herding keep their own pacing.
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

    // Issuing an order sets the think clock organically (RunUO OnCurrentOrderChanged
    // parity): movement orders run active, resting orders run passive. The move clock
    // then resolves through the normal classification — no special-casing.
    [Fact]
    public void OrderIssue_SetsThinkClock()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);
        pet.SetCurrentSpeedToPassive();

        pet.ControlOrder = OrderType.Come;
        Assert.Equal(0.2, pet.CurrentSpeed);
        Assert.Equal(0.3, pet.CurrentMoveSpeed); // organic: verbatim active -> activeMove

        pet.ControlOrder = OrderType.Stay;
        Assert.Equal(0.4, pet.CurrentSpeed);
        Assert.Equal(0.9, pet.CurrentMoveSpeed);

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow;
        Assert.Equal(0.2, pet.CurrentSpeed);

        pet.ControlOrder = OrderType.Guard;
        Assert.Equal(0.2, pet.CurrentSpeed);
    }

    // RunUO AOS parity: a pet following its master sprints — DoOrderFollow writes the
    // bespoke 0.1, which fuses to both clocks through the normal classification.
    [Fact]
    public void FollowMaster_ObeySprints()
    {
        var (master, pet) = Spawn(new Point3D(1000, 1000, 0), new Point3D(1001, 1000, 0));
        pet.SetMoveSpeed(0.3, 0.9);
        pet.AIObject.AITimer?.Stop();

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow; // fixture era is EJ: Core.AOS is true
        pet.AIObject.Obey();

        Assert.Equal(0.1, pet.CurrentSpeed);
        Assert.Equal(0.1, pet.CurrentMoveSpeed);
    }

    // A guarding pet at its master's side stays organically active — never the
    // stale-warmode passive lottery, and no sprint while there is nowhere to go.
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

    // Boundary guard: a pet chasing a combatant keeps the move table.
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

    // Boundary guard: herding overrides obedience pacing.
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

    // Advances simulated time in 8ms lockstep with the wheel, like the real event loop,
    // so wake schedules and Core.TickCount stay in sync.
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

    // Runs past the random spawn-stagger delay to a known think-tick anchor: returns
    // right after a think fires, with the next one a full passive cadence (0.4s) away.
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

        // Mid-wait on the passive cadence: the next think is ~200ms out.
        RunFor(200);
        Assert.Equal(thinksBefore, pet.Thinks);

        // The player issues a command; the pet must not wait out the stale wake.
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

        // Mid-wait on the passive cadence: the next think is ~200ms out.
        RunFor(200);
        Assert.Equal(thinksBefore, pet.Thinks);

        // The pet is sped up (e.g. a buff): the next think must move up to the new
        // 0.1s cadence instead of waiting out the stale 0.4s deadline.
        pet.CurrentSpeed = 0.1;

        RunFor(120);
        Assert.True(pet.Thinks > thinksBefore, "a speed-up must reschedule the pending wake");
    }
}
