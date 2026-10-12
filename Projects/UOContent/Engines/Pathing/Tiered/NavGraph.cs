using System;
using System.Collections.Generic;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// The first level of the navigation graph of one map — the abstract graph of hierarchical A* (Botea, Müller and
/// Schaeffer, 2004).
///
/// <para>
/// <b>Clusters are 16×16 tiles</b>, the size of a map sector and of a step-cache chunk. Wherever a body can step
/// straight across the edge between two clusters, each maximal run of such steps (per pair of surfaces, so a bridge and
/// the road under it are separate) is an entrance: one gate in its middle when the run is at most
/// <see cref="LongEntrance"/> long, two at its ends when it is longer. A gate is a pair of nodes, one on each side, and
/// the step between them is a directed edge each way it can be taken — a drop the engine allows is not always a climb it
/// allows back. Inside each cluster a flood from every node (<see cref="NavWindow.Flood"/>) gives the cost to each of the
/// cluster's other nodes, and those are the cluster's edges.
/// </para>
///
/// <para>
/// <b>Built a cluster at a time</b>, nearest the prioritized place first (<see cref="Prioritize"/>, <see cref="Slice"/>),
/// so the graph is useful round home within seconds of a boot and complete later. A cluster whose ground changes (a
/// house raised or taken down) is torn down with its four edges and built again (<see cref="MarkDirty"/>); its
/// neighbours are re-joined. Node ids are stable while their node exists, so the rest of the graph never needs touching.
/// </para>
///
/// <para>
/// <b>Components.</b> Once built, a union-find over the edges gives every node a component (<see cref="Component"/>):
/// two places whose clusters' nodes share no component are not connected on the ground, which a planner can say without
/// searching. Treated as undirected, which can only call too much connected, never too little.
/// </para>
/// </summary>
public sealed class NavGraph
{
    public const int Side = NavWindow.ClusterSide;

    /// <summary>A run of crossings longer than this across one edge gets two gates, one at each end.</summary>
    public static int LongEntrance { get; set; } = 6;

    private const int East = 2;

    private const int South = 4;

    private const int West = 6;

    private const int North = 0;

    public INavTerrain Terrain { get; }

    /// <summary>The map this graph is of, or null for a synthetic terrain.</summary>
    public Map Map { get; }

    public int ClustersX { get; }

    public int ClustersY { get; }

    private short[] _x = new short[1024];

    private short[] _y = new short[1024];

    private sbyte[] _z = new sbyte[1024];

    private byte[] _s = new byte[1024];

    private int[] _cluster = new int[1024];

    private int[] _local = new int[1024];

    private int[] _refs = new int[1024];

    private int[] _component = new int[1024];

    private int _high;

    private readonly Stack<int> _free = new();

    private readonly Dictionary<long, int> _at = new();

    private readonly NavCluster[] _clusters;

    /// <summary>Bumped whenever a cluster's nodes or edges change, so a region built over it knows it is stale.</summary>
    private readonly int[] _versions;

    private readonly Queue<int> _rejoin = new();

    private readonly Queue<int> _dirty = new();

    private int[] _order = [];

    private int _next;

    private readonly List<int> _toScratch = [];

    private readonly List<int> _costScratch = [];

    private static int _ids;

    /// <summary>A number of its own, for the caches keyed by graph.</summary>
    public int Id { get; } = ++_ids;

    public NavGraph(INavTerrain terrain, Map map = null)
    {
        Terrain = terrain;
        Map = map;
        ClustersX = (terrain.Width + Side - 1) / Side;
        ClustersY = (terrain.Height + Side - 1) / Side;
        _clusters = new NavCluster[ClustersX * ClustersY];
        _versions = new int[ClustersX * ClustersY];
    }

    /// <summary>How many times a cluster's nodes or edges have changed. See <see cref="StrategicPlanner"/>.</summary>
    public int ClusterVersion(int cluster) => _versions[cluster];

    /// <summary>Nodes alive.</summary>
    public int Nodes => _at.Count;

    /// <summary>One more than the highest node id handed out: the size a search's arrays must have.</summary>
    public int Capacity => _high;

    /// <summary>Clusters built.</summary>
    public int Built { get; private set; }

