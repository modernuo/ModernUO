using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Logging;
using CalcMoves = Server.Movement.Movement;

namespace Server.BotAI.V2;

/// <summary>
/// The coarse chart of the ground round home: squares of <see cref="Side"/> tiles, the ways across each square's edges, and
/// which of those ways join inside the square — so that a walk longer than the planner can see is walked as a string of
/// short ones.
///
/// <para>
/// <b>Patrick's order of 26.09.2026: "maybe it is time to find another way of finding a path — if they are banging into a
/// rock, build the way roughly over the squares that can be crossed; we have used your research for almost a month and the
/// bots are still terribly dull."</b> The planner is one search from where the bot stands, paid for with a ceiling of 60 to
/// 150 milliseconds, and a search with a ceiling cannot see a way round longer than its ceiling. The Lantern's hall at
/// 1082,1402 is ninety tiles from the east bank at (1168, 1384) and about 1,170 steps from it by road, round by the south:
/// its company was called at the hall's door three times between 14:05 and 14:14 that day and failed every time, "no way
/// round it was found in 150ms". In the session before (13:16–14:05) the planner ran 72,294 searches for 562 seconds of the
/// loop — a fifth of it — and 27,739 of them ended short, 11,141 no nearer than they began. Every fix of the month before
/// was to the ceiling, the notes and the refusals; this is the method.
/// </para>
///
/// <para>
/// <b>What it is.</b> The two-level search of the literature (hierarchical A*): the ground is cut into squares; wherever a
/// body can step across the edge between two squares, a run of such steps is a gate, with a node on each side of it; inside
/// each square, a small flood from each node says which of the square's other nodes it reaches and in how many steps. A
/// route is an A* over those nodes, from the square the bot stands in to the square it is going to, and what comes back is
/// one point a square: each of them a short walk the planner finds at once. Whether a body can step from a tile, and where
/// to, is what <see cref="BotRoads"/> learned drawing its own flood — the engine's step masks and both flanks of a
/// diagonal — kept per tile (<c>BotRoads.Lend</c>), so the chart costs no look at the world of its own.
/// </para>
///
/// <para>
/// <b>What it does not know.</b> The ground as it was when the road map was drawn: a house raised since, an item standing
/// in a gate. The planner walking each short leg steps round those as it always did, and a gate a leg could not pass is
/// shunned for <see cref="ShunMs"/> (<see cref="Shun"/>).
/// </para>
/// </summary>
public static class BotChart
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotChart));

    /// <summary>Whether the chart is drawn and consulted at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The side of a square of the chart, in tiles. Read when the chart is drawn.</summary>
    public static int Side { get; set; } = 16;

    /// <summary>The most one slice of drawing may spend on the game loop.</summary>
    public static double SliceMs { get; set; } = 8.0;

    /// <summary>A run of crossings longer than this across one edge is two gates, one near each end.</summary>
    public static int LongGate { get; set; } = 8;

    /// <summary>Most nodes one route may expand before it gives up.</summary>
    public static int MostExpanded { get; set; } = 80000;

    /// <summary>How far round a place the chart looks for a tile its flood reached, when the place itself was not.</summary>
    public static int Anchorage { get; set; } = 3;

    /// <summary>How long a gate a leg could not pass is shunned, and what shunning it costs a route.</summary>
    public static int ShunMs { get; set; } = 600000;

    public static int ShunCost { get; set; } = 400;

    private static Map _map;

    private static int _x0;

    private static int _y0;

    private static int _span;

    private static byte[] _exits;

    private static sbyte[] _heights;

    private static int _side;

    private static int _per;

    /// <summary>Per node: the tile's index in the road map's square, and the square of the chart it is in.</summary>
    private static readonly List<int> _tile = [];

    private static readonly List<int> _cellOf = [];

    private static List<int>[] _cellNodes;

    private static readonly Dictionary<int, int> _nodeAt = [];

    private static readonly List<(int From, int To, int Cost)> _edges = [];

    private static int[] _first;

    private static int[] _to;

    private static int[] _cost;

    private static bool _gated;

    private static int _built;

    private static long _begunTick;

    private static readonly Dictionary<int, long> _shunned = [];

    /// <summary>Whether the chart is drawn and may be asked.</summary>
    public static bool Ready { get; private set; }

    public static int Nodes => _tile.Count;

    public static int Gates { get; private set; }

    public static long Slices { get; private set; }

    public static double SpentMs { get; private set; }

    public static long Asked { get; private set; }

    public static long Routed { get; private set; }

    /// <summary>Asks answered with no route because the two places are joined inside one square.</summary>
    public static long Near { get; private set; }

    /// <summary>Asks the chart could not place: off its ground, or no tile its flood reached near enough.</summary>
    public static long Unplaced { get; private set; }

    /// <summary>Asks with no way between the two places on the chart.</summary>
    public static long Apart { get; private set; }

    public static long Expanded { get; private set; }

    public static double SearchMs { get; private set; }

    public static long ShunnedGates { get; private set; }

    /// <summary>Begins drawing the chart from the finished road map. Called by <c>BotRoads</c> when its flood is done.</summary>
    public static void Begin()
    {
        Forget();

        if (!Running || !BotRoads.Ready || !BotRoads.Lend(out _map, out _x0, out _y0, out _span, out _exits, out _heights))
        {
            return;
        }

        _side = Math.Clamp(Side, 4, 64);
        _per = (_span + _side - 1) / _side;
        _cellNodes = new List<int>[_per * _per];
        _begunTick = Core.TickCount;
    }

    /// <summary>One slice of the drawing. Called on the road map's timer until <see cref="Ready"/>.</summary>
    public static bool Slice()
    {
        // Nothing begun (the chart is off, or the road map lent nothing): nothing to draw, and the clock may stop.
        if (Ready || _cellNodes == null)
        {
            return true;
        }

        var started = Stopwatch.GetTimestamp();
        var deadline = started + (long)(SliceMs * Stopwatch.Frequency / 1000.0);

        if (!_gated)
        {
            DrawGates();
            _gated = true;
        }
        else
        {
            var bfs = new int[_side * _side];

            while (_built < _cellNodes.Length)
            {
                Join(_built++, bfs);

                if ((_built & 7) == 0 && Stopwatch.GetTimestamp() >= deadline)
                {
                    break;
                }
            }

            if (_built >= _cellNodes.Length)
            {
                Finish();
            }
        }

        Slices++;
        SpentMs += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        return Ready;
    }

    /// <summary>Every gate across every edge between two squares: runs of tiles a body can step across both ways.</summary>
    private static void DrawGates()
    {
        for (var cy = 0; cy < _per; cy++)
        {
            for (var cx = 0; cx < _per; cx++)
            {
                var x1 = Math.Min((cx + 1) * _side, _span) - 1;
                var y1 = Math.Min((cy + 1) * _side, _span) - 1;

                // East: from the square's last column into the next square's first.
                if (x1 + 1 < _span)
                {
                    Runs(x1, cy * _side, 0, 1, y1 - cy * _side + 1, (int)Direction.East, (int)Direction.West, 1, 0);
                }

                // South: from the square's last row into the next square's first.
                if (y1 + 1 < _span)
                {
                    Runs(cx * _side, y1, 1, 0, x1 - cx * _side + 1, (int)Direction.South, (int)Direction.North, 0, 1);
                }
            }
        }
    }

    /// <summary>
    /// Walks one edge, <paramref name="length"/> tiles from (<paramref name="x"/>, <paramref name="y"/>) along
    /// (<paramref name="ax"/>, <paramref name="ay"/>), and makes a gate of each run of tiles that step across it (direction
    /// <paramref name="over"/>, offset <paramref name="ox"/>, <paramref name="oy"/>) and back (<paramref name="back"/>).
    /// </summary>
    private static void Runs(int x, int y, int ax, int ay, int length, int over, int back, int ox, int oy)
    {
        var start = -1;

        for (var i = 0; i <= length; i++)
        {
            var open = i < length && Crosses(x + ax * i, y + ay * i, over, back, ox, oy);

            if (open && start < 0)
            {
                start = i;
            }
            else if (!open && start >= 0)
            {
                var run = i - start;

                if (run > LongGate)
                {
                    Gate(x + ax * (start + 1), y + ay * (start + 1), ox, oy);
                    Gate(x + ax * (i - 2), y + ay * (i - 2), ox, oy);
                }
                else
                {
                    var middle = start + run / 2;

                    Gate(x + ax * middle, y + ay * middle, ox, oy);
                }

                start = -1;
            }
        }
    }

    private static bool Crosses(int x, int y, int over, int back, int ox, int oy)
    {
        var a = Exits(x, y);
        var b = Exits(x + ox, y + oy);

        return (a & (1 << over)) != 0 && (b & (1 << back)) != 0;
    }

    private static byte Exits(int x, int y) =>
        x < 0 || y < 0 || x >= _span || y >= _span ? (byte)0 : _exits[y * _span + x];

    private static void Gate(int x, int y, int ox, int oy)
    {
        var a = Node(x, y);
        var b = Node(x + ox, y + oy);

        _edges.Add((a, b, 1));
        _edges.Add((b, a, 1));
        Gates++;
    }

    private static int Node(int x, int y)
    {
        var index = y * _span + x;

        if (_nodeAt.TryGetValue(index, out var node))
        {
            return node;
        }

        node = _tile.Count;
        _tile.Add(index);

        var cell = y / _side * _per + x / _side;

        _cellOf.Add(cell);
        (_cellNodes[cell] ??= []).Add(node);
        _nodeAt[index] = node;

        return node;
    }

    /// <summary>Which of one square's nodes reach which, inside the square, and in how many steps.</summary>
    private static void Join(int cell, int[] bfs)
    {
        var nodes = _cellNodes[cell];

        if (nodes == null || nodes.Count < 2)
        {
            return;
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            Flood(cell, _tile[nodes[i]], bfs);

            for (var j = 0; j < nodes.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var steps = bfs[Local(cell, _tile[nodes[j]])];

                if (steps >= 0)
                {
                    _edges.Add((nodes[i], nodes[j], steps));
                }
            }
        }
    }

    private static int Local(int cell, int index)
    {
        var x = index % _span - cell % _per * _side;
        var y = index / _span - cell / _per * _side;

        return y * _side + x;
    }

    private static readonly Queue<int> _queue = new();

    /// <summary>Steps from one tile to every tile of its square, staying inside the square; -1 for a tile not reached.</summary>
    private static void Flood(int cell, int from, int[] steps)
    {
        Array.Fill(steps, -1);

        var cx0 = cell % _per * _side;
        var cy0 = cell / _per * _side;
        var cx1 = Math.Min(cx0 + _side, _span);
        var cy1 = Math.Min(cy0 + _side, _span);

        _queue.Clear();
        steps[Local(cell, from)] = 0;
        _queue.Enqueue(from);

        while (_queue.Count > 0)
        {
            var at = _queue.Dequeue();
            var x = at % _span;
            var y = at / _span;
            var here = steps[(y - cy0) * _side + (x - cx0)];
            var exits = _exits[at];

            for (var d = 0; d < 8; d++)
            {
                if ((exits & (1 << d)) == 0)
                {
                    continue;
                }

                var nx = x;
                var ny = y;

                CalcMoves.Offset((Direction)d, ref nx, ref ny);

                if (nx < cx0 || ny < cy0 || nx >= cx1 || ny >= cy1)
                {
                    continue;
                }

                var local = (ny - cy0) * _side + (nx - cx0);

                if (steps[local] >= 0 || _exits[ny * _span + nx] == 0)
                {
                    continue;
                }

                steps[local] = here + 1;
                _queue.Enqueue(ny * _span + nx);
            }
        }
    }

    private static void Finish()
    {
        var count = _tile.Count;

        _first = new int[count + 1];
        _to = new int[_edges.Count];
        _cost = new int[_edges.Count];

        for (var i = 0; i < _edges.Count; i++)
        {
            _first[_edges[i].From + 1]++;
        }

        for (var i = 0; i < count; i++)
        {
            _first[i + 1] += _first[i];
        }

        var fill = new int[count];

        for (var i = 0; i < _edges.Count; i++)
        {
            var (from, to, cost) = _edges[i];
            var at = _first[from] + fill[from]++;

            _to[at] = to;
            _cost[at] = cost;
        }

        _edges.Clear();
        _edges.TrimExcess();
        Ready = true;

        logger.Information(
            "Chart: {Nodes} nodes and {Gates} gates on {Squares} squares of {Side} tiles, {Edges} ways between them; {Slices} slices, {Ms:F0}ms of the loop over {Seconds:F1}s",
            count,
            Gates,
            _cellNodes.Length,
            _side,
            _to.Length,
            Slices,
            SpentMs,
            (Core.TickCount - _begunTick) / 1000.0
        );
    }

    /// <summary>
    /// The way from <paramref name="from"/> to <paramref name="to"/> over the chart, as one point a square, into
    /// <paramref name="points"/> (cleared first), the last of them in the destination's square. False when the chart is not
    /// drawn or cannot place either end, when the two are joined inside one square (the planner needs no help), and when
    /// the chart knows no way between them.
    /// </summary>
    public static bool Route(Map map, Point3D from, Point3D to, List<Point3D> points)
    {
        points?.Clear();

        if (!Ready || map != _map || points == null)
        {
            return false;
        }

        Asked++;

        var start = Anchor(from.X - _x0, from.Y - _y0);
        var goal = Anchor(to.X - _x0, to.Y - _y0);

        if (start < 0 || goal < 0)
        {
            Unplaced++;

            return false;
        }

        var started = Stopwatch.GetTimestamp();
        var found = Search(start, goal, points);

        SearchMs += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        return found;
    }

    /// <summary>A tile at or near (x, y) of the square that the road map's flood reached, or -1.</summary>
    private static int Anchor(int x, int y)
    {
        for (var r = 0; r <= Anchorage; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    if (Exits(x + dx, y + dy) != 0)
                    {
                        return (y + dy) * _span + x + dx;
                    }
                }
            }
        }

        return -1;
    }

    private static int[] _g = [];

    private static int[] _parent = [];

    private static int[] _stamp = [];

    private static int[] _closed = [];

    private static int _search;

    private static int[] _startSteps = [];

    private static int[] _goalSteps = [];

    private static readonly PriorityQueue<int, int> _open = new();

    private static bool Search(int start, int goal, List<Point3D> points)
    {
        var count = _tile.Count;
        var s = count;
        var g = count + 1;

        if (_g.Length < count + 2)
        {
            _g = new int[count + 2];
            _parent = new int[count + 2];
            _stamp = new int[count + 2];
            _closed = new int[count + 2];
        }

        if (_startSteps.Length != _side * _side)
        {
            _startSteps = new int[_side * _side];
            _goalSteps = new int[_side * _side];
        }

        var startCell = Cell(start);
        var goalCell = Cell(goal);

        Flood(startCell, start, _startSteps);

        if (startCell == goalCell && _startSteps[Local(startCell, goal)] >= 0)
        {
            Near++;

            return false;
        }

        Flood(goalCell, goal, _goalSteps);

        _search++;
        _open.Clear();

        Touch(s, 0, -1);
        _open.Enqueue(s, Heuristic(start, goal));

        var now = Core.TickCount;
        var expanded = 0;

        while (_open.TryDequeue(out var node, out _))
        {
            if (node == g)
            {
                Expanded += expanded;
                Routed++;

                Unwind(g, s, points);

                return true;
            }

            // Popped once for good: every way to a node costs at least the straight steps between its ends, so the first
            // time a node leaves the queue its cost is its least.
            if (_closed[node] == _search)
            {
                continue;
            }

            _closed[node] = _search;

            if (++expanded > MostExpanded)
            {
                break;
            }

            var here = _g[node];

            if (node == s)
            {
                var nodes = _cellNodes[startCell];

                for (var i = 0; nodes != null && i < nodes.Count; i++)
                {
                    var steps = _startSteps[Local(startCell, _tile[nodes[i]])];

                    if (steps >= 0)
                    {
                        Relax(nodes[i], here + steps + Penalty(nodes[i], now), node, goal);
                    }
                }

                continue;
            }

            if (_cellOf[node] == goalCell)
            {
                var steps = _goalSteps[Local(goalCell, _tile[node])];

                if (steps >= 0)
                {
                    Relax(g, here + steps, node, goal);
                }
            }

            for (var e = _first[node]; e < _first[node + 1]; e++)
            {
                Relax(_to[e], here + _cost[e] + Penalty(_to[e], now), node, goal);
            }
        }

        Expanded += expanded;
        Apart++;

        return false;
    }

    private static int Cell(int index) => index / _span / _side * _per + index % _span / _side;

    private static int Heuristic(int index, int goal) =>
        Math.Max(Math.Abs(index % _span - goal % _span), Math.Abs(index / _span - goal / _span));

    private static void Touch(int node, int cost, int parent)
    {
        _stamp[node] = _search;
        _g[node] = cost;
        _parent[node] = parent;
    }

    private static void Relax(int node, int cost, int parent, int goal)
    {
        if (_stamp[node] == _search && _g[node] <= cost)
        {
            return;
        }

        Touch(node, cost, parent);

        var index = node < _tile.Count ? _tile[node] : goal;

        _open.Enqueue(node, cost + Heuristic(index, goal));
    }

    private static int Penalty(int node, long now)
    {
        if (_shunned.Count == 0 || !_shunned.TryGetValue(node, out var until))
        {
            return 0;
        }

        if (now - until >= 0)
        {
            _shunned.Remove(node);

            return 0;
        }

        return ShunCost;
    }

    /// <summary>The nodes from the start to the goal, one point a square: of a gate's two nodes, the one on the far side.</summary>
    private static void Unwind(int g, int s, List<Point3D> points)
    {
        var path = new List<int>();

        for (var node = _parent[g]; node >= 0 && node != s; node = _parent[node])
        {
            path.Add(node);
        }

        path.Reverse();

        for (var i = 0; i < path.Count; i++)
        {
            // The near side of a gate is one step from its far side, and the far side is the one worth walking to.
            if (i + 1 < path.Count && _cellOf[path[i]] != _cellOf[path[i + 1]])
            {
                continue;
            }

            var index = _tile[path[i]];

            points.Add(new Point3D(_x0 + index % _span, _y0 + index / _span, _heights[index]));
        }
    }

    /// <summary>
    /// A leg to <paramref name="point"/> could not be walked: the gate there is shunned for <see cref="ShunMs"/>, so the next
    /// route goes round it if it can.
    /// </summary>
    public static void Shun(Map map, Point3D point)
    {
        if (!Ready || map != _map)
        {
            return;
        }

        var x = point.X - _x0;
        var y = point.Y - _y0;

        if (x < 0 || y < 0 || x >= _span || y >= _span || !_nodeAt.TryGetValue(y * _span + x, out var node))
        {
            return;
        }

        _shunned[node] = Core.TickCount + ShunMs;
        ShunnedGates++;
    }

    public static string Describe() =>
        !Running ? "the chart is not drawn"
        : !Ready ? $"the chart is being drawn: {_tile.Count} nodes, {_built} of {_cellNodes?.Length ?? 0} squares joined"
        : $"the chart: {_tile.Count} nodes and {Gates} gates on squares of {_side}; {Asked} routes asked, {Routed} found, {Near} "
        + $"joined inside one square, {Apart} with no way between, {Unplaced} off the chart; {Expanded} nodes expanded in "
        + $"{SearchMs:F0}ms; {ShunnedGates} gates shunned after a leg that could not pass";

    public static void Forget()
    {
        Ready = false;
        _gated = false;
        _built = 0;
        _tile.Clear();
        _cellOf.Clear();
        _nodeAt.Clear();
        _edges.Clear();
        _shunned.Clear();
        _cellNodes = null;
        _first = null;
        _to = null;
        _cost = null;
        Gates = 0;
        Slices = 0;
        SpentMs = 0.0;
        Asked = 0;
        Routed = 0;
        Near = 0;
        Unplaced = 0;
        Apart = 0;
        Expanded = 0;
        SearchMs = 0.0;
        ShunnedGates = 0;
    }
}
