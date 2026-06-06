using System.Collections.Generic;
using Server.Engines.Pathing.Cache;
using Server.Mobiles;
using Xunit;

namespace Server.Tests.Mobiles.AI;

// AI movement tests drive the real BaseAI primitives against live map statics. The
// pathfinder uses non-reentrant static buffers, so we share the pathfinding sequential
// collection to avoid parallel interference.
[Collection("Sequential Pathfinding Tests")]
public class ApproachTargetTests
{
    private sealed class FollowerStub : BaseCreature
    {
        // Serial ctor + DefaultMobileInit bypasses NPCSpeeds JSON (absent in tests).
        public FollowerStub(Serial serial) : base(serial) => Body = 0xC9;
    }

    private sealed class TargetStub : Mobile
    {
        public TargetStub() => Body = 0xC9;
    }

    private static (FollowerStub bc, BaseAI ai) NewFollower(Map map, Point3D loc)
    {
        var bc = new FollowerStub(World.NewMobile);
        bc.DefaultMobileInit();
        bc.MoveToWorld(loc, map);
        BaseAI ai = new AnimalAI(bc);
        ai.AITimer?.Stop(); // we drive movement manually; no background timer
        return (bc, ai);
    }

    // Drives the pet follow primitive once per tick; returns true once it reaches within
    // arriveDist tiles. Uses InRange (Chebyshev) to match the game's own range semantics —
    // a diagonal neighbor at Euclidean ~2.24 is "within 2" to the engine.
    private static bool DriveFollow(BaseAI ai, Mobile bc, Mobile target, int arriveDist, int maxTicks)
    {
        for (var i = 0; i < maxTicks; i++)
        {
            ai.NextMove = 0;
            ai.WalkMobileRange(target, 1, false, 1, 2);
            if (bc.InRange(target, arriveDist))
            {
                return true;
            }
        }
        return false;
    }

    private static ushort FirstImpassableItemId()
    {
        for (ushort id = 1; id < TileData.MaxItemValue; id++)
        {
            if (TileData.ItemTable[id].ImpassableSurface)
            {
                return id;
            }
        }
        return 0;
    }

    [Fact]
    public void OpenTerrain_ReachesTarget_WithoutPathfinding()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var (bc, ai) = NewFollower(map, new Point3D(1500, 1600, (sbyte)z));
        var target = new TargetStub();
        target.MoveToWorld(new Point3D(1495, 1600, (sbyte)z), map); // 5 tiles west, open

        StepCache.Instance.Clear();
        var buildsBefore = StepCache.Instance.GetStats().BuildsTotal;

        var arrived = DriveFollow(ai, bc, target, 2, 40);

        var buildsAfter = StepCache.Instance.GetStats().BuildsTotal;
        bc.Delete();
        target.Delete();