    /// <summary>Edges, intra- and inter-cluster, over the joined clusters.</summary>
    public long Edges { get; private set; }

    /// <summary>Gates made.</summary>
    public long Gates { get; private set; }

    /// <summary>Whether every cluster in the bake order has been built and nothing waits to be rebuilt or re-joined.</summary>
    public bool Complete => _next >= _order.Length && _dirty.Count == 0 && _rejoin.Count == 0 && _order.Length > 0;

    /// <summary>Whether <see cref="Component"/> answers: the graph is complete and the components were counted since.</summary>
    public bool ComponentsValid { get; private set; }

    public int ComponentCount { get; private set; }

    /// <summary>
    /// How many times the components were counted. A recount after a cluster is rebuilt may name the same land by another
    /// root, so anything that keeps a component (a gate's two lands) holds it only as long as this number stays.
    /// </summary>
    public int ComponentEpoch { get; private set; }

    /// <summary>Cells probed by every window this graph was built with, for the cost of the bake.</summary>
    public long Probes { get; internal set; }

    public int ClusterOf(int x, int y) => y / Side * ClustersX + x / Side;

    public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Terrain.Width && y < Terrain.Height;

    public bool IsBuilt(int cluster) => _clusters[cluster] is { Built: true };

    public bool IsJoined(int cluster) => _clusters[cluster] is { Joined: true };

    public int X(int node) => _x[node];

    public int Y(int node) => _y[node];

    public int Z(int node) => _z[node];

    public int Stratum(int node) => _s[node];

    public int ClusterOfNode(int node) => _cluster[node];

    /// <summary>The node on stratum <paramref name="s"/> of (x, y), or -1 when there is none.</summary>
    public int NodeAt(int x, int y, int s) => _at.TryGetValue(Key(x, y, s), out var id) ? id : -1;

    /// <summary>The component of a node, or -1 when the components are not counted.</summary>
    public int Component(int node) => ComponentsValid ? _component[node] : -1;

    /// <summary>The nodes of a cluster, or null when it has none yet.</summary>
    public IReadOnlyList<int> NodesOf(int cluster) => _clusters[cluster]?.Nodes;

    /// <summary>The range of a node's outgoing edges in its cluster's arrays, or false when the cluster is not joined.</summary>
    public bool EdgesOf(int node, out int[] to, out int[] cost, out int from, out int until)
    {
        to = null;
        cost = null;
        from = 0;
        until = 0;

        var c = _cluster[node];

        if (c < 0 || _clusters[c] is not { Joined: true } cluster)
        {
            return false;
        }

        var local = _local[node];

        to = cluster.To;
        cost = cluster.Cost;
        from = cluster.Start[local];
        until = cluster.Start[local + 1];

        return true;
    }

    /// <summary>
    /// Orders the bake outward from (x, y), nearest clusters first, and starts it over from the beginning of the new order
    /// (built clusters are skipped when their turn comes again).
    /// </summary>
    public void Prioritize(int x, int y)
    {
        var cx = Math.Clamp(x / Side, 0, ClustersX - 1);
        var cy = Math.Clamp(y / Side, 0, ClustersY - 1);
        var order = new int[ClustersX * ClustersY];
        var n = 0;
        var most = Math.Max(Math.Max(cx, ClustersX - 1 - cx), Math.Max(cy, ClustersY - 1 - cy));

        for (var r = 0; r <= most; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                var y0 = cy + dy;

                if (y0 < 0 || y0 >= ClustersY)
                {
                    continue;
                }

                var edge = dy == -r || dy == r;

                for (var dx = -r; dx <= r; dx += edge ? 1 : 2 * r)
                {
                    var x0 = cx + dx;

                    if (x0 >= 0 && x0 < ClustersX)
                    {
                        order[n++] = y0 * ClustersX + x0;
                    }

                    if (r == 0)
                    {
                        break;
                    }
                }
            }
        }

