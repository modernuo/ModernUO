using System;
using System.Collections.Generic;
using System.IO;
using Server.Engines.Pathing.Tiered;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding.Tiered;

/// <summary>
/// The tiered planners read one static hook, <see cref="NavigationService.Danger"/>, and <c>DangerHookTests</c> sets it: run
/// beside another class's routes it changes their costs under them (StrategicPlannerTests' river failed that way on
/// 29.09.2026 whenever the two ran at once). The classes that set it or measure costs run in this collection, one at a
/// time, after everything else.
/// </summary>
[CollectionDefinition("Tiered navigation, one at a time", DisableParallelization = true)]
public class TieredNavigationSequential
{
}

/// <summary>
/// The route memory (<see cref="NavRouteMemory"/>): a road planned once is served again, from its start or from anywhere
/// along it; a cheaper road replaces it and a dearer one does not; a change in the ground forgets it and a cluster drawn
/// again the same does not; it survives a file; and a road through blood is not served while the blood is on it.
/// </summary>
[Collection("Tiered navigation, one at a time")]
public class NavRouteMemoryTests
{
    private readonly ITestOutputHelper _output;

    public NavRouteMemoryTests(ITestOutputHelper output) => _output = output;

    private static readonly Point3D Start = new(5, 56, 0);

    private static readonly Point3D Goal = new(230, 60, 0);

    /// <summary>
    /// A field of 256 tiles with a wall down x 128 and two gaps in it, a near one at y 40–42 and a far one at y 200–202; the
    /// start stands in a walled yard (the cluster x 0–15, y 48–63) whose only way out is one tile on its east side, so that
    /// every road from it begins at the same gate.
    /// </summary>
    private static GridNavTerrain Field(bool nearGap = true)
    {
        var terrain = new GridNavTerrain(256, 256, '.');

        terrain.Fill(128, 0, 128, 255, '#');
        terrain.Fill(128, 200, 128, 202, '.');

        if (nearGap)
        {
            terrain.Fill(128, 40, 128, 42, '.');
        }

        terrain.Fill(0, 48, 15, 48, '#');
        terrain.Fill(0, 63, 15, 63, '#');
        terrain.Fill(0, 48, 0, 63, '#');
        terrain.Fill(15, 48, 15, 63, '#');
        terrain.Set(15, 56, '.');

        return terrain;
    }

    private static NavGraph Build(GridNavTerrain terrain)
    {
        var graph = new NavGraph(terrain);

        graph.BuildAll(new NavWindow());

        return graph;
    }

    /// <summary>A planner that will not use the near gap: the dear road, round by the far one.</summary>
    private static HierarchicalPlanner Dear(NavGraph graph)
    {
        var planner = new HierarchicalPlanner { ShunCost = 1_000_000 };

        planner.Shun(graph, 127, 41);
        planner.Shun(graph, 128, 41);

        return planner;
    }

    private static List<Point3D> Planned(HierarchicalPlanner planner, NavGraph graph, NavRouteMemory memory, out NavOffer offer)
    {
        var points = new List<Point3D>();

        Assert.Equal(NavStatus.Ok, planner.Route(graph, Start.X, Start.Y, Start.Z, Goal.X, Goal.Y, Goal.Z, points));

        offer = memory.Offer(graph, null, planner.Anchorage, Start, planner.LastGoal, planner.LastPath);

        return points;
    }

    private static void AssertWalkable(GridNavTerrain terrain, Point3D from, List<Point3D> points, Point3D goal)
    {
        var at = from;

        foreach (var point in points)
        {
            Assert.True(terrain.Oracle(at.X, at.Y, at.Z, point.X, point.Y, point.Z) >= 0, $"no way from {at} to {point}");
            at = point;
        }

        Assert.True(terrain.Oracle(at.X, at.Y, at.Z, goal.X, goal.Y, goal.Z) >= 0, $"no way from {at} to the goal {goal}");
    }

    private static bool IsSuffix(List<Point3D> whole, List<Point3D> tail)
    {
        if (tail.Count == 0 || tail.Count > whole.Count)
        {
            return false;
        }

        var offset = whole.Count - tail.Count;

        for (var i = 0; i < tail.Count; i++)
        {
            if (whole[offset + i] != tail[i])
            {
                return false;
            }
        }

        return true;
    }

