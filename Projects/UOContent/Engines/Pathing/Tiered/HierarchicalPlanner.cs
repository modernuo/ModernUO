using System;
using System.Collections.Generic;

namespace Server.Engines.Pathing.Tiered;

/// <summary>How a route came out.</summary>
public enum NavStatus
{
    /// <summary>A route: waypoints to walk, a short leg at a time.</summary>
    Ok,

    /// <summary>The two places are joined inside one cluster: walk straight there with the precise search.</summary>
    Direct,

    /// <summary>No way on the ground, as the graph was built: provably, from the components, or after searching all of it.</summary>
    Unreachable,

    /// <summary>The clusters at one end are not built yet.</summary>
    Pending,

    /// <summary>The search spent its budget before it found the goal.</summary>
    BudgetExceeded,

    /// <summary>One end is off the map, or stands nowhere a body can stand.</summary>
    Unplaced
}

/// <summary>
/// The medium tier: hierarchical A* over the first level of the navigation graph.
///
/// <para>
/// The start and the goal are joined to their clusters' nodes by a flood inside each cluster (<see cref="NavWindow"/>),
/// forward from the start and backward to the goal; when the start's flood reaches the goal the answer is
/// <see cref="NavStatus.Direct"/>. Otherwise an A* over the nodes, estimated by <see cref="NavCost.Estimate"/> (never more
/// than the truth, every step costing at least a step), and ties broken towards the larger cost so far, which is the
/// node nearer the goal. What comes back is one point a cluster: of each gate's two nodes the far one, since the near
/// one is a single step before it.
/// </para>
/// </summary>
public sealed class HierarchicalPlanner
{
    /// <summary>Most nodes one route may expand before it answers <see cref="NavStatus.BudgetExceeded"/>.</summary>
    public int MaxExpansions { get; set; } = 20000;

    /// <summary>The estimate is raised by one part in this many (0 for none). See Relax.</summary>
    public int TieBreak { get; set; } = 1000;

    /// <summary>How far round an end the planner looks for a tile a body can stand on, when the end itself has none.</summary>
    public int Anchorage { get; set; } = 2;

    /// <summary>How long a gate a walker could not pass is shunned, in milliseconds, and what shunning it adds to a route.</summary>
    public int ShunMs { get; set; } = 600000;

    public int ShunCost { get; set; } = 40 * NavCost.Step;

    private readonly Dictionary<long, long> _shunned = new();

    public long Shunned { get; private set; }

    /// <summary>Whether any gate is shunned now (or was, and has not been asked about since): a route planned now may go round one.</summary>
    public bool Shunning => _shunned.Count > 0;

    /// <summary>
    /// A walker could not pass the gate at (x, y): every node there is shunned for <see cref="ShunMs"/>, so the next
    /// routes go round it where there is a way round. Something the graph cannot see stands in it — an item, a crowd, a
    /// house raised since its cluster was drawn and not yet redrawn.
    /// </summary>
    public void Shun(NavGraph graph, int x, int y)
    {
        for (var s = 0; s < NavWindow.MaxStrata; s++)
        {
            var node = graph.NodeAt(x, y, s);

            if (node >= 0)
            {
                _shunned[((long)graph.Id << 32) | (uint)node] = Core.TickCount + ShunMs;
                Shunned++;
            }
        }
    }

    private int Penalty(NavGraph graph, int node)
    {
        var danger = NavigationService.DangerAt(graph, node);

        if (_shunned.Count == 0 || !_shunned.TryGetValue(((long)graph.Id << 32) | (uint)node, out var until))
        {
            return danger;
        }

        if (Core.TickCount - until >= 0)
        {
            _shunned.Remove(((long)graph.Id << 32) | (uint)node);

            return danger;
        }

        return ShunCost + danger;
    }

    /// <summary>
    /// When set, the search keeps to the clusters whose region (<see cref="StrategicPlanner.RegionOf"/>) is marked in it: the
    /// corridor the long tier found. Null for the whole map.
    /// </summary>
    public HashSet<int> Corridor { get; set; }

    /// <summary>Where the ends' windows come from: a shared cache of probed clusters, or, when null, windows of its own.</summary>
    public NavWindowCache Cache { get; set; }

    private readonly NavWindow _ownStart = new();

    private readonly NavWindow _ownGoal = new();

    private NavWindow _startWindow;

    private NavWindow _goalWindow;

    private int[] _startCost = new int[NavWindow.States];

    private int[] _goalCost = new int[NavWindow.States];

    private int[] _g = [];

    private int[] _parent = [];