        _order = order;
        _next = 0;
        ComponentsValid = false;
    }

    /// <summary>
    /// Builds, re-joins and rebuilds clusters until the stopwatch passes <paramref name="deadline"/> (in
    /// <see cref="System.Diagnostics.Stopwatch"/> ticks). Returns true when the graph is complete. Counts the components
    /// once on the slice that completes it.
    /// </summary>
    public bool Slice(NavWindow window, long deadline)
    {
        var work = 0;

        while (true)
        {
            if (_dirty.Count > 0)
            {
                Build(_dirty.Dequeue(), window);
            }
            else if (_rejoin.Count > 0)
            {
                var c = _rejoin.Dequeue();

                if (_clusters[c] is { Built: true, Joined: false })
                {
                    Rejoin(c, window);
                }
            }
            else if (_next < _order.Length)
            {
                var c = _order[_next++];

                if (_clusters[c] is not { Built: true })
                {
                    Build(c, window);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                break;
            }

            if ((++work & 3) == 0 && System.Diagnostics.Stopwatch.GetTimestamp() >= deadline)
            {
                break;
            }
        }

        if (Complete && !ComponentsValid)
        {
            CountComponents();
        }

        return Complete;
    }

    /// <summary>Builds everything at once. For tests and small terrains.</summary>
    public void BuildAll(NavWindow window)
    {
        if (_order.Length == 0)
        {
            Prioritize(0, 0);
        }

        Slice(window, long.MaxValue);
    }

    /// <summary>
    /// The ground under a rectangle has changed: every cluster it touches, and the ring round them (a step onto the edge
    /// of a house starts in the next cluster), is torn down and queued to be built again ahead of anything else.
    /// </summary>
    public void MarkDirty(int x1, int y1, int x2, int y2)
    {
        var cx1 = Math.Clamp(Math.Min(x1, x2) / Side - 1, 0, ClustersX - 1);
        var cy1 = Math.Clamp(Math.Min(y1, y2) / Side - 1, 0, ClustersY - 1);
        var cx2 = Math.Clamp(Math.Max(x1, x2) / Side + 1, 0, ClustersX - 1);
        var cy2 = Math.Clamp(Math.Max(y1, y2) / Side + 1, 0, ClustersY - 1);

        for (var cy = cy1; cy <= cy2; cy++)
        {
            for (var cx = cx1; cx <= cx2; cx++)
            {
                var c = cy * ClustersX + cx;

                if (_clusters[c] is not { Built: true })
                {
                    continue;
                }

                TearDown(c);
                _dirty.Enqueue(c);
            }
        }

        ComponentsValid = false;
    }

    private NavCluster Get(int c) => _clusters[c] ??= new NavCluster();

    private void Build(int c, NavWindow w)
    {
        var cluster = Get(c);
        var cx = c % ClustersX;
        var cy = c / ClustersX;

        w.Reset(Terrain, c, cx * Side, cy * Side);

        var probes = w.Probes;

        if (cx + 1 < ClustersX && cluster.East == null)
        {
            Entrances(c, true, w);
        }

        if (cy + 1 < ClustersY && cluster.South == null)
        {
            Entrances(c, false, w);
        }

        if (cx > 0 && Get(c - 1).East == null)
        {
            Entrances(c - 1, true, w);
        }

        if (cy > 0 && Get(c - ClustersX).South == null)
        {
            Entrances(c - ClustersX, false, w);
        }

        if (!cluster.Built)
        {
            cluster.Built = true;
            Built++;
        }

        Join(c, w);

        Probes += w.Probes - probes;
    }

    private void Rejoin(int c, NavWindow w)
    {
        var cx = c % ClustersX;
        var cy = c / ClustersX;

        w.Reset(Terrain, c, cx * Side, cy * Side);

        var probes = w.Probes;

        Join(c, w);

        Probes += w.Probes - probes;
    }

    /// <summary>
    /// The gates on one edge: the east edge of cluster <paramref name="c"/> when <paramref name="east"/>, its south edge
    /// otherwise. The window must hold both sides of the edge — cluster c's own window does for its east and south edges,
    /// and its east or south neighbour's window does too, through its ring.
    /// </summary>
    private void Entrances(int c, bool east, NavWindow w)
    {
        var cx = c % ClustersX;
        var cy = c / ClustersX;
        int ax;
        int ay;
        int stepX;
        int stepY;
        int ox;
        int oy;
        int over;
        int back;
        int length;

        if (east)
        {
            ax = cx * Side + Side - 1;
            ay = cy * Side;
            stepX = 0;
            stepY = 1;
            ox = 1;
            oy = 0;
            over = East;
            back = West;
            length = Math.Min(Side, Terrain.Height - ay);
        }
        else
        {
            ax = cx * Side;
            ay = cy * Side + Side - 1;
            stepX = 1;
            stepY = 0;
            ox = 0;
            oy = 1;
            over = South;
            back = North;
            length = Math.Min(Side, Terrain.Width - ax);
        }

        var pairs = new List<(int A, int B)>();
        Span<int> runStart = stackalloc int[NavWindow.MaxStrata * NavWindow.MaxStrata];

        runStart.Fill(-1);

        for (var i = 0; i <= length; i++)
        {
            var active = 0;

            if (i < length)
            {
                var px = ax + stepX * i;
                var py = ay + stepY * i;
                var na = w.Count(px, py);
                var nb = na > 0 ? w.Count(px + ox, py + oy) : 0;

                for (var sa = 0; sa < na; sa++)
                {
                    for (var sb = 0; sb < nb; sb++)
                    {
                        if (Crosses(w, px, py, sa, px + ox, py + oy, sb, over, back, out _, out _))
                        {
                            active |= 1 << (sa * NavWindow.MaxStrata + sb);
                        }
                    }
                }
            }

            for (var k = 0; k < runStart.Length; k++)
            {
                var on = (active & (1 << k)) != 0;

                if (on && runStart[k] < 0)
                {
                    runStart[k] = i;
                }
                else if (!on && runStart[k] >= 0)
                {
                    var from = runStart[k];
                    var to = i - 1;
                    var sa = k / NavWindow.MaxStrata;
                    var sb = k % NavWindow.MaxStrata;

                    if (to - from + 1 <= LongEntrance)
                    {
                        Gate(w, ax, ay, stepX, stepY, ox, oy, from + (to - from) / 2, sa, sb, over, back, pairs);
                    }
                    else
                    {
                        Gate(w, ax, ay, stepX, stepY, ox, oy, from, sa, sb, over, back, pairs);
                        Gate(w, ax, ay, stepX, stepY, ox, oy, to, sa, sb, over, back, pairs);
                    }

                    runStart[k] = -1;
                }
            }
        }

        if (east)
        {
            Get(c).East = pairs;
        }
        else
        {
            Get(c).South = pairs;
        }
    }

    private static bool Crosses(
        NavWindow w, int px, int py, int sa, int qx, int qy, int sb, int over, int back, out bool forward, out bool backward
    )
    {
        forward = w.Step(px, py, sa, over, out _, out _, out var landed) && landed == sb;
        backward = w.Step(qx, qy, sb, back, out _, out _, out var landedBack) && landedBack == sa;

        return forward || backward;
    }

    private void Gate(
        NavWindow w, int ax, int ay, int stepX, int stepY, int ox, int oy, int i, int sa, int sb, int over, int back,
        List<(int A, int B)> pairs
    )
    {
        var px = ax + stepX * i;
        var py = ay + stepY * i;
        var qx = px + ox;
        var qy = py + oy;

        if (!Crosses(w, px, py, sa, qx, qy, sb, over, back, out var forward, out var backward))
        {
            return;
        }

        var a = Node(px, py, sa, w.Z(px, py, sa));
        var b = Node(qx, qy, sb, w.Z(qx, qy, sb));

        if (forward)
        {
            AddInter(a, b, NavCost.Step);
        }

        if (backward)
        {
            AddInter(b, a, NavCost.Step);
        }

        pairs.Add((a, b));
        Gates++;
    }

    private void AddInter(int from, int to, int cost)
    {
        var cluster = _clusters[_cluster[from]];

        cluster.InterFrom.Add(from);
        cluster.InterTo.Add(to);
        cluster.InterCost.Add(cost);
        Unjoin(_cluster[from]);
    }

    private void RemoveInter(int from, int to)
    {
        var c = _cluster[from];

        if (c < 0 || _clusters[c] is not { } cluster)
        {
            return;
        }

        for (var i = cluster.InterFrom.Count - 1; i >= 0; i--)
        {
            if (cluster.InterFrom[i] != from || cluster.InterTo[i] != to)
            {
                continue;
            }

            var last = cluster.InterFrom.Count - 1;

            cluster.InterFrom[i] = cluster.InterFrom[last];
            cluster.InterTo[i] = cluster.InterTo[last];
            cluster.InterCost[i] = cluster.InterCost[last];
            cluster.InterFrom.RemoveAt(last);
            cluster.InterTo.RemoveAt(last);
            cluster.InterCost.RemoveAt(last);
        }

        Unjoin(c);
    }

    private static long Key(int x, int y, int s) => ((long)x << 32) | ((long)(y & 0xFFFFFF) << 8) | (uint)(s & 0xFF);

    private int Node(int x, int y, int s, sbyte z)
    {
        var key = Key(x, y, s);

        if (_at.TryGetValue(key, out var id))
        {
            _refs[id]++;

            return id;
        }

        id = _free.Count > 0 ? _free.Pop() : _high++;

        Grow(id + 1);

        var c = ClusterOf(x, y);
        var cluster = Get(c);

        _x[id] = (short)x;
        _y[id] = (short)y;
        _z[id] = z;
        _s[id] = (byte)s;
        _cluster[id] = c;
        _local[id] = cluster.Nodes.Count;
        _refs[id] = 1;
        _component[id] = -1;
        cluster.Nodes.Add(id);
        _at[key] = id;

        Unjoin(c);

        return id;
    }

    private void Release(int id)
    {
        if (--_refs[id] > 0)
        {
            return;
        }

        var c = _cluster[id];
        var cluster = _clusters[c];
        var pos = _local[id];
        var last = cluster.Nodes[^1];

        cluster.Nodes[pos] = last;
        _local[last] = pos;
        cluster.Nodes.RemoveAt(cluster.Nodes.Count - 1);

        // Its own gates' edges went with the edge that made them (Clear); anything left is dropped here.
        for (var i = cluster.InterFrom.Count - 1; i >= 0; i--)
        {
            if (cluster.InterFrom[i] != id)
            {
                continue;
            }

            var tail = cluster.InterFrom.Count - 1;

            cluster.InterFrom[i] = cluster.InterFrom[tail];
            cluster.InterTo[i] = cluster.InterTo[tail];
            cluster.InterCost[i] = cluster.InterCost[tail];
            cluster.InterFrom.RemoveAt(tail);
            cluster.InterTo.RemoveAt(tail);
            cluster.InterCost.RemoveAt(tail);
        }

        _at.Remove(Key(_x[id], _y[id], _s[id]));
        _cluster[id] = -1;
        _free.Push(id);

        Unjoin(c);
    }

    /// <summary>A cluster's node set or its edges changed: its arrays are stale until it is joined again.</summary>
    private void Unjoin(int c)
    {
        var cluster = _clusters[c];

        if (cluster is not { Joined: true })
        {
            return;
        }

        cluster.Joined = false;
        Edges -= cluster.To.Length;
        _versions[c]++;

        if (cluster.Built)
        {
            _rejoin.Enqueue(c);
        }

        ComponentsValid = false;
    }

    /// <summary>Takes a cluster's four edges down, both sides of each, so it can be built again from its ground.</summary>
    private void TearDown(int c)
    {
        var cx = c % ClustersX;
        var cy = c / ClustersX;
        var cluster = _clusters[c];

        Clear(cluster.East);
        cluster.East = null;
        Clear(cluster.South);
        cluster.South = null;

        if (cx > 0 && _clusters[c - 1] is { } west)
        {
            Clear(west.East);
            west.East = null;
        }

        if (cy > 0 && _clusters[c - ClustersX] is { } north)
        {
            Clear(north.South);
            north.South = null;
        }

        Unjoin(c);
        cluster.Built = false;
        Built--;
    }

    private void Clear(List<(int A, int B)> pairs)
    {
        if (pairs == null)
        {
            return;
        }

        for (var i = 0; i < pairs.Count; i++)
        {
            var (a, b) = pairs[i];

            RemoveInter(a, b);
            RemoveInter(b, a);
            Release(a);
            Release(b);
        }
    }

    /// <summary>The cluster's edges from each of its nodes: to each other node its flood reaches, and across its gates.</summary>
    private void Join(int c, NavWindow w)
    {
        var cluster = _clusters[c];
        var nodes = cluster.Nodes;
        var n = nodes.Count;
        var start = new int[n + 1];

        _toScratch.Clear();
        _costScratch.Clear();

        for (var i = 0; i < n; i++)
        {
            _local[nodes[i]] = i;
        }

        for (var i = 0; i < n; i++)
        {
            start[i] = _toScratch.Count;

            var id = nodes[i];

            if (n > 1)
            {
                var dist = w.Flood(_x[id], _y[id], _s[id], false);

                for (var j = 0; j < n; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var m = nodes[j];
                    var d = dist[w.State(_x[m], _y[m], _s[m])];

                    if (d >= 0)
                    {
                        _toScratch.Add(m);
                        _costScratch.Add(d);
                    }
                }
            }

            for (var k = 0; k < cluster.InterFrom.Count; k++)
            {
                if (cluster.InterFrom[k] == id)
                {
                    _toScratch.Add(cluster.InterTo[k]);
                    _costScratch.Add(cluster.InterCost[k]);
                }
            }
        }

        start[n] = _toScratch.Count;

        if (cluster.Joined)
        {
            Edges -= cluster.To.Length;
        }

        cluster.Start = start;
        cluster.To = _toScratch.ToArray();
        cluster.Cost = _costScratch.ToArray();
        cluster.Joined = true;
        _versions[c]++;
        Edges += cluster.To.Length;
        ComponentsValid = false;
    }

    /// <summary>
    /// A copy of the graph's arrays for <see cref="NavGraphFile.Write"/>, taken on the loop. Only a complete graph with every
    /// cluster joined is worth keeping; null otherwise.
    /// </summary>
    public NavGraphSnapshot Snapshot(ulong fingerprint, int[] housed)
    {
        if (!Complete)
        {
            return null;
        }

        var snapshot = new NavGraphSnapshot
        {
            Fingerprint = fingerprint,
            Width = Terrain.Width,
            Height = Terrain.Height,
            High = _high,
            X = _x[.._high],
            Y = _y[.._high],
            Z = _z[.._high],
            S = _s[.._high],
            Cluster = _cluster[.._high],
            Refs = _refs[.._high],
            Clusters = new NavClusterSnapshot[_clusters.Length],
            Housed = housed,
            Gates = Gates
        };

        for (var c = 0; c < _clusters.Length; c++)
        {
            if (_clusters[c] is not { } cluster)
            {
                continue;
            }

            snapshot.Clusters[c] = new NavClusterSnapshot
            {
                Built = cluster.Built,
                Nodes = cluster.Nodes.ToArray(),
                InterFrom = cluster.InterFrom.ToArray(),
                InterTo = cluster.InterTo.ToArray(),
                InterCost = cluster.InterCost.ToArray(),
                East = cluster.East?.ToArray(),
                South = cluster.South?.ToArray(),
                // Replaced whole by every join, never changed in place, so shared rather than copied.
                Start = cluster.Start,
                To = cluster.To,
                Cost = cluster.Cost
            };
        }

        return snapshot;
    }

    /// <summary>
    /// Puts a graph read from a file in place of whatever this one holds, on the loop, and orders the bake from
    /// <paramref name="x"/>, <paramref name="y"/> as finished. Houses are the caller's: see <see cref="MarkDirtyCluster"/>.
    /// </summary>
    public void Restore(NavGraphSnapshot snapshot, int x, int y)
    {
        _free.Clear();
        _at.Clear();
        _rejoin.Clear();
        _dirty.Clear();
        _high = snapshot.High;
        Grow(Math.Max(1, _high));

        Array.Copy(snapshot.X, _x, _high);
        Array.Copy(snapshot.Y, _y, _high);
        Array.Copy(snapshot.Z, _z, _high);
        Array.Copy(snapshot.S, _s, _high);
        Array.Copy(snapshot.Cluster, _cluster, _high);
        Array.Copy(snapshot.Refs, _refs, _high);

        for (var i = 0; i < _high; i++)
        {
            _component[i] = -1;

            if (_cluster[i] >= 0)
            {
                _at[Key(_x[i], _y[i], _s[i])] = i;
            }
            else
            {
                _free.Push(i);
            }
        }

        Built = 0;
        Edges = 0;
        Gates = snapshot.Gates;

        for (var c = 0; c < _clusters.Length; c++)
        {
            _versions[c]++;
            _clusters[c] = null;

            if (snapshot.Clusters[c] is not { } saved)
            {
                continue;
            }

            var cluster = new NavCluster
            {
                Built = saved.Built,
                Joined = saved.Built,
                East = saved.East == null ? null : new List<(int A, int B)>(saved.East),
                South = saved.South == null ? null : new List<(int A, int B)>(saved.South),
                Start = saved.Start,
                To = saved.To,
                Cost = saved.Cost
            };

            cluster.Nodes.AddRange(saved.Nodes);
            cluster.InterFrom.AddRange(saved.InterFrom);
            cluster.InterTo.AddRange(saved.InterTo);
            cluster.InterCost.AddRange(saved.InterCost);

            for (var i = 0; i < cluster.Nodes.Count; i++)
            {
                _local[cluster.Nodes[i]] = i;
            }

            _clusters[c] = cluster;

            if (cluster.Built)
            {
                Built++;
                Edges += cluster.To.Length;
            }
        }

        Prioritize(x, y);
        _next = _order.Length;
        ComponentsValid = false;
    }

    /// <summary>Tears one cluster down and queues it to be drawn again. See <see cref="MarkDirty"/>.</summary>
    public void MarkDirtyCluster(int c)
    {
        if (c < 0 || c >= _clusters.Length || _clusters[c] is not { Built: true })
        {
            return;
        }

        TearDown(c);
        _dirty.Enqueue(c);
        ComponentsValid = false;
    }

    /// <summary>Union-find over every edge, undirected: a component per node. Run when the graph completes.</summary>
    public void CountComponents()
    {
        var parent = new int[_high];

        for (var i = 0; i < _high; i++)
        {
            parent[i] = i;
        }

        for (var c = 0; c < _clusters.Length; c++)
        {
            if (_clusters[c] is not { Joined: true } cluster)
            {
                continue;
            }

            for (var i = 0; i < cluster.Nodes.Count; i++)
            {
                var a = cluster.Nodes[i];

                for (var e = cluster.Start[i]; e < cluster.Start[i + 1]; e++)
                {
                    Union(parent, a, cluster.To[e]);
                }
            }
        }

        var count = 0;

        for (var i = 0; i < _high; i++)
        {
            if (_cluster[i] < 0)
            {
                _component[i] = -1;

                continue;
            }

            var root = Find(parent, i);

            _component[i] = root;

            if (root == i)
            {
                count++;
            }
        }

        ComponentCount = count;
        ComponentsValid = true;
        ComponentEpoch++;
    }

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }

        return i;
    }

    private static void Union(int[] parent, int a, int b)
    {
        var ra = Find(parent, a);
        var rb = Find(parent, b);

        if (ra != rb)
        {
            parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }
    }

    private void Grow(int size)
    {
        if (size <= _x.Length)
        {
            return;
        }

        var n = Math.Max(size, _x.Length * 2);

        Array.Resize(ref _x, n);
        Array.Resize(ref _y, n);
        Array.Resize(ref _z, n);
        Array.Resize(ref _s, n);
        Array.Resize(ref _cluster, n);
        Array.Resize(ref _local, n);
        Array.Resize(ref _refs, n);
        Array.Resize(ref _component, n);
    }

    public string Describe() =>
        $"{Built} of {_clusters.Length} clusters built ({(Complete ? "complete" : $"{_order.Length - _next} to go")}), "
        + $"{Nodes} nodes, {Gates} gates, {Edges} edges, {(ComponentsValid ? $"{ComponentCount} components" : "components not counted")}, "
        + $"{Probes} cells probed";
}

/// <summary>One cluster's nodes, its gates' pairs on its east and south edges, and its edges.</summary>
internal sealed class NavCluster
{
    public readonly List<int> Nodes = [];

    public readonly List<int> InterFrom = [];

    public readonly List<int> InterTo = [];

    public readonly List<int> InterCost = [];

    public List<(int A, int B)> East;

    public List<(int A, int B)> South;

    public int[] Start = [0];

    public int[] To = [];

    public int[] Cost = [];

    public bool Built;

    public bool Joined;
}
