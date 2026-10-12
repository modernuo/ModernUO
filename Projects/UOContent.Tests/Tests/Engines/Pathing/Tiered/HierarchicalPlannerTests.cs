using System;
using System.Collections.Generic;
using Server.Engines.Pathing.Tiered;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding.Tiered;

public class HierarchicalPlannerTests
{
    private readonly ITestOutputHelper _output;

    public HierarchicalPlannerTests(ITestOutputHelper output) => _output = output;

    private static NavGraph Build(GridNavTerrain terrain)
    {
        var graph = new NavGraph(terrain);

        graph.BuildAll(new NavWindow());

        return graph;
    }

    /// <summary>Every leg of a route — start, the points, the goal — must be walkable on its own, and cost no more in all than the route says.</summary>
    private static void AssertWalkable(GridNavTerrain terrain, Point3D start, List<Point3D> points, Point3D goal, int cost)
    {
        var at = start;
        var sum = 0;

        foreach (var point in points)
        {
            var leg = terrain.Oracle(at.X, at.Y, at.Z, point.X, point.Y, point.Z);

            Assert.True(leg >= 0, $"no way from {at} to the waypoint {point}");
            sum += leg;
            at = point;
        }

        var last = terrain.Oracle(at.X, at.Y, at.Z, goal.X, goal.Y, goal.Z);

        Assert.True(last >= 0, $"no way from the last waypoint {at} to the goal {goal}");
        sum += last;

        Assert.True(sum <= cost, $"the legs cost {sum}, more than the route's {cost}");
    }

    [Fact]
    public void OpenField_RouteIsWithinTenPercentOfOptimal()
    {
        var terrain = new GridNavTerrain(64, 64, '.');
        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        var status = planner.Route(graph, 2, 3, 0, 60, 57, 0, points);
        var oracle = terrain.Oracle(2, 3, 0, 60, 57, 0);

        _output.WriteLine($"{status}: cost {planner.LastCost} against {oracle}, {points.Count} points, {planner.LastExpanded} expanded");

        Assert.Equal(NavStatus.Ok, status);
        Assert.True(planner.LastCost >= oracle);
        Assert.True(planner.LastCost <= oracle * 1.1 + 2 * NavCost.Step);
        Assert.NotEmpty(points);
        AssertWalkable(terrain, new Point3D(2, 3, 0), points, new Point3D(60, 57, 0), planner.LastCost);
    }

    [Fact]
    public void SameClusterAndJoinedInside_IsDirect()
    {
        var terrain = new GridNavTerrain(64, 64, '.');
        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        Assert.Equal(NavStatus.Direct, planner.Route(graph, 1, 1, 0, 10, 12, 0, points));
        Assert.Equal(terrain.Oracle(1, 1, 0, 10, 12, 0), planner.LastCost);
    }

    [Fact]
    public void WallWithAGap_RouteGoesThroughTheGap()
    {
        var terrain = new GridNavTerrain(64, 64, '.');

        terrain.Fill(32, 0, 32, 63, '#');
        terrain.Fill(32, 50, 32, 52, '.');

        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        var status = planner.Route(graph, 5, 5, 0, 60, 5, 0, points);
        var oracle = terrain.Oracle(5, 5, 0, 60, 5, 0);

        _output.WriteLine($"{status}: cost {planner.LastCost} against {oracle}, points {string.Join(" ", points)}");

        Assert.Equal(NavStatus.Ok, status);
        Assert.True(planner.LastCost >= oracle);
        Assert.True(planner.LastCost <= oracle * 1.1 + 2 * NavCost.Step);
        Assert.Contains(points, p => p.X >= 31 && p.X <= 33 && p.Y >= 49 && p.Y <= 53);
        AssertWalkable(terrain, new Point3D(5, 5, 0), points, new Point3D(60, 5, 0), planner.LastCost);
    }

    [Fact]
    public void SealedWall_IsUnreachableWithoutSearching()
    {
        var terrain = new GridNavTerrain(64, 64, '.');

        terrain.Fill(32, 0, 32, 63, '#');

        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();

        Assert.True(graph.ComponentsValid);
        Assert.Equal(NavStatus.Unreachable, planner.Route(graph, 5, 5, 0, 60, 5, 0, new List<Point3D>()));
        Assert.Equal(0, planner.LastExpanded);
    }

