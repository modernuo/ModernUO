using System;
using System.Collections.Generic;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// The long tier: regions of <see cref="RegionClusters"/>×<see cref="RegionClusters"/> clusters (128 tiles) over the first
/// level of the graph, and a corridor through them for the medium tier to search in.
///
/// <para>
/// <b>Why a second level.</b> The medium tier's estimate is the straight line, and round a river the straight line lies:
/// from the east bank to The Lantern's hall is 84 tiles straight and 1,381 steps by road, and an A* over the first level
/// expanded 29,819 nodes to find it. Over regions the same detour is a few dozen nodes. The region's border nodes are the
/// first-level nodes on its outer edge (a gate across a region's edge is always a gate across a cluster's); inside a
/// region, a Dijkstra from each border node over the region's first-level edges gives its edges to the others.
/// Regions are built when first needed, or ahead of time a slice at a time (<see cref="Prebuild"/>), and rebuilt when any
/// of their clusters has changed since (<see cref="NavGraph.ClusterVersion"/>).
/// </para>
///
/// <para>
/// <b>What it answers.</b> An A* over the border nodes from the start's region to the goal's, estimated by the straight
/// line (admissible: every step costs at least <see cref="NavCost.Step"/>). The regions it passes through, and the ring
/// round them, are the corridor; the medium tier then finds the route in the corridor alone
/// (<see cref="HierarchicalPlanner.Corridor"/>), which is where the thousands of nodes it expanded round the river were not.
/// </para>
/// </summary>
public sealed class StrategicPlanner
{
    public const int RegionClusters = 8;

    public const int RegionTiles = RegionClusters * NavGraph.Side;

    /// <summary>Most border nodes one route may expand.</summary>
    public int MaxExpansions { get; set; } = 20000;

    private readonly HierarchicalPlanner _medium;

    private readonly NavWindowCache _cache;

    private readonly Dictionary<long, NavRegion> _regions = new();

    private readonly HashSet<int> _corridor = new();

    private int[] _g = [];

    private int[] _parent = [];

    private int[] _seen = [];

    private int[] _closed = [];

    private int _search;

    private readonly PriorityQueue<int, long> _open = new();

    private readonly Dictionary<int, int> _startCost = new();

    private readonly Dictionary<int, int> _goalCost = new();

    private readonly PriorityQueue<int, int> _local = new();

    private readonly Dictionary<int, List<(int From, int Cost)>> _reverse = new();

    private int _prebuilt;

    public StrategicPlanner(HierarchicalPlanner medium, NavWindowCache cache)
    {
        _medium = medium;
        _cache = cache;
    }

    public long Asked { get; private set; }

    public long Expanded { get; private set; }

    public int LastExpanded { get; private set; }

    public long RegionsBuilt { get; private set; }

    public int Regions => _regions.Count;

    /// <summary>Parent chains found looping in <c>Corridor</c> and cut, and the node the cut was made at. See the note there.</summary>
    public long Cycles { get; private set; }

    public int LastCycleNode { get; private set; } = -1;

    /// <summary>Whether every region of the graph has been built ahead of time.</summary>
    public bool Prebuilt(NavGraph graph) => _prebuilt >= RegionsX(graph) * RegionsY(graph);

    public static int RegionsX(NavGraph graph) => (graph.ClustersX + RegionClusters - 1) / RegionClusters;

    public static int RegionsY(NavGraph graph) => (graph.ClustersY + RegionClusters - 1) / RegionClusters;

    public static int RegionOf(NavGraph graph, int cluster) =>
        cluster / graph.ClustersX / RegionClusters * RegionsX(graph) + cluster % graph.ClustersX / RegionClusters;

    private static long Key(NavGraph graph, int region) => ((long)graph.Id << 32) | (uint)region;

    /// <summary>Builds regions ahead of time until the stopwatch passes <paramref name="deadline"/>. True when all are built.</summary>
    public bool Prebuild(NavGraph graph, long deadline)
    {
        var total = RegionsX(graph) * RegionsY(graph);

        while (_prebuilt < total)
        {
            Ensure(graph, _prebuilt++);

            if (System.Diagnostics.Stopwatch.GetTimestamp() >= deadline)
            {
                break;
            }
        }

        return _prebuilt >= total;
    }