    private int[] _seen = [];

    private int[] _closed = [];

    private int _search;

    private readonly PriorityQueue<int, long> _open = new();

    private readonly List<int> _path = [];

    public long Asked { get; private set; }

    public long Expanded { get; private set; }

    /// <summary>Parent chains found looping in <c>Unwind</c> and cut. See StrategicPlanner.Corridor.</summary>
    public long Cycles { get; private set; }

    public int LastExpanded { get; private set; }

    public int LastCost { get; private set; }

    /// <summary>
    /// The nodes of the last <see cref="NavStatus.Ok"/> route, start end first, the gates' near sides included; empty after
    /// any other answer. What the route memory keeps (<see cref="NavRouteMemory"/>): the points handed to the walker are
    /// these less every gate's near side.
    /// </summary>
    public IReadOnlyList<int> LastPath => _path;

    /// <summary>
    /// The route from (sx, sy, sz) to (gx, gy, gz) over <paramref name="graph"/>, into <paramref name="points"/> (cleared
    /// first), when the answer is <see cref="NavStatus.Ok"/>.
    /// </summary>
    public NavStatus Route(NavGraph graph, int sx, int sy, int sz, int gx, int gy, int gz, List<Point3D> points) =>
        Route(graph, sx, sy, sz, gx, gy, gz, points, 0);

    /// <summary>The goal the last route was planned to: the cell asked for, or the one within reach it settled on.</summary>
    public Point3D LastGoal { get; private set; }