    /// <summary>
    /// A bridge deck at 20 runs east and west over a river; a road at 0 runs north and south under it, walled in by
    /// water. The road passes under the deck, the deck passes over the road, and neither can reach the other.
    /// </summary>
    [Fact]
    public void BridgeOverARoad_TheTwoLevelsStayApart()
    {
        var terrain = new GridNavTerrain(64, 64, '~');

        // The banks, west and east, and the approaches up to the deck: 0, 5, 10, 15, 20.
        terrain.Fill(0, 18, 7, 22, '.');
        terrain.Fill(56, 18, 63, 22, '.');
        terrain.Fill(8, 18, 8, 22, '/');
        terrain.Fill(55, 18, 55, 22, '/');
        terrain.Fill(9, 18, 9, 22, 'h');
        terrain.Fill(54, 18, 54, 22, 'h');

        for (var y = 18; y <= 22; y++)
        {
            terrain.Set(10, y, 'r');
            terrain.Set(53, y, 'r');
        }

        terrain.Fill(11, 18, 52, 22, '=');

        // The road, north to south, under the deck.
        terrain.Fill(30, 0, 34, 63, '.');
        terrain.Fill(30, 18, 34, 22, '+');

        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        var under = planner.Route(graph, 32, 2, 0, 32, 60, 0, points);

        _output.WriteLine($"under: {under}, cost {planner.LastCost}, points {string.Join(" ", points)}");
        Assert.Equal(NavStatus.Ok, under);
        Assert.All(points, p => Assert.Equal(0, p.Z));
        AssertWalkable(terrain, new Point3D(32, 2, 0), points, new Point3D(32, 60, 0), planner.LastCost);

        var over = planner.Route(graph, 2, 20, 0, 61, 20, 0, points);

        _output.WriteLine($"over: {over}, cost {planner.LastCost}, points {string.Join(" ", points)}");
        Assert.Equal(NavStatus.Ok, over);
        Assert.Contains(points, p => p.X >= 30 && p.X <= 34 && p.Z == 20);
        AssertWalkable(terrain, new Point3D(2, 20, 0), points, new Point3D(61, 20, 0), planner.LastCost);

        Assert.Equal(NavStatus.Unreachable, planner.Route(graph, 2, 20, 0, 32, 2, 0, points));
    }

    /// <summary>High ground drops to the ground beside it and cannot be climbed back: the graph is directed.</summary>
    [Fact]
    public void OneWayDrop_IsWalkedDownButNotUp()
    {
        var terrain = new GridNavTerrain(64, 64, '.');

        terrain.Fill(32, 0, 63, 63, 'h');

        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        Assert.Equal(NavStatus.Ok, planner.Route(graph, 50, 30, 10, 10, 30, 0, points));
        AssertWalkable(terrain, new Point3D(50, 30, 10), points, new Point3D(10, 30, 0), planner.LastCost);
        Assert.Equal(-1, terrain.Oracle(10, 30, 0, 50, 30, 10));
        Assert.Equal(NavStatus.Unreachable, planner.Route(graph, 10, 30, 0, 50, 30, 10, points));
    }

    /// <summary>
    /// A goal walled in on every side, as a herb on a table or a fire in a closed corner is: unreachable to stand on, and
    /// reached all the same by a walker that arrives within two tiles of it.
    /// </summary>
    [Fact]
    public void GoalInAPocket_ReachSettlesOnACellBeside()
    {
        var terrain = new GridNavTerrain(64, 64, '.');

        terrain.Fill(39, 39, 41, 41, '#');
        terrain.Set(40, 40, '.');

        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        Assert.Equal(NavStatus.Unreachable, planner.Route(graph, 5, 5, 0, 40, 40, 0, points, 0));

        var status = planner.Route(graph, 5, 5, 0, 40, 40, 0, points, 2);

        _output.WriteLine($"{status}: settled on {planner.LastGoal}, cost {planner.LastCost}");
        Assert.True(status is NavStatus.Ok or NavStatus.Direct);
        Assert.True(Math.Max(Math.Abs(planner.LastGoal.X - 40), Math.Abs(planner.LastGoal.Y - 40)) <= 2);
        Assert.True(terrain.Oracle(5, 5, 0, planner.LastGoal.X, planner.LastGoal.Y, 0) >= 0);
    }

