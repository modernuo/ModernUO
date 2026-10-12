using System.Collections.Generic;
using Server.Engines.Pathing.Tiered;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding.Tiered;

public class StrategicPlannerTests
{
    private readonly ITestOutputHelper _output;

    public StrategicPlannerTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// A river across the whole map with one ford far to the east, and two places a short way apart on either bank in the
    /// west: the straight line says near, the road says far. The medium tier alone runs out of budget; the long tier finds
    /// the corridor, and the medium tier's route inside it is walkable and close to the oracle's.
    /// </summary>
    [Fact]
    public void RiverWithAFarFord_TheLongTierFindsTheCorridor()
    {
        var terrain = new GridNavTerrain(768, 768, '.');

        terrain.Fill(0, 384, 767, 385, '~');
        terrain.Fill(600, 384, 603, 385, '.');

        var graph = new NavGraph(terrain);

        graph.BuildAll(new NavWindow());

        var points = new List<Point3D>();
        var medium = new HierarchicalPlanner { MaxExpansions = 300 };

        Assert.Equal(NavStatus.BudgetExceeded, medium.Route(graph, 20, 300, 0, 20, 470, 0, points));

        medium.MaxExpansions = 1_000_000;

        Assert.Equal(NavStatus.Ok, medium.Route(graph, 20, 300, 0, 20, 470, 0, points));

        var unbounded = medium.LastExpanded;
        var mediumCost = medium.LastCost;

        var strategic = new StrategicPlanner(medium, new NavWindowCache());
        var status = strategic.Route(graph, 20, 300, 0, 20, 470, 0, points);
        var oracle = terrain.Oracle(20, 300, 0, 20, 470, 0);

        _output.WriteLine(
            $"{status}: cost {medium.LastCost} against the oracle's {oracle} and the unbounded medium's {mediumCost}; "
            + $"{strategic.LastExpanded} region nodes and {medium.LastExpanded} corridor nodes expanded against {unbounded}, "
            + $"{strategic.Regions} regions built"
        );

        Assert.Equal(NavStatus.Ok, status);
        Assert.True(medium.LastCost >= oracle);
        Assert.True(medium.LastCost <= oracle * 1.15);
        Assert.True(strategic.LastExpanded + medium.LastExpanded < unbounded);

        var at = new Point3D(20, 300, 0);

        foreach (var point in points)
        {
            Assert.True(terrain.Oracle(at.X, at.Y, at.Z, point.X, point.Y, point.Z) >= 0, $"no way from {at} to {point}");
            at = point;
        }

        Assert.True(terrain.Oracle(at.X, at.Y, at.Z, 20, 470, 0) >= 0);
    }

    [Fact]
    public void SealedRiver_IsUnreachableForTheLongTierToo()
    {
        var terrain = new GridNavTerrain(512, 512, '.');

        terrain.Fill(0, 256, 511, 257, '~');

        var graph = new NavGraph(terrain);

        graph.BuildAll(new NavWindow());

        var medium = new HierarchicalPlanner();
        var strategic = new StrategicPlanner(medium, new NavWindowCache());

        Assert.Equal(NavStatus.Unreachable, strategic.Route(graph, 20, 100, 0, 20, 400, 0, new List<Point3D>()));
    }
}