    /// <summary>
    /// The route from (sx, sy, sz) to (gx, gy, gz): a corridor of regions by the long tier, then the medium tier's route
    /// inside it, into <paramref name="points"/>.
    /// </summary>
    public NavStatus Route(NavGraph graph, int sx, int sy, int sz, int gx, int gy, int gz, List<Point3D> points)
    {
        points.Clear();
        LastExpanded = 0;
        Asked++;

        if (!graph.Contains(sx, sy) || !graph.Contains(gx, gy))
        {
            return NavStatus.Unplaced;
        }

        var sc = graph.ClusterOf(sx, sy);
        var gc = graph.ClusterOf(gx, gy);

        if (!graph.IsJoined(sc) || !graph.IsJoined(gc))
        {
            return NavStatus.Pending;
        }

        var ws = _cache.Get(graph, sc);

        if (!HierarchicalPlanner.Anchor(ws, ref sx, ref sy, sz, _medium.Anchorage, out var ss))
        {
            return NavStatus.Unplaced;
        }

        var wg = _cache.Get(graph, gc);

        if (!HierarchicalPlanner.Anchor(wg, ref gx, ref gy, gz, _medium.Anchorage, out var gs))
        {
            return NavStatus.Unplaced;
        }

        var rs = RegionOf(graph, sc);
        var rg = RegionOf(graph, gc);

        // The start's costs to its cluster's nodes, then over its region to the region's border.
        _startCost.Clear();

        var flood = ws.Flood(sx, sy, ss, false);
        var startNodes = graph.NodesOf(sc);

        for (var i = 0; startNodes != null && i < startNodes.Count; i++)
        {
            var n = startNodes[i];
            var cost = flood[ws.State(graph.X(n), graph.Y(n), graph.Stratum(n))];

            if (cost >= 0)
            {
                _startCost[n] = cost;
            }
        }

        Spread(graph, rs, _startCost, false);

        // The goal's costs from its cluster's nodes, then backward over its region from the region's border.
        _goalCost.Clear();

        flood = wg.Flood(gx, gy, gs, true);

        var goalNodes = graph.NodesOf(gc);

        for (var i = 0; goalNodes != null && i < goalNodes.Count; i++)
        {
            var n = goalNodes[i];
            var cost = flood[wg.State(graph.X(n), graph.Y(n), graph.Stratum(n))];

            if (cost >= 0)
            {
                _goalCost[n] = cost;
            }
        }

        Spread(graph, rg, _goalCost, true);

        if (_startCost.Count == 0 || _goalCost.Count == 0)
        {
            return NavStatus.Unreachable;
        }

        var status = Search(graph, rs, rg, gx, gy);

        if (status != NavStatus.Ok)
        {
            return status;
        }

        // The medium tier's route, inside the corridor.
        _medium.Corridor = _corridor;

        try
        {
            return _medium.Route(graph, sx, sy, sz, gx, gy, gz, points);
        }
        finally
        {
            _medium.Corridor = null;
        }
    }

    /// <summary>
    /// Dijkstra over one region's first-level edges from the costs already in <paramref name="cost"/> (forward), or towards
    /// them over the reversed edges (<paramref name="backward"/>), adding every node of the region it reaches.
    /// </summary>
    private void Spread(NavGraph graph, int region, Dictionary<int, int> cost, bool backward)
    {
        _local.Clear();

        if (backward)
        {
            Reverse(graph, region);
        }

        foreach (var (node, c) in cost)
        {
            _local.Enqueue(node, c);
        }

        while (_local.TryDequeue(out var node, out var here))
        {
            if (here > cost[node])
            {
                continue;
            }

            if (!backward)
            {
                if (!graph.EdgesOf(node, out var to, out var weights, out var from, out var until))
                {
                    continue;
                }

                for (var e = from; e < until; e++)
                {
                    var next = to[e];

                    if (RegionOf(graph, graph.ClusterOfNode(next)) != region)
                    {
                        continue;
                    }

                    var c = here + weights[e];

                    if (!cost.TryGetValue(next, out var old) || c < old)
                    {
                        cost[next] = c;
                        _local.Enqueue(next, c);
                    }
                }
            }
            else if (_reverse.TryGetValue(node, out var into))
            {
                for (var i = 0; i < into.Count; i++)
                {
                    var (prev, w) = into[i];
                    var c = here + w;

                    if (!cost.TryGetValue(prev, out var old) || c < old)
                    {
                        cost[prev] = c;
                        _local.Enqueue(prev, c);
                    }
                }
            }
        }
    }

