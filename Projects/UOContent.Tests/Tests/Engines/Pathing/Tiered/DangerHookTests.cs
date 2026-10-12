using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server.Engines.Pathing.Tiered;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding.Tiered;

/// <summary>
/// The danger hook (<see cref="NavigationService.Danger"/>) hung the shard's loop twice on 29.09.2026 inside
/// <c>StrategicPlanner.Corridor</c>, the unwind of a route's parent chain. These routes cross a river by a far ford with a
/// heavily penalised band in the way, many times, each on a clock: a route that does not come back is the hang. It sets
/// the static hook, so it runs alone (see TieredNavigationSequential).
/// </summary>
[Collection("Tiered navigation, one at a time")]
public class DangerHookTests
{
    private readonly ITestOutputHelper _output;

    public DangerHookTests(ITestOutputHelper output) => _output = output;

    private static NavGraph River()
    {
        var terrain = new GridNavTerrain(768, 768, '.');
        terrain.Fill(0, 384, 767, 385, '~');
        terrain.Fill(600, 384, 603, 385, '.');
        var graph = new NavGraph(terrain);
        graph.BuildAll(new NavWindow());

        return graph;
    }

    private static bool Within(Func<NavStatus> route, int ms, out NavStatus status)
    {
        var task = Task.Run(route);

        if (task.Wait(ms))
        {
            status = task.Result;

            return true;
        }

        status = NavStatus.Unplaced;

        return false;
    }

    /// <summary>
    /// The shard's danger moves: quarantines are declared and lifted, blood fades. The strategic planner caches a region's
    /// internal edges (<c>Ensure</c>) and adds the hook's cost on top at relaxation, so a band that moves between routes
    /// makes the cached edges disagree with the costs the search sees. This is the shard's case, not the constant band's.
    /// </summary>
    [Theory]
    [InlineData(60 * NavCost.Step)]
    [InlineData(800 * NavCost.Step)]
    public void MovingBand_EveryRouteComesBack(int penalty)
    {
        var graph = River();
        var medium = new HierarchicalPlanner();
        var strategic = new StrategicPlanner(medium, new NavWindowCache());
        var random = new System.Random(29092027);
        var points = new List<Point3D>();
        var band = 0;

        try
        {
            NavigationService.Danger = (x, y) => x >= band && x <= band + 150 || y >= band / 2 && y <= band / 2 + 60 ? penalty : 0;

            for (var i = 0; i < 160; i++)
            {
                band = random.Next(0, 600);
                var sx = random.Next(10, 750);
                var sy = random.Next(10, 370);
                var gx = random.Next(10, 750);
                var gy = random.Next(400, 760);

                var came = Within(() => strategic.Route(graph, sx, sy, 0, gx, gy, 0, points), 10000, out var status);

                Assert.True(came, $"route {i} from ({sx}, {sy}) to ({gx}, {gy}) with the band at {band} and a penalty of {penalty} did not come back in ten seconds");
                Assert.True(status is NavStatus.Ok or NavStatus.Direct, $"route {i}: {status}");
            }

            _output.WriteLine($"moving band, penalty {penalty}: 160 routes, {strategic.Regions} regions built, {strategic.Expanded} region nodes expanded");
        }
        finally
        {
            NavigationService.Danger = null;
        }
    }

    /// <summary>
    /// The hang itself: a route whose start and goal lie in one region, forced onto the long tier (a medium budget too small
    /// to finish, which is what a heavy danger penalty does to it). The strategic search handed the goal a border node as
    /// its parent without relaxing that node, and the corridor walk ran from the goal into the array's default parent,
    /// node 0, whose parent is 0. Fixed on 29.09.2026 (build 308): the node is relaxed from the start first.
    /// </summary>
    [Fact]
    public void SameRegionOnTheLongTier_ComesBack()
    {
        var terrain = new GridNavTerrain(512, 512, '.');
        var graph = new NavGraph(terrain);
        graph.BuildAll(new NavWindow());
        var medium = new HierarchicalPlanner { MaxExpansions = 1 };
        var strategic = new StrategicPlanner(medium, new NavWindowCache());
        var points = new List<Point3D>();
        var random = new System.Random(29092028);

        for (var i = 0; i < 40; i++)
        {
            // Both ends inside the region of 128 tiles at the origin, far enough apart to need more than one expansion.
            var sx = random.Next(2, 60);
            var sy = random.Next(2, 60);
            var gx = random.Next(70, 126);
            var gy = random.Next(70, 126);

            var came = Within(() => strategic.Route(graph, sx, sy, 0, gx, gy, 0, points), 10000, out var status);

            Assert.True(came, $"same-region route {i} from ({sx}, {sy}) to ({gx}, {gy}) did not come back in ten seconds");
            Assert.True(status is NavStatus.Ok or NavStatus.Direct or NavStatus.BudgetExceeded, $"route {i}: {status}");
        }

        Assert.Equal(0, strategic.Cycles);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60 * NavCost.Step)]
    [InlineData(800 * NavCost.Step)]
    [InlineData(5000 * NavCost.Step)]
    public void PenalisedBand_EveryRouteComesBack(int penalty)
    {
        var graph = River();
        var medium = new HierarchicalPlanner();
        var strategic = new StrategicPlanner(medium, new NavWindowCache());
        var random = new System.Random(29092026);
        var points = new List<Point3D>();

        try
        {
            // A band across the whole field, both banks, that costs `penalty` a node to enter.
            NavigationService.Danger = penalty == 0 ? null : (x, _) => x is >= 300 and <= 450 ? penalty : 0;

            for (var i = 0; i < 120; i++)
            {
                var sx = random.Next(10, 750);
                var sy = random.Next(10, 370);
                var gx = random.Next(10, 750);
                var gy = random.Next(400, 760);

                var came = Within(() => strategic.Route(graph, sx, sy, 0, gx, gy, 0, points), 10000, out var status);

                Assert.True(came, $"route {i} from ({sx}, {sy}) to ({gx}, {gy}) with a penalty of {penalty} did not come back in ten seconds");
                Assert.True(status is NavStatus.Ok or NavStatus.Direct, $"route {i}: {status}");

                if (i % 3 == 0)
                {
                    // The medium tier on its own, as the walk asks it first.
                    came = Within(() => medium.Route(graph, sx, sy, 0, gx, gy, 0, points), 10000, out status);
                    Assert.True(came, $"medium route {i} with a penalty of {penalty} did not come back");
                }
            }

            _output.WriteLine($"penalty {penalty}: 120 strategic routes, {strategic.Regions} regions built, {strategic.Expanded} region nodes and {medium.Expanded} corridor nodes expanded");
        }
        finally
        {
            NavigationService.Danger = null;
        }
    }
}
