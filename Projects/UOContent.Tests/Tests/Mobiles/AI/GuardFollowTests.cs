using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Guard-following may pathfind, so this shares the pathfinding collection.
[Collection("Sequential Pathfinding Tests")]
public class GuardFollowTests
{
    [Fact]
    public void GuardFollow_StepsTowardMaster_AndRegistersMoveIntent()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        master.MoveToWorld(new Point3D(1494, 1600, (sbyte)z), map);

        var pet = new PetTestStub();
        pet.MoveToWorld(new Point3D(1500, 1600, (sbyte)z), map); // 6 tiles east, open terrain
        pet.SetControlMaster(master);

        var ai = pet.AIObject;
        ai.AITimer?.Stop(); // drive manually
        pet.ControlOrder = OrderType.Guard;
        ai.AITimer?.Stop(); // the order change may restart the timer

        var start = pet.Location;
        ai.NextMove = 0;
        ai.Obey();

        var moved = pet.Location != start;
        var hasIntent = ai.TryGetMoveWake(out _);
        var currentSpeed = pet.CurrentSpeed;
        var currentMoveSpeed = pet.CurrentMoveSpeed;

        pet.Delete();
        master.Delete();

        Assert.True(moved, "a guarding pet beyond guard range must step toward its master");
        // Without a move intent, guard-following only steps on the think grid.
        Assert.True(hasIntent, "guard-following must register a move intent");

        // AOS return sprint on both clocks; the per-step speed flip must not undo it.
        Assert.Equal(0.1, currentSpeed);
        Assert.Equal(0.1, currentMoveSpeed);
    }

    [Fact]
    public void GuardReturn_PreAOS_RunsActive()
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.UOR;

            var map = Map.Maps[1];
            Assert.NotNull(map);
            map.GetAverageZ(1500, 1600, out _, out var z, out _);

            var master = new PlayerMobile(World.NewMobile);
            master.DefaultMobileInit();
            master.MoveToWorld(new Point3D(1494, 1600, (sbyte)z), map);

            var pet = new PetTestStub();
            pet.MoveToWorld(new Point3D(1500, 1600, (sbyte)z), map);
            pet.SetControlMaster(master);

            var ai = pet.AIObject;
            ai.AITimer?.Stop();
            pet.ControlOrder = OrderType.Guard;
            ai.AITimer?.Stop();
            pet.SetCurrentSpeedToPassive(); // a stale passive state must not persist

            ai.NextMove = 0;
            ai.Obey();

            var currentSpeed = pet.CurrentSpeed;

            pet.Delete();
            master.Delete();

            // No sprint pre-AOS: the return runs active.
            Assert.Equal(0.2, currentSpeed);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }
}