    /// <summary>Every edge of a region, reversed, for the backward spread from the goal.</summary>
    private void Reverse(NavGraph graph, int region)
    {
        foreach (var list in _reverse.Values)
        {
            list.Clear();
        }

        ForEachNode(graph, region, node =>
        {
            if (!graph.EdgesOf(node, out var to, out var weights, out var from, out var until))
            {
                return;
            }

            for (var e = from; e < until; e++)
            {
                var next = to[e];

                if (RegionOf(graph, graph.ClusterOfNode(next)) != region)
                {
                    continue;
                }

                if (!_reverse.TryGetValue(next, out var list))
                {
                    _reverse[next] = list = [];
                }

                list.Add((node, weights[e]));
            }
        });
    }

    private static void ForEachNode(NavGraph graph, int region, Action<int> each)
    {
        var rx = region % RegionsX(graph);
        var ry = region / RegionsX(graph);

        for (var cy = ry * RegionClusters; cy < Math.Min((ry + 1) * RegionClusters, graph.ClustersY); cy++)
        {
            for (var cx = rx * RegionClusters; cx < Math.Min((rx + 1) * RegionClusters, graph.ClustersX); cx++)
            {
                var nodes = graph.NodesOf(cy * graph.ClustersX + cx);

                for (var i = 0; nodes != null && i < nodes.Count; i++)
                {
                    each(nodes[i]);
                }
            }
        }
    }

    private NavStatus Search(NavGraph graph, int rs, int rg, int gx, int gy)
    {
        var s = graph.Capacity;
        var g = s + 1;

        if (_g.Length < g + 1)
        {
            var n = Math.Max(g + 1, _g.Length * 2);

            _g = new int[n];
            _parent = new int[n];
            _seen = new int[n];
            _closed = new int[n];
        }

        if (++_search == int.MaxValue)
        {
            Array.Clear(_seen);
            Array.Clear(_closed);
            _search = 1;
        }

        _open.Clear();
        _corridor.Clear();

        var startRegion = Ensure(graph, rs);

        for (var i = 0; i < startRegion.Border.Count; i++)
        {
            var b = startRegion.Border[i];

            if (_startCost.TryGetValue(b, out var c))
            {
                Relax(graph, b, c, s, gx, gy);
            }
        }

        // Start and goal in one region: the goal's own nodes are reached from the start's spread.
        if (rs == rg)
        {
            foreach (var (n, c) in _goalCost)
            {
                if (_startCost.TryGetValue(n, out var toHere))
                {
                    // <b>The goal's parent must itself have a parent in this search (29.09.2026).</b> This handed the goal a
                    // border node as its parent without ever relaxing that node, so the node's own parent was whatever
                    // the last search had left in the array, or nought; Corridor then walked the goal's chain into node 0,
                    // whose parent is 0, and the loop hung the shard twice. Relaxed from the start first, the chain is
                    // goal, node, start.
                    Relax(graph, n, toHere, s, gx, gy);
                    RelaxGoal(g, toHere + c, n);
                }
            }
        }

        var expanded = 0;

        while (_open.TryDequeue(out var node, out _))
        {
            if (_closed[node] == _search)
            {
                continue;
            }

            _closed[node] = _search;

            if (node == g)
            {
                Expanded += expanded;
                LastExpanded = expanded;
                Corridor(graph, s, g, rs, rg);

                return NavStatus.Ok;
            }

            if (++expanded > MaxExpansions)
            {
                Expanded += expanded;
                LastExpanded = expanded;

                return NavStatus.BudgetExceeded;
            }

            var here = _g[node];
            var region = RegionOf(graph, graph.ClusterOfNode(node));

            if (region == rg && _goalCost.TryGetValue(node, out var toGoal))
            {
                RelaxGoal(g, here + toGoal, node);
            }

            var built = Ensure(graph, region);

            if (built.Edges.TryGetValue(node, out var inside))
            {
                for (var i = 0; i < inside.Count; i++)
                {
                    var (to, cost) = inside[i];

                    if (_closed[to] != _search)
                    {
                        Relax(graph, to, here + cost + NavigationService.DangerAt(graph, to), node, gx, gy);
                    }
                }
            }

            if (graph.EdgesOf(node, out var next, out var weights, out var from, out var until))
            {
                for (var e = from; e < until; e++)
                {
                    var to = next[e];

                    if (_closed[to] == _search || RegionOf(graph, graph.ClusterOfNode(to)) == region)
                    {
                        continue;
                    }

                    Relax(graph, to, here + weights[e] + NavigationService.DangerAt(graph, to), node, gx, gy);
                }
            }
        }

        Expanded += expanded;
        LastExpanded = expanded;

        return NavStatus.Unreachable;
    }

