using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Server.Engines.Pathing.Cache;
using Server.Logging;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// The tiered navigation, as the rest of the shard sees it: one graph a map, drawn on the game loop a slice at a time,
/// kept in step with the houses raised and taken down, and asked for routes.
///
/// <para>
/// <b>The tiers.</b> A walk nearer than a cluster or two is the precise search's alone (Short: the caller's own tile
/// planner, with everything dynamic in it — doors, creatures, items). A longer one is asked of the first level of the
/// graph (<see cref="HierarchicalPlanner"/>, Medium), which answers with one point a cluster for the precise search to
/// walk a leg at a time, <see cref="NavStatus.Direct"/> when the two ends are joined inside one cluster, and
/// <see cref="NavStatus.Unreachable"/> — without searching, from the components — when the ground has no way. The long
/// tier (regions of 128 tiles, teleporters, a route cache) sits above it.
/// </para>
///
/// <para>
/// <b>On the loop, and never off it.</b> The graph is drawn from the map's tiles, which are game state: a slice of at
/// most <see cref="SliceMs"/> every <see cref="EveryMs"/>, nearest the prioritized place first (<see cref="Prepare"/>),
/// so the ground round home is ready within seconds of a boot and the rest follows.
/// </para>
/// </summary>
public static class NavigationService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NavigationService));

    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// Extra cost, in <see cref="NavCost"/> units, for routing through the node at (x, y) — set by whoever knows where the
    /// ground is dangerous (the bots' quadrant map and barred boxes), null for none. Both planners add it to every node
    /// they relax, so a road through blood costs more than a longer road round it and the route goes round. Consulted
    /// once per relaxation: keep it to a lookup.
    /// </summary>
    public static Func<int, int, int> Danger { get; set; }

    /// <summary>
    /// Told whenever the ground under a rectangle changes (<see cref="GroundChanged"/>: a house raised, moved or taken down),
    /// on the loop, before the graph redraws. For whoever keeps a map of the same ground — the bots' reach map (01.10.2026).
    /// </summary>
    public static Action<Map, Rectangle2D> GroundChangedHook { get; set; }

    /// <summary>The danger at a node, or nought without a hook. See <see cref="Danger"/>.</summary>
    internal static int DangerAt(NavGraph graph, int node)
    {
        var danger = Danger;

        return danger == null ? 0 : danger(graph.X(node), graph.Y(node));
    }

    /// <summary>The most one slice of drawing may spend on the game loop, in milliseconds.</summary>
    public static int SliceMs { get; set; } = 8;

    /// <summary>How often a slice runs, in milliseconds.</summary>
    public static int EveryMs { get; set; } = 50;

    private static readonly Dictionary<int, NavGraph> _graphs = new();

    private static readonly Dictionary<int, long> _begun = new();

    private static readonly Dictionary<int, double> _spent = new();

    private static readonly NavWindow _bakeWindow = new();

    private static readonly NavWindowCache _cache = new();

    private static readonly HierarchicalPlanner _medium = new() { Cache = _cache, MaxExpansions = 12000 };

    private static readonly StrategicPlanner _long = new(_medium, _cache);

    private static readonly NavRouteMemory _memory = new();

    private static readonly List<Point3D> _groundPoints = [];

    /// <summary>The roads found, remembered and handed out again. See <see cref="NavRouteMemory"/>.</summary>
    public static NavRouteMemory Memory => _memory;

    /// <summary>Routes served from memory, a way round the danger among them, and the loop's milliseconds they cost.</summary>
    public static long Remembered { get; private set; }

    public static double RememberedMs { get; private set; }

    /// <summary>
    /// Routes the planners were asked for, and their milliseconds; of them, those the memory could have served had it held
    /// a road (both ends placed, three clusters or more apart, and a way found), whose mean is the price a served route saves.
    /// </summary>
    public static long Plans { get; private set; }

    public static double PlansMs { get; private set; }

    public static long EligiblePlans { get; private set; }

    public static double EligiblePlansMs { get; private set; }

    /// <summary>Roads planned with the danger hook off, to be remembered; and ways round the danger planned after them.</summary>
    public static long GroundPlans { get; private set; }

    public static long RoundPlans { get; private set; }

    /// <summary>Ways round the danger the planner could not find within its budget, walked as the ground road instead.</summary>
    public static long RoundFallbacks { get; private set; }

    /// <summary>
    /// The loop's milliseconds the memory has saved: the routes served times what a route it could have served cost to plan,
    /// less what serving them cost. An estimate, and a low one: the roads served are the ones asked most, and far ones.
    /// </summary>
    public static double SavedMs =>
        EligiblePlans <= 0 ? 0.0 : Math.Max(0.0, Remembered * (EligiblePlansMs / EligiblePlans) - RememberedMs);

    /// <summary>
    /// How often the remembered roads are written to disk when they have changed, in milliseconds. Ten minutes: the shard is
    /// stopped by a kill as often as by a shutdown, and ten minutes of roads is a few dozen plans to find again.
    /// </summary>
    public static int RouteWriteEveryMs { get; set; } = 600000;

    public static long RoutesWritten { get; private set; }

    private static volatile bool _routesWriting;

    private static Timer _routeTimer;

    private static readonly Dictionary<int, ulong> _fingerprints = new();

    /// <summary>Routes the medium tier could not find within its budget, handed to the long tier.</summary>
    public static long LongRoutes { get; private set; }

    private static readonly long[] _statuses = new long[Enum.GetValues<NavStatus>().Length];

    /// <summary>How many routes have ended with that status.</summary>
    public static long Status(NavStatus status) =>
        (int)status >= 0 && (int)status < _statuses.Length ? _statuses[(int)status] : 0;

    private static Timer _timer;

    /// <summary>Whether graphs are kept on disk between boots. See <see cref="NavGraphFile"/>.</summary>
    public static bool Persist { get; set; } = true;

    /// <summary>A graph being read from disk on a worker thread, per map, and what it read.</summary>
    private sealed class Load
    {
        public ulong Fingerprint;

        public Point3D Center;

        public volatile bool Done;

        public volatile NavGraphSnapshot Result;

        public string Error;

        /// <summary>The remembered roads of the map, read by the same worker after the graph. See <see cref="NavRouteFile"/>.</summary>
        public List<NavRouteRecord> Routes;

        public string RoutesError;
    }

    private static readonly Dictionary<int, Load> _loads = new();

    /// <summary>Maps whose graph has been written to disk this boot, or is being.</summary>
    private static readonly HashSet<int> _saved = new();

    private static volatile bool _writing;

    public static long Routes { get; private set; }

    public static double RouteMs { get; private set; }

    public static double WorstRouteMs { get; private set; }

    /// <summary>
    /// A route that holds the loop this long is written down with its ends and tiers (29.09.2026, build 322). The worst route
    /// of 319 took 2164.8 ms and of 320 1164.4 ms — the whole world stood still that long — and the summary kept only the
    /// number. An instrument before a fix.
    /// </summary>
    public static double SlowRouteMs { get; set; } = 250.0;

    /// <summary>Routes slower than <see cref="SlowRouteMs"/>.</summary>
    public static long SlowRoutes { get; private set; }

    private static int _routeMediumCalls;
    private static long _routeMediumExpanded;
    private static int _routeLongCalls;
    private static long _routeLongExpanded;

    public static HierarchicalPlanner Medium => _medium;

    public static StrategicPlanner Long => _long;

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("pathfinding.tiered.enable", true);
        Persist = ServerConfiguration.GetOrUpdateSetting("pathfinding.tiered.persist", true);
        SliceMs = ServerConfiguration.GetOrUpdateSetting("pathfinding.tiered.sliceMs", SliceMs);
        EveryMs = ServerConfiguration.GetOrUpdateSetting("pathfinding.tiered.everyMs", EveryMs);
        _medium.MaxExpansions = ServerConfiguration.GetOrUpdateSetting("pathfinding.medium.maxExpansions", _medium.MaxExpansions);
        _long.MaxExpansions = ServerConfiguration.GetOrUpdateSetting("pathfinding.long.maxExpansions", _long.MaxExpansions);
        _cache.Capacity = ServerConfiguration.GetOrUpdateSetting("pathfinding.medium.windowCache", _cache.Capacity);
        _memory.Enabled = ServerConfiguration.GetOrUpdateSetting("pathfinding.routes.remember", _memory.Enabled);
        _memory.Capacity = ServerConfiguration.GetOrUpdateSetting("pathfinding.routes.capacity", _memory.Capacity);
        _memory.DetourMs = ServerConfiguration.GetOrUpdateSetting("pathfinding.routes.detourMs", _memory.DetourMs);
        RouteWriteEveryMs = ServerConfiguration.GetOrUpdateSetting("pathfinding.routes.writeEveryMs", RouteWriteEveryMs);

        // A shutdown writes what the ten-minute clock has not; a kill loses at most ten minutes of roads.
        EventSink.Shutdown += () => WriteRoutes(true);
    }

    /// <summary>The graph of a map, or null when nobody has asked for one.</summary>
    public static NavGraph Graph(Map map) =>
        map != null && _graphs.TryGetValue(map.MapID, out var graph) ? graph : null;

    /// <summary>
    /// Makes sure <paramref name="map"/> has a graph and that it is drawn outward from <paramref name="center"/> first. The
    /// first call for a map starts drawing it; a later one re-orders what is left.
    /// </summary>
    public static void Prepare(Map map, Point3D center)
    {
        if (!Enabled || map == null || map == Map.Internal)
        {
            return;
        }

        if (!_graphs.TryGetValue(map.MapID, out var graph))
        {
            graph = new NavGraph(new MapNavTerrain(map), map);
            _graphs[map.MapID] = graph;
            _begun[map.MapID] = Core.TickCount;
            _spent[map.MapID] = 0.0;

            logger.Information(
                "Navigation: drawing the graph of {Map}, {Clusters} clusters of {Side} tiles, outward from {Center}",
                map,
                graph.ClustersX * graph.ClustersY,
                NavGraph.Side,
                center
            );
        }

        graph.Prioritize(center.X, center.Y);

        // A graph kept from the last boot, read on a worker thread while the loop goes on; the bake waits for it.
        if (Persist && !_loads.ContainsKey(map.MapID) && graph.Built == 0)
        {
            var load = new Load { Fingerprint = StepCacheFile.ComputeFingerprint(map.MapID), Center = center };
            var path = NavGraphFile.PathFor(map);
            var routes = NavRouteFile.PathFor(map);
            var width = map.Width;
            var height = map.Height;

            _loads[map.MapID] = load;
            _fingerprints[map.MapID] = load.Fingerprint;

            Task.Run(() =>
            {
                try
                {
                    load.Result = NavGraphFile.Read(path, load.Fingerprint, width, height);
                }
                catch (Exception e)
                {
                    load.Error = e.Message;
                }

                load.Routes = NavRouteFile.TryRead(routes, load.Fingerprint, width, height, out load.RoutesError);
                load.Done = true;
            });
        }

        // The remembered roads are written on their own clock, when they have changed. See WriteRoutes.
        if (Persist && _routeTimer == null && RouteWriteEveryMs > 0)
        {
            var every = TimeSpan.FromMilliseconds(RouteWriteEveryMs);

            _routeTimer = Timer.DelayCall(every, every, () => WriteRoutes(false));
        }

        _timer ??= new DrawTimer();

        if (!_timer.Running)
        {
            _timer.Start();
        }
    }

    /// <summary>
    /// A route over the map's graph from <paramref name="from"/> to <paramref name="to"/>: the points to walk
    /// (<see cref="NavStatus.Ok"/>), or why there are none.
    /// </summary>
    public static NavStatus Route(Map map, Point3D from, Point3D to, List<Point3D> points) => Route(map, from, to, points, 0);

    /// <summary>
    /// The same, for a walker that arrives within <paramref name="reach"/> tiles of <paramref name="to"/>: when the goal's own
    /// cell is shut off from the start, the nearest one within reach that is not. See <see cref="HierarchicalPlanner.Route(NavGraph, int, int, int, int, int, int, List{Point3D}, int)"/>.
    /// </summary>
    public static NavStatus Route(Map map, Point3D from, Point3D to, List<Point3D> points, int reach)
    {
        points.Clear();

        if (!Enabled || map == null || map == Map.Internal)
        {
            return NavStatus.Unplaced;
        }

        if (!_graphs.TryGetValue(map.MapID, out var graph))
        {
            return NavStatus.Pending;
        }

        var started = Stopwatch.GetTimestamp();

        _routeMediumCalls = 0;
        _routeMediumExpanded = 0;
        _routeLongCalls = 0;
        _routeLongExpanded = 0;

        // <b>Remembered first (29.09.2026, "paths and graphs must be remembered; they can only be optimised").</b> A road the
        // shard has found before is proved against the graph and the danger in microseconds and handed out; only what the
        // memory cannot answer is planned. See NavRouteMemory.
        var recall = _memory.Recall(graph, _cache, _medium.Anchorage, from, to, points, Danger);
        NavStatus status;

        if (recall is NavRecall.Served or NavRecall.Detoured)
        {
            status = NavStatus.Ok;
            Remembered++;
            RememberedMs += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        }
        else
        {
            status = Planned(graph, from, to, points, reach, recall);

            var planMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

            Plans++;
            PlansMs += planMs;

            if (recall != NavRecall.Skipped && status == NavStatus.Ok)
            {
                EligiblePlans++;
                EligiblePlansMs += planMs;
            }
        }

        var ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        Routes++;
        RouteMs += ms;
        WorstRouteMs = Math.Max(WorstRouteMs, ms);
        _statuses[(int)status]++;

        if (ms >= SlowRouteMs)
        {
            SlowRoutes++;
            logger.Information(
                "Navigation: a slow route, {Ms:F0}ms, from ({FX}, {FY}, {FZ}) to ({TX}, {TY}, {TZ}), {Apart} tiles apart: {Status}, memory {Recall}; the medium tier asked {MediumCalls} times over {MediumExpanded} nodes, the long tier {LongCalls} times over {LongExpanded}",
                ms,
                from.X,
                from.Y,
                from.Z,
                to.X,
                to.Y,
                to.Z,
                Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y)),
                status,
                recall,
                _routeMediumCalls,
                _routeMediumExpanded,
                _routeLongCalls,
                _routeLongExpanded
            );
        }

        return status;
    }

    /// <summary>The medium tier, and the long tier when the medium runs out of budget: the planners as they always were.</summary>
    private static NavStatus Plan(NavGraph graph, Point3D from, Point3D to, List<Point3D> points, int reach)
    {
        var status = _medium.Route(graph, from.X, from.Y, from.Z, to.X, to.Y, to.Z, points, reach);

        _routeMediumCalls++;
        _routeMediumExpanded += _medium.LastExpanded;

        // Round a river the straight line lies and the medium tier's search runs out: the long tier finds the corridor and the
        // medium tier searches it alone.
        if (status == NavStatus.BudgetExceeded)
        {
            LongRoutes++;

            // To the goal the medium tier settled on within reach.
            var goal = _medium.LastGoal;

            status = _long.Route(graph, from.X, from.Y, from.Z, goal.X, goal.Y, goal.Z, points);

            _routeLongCalls++;
            _routeLongExpanded += _long.LastExpanded;
        }

        return status;
    }

    /// <summary>
    /// What the memory could not answer, planned, and offered to it.
    ///
    /// <para>
    /// <b>The road is planned on the ground; the danger is read over it after (29.09.2026).</b> A road not remembered is
    /// planned with the danger hook off and offered: the memory keeps ground roads, whose cost does not change by the minute,
    /// so "only cheaper replaces" means something. If nothing along it is charged now, it is the road the danger-aware
    /// planner would have found (every road costs at least its ground price, and none is cheaper on the ground) and it is
    /// walked as it is — one plan, and the cheaper one: build 297 measured 940 nodes a route without the hook against
    /// 2500–4400 with it. Only a road that crosses blood is planned again with the hook on, and that way round is kept a
    /// while as a detour (<see cref="NavRouteMemory.OfferDetour"/>). A remembered road that crosses blood now
    /// (<see cref="NavRecall.Bloodied"/>) goes straight to that second plan.
    /// </para>
    /// </summary>
    private static NavStatus Planned(NavGraph graph, Point3D from, Point3D to, List<Point3D> points, int reach, NavRecall recall)
    {
        var danger = Danger;

        if (recall == NavRecall.Skipped || !_memory.Enabled)
        {
            return Plan(graph, from, to, points, reach);
        }

        if (recall == NavRecall.Bloodied)
        {
            var groundDanger = _memory.LastGroundDanger;
            var round = Plan(graph, from, to, points, reach);

            RoundPlans++;

            if (round == NavStatus.Ok)
            {
                _memory.OfferDetour(graph, _cache, _medium.Anchorage, from, _medium.LastGoal, _medium.LastPath, groundDanger, danger);

                return round;
            }

            // No way round within the planners' budgets: the remembered ground road, blood and all, rather than the old chart.
            if (_memory.LastGround(points))
            {
                RoundFallbacks++;

                return NavStatus.Ok;
            }

            return round;
        }

        NavStatus ground;

        Danger = null;

        try
        {
            ground = Plan(graph, from, to, points, reach);
        }
        finally
        {
            Danger = danger;
        }

        GroundPlans++;

        if (ground != NavStatus.Ok)
        {
            return ground;
        }

        // A road planned while a gate is shunned may go round it; kept only until the shun runs out (NavRouteMemory.Expired).
        _memory.Offer(
            graph,
            _cache,
            _medium.Anchorage,
            from,
            _medium.LastGoal,
            _medium.LastPath,
            _medium.Shunning ? _medium.ShunMs : 0
        );

        if (NavRouteMemory.Quiet(graph, _medium.LastPath, danger, out var along))
        {
            return NavStatus.Ok;
        }

        _groundPoints.Clear();
        _groundPoints.AddRange(points);

        var status = Plan(graph, from, to, points, reach);

        RoundPlans++;

        if (status == NavStatus.Ok)
        {
            _memory.OfferDetour(graph, _cache, _medium.Anchorage, from, _medium.LastGoal, _medium.LastPath, along, danger);

            return status;
        }

        // No way round within the planners' budgets: the ground road, blood and all, rather than the old chart.
        RoundFallbacks++;
        points.Clear();
        points.AddRange(_groundPoints);

        return NavStatus.Ok;
    }

    /// <summary>
    /// The component of the map's graph that a body at <paramref name="at"/> stands in, or -1 while the graph is not whole or its
    /// components are not counted. Two points in one component are joined by ground; two in different components are not, and
    /// only a teleporter joins them. See <see cref="HierarchicalPlanner.ComponentAt"/>.
    /// </summary>
    public static int ComponentOf(Map map, Point3D at) =>
        Enabled && map != null && map != Map.Internal && _graphs.TryGetValue(map.MapID, out var graph)
            ? _medium.ComponentAt(graph, at.X, at.Y, at.Z)
            : -1;

    /// <summary>How many times the map's components were counted; a component kept across a change of this is stale.</summary>
    public static int ComponentEpoch(Map map) =>
        map != null && _graphs.TryGetValue(map.MapID, out var graph) ? graph.ComponentEpoch : 0;

    /// <summary>Whether <see cref="ComponentOf"/> can answer for this map at all.</summary>
    public static bool ComponentsCounted(Map map) =>
        map != null && _graphs.TryGetValue(map.MapID, out var graph) && graph.ComponentsValid;

    /// <summary>How the two ends join the map's graph. See <see cref="HierarchicalPlanner.Explain"/>.</summary>
    public static string Explain(Map map, Point3D from, Point3D to) =>
        map != null && _graphs.TryGetValue(map.MapID, out var graph)
            ? _medium.Explain(graph, from.X, from.Y, from.Z, to.X, to.Y, to.Z)
            : "no graph for that map";

    /// <summary>A walker could not pass the gate at <paramref name="point"/>. See <see cref="HierarchicalPlanner.Shun"/>.</summary>
    public static void Shun(Map map, Point3D point)
    {
        if (map != null && _graphs.TryGetValue(map.MapID, out var graph))
        {
            _medium.Shun(graph, point.X, point.Y);
            _memory.Shun(graph, point.X, point.Y);
        }
    }

    /// <summary>
    /// The ground under a rectangle changed — a house raised, moved or taken down: its clusters are torn down and drawn
    /// again ahead of anything else, and the planners forget their windows there.
    /// </summary>
    public static void GroundChanged(Map map, Rectangle2D bounds)
    {
        // Told first, whether or not this map has a graph: the bots' reach map (BotReachMap) draws the same ground again.
        GroundChangedHook?.Invoke(map, bounds);

        if (map == null || !_graphs.TryGetValue(map.MapID, out var graph))
        {
            return;
        }

        graph.MarkDirty(bounds.Start.X, bounds.Start.Y, bounds.End.X, bounds.End.Y);
        _cache.Invalidate(graph, bounds.Start.X, bounds.Start.Y, bounds.End.X, bounds.End.Y);
        _memory.GroundChanged(graph, bounds.Start.X, bounds.Start.Y, bounds.End.X, bounds.End.Y);

        _timer ??= new DrawTimer();

        if (!_timer.Running)
        {
            _timer.Start();
        }
    }

    private static void Draw()
    {
        var idle = true;

        foreach (var (mapId, graph) in _graphs)
        {
            // Waiting for the graph from disk; the chart answers meanwhile.
            if (_loads.TryGetValue(mapId, out var load) && load != null)
            {
                if (!load.Done)
                {
                    idle = false;

                    continue;
                }

                _loads[mapId] = null;

                if (load.Result != null)
                {
                    var began = Stopwatch.GetTimestamp();

                    graph.Restore(load.Result, load.Center.X, load.Center.Y);

                    var housed = 0;

                    foreach (var c in load.Result.Housed)
                    {
                        graph.MarkDirtyCluster(c);
                        housed++;
                    }

                    foreach (var c in Housed(graph))
                    {
                        graph.MarkDirtyCluster(c);
                        housed++;
                    }

                    _saved.Add(mapId);

                    logger.Information(
                        "Navigation: the graph of {Map} was read from disk: {Graph}; {Housed} clusters by houses drawn again; {Ms:F0}ms of the loop to put it in place",
                        graph.Map,
                        graph.Describe(),
                        housed,
                        (Stopwatch.GetTimestamp() - began) * 1000.0 / Stopwatch.Frequency
                    );
                }
                else if (load.Error != null)
                {
                    logger.Warning("Navigation: the graph of {Map} on disk could not be read ({Error}); drawing it", graph.Map, load.Error);
                }

                if (load.Routes != null)
                {
                    var adopted = _memory.Adopt(graph, load.Routes);

                    logger.Information(
                        "Navigation: {Adopted} remembered roads of {Map} read from disk ({Read} in the file); each is proved against the graph by its clusters' signatures the first time it is asked",
                        adopted,
                        graph.Map,
                        load.Routes.Count
                    );
                }
                else if (load.RoutesError != null)
                {
                    logger.Warning("Navigation: the remembered roads of {Map} could not be read ({Error}); they are found again", graph.Map, load.RoutesError);
                }
            }

            if (graph.Complete && graph.ComponentsValid)
            {
                // Kept on disk once whole, from a copy taken here, written by a worker; never while the world saves.
                if (Persist && !_saved.Contains(mapId) && !_writing && !World.Saving)
                {
                    Save(mapId, graph);
                }

                // The long tier's regions, ahead of time, once the first level is whole.
                if (!_long.Prebuilt(graph))
                {
                    idle = false;

                    var begun = Stopwatch.GetTimestamp();

                    if (_long.Prebuild(graph, begun + (long)(SliceMs * Stopwatch.Frequency / 1000.0)))
                    {
                        logger.Information(
                            "Navigation: the regions of {Map} are built: {Regions} regions of {Tiles} tiles",
                            graph.Map,
                            _long.Regions,
                            StrategicPlanner.RegionTiles
                        );
                    }

                    _spent[mapId] += (Stopwatch.GetTimestamp() - begun) * 1000.0 / Stopwatch.Frequency;

                    break;
                }

                continue;
            }

            idle = false;

            var started = Stopwatch.GetTimestamp();
            var deadline = started + (long)(SliceMs * Stopwatch.Frequency / 1000.0);
            var wasComplete = graph.Complete;

            graph.Slice(_bakeWindow, deadline);

            _spent[mapId] += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

            if (graph.Complete && !wasComplete)
            {
                logger.Information(
                    "Navigation: the graph of {Map} is drawn: {Graph}; {Ms:F0}ms of the loop over {Seconds:F0}s",
                    graph.Map,
                    graph.Describe(),
                    _spent[mapId],
                    (Core.TickCount - _begun[mapId]) / 1000.0
                );
            }

            // One map a slice.
            break;
        }

        if (idle)
        {
            _timer?.Stop();
        }
    }

    /// <summary>The clusters in or beside a sector with a multi in it now: the ground a saved graph may have wrong.</summary>
    private static List<int> Housed(NavGraph graph)
    {
        var housed = new List<int>();
        var map = graph.Map;

        if (map == null)
        {
            return housed;
        }

        for (var cy = 0; cy < graph.ClustersY; cy++)
        {
            for (var cx = 0; cx < graph.ClustersX; cx++)
            {
                if (map.GetRealSector(cx, cy).HasMultis)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (cx + dx >= 0 && cy + dy >= 0 && cx + dx < graph.ClustersX && cy + dy < graph.ClustersY)
                            {
                                housed.Add((cy + dy) * graph.ClustersX + cx + dx);
                            }
                        }
                    }
                }
            }
        }

        return housed;
    }

    private static void Save(int mapId, NavGraph graph)
    {
        var snapshot = graph.Snapshot(StepCacheFile.ComputeFingerprint(mapId), Housed(graph).ToArray());

        if (snapshot == null)
        {
            return;
        }

        _saved.Add(mapId);
        _writing = true;

        var path = NavGraphFile.PathFor(graph.Map);
        var map = graph.Map;

        Task.Run(() =>
        {
            try
            {
                var watch = Stopwatch.StartNew();

                NavGraphFile.Write(path, snapshot);
                logger.Information("Navigation: the graph of {Map} was written to disk in {Ms}ms", map, watch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                logger.Warning("Navigation: the graph of {Map} could not be written ({Error})", map, e.Message);
            }

            _writing = false;
        });
    }

    /// <summary>
    /// The remembered roads written to disk when they have changed: the records taken on the loop (arrays that never change
    /// once a road is made), written by a worker, never while the world saves. <paramref name="now"/>, at shutdown, writes on
    /// the calling thread.
    /// </summary>
    private static void WriteRoutes(bool now)
    {
        if (!Persist || !_memory.Enabled || !_memory.Dirty || _routesWriting || !now && World.Saving)
        {
            return;
        }

        var files = new List<(string Path, ulong Fingerprint, int Width, int Height, string Map, List<NavRouteRecord> Routes)>();

        foreach (var (mapId, graph) in _graphs)
        {
            if (graph.Map != null && _fingerprints.TryGetValue(mapId, out var fingerprint))
            {
                files.Add((NavRouteFile.PathFor(graph.Map), fingerprint, graph.Terrain.Width, graph.Terrain.Height, graph.Map.ToString(), _memory.Records(graph)));
            }
        }

        if (files.Count == 0)
        {
            return;
        }

        _memory.Dirty = false;
        _routesWriting = true;

        void Write()
        {
            foreach (var f in files)
            {
                try
                {
                    var watch = Stopwatch.StartNew();

                    NavRouteFile.Write(f.Path, f.Fingerprint, f.Width, f.Height, f.Routes);
                    logger.Information("Navigation: {Roads} remembered roads of {Map} written to disk in {Ms}ms", f.Routes.Count, f.Map, watch.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    logger.Warning("Navigation: the remembered roads of {Map} could not be written ({Error})", f.Map, e.Message);
                }
            }

            _routesWriting = false;
        }

        foreach (var f in files)
        {
            RoutesWritten += f.Routes.Count;
        }

        if (now)
        {
            Write();
        }
        else
        {
            Task.Run(Write);
        }
    }

    public static string Describe()
    {
        if (!Enabled)
        {
            return "tiered navigation is off";
        }

        if (_graphs.Count == 0)
        {
            return "tiered navigation: no graph asked for yet";
        }

        var parts = new List<string>();

        foreach (var (mapId, graph) in _graphs)
        {
            parts.Add($"{graph.Map}: {graph.Describe()}, {_spent[mapId]:F0}ms of the loop");
        }

        return $"tiered navigation: {string.Join("; ", parts)}; {Routes} routes asked in {RouteMs:F0}ms "
            + $"(worst {WorstRouteMs:F1}ms, {SlowRoutes} slower than {SlowRouteMs:F0}ms, {_medium.Expanded} nodes expanded): {_statuses[(int)NavStatus.Ok]} routed, "
            + $"{_statuses[(int)NavStatus.Direct]} direct, {_statuses[(int)NavStatus.Unreachable]} unreachable, "
            + $"{_statuses[(int)NavStatus.Pending]} not drawn yet, {_statuses[(int)NavStatus.BudgetExceeded]} over budget, "
            + $"{_statuses[(int)NavStatus.Unplaced]} unplaced; windows {_cache.Hits} kept and {_cache.Misses} probed; "
            + $"{_medium.Shunned} gates shunned; {_medium.Cycles + _long.Cycles} parent chains found looping and cut (the last at node {_long.LastCycleNode}); the long tier took {LongRoutes} routes over budget ({_long.Expanded} region "
            + $"nodes expanded, {_long.Regions} regions built); routes from memory: {Remembered} served in {RememberedMs:F0}ms against {Plans} planned in {PlansMs:F0}ms "
            + $"({EligiblePlans} of them far enough to remember, {(EligiblePlans > 0 ? EligiblePlansMs / EligiblePlans : 0.0):F2}ms each), about {SavedMs:F0}ms saved; "
            + $"{GroundPlans} planned on the ground, {RoundPlans} round the danger after, {RoundFallbacks} walked on the ground for want of a way round; "
            + $"{_memory.Describe()}; {RoutesWritten} written to disk";
    }

    private sealed class DrawTimer : Timer
    {
        public DrawTimer() : base(TimeSpan.FromMilliseconds(EveryMs), TimeSpan.FromMilliseconds(EveryMs))
        {
        }

        protected override void OnTick() => Draw();
    }
}