        Assert.True(arrived, "creature should reach an open-terrain target by stepping");
        Assert.Equal(buildsBefore, buildsAfter); // greedy fast path: no chunk builds
    }

    [Fact]
    public void BritainInnDesk_PetReachesMaster()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);

        // Exact in-game repro: pet south of the L-desk, master north of it.
        var (bc, ai) = NewFollower(map, new Point3D(1493, 1614, 20));
        var target = new TargetStub();
        target.MoveToWorld(new Point3D(1494, 1605, 21), map);

        StepCache.Instance.Clear();

        // 200 ticks is generous for the ~17-step detour at one step per tick.
        var arrived = DriveFollow(ai, bc, target, 2, 200);

        bc.Delete();
        target.Delete();

        Assert.True(arrived, "pet must navigate around the L-desk to reach the master");
    }

    [Fact]
    public void MoveTo_ReachesTargetAroundDesk()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var (bc, ai) = NewFollower(map, new Point3D(1493, 1614, 20));
        var target = new TargetStub();
        target.MoveToWorld(new Point3D(1494, 1605, 21), map);

        StepCache.Instance.Clear();

        var arrived = false;
        for (var i = 0; i < 200; i++)
        {
            ai.NextMove = 0;
            ai.MoveTo(target, false, 1);
            if (bc.InRange(target, 1))
            {
                arrived = true;
                break;
            }
        }

        bc.Delete();
        target.Delete();

        Assert.True(arrived, "MoveTo chaser must reach the target around the desk");
    }

    [Fact]
    public void MoveTo_CatchesTargetWalkingAway_OpenTerrain()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var (bc, ai) = NewFollower(map, new Point3D(1500, 1600, (sbyte)z));
        var target = new TargetStub();
        target.MoveToWorld(new Point3D(1498, 1600, (sbyte)z), map);

        StepCache.Instance.Clear();

        var caught = false;
        for (var i = 0; i < 60; i++)
        {
            ai.NextMove = 0;
            ai.MoveTo(target, true, 1);

            // Target walks west every other tick for its first several steps, then stops,
            // so a same-speed chaser eventually closes the gap.
            if (i % 2 == 0 && i < 16 && target.X > 1490)
            {
                target.MoveToWorld(new Point3D(target.X - 1, target.Y, target.Z), map);
            }

            if (bc.InRange(target, 1))
            {
                caught = true;
                break;
            }
        }

        bc.Delete();
        target.Delete();

        Assert.True(caught, "chaser must catch a target that walks away then stops");
    }

    [Fact]
    public void UnreachableTarget_GivesUp_AndIdles()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1601, out _, out var z, out _);

        var (bc, ai) = NewFollower(map, new Point3D(1500, 1601, (sbyte)z));
        var target = new TargetStub();
        target.MoveToWorld(new Point3D(1500, 1596, (sbyte)z), map);

        // Full impassable ring around the target cell -> no path to within range 1.
        var id = FirstImpassableItemId();
        Assert.NotEqual<ushort>(0, id);
        var ring = new List<Item>();
        for (var x = 1499; x <= 1501; x++)
        {
            for (var y = 1595; y <= 1597; y++)
            {
                if (x == 1500 && y == 1596)
                {
                    continue; // leave the target's own cell
                }
                map.GetAverageZ(x, y, out _, out var rz, out _);
                ring.Add(new Item(World.NewItem)
                {
                    ItemID = id, Map = map, Location = new Point3D(x, y, (sbyte)rz)
                });
            }
        }

        StepCache.Instance.Clear();

        // Run well past the stuck window so give-up engages.
        for (var i = 0; i < 120; i++)
        {
            ai.NextMove = 0;
            ai.MoveTo(target, false, 1);
        }

        // After giving up, the creature must idle (not oscillate) while the goal is still.
        var idleStart = bc.Location;
        var stayedIdle = true;
        for (var i = 0; i < 20; i++)
        {
            ai.NextMove = 0;
            ai.MoveTo(target, false, 1);
            if (bc.Location != idleStart)
            {
                stayedIdle = false;
                break;
            }
        }

        var reached = bc.InRange(target, 1);

        bc.Delete();
        target.Delete();
        foreach (var it in ring)
        {
            it.Delete();
        }

        Assert.False(reached, "walled-off target must not be reached");
        Assert.True(stayedIdle, "after giving up, the creature must idle, not shuffle");
    }

    [Fact]
    public void WallBetween_RoutesAround_ReachesTarget()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var (bc, ai) = NewFollower(map, new Point3D(1500, 1600, (sbyte)z));
        var target = new TargetStub();
        target.MoveToWorld(new Point3D(1500, 1595, (sbyte)z), map); // 5 tiles north

        // Impassable wall on Y=1598 from X=1497..1503: blocks the straight line; ends open.
        var id = FirstImpassableItemId();
        Assert.NotEqual<ushort>(0, id);
        var wall = new List<Item>();
        for (var x = 1497; x <= 1503; x++)
        {
            map.GetAverageZ(x, 1598, out _, out var wz, out _);
            wall.Add(new Item(World.NewItem)
            {
                ItemID = id, Map = map, Location = new Point3D(x, 1598, (sbyte)wz)
            });
        }

        StepCache.Instance.Clear();

        var arrived = DriveFollow(ai, bc, target, 2, 200);

        bc.Delete();
        target.Delete();
        foreach (var it in wall)
        {
            it.Delete();
        }

        Assert.True(arrived, "creature must route around the wall to reach the target");
    }
}
