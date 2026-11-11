using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// A following pet steps once per master step, on the master's step: arrival clears the move
// intent, and a think-gated re-approach would pace the pet off its own clock instead.
[Collection("Sequential UOContent Tests")]
public class PetFollowCadenceTests : IDisposable
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

    // Medium-class think clock (250 ms active), slower than a running master's 200 ms steps.
    private sealed class FollowProbe : PetTestStub
    {
        public readonly List<long> StepTicks = new();

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.25;
            passiveSpeed = 0.5;
        }

        public override void OnAfterMove(Point3D oldLocation)
        {
            StepTicks.Add(Core.TickCount);
            base.OnAfterMove(oldLocation);
        }
    }

    private static void RunFor(long ms)
    {
        var deadline = Core._tickCount + ms;

        while (Core._tickCount < deadline)
        {
            Core._tickCount += 8;
            Timer.Slice(Core._tickCount);
        }
    }

    [Theory]
    [InlineData(200)] // running on foot
    [InlineData(400)] // walking on foot
    public void FollowingPet_StepsOncePerMasterStep(int masterStepMs)
    {
        Core._tickCount = 0;
        Timer.Init(0);

        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        master.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        master.Direction = Direction.West;
        _created.Add(master);

        var pet = new FollowProbe();
        pet.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
        pet.SetControlMaster(master);
        pet.SetMoveSpeed(0.45, 1.05); // AOS follow pace caps the step at 0.1
        _created.Add(pet);

        pet.ControlTarget = master;
        pet.ControlOrder = OrderType.Follow;

        RunFor(2000); // past the spawn stagger
        pet.StepTicks.Clear();

        const int masterSteps = 40;

        for (var i = 0; i < masterSteps; i++)
        {
            Assert.True(master.Move(Direction.West | Direction.Running), $"master step {i} refused");
            Assert.True(pet.GetDistanceToSqrt(master) <= 3, $"pet fell {pet.GetDistanceToSqrt(master)} tiles behind at step {i}");
            RunFor(masterStepMs);
        }

        Assert.Equal(masterSteps - 1, pet.StepTicks.Count);

        for (var i = 1; i < pet.StepTicks.Count; i++)
        {
            var interval = pet.StepTicks[i] - pet.StepTicks[i - 1];
            Assert.True(Math.Abs(interval - masterStepMs) <= 8, $"pet step {i} came {interval} ms after the previous one");
        }
    }
}
