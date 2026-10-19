using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Pathing.Cache;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Driven manually against live map statics; the pathfinder's buffers are not reentrant.
[Collection("Sequential Pathfinding Tests")]
public class ApproachOutcomeTests : IDisposable
{
    private readonly List<IEntity> _created = [];

    public void Dispose()
    {
        for (var i = _created.Count - 1; i >= 0; i--)
        {
            _created[i].Delete();
        }

        _created.Clear();
    }

    private (BareCreatureStub bc, BaseAI ai) NewFollower(Map map, int x, int y)
    {
        map.GetAverageZ(x, y, out _, out var z, out _);
        var bc = new BareCreatureStub(World.NewMobile);
        bc.DefaultMobileInit();
        bc.MoveToWorld(new Point3D(x, y, (sbyte)z), map);
        _created.Add(bc);
        BaseAI ai = new AnimalAI(bc);
        ai.AITimer?.Stop();
        return (bc, ai);
    }

    private WalkerStub NewTarget(Map map, int x, int y)
    {
        map.GetAverageZ(x, y, out _, out var z, out _);
        var t = new WalkerStub();
        t.MoveToWorld(new Point3D(x, y, (sbyte)z), map);
        _created.Add(t);
        return t;
    }

    [SkippableFact]
    public void OpenGround_ReportsDirectProgress_ThenArrived()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Maps[1];
        var (_, ai) = NewFollower(map, 1500, 1600);
        var target = NewTarget(map, 1497, 1600);
        StepCache.Instance.Clear();

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

    [SkippableFact]
    public void WalledTarget_ReportsGaveUp()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Maps[1];
        var (_, ai) = NewFollower(map, 1500, 1601);
        var target = NewTarget(map, 1500, 1596);
        var id = TestTiles.FirstImpassableItemId();
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
                _created.Add(new Item(World.NewItem) { ItemID = id, Map = map, Location = new Point3D(x, y, (sbyte)rz) });
            }
        }

        StepCache.Instance.Clear();

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
}