    [Fact]
    public void Unbuilt_IsPending()
    {
        var terrain = new GridNavTerrain(64, 64, '.');
        var graph = new NavGraph(terrain);

        graph.Prioritize(0, 0);

        Assert.Equal(NavStatus.Pending, new HierarchicalPlanner().Route(graph, 2, 3, 0, 60, 57, 0, new List<Point3D>()));
    }

    /// <summary>A house raised across the field, then taken down: the graph follows, and leaks no node either way.</summary>
    [Fact]
    public void MarkDirty_RebuildsAndLeaksNothing()
    {
        var terrain = new GridNavTerrain(64, 64, '.');
        var graph = Build(terrain);
        var nodes = graph.Nodes;
        var edges = graph.Edges;
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        terrain.Fill(20, 0, 20, 63, '#');
        terrain.Fill(20, 40, 20, 41, '.');
        graph.MarkDirty(20, 0, 20, 63);
        graph.Slice(new NavWindow(), long.MaxValue);

        Assert.True(graph.Complete);

        var open = terrain.Oracle(5, 5, 0, 40, 5, 0);

        Assert.Equal(NavStatus.Ok, planner.Route(graph, 5, 5, 0, 40, 5, 0, points));

        // The gap lies inside a cluster, so no waypoint need stand in it; the route must go round by it all the same.
        Assert.True(planner.LastCost >= 2 * 35 * NavCost.Step, $"{planner.LastCost} does not go round by the gap");
        Assert.True(planner.LastCost >= open);
        AssertWalkable(terrain, new Point3D(5, 5, 0), points, new Point3D(40, 5, 0), planner.LastCost);

        terrain.Fill(20, 0, 20, 63, '.');
        graph.MarkDirty(20, 0, 20, 63);
        graph.Slice(new NavWindow(), long.MaxValue);

        Assert.Equal(nodes, graph.Nodes);
        Assert.Equal(edges, graph.Edges);
        Assert.Equal(NavStatus.Ok, planner.Route(graph, 5, 5, 0, 40, 5, 0, points));
        Assert.True(planner.LastCost <= terrain.Oracle(5, 5, 0, 40, 5, 0) * 1.1 + 2 * NavCost.Step);
    }

    /// <summary>Random pairs on a field of random walls: never a route cheaper than the oracle, never a false "unreachable".</summary>
    [Fact]
    public void RandomObstacles_AgreeWithTheOracle()
    {
        var random = new System.Random(26092026);
        var terrain = new GridNavTerrain(96, 96, '.');

        for (var i = 0; i < 700; i++)
        {
            var x = random.Next(96);
            var y = random.Next(96);

            terrain.Fill(x, y, Math.Min(95, x + random.Next(4)), Math.Min(95, y + random.Next(4)), '#');
        }

        var graph = Build(terrain);
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();
        var routes = 0;
        var excess = 0.0;
        var worst = 0.0;

        for (var i = 0; i < 300; i++)
        {
            int sx, sy, gx, gy;

            do
            {
                sx = random.Next(96);
                sy = random.Next(96);
            } while (terrain.At(sx, sy).Length == 0);

            do
            {
                gx = random.Next(96);
                gy = random.Next(96);
            } while (terrain.At(gx, gy).Length == 0);

            var oracle = terrain.Oracle(sx, sy, 0, gx, gy, 0);
            var status = planner.Route(graph, sx, sy, 0, gx, gy, 0, points);

            if (oracle < 0)
            {
                Assert.True(status == NavStatus.Unreachable, $"({sx},{sy}) to ({gx},{gy}): {status}, but there is no way");

                continue;
            }

            Assert.True(status is NavStatus.Ok or NavStatus.Direct, $"({sx},{sy}) to ({gx},{gy}): {status}, but the oracle walks it in {oracle}");
            Assert.True(planner.LastCost >= oracle, $"({sx},{sy}) to ({gx},{gy}): {planner.LastCost} beats the oracle's {oracle}");

            if (status == NavStatus.Ok)
            {
                AssertWalkable(terrain, new Point3D(sx, sy, 0), points, new Point3D(gx, gy, 0), planner.LastCost);
            }

            if (oracle > 0)
            {
                var over = (planner.LastCost - oracle) / (double)oracle;

                excess += over;
                worst = Math.Max(worst, over);
                routes++;
            }
        }

        _output.WriteLine($"{routes} routes: {100 * excess / routes:F1}% over the oracle on average, {100 * worst:F1}% at worst");
        Assert.True(excess / routes <= 0.10, $"{100 * excess / routes:F1}% over the oracle on average");
    }
}
