using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Engines.Pathing.Cache;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>What the check found, published whole for the dashboard through one volatile reference.</summary>
public sealed record BotReachCheckReport(
    int Version,
    int Asked,
    int Done,
    int[] Agree,
    int[] Disagree,
    int[] Unsure,
    string[] Misses,
    string State,
    double Ms,
    int Stood
);

/// <summary>
/// The reach map examined where it is most likely wrong: on its borders, tile by tile, by the walker's own means rather than
/// the map's.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, night: "Argus and the debuggers must help."</b> The map is drawn by the navigation graph's
/// probe (<see cref="BotReachBuilder"/>); the check asks the questions a bot's walker asks, through code that shares none of
/// the map's own path: the walker's search (<see cref="BotPath.Find"/>, over the engine's step cache and the items on the
/// ground), a flood of the walker's own steps (<see cref="BotStep.Mask"/>), and the walker's footing test. A free debugger is
/// carried to each tile as it is examined, so a client standing with Argus sees where the map is being proved.
/// </para>
///
/// <para>
/// <b>What each class is asked.</b> Open ground: can the walker's search get onto the tile from open ground a few tiles off?
/// Isle and pocket: can it — and if the search runs out of clock, does a flood of the walker's steps from the tile close
/// within <see cref="FloodCells"/> (a pocket walked to its edges)? Water and blocked: can a walker standing on open ground
/// beside it step onto it? An answer the walker's means cannot give (the clock ran out, the flood reached the world) is
/// counted as unsure, never as agreement.
/// </para>
/// </summary>
public static class BotReachCheck
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotReachCheck));

    /// <summary>Whether the check runs by itself once the first map of a session is solved.</summary>
    public static bool OnBoot { get; set; } = true;

    /// <summary>Border tiles examined by a check.</summary>
    public static int Samples { get; set; } = 240;

    /// <summary>The loop's milliseconds a beat may spend examining, and how often a beat runs.</summary>
    public static double BudgetMs { get; set; } = 10.0;

    public static int TickMs { get; set; } = 400;

    /// <summary>The walker's search's clock for one tile, in milliseconds.</summary>
    public static double CeilingMs { get; set; } = 30.0;

    /// <summary>How far off the tile the open ground a search starts from may lie.</summary>
    public static int AnchorReach { get; set; } = 8;

    /// <summary>Cells a flood from a tile may count before it is ground that goes on: the world, not a pocket.</summary>
    public static int FloodCells { get; set; } = 3000;

    /// <summary>Whether a free debugger is carried to each tile as it is examined.</summary>
    public static bool Carry { get; set; } = true;

    /// <summary>The watchers free to walk, and their teleport: filled by the debugger (BotDebugModule), as for the zones tour.</summary>
    public static Func<IReadOnlyList<(string Name, Mobile Body)>> Walkers { get; set; }

    public static Func<Mobile, Map, Point3D, bool> Move { get; set; }

    private static volatile BotReachCheckReport _report = new(0, 0, 0, new int[6], new int[6], new int[6], [], "not run", 0, 0);

    /// <summary>The last report, safe from any thread.</summary>
    public static BotReachCheckReport Report => _report;

    private sealed class Sample
    {
        public Point3D At;

        public BotReachClass Class;
    }

    private static readonly Queue<Sample> _queue = new();

    private static readonly int[] _agree = new int[6];

    private static readonly int[] _disagree = new int[6];

    private static readonly int[] _unsure = new int[6];

    private static readonly List<string> _misses = [];

    private static readonly List<Point3D> _path = [];

    private static int _asked;

    private static int _done;

    private static int _stood;

    private static double _ms;

    private static int _version;

    private static string _state = "not run";

    private static bool _ranOnce;

    private static Timer _timer;

    private static System.Random _random = new(1102026);

    /// <summary>A map was solved: the first of the session is examined by itself.</summary>
    public static void Solved(BotReachSnapshot snap)
    {
        if (!OnBoot || _ranOnce || snap == null || !snap.Homed)
        {
            return;
        }

        Start(Samples, "the boot");
    }

    /// <summary>Starts a check of <paramref name="n"/> border tiles. <c>reach check [n]</c> at the door.</summary>
    public static string Start(int n, string who)
    {
        var snap = BotReachMap.Snapshot;

        if (snap == null)
        {
            return $"the reach map is not solved yet ({BotReachMap.State}).";
        }

        if (snap.Mixed.Length == 0)
        {
            return "the reach map has no border between open ground and anything else to examine.";
        }

        _ranOnce = true;
        _queue.Clear();
        Array.Clear(_agree);
        Array.Clear(_disagree);
        Array.Clear(_unsure);
        _misses.Clear();
        _done = 0;
        _stood = 0;
        _ms = 0;
        _version = snap.Version;
        _random = new System.Random(1102026 + snap.Version);

        n = Math.Clamp(n, 1, 5000);

        // Half the tiles on the open side of a border, half on the other: both mistakes matter.
        for (var tries = 0; _queue.Count < n && tries < n * 20; tries++)
        {
            var c = snap.Mixed[_random.Next(snap.Mixed.Length)];

            if (Border(snap, c, _queue.Count % 2 == 0, out var at, out var k))
            {
                _queue.Enqueue(new Sample { At = at, Class = k });
            }
        }

        _asked = _queue.Count;
        _state = $"running for {who}";
        Publish();

        _timer?.Stop();
        _timer = new CheckTimer(TimeSpan.FromMilliseconds(Math.Max(50, TickMs)));
        _timer.Start();

        logger.Information("Reach check: {Asked} border tiles of map v{Version} to examine, for {Who}", _asked, snap.Version, who);

        return $"examining {_asked} border tiles of map v{snap.Version}; ask \"reach check\" again for the tally.";
    }

    public static void Forget()
    {
        _timer?.Stop();
        _timer = null;
        _queue.Clear();
        _ranOnce = false;
        _state = "not run";
    }

    /// <summary>A tile on a border in cluster <paramref name="c"/>: on its open side, or on the other.</summary>
    private static bool Border(BotReachSnapshot snap, int c, bool openSide, out Point3D at, out BotReachClass k)
    {
        at = Point3D.Zero;
        k = BotReachClass.Unknown;

        var cells = snap.Classes[c];
        var x0 = c % snap.ClustersX * 16;
        var y0 = c / snap.ClustersX * 16;
        var start = _random.Next(256);

        for (var j = 0; j < 256; j++)
        {
            var i = (start + j) & 255;

            if (cells[i] != (byte)BotReachClass.Open)
            {
                continue;
            }

            var x = x0 + (i & 15);
            var y = y0 + (i >> 4);
            var d = _random.Next(4);

            for (var t = 0; t < 4; t++)
            {
                var (dx, dy) = ((d + t) & 3) switch
                {
                    0 => (1, 0),
                    1 => (0, 1),
                    2 => (-1, 0),
                    _ => (0, -1)
                };

                var other = snap.ClassAt(x + dx, y + dy);

                if (other is BotReachClass.Open or BotReachClass.Unknown)
                {
                    continue;
                }

                if (openSide)
                {
                    at = new Point3D(x, y, 0);
                    k = BotReachClass.Open;
                }
                else
                {
                    at = new Point3D(x + dx, y + dy, 0);
                    k = other;
                }

                return true;
            }
        }

        return false;
    }

    private static void Tick()
    {
        var snap = BotReachMap.Snapshot;

        if (snap == null || _queue.Count == 0)
        {
            Finish(snap == null ? "stopped: no map" : "done");

            return;
        }

        var map = snap.Map;
        var started = Stopwatch.GetTimestamp();
        var deadline = started + (long)(BudgetMs * Stopwatch.Frequency / 1000.0);

        while (_queue.Count > 0 && Stopwatch.GetTimestamp() < deadline)
        {
            var sample = _queue.Dequeue();

            Examine(snap, map, sample);
            _done++;
        }

        _ms += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        Publish();

        if (_queue.Count == 0)
        {
            Finish("done");
        }
    }

    private static void Finish(string how)
    {
        _timer?.Stop();
        _timer = null;
        _state = how;
        Publish();

        logger.Information("Reach check: {What}", Describe());

        for (var i = 0; i < _misses.Count && i < 12; i++)
        {
            logger.Information("Reach check: {Miss}", _misses[i]);
        }
    }

    private static void Publish() =>
        _report = new BotReachCheckReport(
            _version,
            _asked,
            _done,
            (int[])_agree.Clone(),
            (int[])_disagree.Clone(),
            (int[])_unsure.Clone(),
            _misses.ToArray(),
            _state,
            _ms,
            _stood
        );

    private static void Examine(BotReachSnapshot snap, Map map, Sample sample)
    {
        var at = sample.At;
        var k = (int)sample.Class;

        // A walker the zones tour does not hold: the tour takes the first of the squad (BotZoneTour.MostWalkers), so the last
        // one past them is free; with the tour off, any.
        if (Carry && Move != null && Walkers?.Invoke() is { } walkers
            && walkers.Count > (BotZoneTour.Running ? Math.Max(0, BotZoneTour.MostWalkers) : 0))
        {
            var (_, body) = walkers[walkers.Count - 1];

            if (body != null && Move(body, map, at))
            {
                _stood++;
            }
        }

        string verdict;
        bool? agrees;

        BotReachMap.Bypass = true;

        try
        {
            (agrees, verdict) = sample.Class switch
            {
                BotReachClass.Open => AskOpen(snap, map, at),
                BotReachClass.Isle or BotReachClass.Pocket => AskShut(snap, map, at),
                _ => AskGround(snap, map, at, sample.Class)
            };
        }
        finally
        {
            BotReachMap.Bypass = false;
        }

        if (agrees == true)
        {
            _agree[k]++;
        }
        else if (agrees == false)
        {
            _disagree[k]++;

            if (_misses.Count < 40)
            {
                _misses.Add($"({at.X}, {at.Y}) the map says {sample.Class}, {verdict}");
            }
        }
        else
        {
            _unsure[k]++;
        }
    }

    /// <summary>
    /// One tile examined now, by the door: the walker's own answer set beside the map's. Not counted in the check's tally.
    /// </summary>
    public static string Probe(int x, int y)
    {
        var snap = BotReachMap.Snapshot;

        if (snap == null || !snap.Inside(x, y))
        {
            return "The walker was not asked: there is no map there.";
        }

        var k = snap.ClassAt(x, y);
        var at = new Point3D(x, y, 0);
        string verdict;
        bool? agrees;

        BotReachMap.Bypass = true;

        try
        {
            (agrees, verdict) = k switch
            {
                BotReachClass.Open => AskOpen(snap, snap.Map, at),
                BotReachClass.Isle or BotReachClass.Pocket => AskShut(snap, snap.Map, at),
                _ => AskGround(snap, snap.Map, at, k)
            };
        }
        finally
        {
            BotReachMap.Bypass = false;
        }

        return $"The walker's own means: the map says {k} {verdict} — {(agrees == true ? "they agree" : agrees == false ? "they DISAGREE" : "they cannot tell")}.";
    }

    /// <summary>Open ground: the walker's search from open ground a few tiles off, onto the tile itself.</summary>
    private static (bool?, string) AskOpen(BotReachSnapshot snap, Map map, Point3D at)
    {
        if (!Stand(snap, map, at.X, at.Y, BotReachClass.Open, out var z))
        {
            return (false, "and the walker's footing test finds nowhere to stand on it");
        }

        var goal = new Point3D(at.X, at.Y, z);

        if (!Anchor(snap, map, at, out var from))
        {
            return (null, "with no open ground near it to walk from");
        }

        var outcome = BotPath.Find(map, from, goal, BotArrival.Exactly, _path, default, CeilingMs);

        return outcome switch
        {
            BotPathOutcome.Reached => (true, "and the walker got onto it"),
            BotPathOutcome.Sealed => (false, $"and the walker's search from {from} proved there is no way onto it"),
            _ => Flood(map, goal, out var cells) && cells > 0 ? (false, $"and a flood of the walker's steps from it closes after {cells} cells") : (null, "and the walker's search ran out of clock")
        };
    }

    /// <summary>Ground no walk reaches: the walker's search from open ground beside it, then a flood of the walker's steps from the tile.</summary>
    private static (bool?, string) AskShut(BotReachSnapshot snap, Map map, Point3D at)
    {
        if (!Stand(snap, map, at.X, at.Y, snap.ClassAt(at.X, at.Y), out var z))
        {
            return (false, "but the walker's footing test finds nowhere to stand on it");
        }

        var goal = new Point3D(at.X, at.Y, z);

        if (Anchor(snap, map, at, out var from))
        {
            var outcome = BotPath.Find(map, from, goal, BotArrival.Exactly, _path, default, CeilingMs);

            if (outcome == BotPathOutcome.Reached)
            {
                return (false, $"but the walker walked onto it from open ground at {from}");
            }

            if (outcome == BotPathOutcome.Sealed)
            {
                return (true, "and the walker's search proved there is no way onto it");
            }
        }

        if (Flood(map, goal, out var cells))
        {
            return (true, $"and a flood of the walker's steps from it closes after {cells} cells");
        }

        // Ground too wide to walk to its edges: an island, a dungeon no gate opens. The navigation graph is a second opinion
        // drawn another way (gates on the cluster edges, lands joined over the teleporters); its "no land of home's" is
        // counted as agreement, its "joined" left unsure — it joins a ledge to the ground below it, the map does not.
        if (BotGates.Ready && !BotGates.Joined(map, BotPopulation.Where, goal))
        {
            return (true, "and the navigation graph finds no land joined to home's there (the walker's steps from it go on past the flood's count)");
        }

        return (null, "and the walker's steps from it go on past the flood's count");
    }

    /// <summary>Nowhere to stand: can a walker on open ground beside it step onto it?</summary>
    private static (bool?, string) AskGround(BotReachSnapshot snap, Map map, Point3D at, BotReachClass k)
    {
        var wet = BotStep.Wet(map, at.X, at.Y);

        for (var d = 0; d < 8; d++)
        {
            var nx = at.X - Dx(d);
            var ny = at.Y - Dy(d);

            if (snap.ClassAt(nx, ny) != BotReachClass.Open || !Stand(snap, map, nx, ny, BotReachClass.Open, out var nz))
            {
                continue;
            }

            var mask = BotStep.Mask(map, nx, ny, nz);

            if ((mask.WalkMask & (1 << d)) == 0)
            {
                continue;
            }

            if ((d & 1) != 0 && ((mask.WalkMask & (1 << ((d + 7) & 7))) == 0 || (mask.WalkMask & (1 << ((d + 1) & 7))) == 0
                                 || BotStep.FlankBlocked(map, nx, ny, (d + 7) & 7, mask) || BotStep.FlankBlocked(map, nx, ny, (d + 1) & 7, mask)))
            {
                continue;
            }

            var landZ = mask.GetWalkZ((Direction)d);

            if (!BotStep.BlockedByItems(map, at.X, at.Y, landZ))
            {
                return (false, $"but a walker at ({nx}, {ny}, {nz}) steps onto it at z {landZ}");
            }
        }

        // Nobody steps onto it. Whether it is wet is the other half of the class, and counted as a miss of its own kind.
        if (k == BotReachClass.Water && !wet || k == BotReachClass.Blocked && wet)
        {
            return (false, $"and nobody can step onto it, but it is {(wet ? "water" : "dry")}");
        }

        return (true, "and nobody standing beside it can step onto it");
    }

    private static int Dx(int d) => d switch { 1 or 2 or 3 => 1, 5 or 6 or 7 => -1, _ => 0 };

    private static int Dy(int d) => d switch { 7 or 0 or 1 => -1, 3 or 4 or 5 => 1, _ => 0 };

    /// <summary>
    /// A height a body fits at on a tile, by the walker's footing test, of the standing place the map calls
    /// <paramref name="kind"/>: a tile's floors may differ — a room under a roof, a road under a bridge — and the map's word on
    /// the tile is its best one, so the one examined is the one the word is about.
    /// </summary>
    private static bool Stand(BotReachSnapshot snap, Map map, int x, int y, BotReachClass kind, out sbyte z)
    {
        z = 0;

        if (!BotStep.OnMap(map, x, y))
        {
            return false;
        }

        Span<sbyte> zs = stackalloc sbyte[16];

        var housed = map.GetRealSector(x >> Map.SectorShift, y >> Map.SectorShift).HasMultis;
        var n = housed ? StepProbe.ComputeStandableSurfaceZsWithMultis(map, x, y, zs) : StepProbe.ComputeStandableSurfaceZs(map, x, y, zs);
        var found = false;

        for (var i = 0; i < n; i++)
        {
            if (BotStep.BlockedByItems(map, x, y, zs[i]))
            {
                continue;
            }

            if (snap.ClassAt(x, y, zs[i]) == kind)
            {
                z = zs[i];

                return true;
            }

            if (!found)
            {
                z = zs[i];
                found = true;
            }
        }

        return found;
    }

    /// <summary>Open ground between three and <see cref="AnchorReach"/> tiles from the tile, that a body fits on.</summary>
    private static bool Anchor(BotReachSnapshot snap, Map map, Point3D at, out Point3D from)
    {
        for (var r = 3; r <= AnchorReach; r++)
        {
            var d0 = _random.Next(8 * r);

            for (var j = 0; j < 8 * r; j++)
            {
                var i = (d0 + j) % (8 * r);
                int dx, dy;

                // The ring of radius r, walked from a random place on it.
                if (i < 2 * r)
                {
                    dx = -r + i;
                    dy = -r;
                }
                else if (i < 4 * r)
                {
                    dx = r;
                    dy = -r + (i - 2 * r);
                }
                else if (i < 6 * r)
                {
                    dx = r - (i - 4 * r);
                    dy = r;
                }
                else
                {
                    dx = -r;
                    dy = r - (i - 6 * r);
                }

                var x = at.X + dx;
                var y = at.Y + dy;

                if (snap.ClassAt(x, y) == BotReachClass.Open && Stand(snap, map, x, y, BotReachClass.Open, out var z))
                {
                    from = new Point3D(x, y, z);

                    return true;
                }
            }
        }

        from = Point3D.Zero;

        return false;
    }

    private static readonly HashSet<int> _seen = [];

    private static readonly Queue<Point3D> _frontier = new();

    /// <summary>
    /// A flood of the walker's own steps from a tile: true when it closes within <see cref="FloodCells"/> — the tile's ground
    /// is walked to its edges — with the cells it counted.
    /// </summary>
    private static bool Flood(Map map, Point3D start, out int cells)
    {
        _seen.Clear();
        _frontier.Clear();
        _seen.Add(BotStep.Cell(start.X, start.Y, (sbyte)start.Z));
        _frontier.Enqueue(start);

        var began = Stopwatch.GetTimestamp();
        var deadline = began + (long)(CeilingMs * Stopwatch.Frequency / 1000.0);

        while (_frontier.Count > 0)
        {
            if (_seen.Count > FloodCells || (_seen.Count & 63) == 0 && Stopwatch.GetTimestamp() >= deadline)
            {
                cells = 0;

                return false;
            }

            var p = _frontier.Dequeue();
            var mask = BotStep.Mask(map, p.X, p.Y, (sbyte)p.Z);

            for (var d = 0; d < 8; d++)
            {
                if ((mask.WalkMask & (1 << d)) == 0)
                {
                    continue;
                }

                if ((d & 1) != 0 && ((mask.WalkMask & (1 << ((d + 7) & 7))) == 0 || (mask.WalkMask & (1 << ((d + 1) & 7))) == 0
                                     || BotStep.FlankBlocked(map, p.X, p.Y, (d + 7) & 7, mask) || BotStep.FlankBlocked(map, p.X, p.Y, (d + 1) & 7, mask)))
                {
                    continue;
                }

                var nx = p.X + Dx(d);
                var ny = p.Y + Dy(d);
                var nz = mask.GetWalkZ((Direction)d);

                if (!BotStep.OnMap(map, nx, ny) || BotStep.BlockedByItems(map, nx, ny, nz))
                {
                    continue;
                }

                if (_seen.Add(BotStep.Cell(nx, ny, nz)))
                {
                    _frontier.Enqueue(new Point3D(nx, ny, nz));
                }
            }
        }

        cells = _seen.Count;

        return true;
    }

    public static string Describe()
    {
        var r = _report;
        var agree = 0;
        var disagree = 0;
        var unsure = 0;

        for (var k = 0; k < 6; k++)
        {
            agree += r.Agree[k];
            disagree += r.Disagree[k];
            unsure += r.Unsure[k];
        }

        if (r.Asked == 0)
        {
            return $"the reach check has not run ({r.State})";
        }

        var byClass = new List<string>();

        for (var k = 1; k < 6; k++)
        {
            var seen = r.Agree[k] + r.Disagree[k] + r.Unsure[k];

            if (seen > 0)
            {
                byClass.Add($"{(BotReachClass)k} {r.Agree[k]}/{r.Agree[k] + r.Disagree[k]}{(r.Unsure[k] > 0 ? $" (+{r.Unsure[k]} unsure)" : "")}");
            }
        }

        var accuracy = agree + disagree > 0 ? 100.0 * agree / (agree + disagree) : 0.0;

        return $"the reach check of map v{r.Version} ({r.State}): {r.Done} of {r.Asked} border tiles examined in {r.Ms:F0}ms of the loop, "
            + $"{agree} agree and {disagree} do not — {accuracy:F1}% where the walker's means could answer, {unsure} they could not; by class {string.Join(", ", byClass)}; "
            + $"a debugger stood on {r.Stood} of them";
    }

    private sealed class CheckTimer : Timer
    {
        public CheckTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                BotReachMap.Bypass = false;
                logger.Error(e, "Reach check: a beat threw; the check stops");
                Finish("stopped by a fault");
            }
        }
    }
}