    /// <summary>
    /// The same, for a walker that arrives within <paramref name="reach"/> tiles of the goal: when the goal's own cell is
    /// not joined to the start — a herb on a table, a fire in a walled corner, the tile under a stall — the nearest cell
    /// within reach that is, as the precise search itself settles for one.
    /// </summary>
    public NavStatus Route(NavGraph graph, int sx, int sy, int sz, int gx, int gy, int gz, List<Point3D> points, int reach)
    {
        points.Clear();
        _path.Clear();
        LastExpanded = 0;
        LastCost = 0;
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

        _startWindow = Window(graph, sc, _ownStart);

        if (!Anchor(_startWindow, ref sx, ref sy, sz, Anchorage, out var ss))
        {
            return NavStatus.Unplaced;
        }

        _goalWindow = Window(graph, gc, _ownGoal);

        Array.Copy(_startWindow.Flood(sx, sy, ss, false), _startCost, NavWindow.States);

        var startNodes = graph.NodesOf(sc);
        var goalNodes = graph.NodesOf(gc);
        var radius = Math.Max(Anchorage, reach);
        var placed = false;
        Span<int> order = stackalloc int[NavWindow.MaxStrata];

        // The goal's own cell first, then ring by ring out to the reach; on each cell, its surfaces nearest the height asked.
        // Past the reach only the anchor counts: the first cell with anywhere to stand, for a goal on a tree or a wall.
        for (var r = 0; r <= radius; r++)
        {
            if (placed && r > reach)
            {
                break;
            }

            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    var x = gx + dx;
                    var y = gy + dy;

                    if (!_goalWindow.InCluster(x, y))
                    {
                        continue;
                    }

                    var n = _goalWindow.Count(x, y);

                    if (n == 0 || placed && r > reach)
                    {
                        continue;
                    }

                    placed = true;
                    Nearest(_goalWindow, x, y, gz, n, order);

                    for (var k = 0; k < n; k++)
                    {
                        var s = order[k];

                        LastGoal = new Point3D(x, y, _goalWindow.Z(x, y, s));

                        if (sc == gc && _startCost[_goalWindow.State(x, y, s)] >= 0)
                        {
                            LastCost = _startCost[_goalWindow.State(x, y, s)];

                            return NavStatus.Direct;
                        }

                        if (startNodes == null || goalNodes == null)
                        {
                            continue;
                        }

                        Array.Copy(_goalWindow.Flood(x, y, s, true), _goalCost, NavWindow.States);

                        if (!graph.ComponentsValid || Share(graph, startNodes, goalNodes))
                        {
                            return Search(graph, sc, gc, x, y, startNodes, points);
                        }
                    }
                }
            }
        }

        return placed ? NavStatus.Unreachable : NavStatus.Unplaced;
    }

    /// <summary>A cell's strata, nearest the height <paramref name="z"/> first.</summary>
    private static void Nearest(NavWindow w, int x, int y, int z, int n, Span<int> order)
    {
        for (var i = 0; i < n; i++)
        {
            order[i] = i;
        }

        for (var i = 1; i < n; i++)
        {
            for (var j = i; j > 0 && Math.Abs(w.Z(x, y, order[j]) - z) < Math.Abs(w.Z(x, y, order[j - 1]) - z); j--)
            {
                (order[j], order[j - 1]) = (order[j - 1], order[j]);
            }
        }
    }

    private NavWindow Window(NavGraph graph, int cluster, NavWindow own)
    {
        if (Cache != null)
        {
            return Cache.Get(graph, cluster);
        }

        own.Reset(graph.Terrain, cluster, cluster % graph.ClustersX * NavGraph.Side, cluster / graph.ClustersX * NavGraph.Side);

        return own;
    }

    /// <summary>
    /// The component of the graph a body standing at (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) belongs
    /// to: the component of any node of its cluster that its own window's flood reaches. Minus one when the components are
    /// not counted yet, the cluster is not built, the point cannot be anchored, or the cell is a pocket no gate leaves.
    ///
    /// <para>
    /// One flood of a sixteen-tile window and a walk over the cluster's nodes — microseconds. It is what a teleporter's two
    /// ends are asked, so that a route can be drawn to the gate whose far side is joined to the goal (BotGates, 29.09.2026).
    /// </para>
    /// </summary>
    public int ComponentAt(NavGraph graph, int x, int y, int z)
    {
        if (graph == null || !graph.ComponentsValid || !graph.Contains(x, y))
        {
            return -1;
        }

        var c = graph.ClusterOf(x, y);

        if (!graph.IsJoined(c))
        {
            return -1;
        }

        var w = Window(graph, c, _ownStart);

        if (!Anchor(w, ref x, ref y, z, Anchorage, out var s))
        {
            return -1;
        }

        var cost = w.Flood(x, y, s, false);
        var nodes = graph.NodesOf(c);

        if (nodes == null)
        {
            return -1;
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];

            if (cost[w.State(graph.X(n), graph.Y(n), graph.Stratum(n))] >= 0)
            {
                return graph.Component(n);
            }
        }

        return -1;
    }

    /// <summary>
    /// How the two ends of a route join the graph: the stratum each was anchored to, and every node of its cluster with the
    /// cost from the start (or to the goal) and its component. For the door, when the graph says there is no way and a
    /// walker finds one.
    /// </summary>
    public string Explain(NavGraph graph, int sx, int sy, int sz, int gx, int gy, int gz)
    {
        if (!graph.Contains(sx, sy) || !graph.Contains(gx, gy))
        {
            return "an end is off the map";
        }

        var sc = graph.ClusterOf(sx, sy);
        var gc = graph.ClusterOf(gx, gy);
        var parts = new List<string>();

        foreach (var (c, x0, y0, z0, backward) in new[] { (sc, sx, sy, sz, false), (gc, gx, gy, gz, true) })
        {
            var x = x0;
            var y = y0;
            var w = Window(graph, c, backward ? _ownGoal : _ownStart);

            if (!Anchor(w, ref x, ref y, z0, Anchorage, out var s))
            {
                parts.Add($"{(backward ? "goal" : "start")} ({x0}, {y0}, {z0}) in cluster {c}: nowhere to stand within {Anchorage}");

                continue;
            }

            var cost = w.Flood(x, y, s, backward);
            var nodes = graph.NodesOf(c);
            var list = new List<string>();

            for (var i = 0; nodes != null && i < nodes.Count; i++)
            {
                var n = nodes[i];
                var d = cost[w.State(graph.X(n), graph.Y(n), graph.Stratum(n))];

                list.Add($"({graph.X(n)},{graph.Y(n)},{graph.Z(n)}) {(d >= 0 ? d.ToString() : "-")} comp {graph.Component(n)}");
            }

            var levels = new List<string>();

            for (var k = 0; k < w.Count(x, y); k++)
            {
                levels.Add(w.Z(x, y, k).ToString());
            }

            parts.Add(
                $"{(backward ? "goal" : "start")} ({x0}, {y0}, {z0}) anchored at ({x}, {y}) stratum {s} of [{string.Join(",", levels)}] in cluster {c} "
                + $"(joined {graph.IsJoined(c)}): {string.Join("; ", list)}"
            );
        }

        return string.Join(" | ", parts);
    }

    /// <summary>The stratum of the end's cell nearest its height, or of the nearest cell within <see cref="Anchorage"/> with any.</summary>
    internal static bool Anchor(NavWindow w, ref int x, ref int y, int z, int anchorage, out int stratum)
    {
        for (var r = 0; r <= anchorage; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r || !w.InCluster(x + dx, y + dy))
                    {
                        continue;
                    }

                    stratum = w.Nearest(x + dx, y + dy, z);

                    if (stratum >= 0)
                    {
                        x += dx;
                        y += dy;

                        return true;
                    }
                }
            }
        }

        stratum = -1;

        return false;
    }

    /// <summary>Whether a node the start reaches in its cluster and a node that reaches the goal in its share a component.</summary>
    private bool Share(NavGraph graph, IReadOnlyList<int> startNodes, IReadOnlyList<int> goalNodes)
    {
        for (var i = 0; i < startNodes.Count; i++)
        {
            var a = startNodes[i];

            if (_startCost[_startWindow.State(graph.X(a), graph.Y(a), graph.Stratum(a))] < 0)
            {
                continue;
            }

            var ca = graph.Component(a);

            for (var j = 0; j < goalNodes.Count; j++)
            {
                var b = goalNodes[j];

                if (_goalCost[_goalWindow.State(graph.X(b), graph.Y(b), graph.Stratum(b))] >= 0 && graph.Component(b) == ca)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private NavStatus Search(NavGraph graph, int sc, int gc, int gx, int gy, IReadOnlyList<int> startNodes, List<Point3D> points)
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
        Relax(s, 0, -1, 0);

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
                LastExpanded = expanded;
                Expanded += expanded;
                LastCost = _g[g];
                Unwind(graph, s, g, points);

                return NavStatus.Ok;
            }

            if (++expanded > MaxExpansions)
            {
                LastExpanded = expanded;
                Expanded += expanded;

                return NavStatus.BudgetExceeded;
            }

            var here = _g[node];

            if (node == s)
            {
                for (var i = 0; i < startNodes.Count; i++)
                {
                    var a = startNodes[i];
                    var cost = _startCost[_startWindow.State(graph.X(a), graph.Y(a), graph.Stratum(a))];

                    if (cost >= 0)
                    {
                        Relax(a, here + cost, s, NavCost.Estimate(graph.X(a), graph.Y(a), gx, gy));
                    }
                }

                continue;
            }

            if (graph.ClusterOfNode(node) == gc)
            {
                var cost = _goalCost[_goalWindow.State(graph.X(node), graph.Y(node), graph.Stratum(node))];

                if (cost >= 0)
                {
                    Relax(g, here + cost, node, 0);
                }
            }

            if (!graph.EdgesOf(node, out var to, out var weights, out var from, out var until))
            {
                continue;
            }

            for (var e = from; e < until; e++)
            {
                var next = to[e];

                if (_closed[next] == _search)
                {
                    continue;
                }

                if (Corridor != null && !Corridor.Contains(StrategicPlanner.RegionOf(graph, graph.ClusterOfNode(next))))
                {
                    continue;
                }

                Relax(next, here + weights[e] + Penalty(graph, next), node, NavCost.Estimate(graph.X(next), graph.Y(next), gx, gy));
            }
        }

        LastExpanded = expanded;
        Expanded += expanded;

        return NavStatus.Unreachable;
    }

    private void Relax(int node, int cost, int parent, int estimate)
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

        // f first; at equal f, the larger g, which is the node nearer the goal. The estimate is raised by one part in
        // TieBreak: a step costs the same straight or diagonal, so the straight-line metric leaves wide plateaus of routes of
        // exactly equal cost, and A* explores all of every plateau unless the estimate leans, however slightly, towards
        // the goal. A route at most one part in TieBreak dearer than the best.
        var lean = TieBreak > 0 ? estimate / TieBreak : 0;

        _open.Enqueue(node, ((long)(cost + estimate + lean) << 32) | (uint)(int.MaxValue - cost));
    }

    private void Unwind(NavGraph graph, int s, int g, List<Point3D> points)
    {
        _path.Clear();
        var steps = 0;

        for (var node = _parent[g]; node >= 0 && node != s; node = _parent[node])
        {
            // Bounded as StrategicPlanner.Corridor is: a chain longer than the graph is a loop.
            if (++steps > graph.Capacity + 2)
            {
                Cycles++;
                _path.Clear();

                break;
            }

            _path.Add(node);
        }

        _path.Reverse();

        for (var i = 0; i < _path.Count; i++)
        {
            var node = _path[i];

            // The near side of a gate is a step before its far side, and the far side is the one worth walking to.
            if (i + 1 < _path.Count && graph.ClusterOfNode(_path[i + 1]) != graph.ClusterOfNode(node))
            {
                continue;
            }

            points.Add(new Point3D(graph.X(node), graph.Y(node), graph.Z(node)));
        }
    }
}
