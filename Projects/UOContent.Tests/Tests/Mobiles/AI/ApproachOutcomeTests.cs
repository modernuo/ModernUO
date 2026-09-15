using System.Collections.Generic;
using Server;
using Server.Engines.Pathing.Cache;
using Server.Mobiles;
using Server.Tests;
using Server.Tests.Mobiles.AI;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Driven manually against live map statics; the pathfinder's buffers are not reentrant.
[Collection("Sequential Pathfinding Tests")]
public class ApproachOutcomeTests
{
    private sealed class Stub : BaseCreature
    {
        public Stub(Serial serial) : base(serial) => Body = 0xC9;
    }

    private sealed class Target : Mobile
    {
        public Target() => Body = 0xC9;
    }

    private static (Stub bc, BaseAI ai) NewFollower(Map map, int x, int y)
    {
        map.GetAverageZ(x, y, out _, out var z, out _);
        var bc = new Stub(World.NewMobile);
        bc.DefaultMobileInit();
        bc.MoveToWorld(new Point3D(x, y, (sbyte)z), map);
        BaseAI ai = new AnimalAI(bc);
        ai.AITimer?.Stop();
        return (bc, ai);
    }

    private static Target NewTarget(Map map, int x, int y)
    {
        map.GetAverageZ(x, y, out _, out var z, out _);
        var t = new Target();
        t.MoveToWorld(new Point3D(x, y, (sbyte)z), map);
        return t;
    }

    [SkippableFact]
    public void OpenGround_ReportsDirectProgress_ThenArrived()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Maps[1];
        var (bc, ai) = NewFollower(map, 1500, 1600);
        var target = NewTarget(map, 1497, 1600);
        StepCache.Instance.Clear();

        try
        {
            ai.NextMove = 0;
            ai.MoveTo(target, 1);
            Assert.Equal(ApproachOutcome.DirectProgress, ai.LastApproach);

            ai.MoveTo(target, 1); // budget consumed: no step
            Assert.Equal(ApproachOutcome.Waiting, ai.LastApproach);

            ai.NextMove = 0;
            ai.MoveTo(target, 1); // lands adjacent: still a progress step
            Assert.Equal(ApproachOutcome.DirectProgress, ai.LastApproach);

            ai.NextMove = 0;
            ai.MoveTo(target, 1);
            Assert.Equal(ApproachOutcome.Arrived, ai.LastApproach);
        }
        finally
        {
            bc.Delete();
            target.Delete();
        }
    }

    [SkippableFact]
    public void WalledTarget_ReportsGaveUp()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Maps[1];
        var (bc, ai) = NewFollower(map, 1500, 1601);
        var target = NewTarget(map, 1500, 1596);
        var ring = new List<Item>();
        var id = ApproachTargetTests.FirstImpassableItemId();
        Assert.NotEqual<ushort>(0, id);

        // Impassable ring around the target: unreachable.
        for (var x = 1499; x <= 1501; x++)
        {
            for (var y = 1595; y <= 1597; y++)
            {
                if (x == 1500 && y == 1596)
                {
                    continue;
                }

                map.GetAverageZ(x, y, out _, out var rz, out _);
                ring.Add(new Item(World.NewItem) { ItemID = id, Map = map, Location = new Point3D(x, y, (sbyte)rz) });
            }
        }

        StepCache.Instance.Clear();

        try
        {
            var sawRouting = false;

            for (var i = 0; i < 120; i++)
            {
                ai.NextMove = 0;
                ai.MoveTo(target, 1);
                sawRouting |= ai.LastApproach is ApproachOutcome.Routing or ApproachOutcome.Blocked;
            }

            Assert.True(sawRouting, "a walled target must route or block before giving up");
            Assert.Equal(ApproachOutcome.GaveUp, ai.LastApproach);
        }
        finally
        {
            bc.Delete();
            target.Delete();

            for (var i = 0; i < ring.Count; i++)
            {
                ring[i].Delete();
            }
        }
    }
}