    [Fact]
    public void RememberedRoute_IsServedTheSame_FromItsStartAndFromAlongIt()
    {
        var terrain = Field();
        var graph = Build(terrain);
        var memory = new NavRouteMemory();
        var planned = Planned(new HierarchicalPlanner(), graph, memory, out var offer);

        Assert.Equal(NavOffer.Stored, offer);
        Assert.Equal(1, memory.Count);

        var recalled = new List<Point3D>();

        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(planned, recalled);

        // Another start in the same yard, another goal in the goal's cluster: the same road, joined at the same gate.
        var elsewhere = new Point3D(9, 60, 0);
        var near = new Point3D(236, 52, 0);

        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, elsewhere, near, recalled, null));
        AssertWalkable(terrain, elsewhere, recalled, near);

        // From the middle of the road: a suffix of it, without a search.
        var middle = planned[planned.Count / 2];

        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, middle, Goal, recalled, null));
        Assert.True(IsSuffix(planned, recalled), $"from {middle}: {string.Join(" ", recalled)} is not the tail of {string.Join(" ", planned)}");
        Assert.Equal(1, memory.Middles);

        // Too near to be worth remembering: the planner's.
        Assert.Equal(NavRecall.Skipped, memory.Recall(graph, null, 2, new Point3D(20, 56, 0), new Point3D(40, 56, 0), recalled, null));

        _output.WriteLine($"{planned.Count} points; {memory.Describe()}");
    }

    [Fact]
    public void CheaperRoute_Replaces_AndADearerOneDoesNot()
    {
        var terrain = Field();
        var graph = Build(terrain);
        var memory = new NavRouteMemory();
        var dear = Planned(Dear(graph), graph, memory, out var offer);

        Assert.Equal(NavOffer.Stored, offer);

        var cheap = Planned(new HierarchicalPlanner(), graph, memory, out offer);

        Assert.Equal(NavOffer.Improved, offer);
        Assert.NotEqual(dear, cheap);

        var recalled = new List<Point3D>();

        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(cheap, recalled);

        // The dear road again: nowhere cheaper than what is held, so nothing changes.
        var count = memory.Count;

        Planned(Dear(graph), graph, memory, out offer);

        Assert.Equal(NavOffer.Kept, offer);
        Assert.Equal(count, memory.Count);
        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(cheap, recalled);

        // The cheap one again: the same road, kept as it was.
        Planned(new HierarchicalPlanner(), graph, memory, out offer);

        Assert.Equal(NavOffer.Kept, offer);
        Assert.Equal(count, memory.Count);

        _output.WriteLine(memory.Describe());
    }

    [Fact]
    public void GroundChange_Forgets_AndAClusterDrawnAgainTheSameDoesNot()
    {
        var terrain = Field();
        var graph = Build(terrain);
        var memory = new NavRouteMemory();
        var planned = Planned(new HierarchicalPlanner(), graph, memory, out _);
        var recalled = new List<Point3D>();

        // Drawn again from the same ground — a house raised and taken down, a boot: the versions change, the signatures do not.
        graph.MarkDirty(128, 40, 128, 42);
        graph.Slice(new NavWindow(), long.MaxValue);

        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(planned, recalled);
        Assert.True(memory.Rejoined > 0);
        Assert.Equal(0, memory.Invalidated);

        // The near gap is walled up: the road through it is forgotten, not served.
        terrain.Fill(128, 40, 128, 42, '#');
        graph.MarkDirty(128, 40, 128, 42);
        graph.Slice(new NavWindow(), long.MaxValue);

        Assert.Equal(NavRecall.Missed, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(1, memory.Invalidated);
        Assert.Equal(0, memory.Count);

        // Planned afresh round by the far gap, and that is what is remembered now.
        var round = Planned(new HierarchicalPlanner(), graph, memory, out var offer);

        Assert.Equal(NavOffer.Stored, offer);
        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(round, recalled);
        AssertWalkable(terrain, Start, recalled, Goal);
    }

    [Fact]
    public void Persistence_RoundTrips_AndProvesItselfAgainstTheGraph()
    {
        var terrain = Field();
        var graph = Build(terrain);
        var memory = new NavRouteMemory();
        var planned = Planned(new HierarchicalPlanner(), graph, memory, out _);
        var path = Path.Combine(Path.GetTempPath(), $"navroutes-test-{Guid.NewGuid():N}.nvr");

        try
        {
            NavRouteFile.Write(path, 42, 256, 256, memory.Records(graph));

            Assert.Null(NavRouteFile.Read(path, 43, 256, 256));
            Assert.Null(NavRouteFile.Read(path, 42, 512, 256));

            var read = NavRouteFile.Read(path, 42, 256, 256);

            Assert.NotNull(read);
            Assert.Single(read);

            // A new boot: the same ground drawn again, every node id and version its own.
            var again = Build(terrain);
            var restored = new NavRouteMemory();

            Assert.Equal(1, restored.Adopt(again, read));

            var recalled = new List<Point3D>();

            Assert.Equal(NavRecall.Served, restored.Recall(again, null, 2, Start, Goal, recalled, null));
            Assert.Equal(planned, recalled);
            Assert.True(restored.Rejoined > 0);

            // Another boot on other ground: the near gap walled up. The road is read, and forgotten the first time it is asked.
            var walled = Build(Field(nearGap: false));
            var stale = new NavRouteMemory();

            Assert.Equal(1, stale.Adopt(walled, read));
            Assert.Equal(NavRecall.Missed, stale.Recall(walled, null, 2, Start, Goal, recalled, null));
            Assert.Equal(1, stale.Invalidated);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BloodOnTheRoad_IsPlannedRound_AndTheWayRoundIsKeptWhileItsReasonStands()
    {
        var terrain = Field();
        var graph = Build(terrain);
        var now = 1000L;
        var memory = new NavRouteMemory { Clock = () => now };
        var cheap = Planned(new HierarchicalPlanner(), graph, memory, out _);
        var recalled = new List<Point3D>();

        // Blood round the near gap: the ground road crosses it.
        Func<int, int, int> nearBlood = (x, y) => x is >= 112 and <= 143 && y is >= 32 and <= 50 ? 60 * NavCost.Step : 0;

        Assert.Equal(NavRecall.Bloodied, memory.Recall(graph, null, 2, Start, Goal, recalled, nearBlood));
        Assert.Empty(recalled);

        var ground = memory.LastGroundDanger;

        Assert.True(ground > 0);

        // The ground road is kept at hand for a walker whose way round cannot be found.
        var fallback = new List<Point3D>();

        Assert.True(memory.LastGround(fallback));
        Assert.Equal(cheap, fallback);

        // The way round, as the danger-aware planner would find it, kept as a detour.
        var dear = Dear(graph);
        var round = new List<Point3D>();

        Assert.Equal(NavStatus.Ok, dear.Route(graph, Start.X, Start.Y, Start.Z, Goal.X, Goal.Y, Goal.Z, round));
        Assert.True(memory.OfferDetour(graph, null, dear.Anchorage, Start, dear.LastGoal, dear.LastPath, ground, nearBlood));

        Assert.Equal(NavRecall.Detoured, memory.Recall(graph, null, 2, Start, Goal, recalled, nearBlood));
        Assert.Equal(round, recalled);

        // The blood dries: the ground road again.
        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(cheap, recalled);

        // Blood on the way round too: it is no longer the better road, and it is dropped.
        Func<int, int, int> both = (x, y) => nearBlood(x, y) + (x is >= 112 and <= 143 && y is >= 190 and <= 210 ? 60 * NavCost.Step : 0);

        Assert.Equal(NavRecall.Bloodied, memory.Recall(graph, null, 2, Start, Goal, recalled, both));
        Assert.Equal(0, memory.Detours);

        // Kept again, then past its time.
        Assert.True(memory.OfferDetour(graph, null, dear.Anchorage, Start, dear.LastGoal, dear.LastPath, ground, nearBlood));
        Assert.Equal(NavRecall.Detoured, memory.Recall(graph, null, 2, Start, Goal, recalled, nearBlood));

        now += memory.DetourMs;

        Assert.Equal(NavRecall.Bloodied, memory.Recall(graph, null, 2, Start, Goal, recalled, nearBlood));
        Assert.Equal(0, memory.Detours);
        Assert.Equal(2, memory.DetoursDropped);
    }

    [Fact]
    public void RoadRoundAShunnedGate_IsForgottenWhenTheShunRunsOut()
    {
        var terrain = Field();
        var graph = Build(terrain);
        var now = 1000L;
        var memory = new NavRouteMemory { Clock = () => now };
        var dear = Dear(graph);
        var points = new List<Point3D>();
        var recalled = new List<Point3D>();

        Assert.Equal(NavStatus.Ok, dear.Route(graph, Start.X, Start.Y, Start.Z, Goal.X, Goal.Y, Goal.Z, points));
        Assert.Equal(NavOffer.Stored, memory.Offer(graph, null, dear.Anchorage, Start, dear.LastGoal, dear.LastPath, 60000));

        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(points, recalled);

        // Not kept past a boot: the shuns are not.
        Assert.Empty(memory.Records(graph));

        now += 60000;

        Assert.Equal(NavRecall.Missed, memory.Recall(graph, null, 2, Start, Goal, recalled, null));
        Assert.Equal(1, memory.Expired);
        Assert.Equal(0, memory.Count);
    }

    /// <summary>
    /// The price, measured: routes across a river with one far ford, planned by the tiers as the shard plans them and
    /// offered, then asked again. Every one comes back from memory, the same, and for a fraction of the planning.
    /// </summary>
    [Fact]
    public void Recalling_CostsAFractionOfPlanning()
    {
        var terrain = new GridNavTerrain(768, 768, '.');

        terrain.Fill(0, 384, 767, 385, '~');
        terrain.Fill(600, 384, 603, 385, '.');

        var graph = Build(terrain);
        var cache = new NavWindowCache();
        var medium = new HierarchicalPlanner { Cache = cache, MaxExpansions = 12000 };
        var strategic = new StrategicPlanner(medium, cache);
        var memory = new NavRouteMemory();
        var random = new System.Random(29092029);
        var pairs = new List<(Point3D From, Point3D To, List<Point3D> Points)>();
        var watch = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < 60; i++)
        {
            var from = new Point3D(random.Next(10, 750), random.Next(10, 370), 0);
            var to = new Point3D(random.Next(10, 750), random.Next(400, 760), 0);
            var points = new List<Point3D>();
            var status = medium.Route(graph, from.X, from.Y, from.Z, to.X, to.Y, to.Z, points);

            if (status == NavStatus.BudgetExceeded)
            {
                status = strategic.Route(graph, from.X, from.Y, from.Z, to.X, to.Y, to.Z, points);
            }

            Assert.Equal(NavStatus.Ok, status);
            memory.Offer(graph, cache, medium.Anchorage, from, medium.LastGoal, medium.LastPath);
            pairs.Add((from, to, points));
        }

        var planned = watch.Elapsed.TotalMilliseconds;
        var recalled = new List<Point3D>();

        foreach (var (from, to, points) in pairs)
        {
            Assert.Equal(NavRecall.Served, memory.Recall(graph, cache, medium.Anchorage, from, to, recalled, null));
            Assert.Equal(points, recalled);
        }

        // Timed on the second asking, the code warm, as it is on the shard.
        watch.Restart();

        foreach (var (from, to, _) in pairs)
        {
            memory.Recall(graph, cache, medium.Anchorage, from, to, recalled, null);
        }

        var served = watch.Elapsed.TotalMilliseconds;

        watch.Restart();

        foreach (var (from, to, points) in pairs)
        {
            medium.Route(graph, from.X, from.Y, from.Z, to.X, to.Y, to.Z, points);
        }

        var replanned = watch.Elapsed.TotalMilliseconds;

        _output.WriteLine($"60 routes planned and offered in {planned:F1}ms, planned again in {replanned:F1}ms, recalled in {served:F1}ms; {memory.Describe()}");

        Assert.True(served < planned, $"recalling took {served:F1}ms against planning's {planned:F1}ms");
    }

    [Fact]
    public void Bounded_TheLeastLatelyUsedGoesFirst()
    {
        var terrain = Field();
        var graph = Build(terrain);
        var memory = new NavRouteMemory { Capacity = 1 };
        var planner = new HierarchicalPlanner();
        var points = new List<Point3D>();

        Planned(planner, graph, memory, out _);

        // Another pair of clusters entirely: from the south-west to the north-east.
        Assert.Equal(NavStatus.Ok, planner.Route(graph, 20, 240, 0, 240, 20, 0, points));
        Assert.Equal(NavOffer.Stored, memory.Offer(graph, null, planner.Anchorage, new Point3D(20, 240, 0), planner.LastGoal, planner.LastPath));

        Assert.Equal(1, memory.Count);
        Assert.Equal(1, memory.Evicted);
        Assert.Equal(NavRecall.Missed, memory.Recall(graph, null, 2, Start, Goal, points, null));
        Assert.Equal(NavRecall.Served, memory.Recall(graph, null, 2, new Point3D(20, 240, 0), new Point3D(240, 20, 0), points, null));
    }
}