    private void Relax(NavGraph graph, int node, int cost, int parent, int gx, int gy)
    {
        if (cost < 0 || cost > NavCost.Unaffordable)
        {
            return;
        }

        if (_seen[node] == _search && _g[node] <= cost)
        {
            return;
        }

        _seen[node] = _search;
        _g[node] = cost;
        _parent[node] = parent;

        var f = cost + NavCost.Estimate(graph.X(node), graph.Y(node), gx, gy);

        _open.Enqueue(node, ((long)f << 32) | (uint)(int.MaxValue - cost));
    }

    private void RelaxGoal(int g, int cost, int parent)
    {
        if (_seen[g] == _search && _g[g] <= cost)
        {
            return;
        }

        _seen[g] = _search;
        _g[g] = cost;
        _parent[g] = parent;
        _open.Enqueue(g, ((long)cost << 32) | (uint)(int.MaxValue - cost));
    }

    /// <summary>The regions the route passes through, with the ring round each.</summary>
    private void Corridor(NavGraph graph, int s, int g, int rs, int rg)
    {
        var rx = RegionsX(graph);
        var ry = RegionsY(graph);

        void Add(int region)
        {
            var x = region % rx;
            var y = region / rx;

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (x + dx >= 0 && y + dy >= 0 && x + dx < rx && y + dy < ry)
                    {
                        _corridor.Add((y + dy) * rx + x + dx);
                    }
                }
            }
        }

        Add(rs);
        Add(rg);

        // <b>A parent chain with a loop in it never reaches the start (29.09.2026).</b> The shard's loop hung twice inside
        // this walk with the danger hook on, the stack taken the second time; the loop is not reproduced on a grid, so
        // the walk is bounded: more steps than the graph has nodes is a loop, and the corridor is what was gathered.
        var steps = 0;

        for (var node = _parent[g]; node >= 0 && node != s; node = _parent[node])
        {
            if (++steps > graph.Capacity + 2)
            {
                Cycles++;
                LastCycleNode = node;

                break;
            }

            Add(RegionOf(graph, graph.ClusterOfNode(node)));
        }
    }

    /// <summary>A region's border nodes and their edges to each other inside it, built or rebuilt as needed.</summary>
    private NavRegion Ensure(NavGraph graph, int region)
    {
        var version = Version(graph, region);
        var key = Key(graph, region);

        if (_regions.TryGetValue(key, out var built) && built.Version == version)
        {
            return built;
        }

        built ??= new NavRegion();
        built.Version = version;
        built.Border.Clear();
        built.Edges.Clear();

        var rx = region % RegionsX(graph);
        var ry = region / RegionsX(graph);
        var x0 = rx * RegionTiles;
        var y0 = ry * RegionTiles;
        var x1 = x0 + RegionTiles - 1;
        var y1 = y0 + RegionTiles - 1;

        ForEachNode(graph, region, node =>
        {
            var x = graph.X(node);
            var y = graph.Y(node);

            if (x == x0 || x == x1 || y == y0 || y == y1)
            {
                built.Border.Add(node);
            }
        });

        var dist = new Dictionary<int, int>();

        for (var i = 0; i < built.Border.Count; i++)
        {
            var b = built.Border[i];

            dist.Clear();
            dist[b] = 0;
            Spread(graph, region, dist, false);

            List<(int, int)> edges = null;

            for (var j = 0; j < built.Border.Count; j++)
            {
                if (j != i && dist.TryGetValue(built.Border[j], out var cost))
                {
                    (edges ??= []).Add((built.Border[j], cost));
                }
            }

            if (edges != null)
            {
                built.Edges[b] = edges;
            }
        }

        _regions[key] = built;
        RegionsBuilt++;

        return built;
    }

    private static int Version(NavGraph graph, int region)
    {
        var rx = region % RegionsX(graph);
        var ry = region / RegionsX(graph);
        var sum = 0;

        unchecked
        {
            for (var cy = ry * RegionClusters; cy < Math.Min((ry + 1) * RegionClusters, graph.ClustersY); cy++)
            {
                for (var cx = rx * RegionClusters; cx < Math.Min((rx + 1) * RegionClusters, graph.ClustersX); cx++)
                {
                    sum = sum * 31 + graph.ClusterVersion(cy * graph.ClustersX + cx);
                }
            }
        }

        return sum;
    }
}

/// <summary>One region's border nodes and the edges between them inside it.</summary>
internal sealed class NavRegion
{
    public int Version;

    public readonly List<int> Border = [];

    public readonly Dictionary<int, List<(int To, int Cost)>> Edges = new();
}
