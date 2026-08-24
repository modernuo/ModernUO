using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Guard-order following drives real movement primitives (and may pathfind), so it shares
// the pathfinding sequential collection like ApproachTargetTests.
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

        pet.Delete();
        master.Delete();

        Assert.True(moved, "a guarding pet beyond guard range must step toward its master");
        // Between-think move wakes require a registered move intent; bare greedy stepping
        // quantizes guard-following to the think grid (issue #2593).
        Assert.True(hasIntent, "guard-following must register a move intent");
    }
}
