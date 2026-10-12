using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Server.Engines.Pathing.Tiered;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Where a walker from home can get to, tile by tile: the reach map.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, night, point 4:</b> "build unreachability zones as precise as possible — no path may be
/// formed into them, one tile of precision; Argus and the debuggers help; I want to see it on the web dashboard. Water is
/// unreachable ground too, but it can be sailed: in a boat the water's unreachability is lifted, and the land stays
/// unreachable." Every tile of the population's map is one of <see cref="BotReachClass"/>: open (a walk from home reaches it,
/// over the ground and through the gates), isle (standing ground no walk reaches, with a shore a ship could land on), pocket
/// (standing ground nothing reaches), water (a ship's, never a walker's), blocked (nowhere to stand).
/// </para>
///
/// <para>
/// <b>How it is drawn.</b> A cluster of 16 × 16 tiles at a time on the game loop, never more than <see cref="SliceMs"/> a
/// tick (the tiles are game state: CLAUDE.md rule 3), through the navigation graph's own window and step rule
/// (<see cref="BotReachBuilder"/>): each tile's standing places, the regions they make inside the cluster, and every step out
/// of it. Then the whole map is solved at once — the regions joined, the gates added, a search from home along every step
/// the way it can be taken (<see cref="BotReachSolve"/>) — on a worker when the machine has the cores, since it reads nothing
/// but the clusters. Kept on disk (<see cref="BotReachFile"/>), so a boot draws only what a house or an item changed.
/// </para>
///
/// <para>
/// <b>What it refuses.</b> A walker standing on open ground is refused any place with no open tile within its reach
/// (<see cref="Refuses"/>): that place is not reachable from home, and nothing reachable from home reaches it. Asked by the
/// reach ledger (<see cref="BotReach.Ask"/>, which every chooser and the walker's own search consult), the population's
/// sampler (<see cref="BotPopulation.Reachable"/>) and the route planner (<see cref="BotJourney"/>). A walker aboard a ship
/// is refused the land: only water is somewhere a ship goes. A walker standing anywhere else — a pocket, an island no road
/// reaches — gets no answer from the map, and the old questions stand.
/// </para>
/// </summary>
public static class BotReachMap
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotReachMap));

    /// <summary>Whether the reach map is drawn and asked at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The most one tick of drawing may spend on the game loop, in milliseconds.</summary>
    public static double SliceMs { get; set; } = 6.0;

    /// <summary>How often a tick runs, in milliseconds.</summary>
    public static int EveryMs { get; set; } = 50;

    /// <summary>Whether the clusters are kept on disk between boots.</summary>
    public static bool Persist { get; set; } = true;

    /// <summary>Cores the machine needs for the solve to run off the loop (CLAUDE.md rule 10); fewer, and it runs inline.</summary>
    public static int OffLoopCores { get; set; } = 3;

    /// <summary>How near a sampled place an open tile must be for a walker to be sent there. Two: the planner's own anchorage.</summary>
    public static int SampleReach { get; set; } = 2;

    /// <summary>How often an idle map looks at whether the gates or home have changed since the last solve.</summary>
    public static int IdleCheckMs { get; set; } = 10000;

    /// <summary>How often the dashboard's summary is written, in milliseconds.</summary>
    public static int JsonEveryMs { get; set; } = 2000;

    /// <summary>Steps onto ground the map calls unreachable, in one cluster, after which the cluster is drawn again.</summary>
    public static int RedrawAfter { get; set; } = 3;

    /// <summary>How many of those steps are said in the log a session, with their tiles.</summary>
    public static int SayContradictions { get; set; } = 25;

    private static volatile BotReachSnapshot _snapshot;

    /// <summary>The map as last solved; null until the first solve. Immutable, safe from any thread.</summary>
    public static BotReachSnapshot Snapshot => _snapshot;

    public static bool Ready => _snapshot != null;

    /// <summary>
    /// Set while the check's oracles ask the walker's own search, so the map does not answer its own examination
    /// (<see cref="BotReachCheck"/>). The loop is single-threaded; a flag is enough.
    /// </summary>
    public static bool Bypass { get; set; }

    private enum Stage
    {
        Off,
        Loading,
        Signing,
        Drawing,
        Idle
    }

    private static Stage _stage;

    private static Map _map;

    private static BotReachBuilder _builder;

    private static BotReachCluster[] _clusters;

    private static int _next = -1;

    private static int _signNext = -1;

    private static readonly SortedSet<int> _dirty = [];

    private static bool _changed;

    private static bool _unsaved;

    private static bool _solveAgain;

    private static volatile bool _solving;

    private static volatile string _solveError;

    private static int _version;

    private static int _seenVersion;

    private static int _gatesSolved = -1;

    private static Point3D _homeSolved;

    private static long _idleChecked;

    private static long _jsonAt;

    private static long _began;

    private static Timer _timer;

    private static bool _hooked;

    private static volatile bool _loadDone;

    private static volatile BotReachCluster[] _loaded;

    private static volatile string _loadWhy;

    private static volatile bool _writing;

    private static readonly Dictionary<int, int> _contradicted = [];

    private static int _docksAudited;

    public static Map Map => _map;

    /// <summary>Clusters on the map, and clusters drawn.</summary>
    public static int Clusters => _clusters?.Length ?? 0;

    public static int ClustersDrawn { get; private set; }

    /// <summary>Clusters drawn from the ground, drawn again for a change, and checked against the houses and items at a boot.</summary>
    public static long Drawn { get; private set; }

    public static long Redrawn { get; private set; }

    public static long Signed { get; private set; }

    public static long SignChanged { get; private set; }

    /// <summary>The loop's milliseconds spent drawing, checking signatures and handing the solve its input; the worst tick.</summary>
    public static double DrawMs { get; private set; }

    public static double SignMs { get; private set; }

    public static double HandMs { get; private set; }

    public static double WorstTickMs { get; private set; }

    public static long Ticks { get; private set; }

    /// <summary>Solves run, their milliseconds (off the loop unless <see cref="LastSolveInline"/>), the last one's.</summary>
    public static int Solves { get; private set; }

    public static double SolveMs { get; private set; }

    public static double LastSolveMs { get; private set; }

    public static bool LastSolveInline { get; private set; }

    /// <summary>The file: what became of reading it, its size, and times written.</summary>
    public static string FileState { get; private set; } = "not read";

    public static long FileBytes { get; private set; }

    public static int FileWrites { get; private set; }

    /// <summary>Questions put to the map by the walkers, and what it refused.</summary>
    public static long Asked { get; private set; }

    public static long Refused { get; private set; }

    public static long RefusedAboard { get; private set; }

    /// <summary>Places refused to the population's samplers, and routes the planner was not asked for.</summary>
    public static long SamplerRefused { get; private set; }

    public static long PlannerRefused { get; private set; }

    /// <summary>Steps the walkers took that the map was asked about; steps from open ground onto ground it calls anything else.</summary>
    public static long StepsSeen { get; private set; }

    public static long Contradicted { get; private set; }

    /// <summary>Steps taken on ground no walk from home reaches (bots on islands and in pockets), and clusters drawn again for contradictions.</summary>
    public static long StepsAway { get; private set; }

    public static long RedrawnForSteps { get; private set; }

    public static void Start(Map map)
    {
        if (!Running || map == null || map == Map.Internal)
        {
            _stage = Stage.Off;

            return;
        }

        _map = map;
        _builder = new BotReachBuilder(map);
        _clusters = new BotReachCluster[_builder.ClustersX * _builder.ClustersY];
        _dirty.Clear();
        _changed = false;
        ClustersDrawn = 0;
        _began = Core.TickCount;

        // Seeded from a real tick (CLAUDE.md rule 20): the clocks below are compared by subtraction only.
        _jsonAt = _began - JsonEveryMs;
        _idleChecked = _began;

        if (!_hooked)
        {
            _hooked = true;
            NavigationService.GroundChangedHook += OnGroundChanged;
        }

        if (Persist)
        {
            _stage = Stage.Loading;
            _loadDone = false;

            var path = BotReachFile.PathFor(map);
            var id = map.MapID;
            var fingerprint = map.Tiles.MapFilesFingerprint;
            var width = map.Width;
            var height = map.Height;

            Task.Run(
                () =>
                {
                    try
                    {
                        _loaded = BotReachFile.Read(path, id, fingerprint, width, height, out var why);
                        _loadWhy = why;
                    }
                    catch (Exception e)
                    {
                        _loadWhy = e.Message;
                    }

                    _loadDone = true;
                }
            );
        }
        else
        {
            Survey();
        }

        _timer?.Stop();
        _timer = new TickTimer(TimeSpan.FromMilliseconds(Math.Max(10, EveryMs)));
        _timer.Start();

        logger.Information(
            "Reach: the map of {Map} is {How}: {Clusters} clusters of 16 tiles, drawn {Slice:F1}ms a tick every {Every}ms, solved {Where}",
            map.Name,
            Persist ? "read from disk, and drawn where the houses and items have changed" : "drawn from the ground",
            _clusters.Length,
            SliceMs,
            EveryMs,
            Environment.ProcessorCount >= OffLoopCores ? $"off the loop ({Environment.ProcessorCount} cores)" : $"on the loop ({Environment.ProcessorCount} cores)"
        );
    }

    /// <summary>Everything dropped, for a reload of the world.</summary>
    public static void Forget()
    {
        _timer?.Stop();
        _timer = null;
        _stage = Stage.Off;
        _snapshot = null;
        _clusters = null;
        _builder = null;
        _dirty.Clear();
        _contradicted.Clear();
        _map = null;
        Asked = Refused = RefusedAboard = SamplerRefused = PlannerRefused = 0;
        StepsSeen = Contradicted = StepsAway = RedrawnForSteps = 0;
        Drawn = Redrawn = Signed = SignChanged = 0;
        DrawMs = SignMs = HandMs = WorstTickMs = 0;
        Ticks = 0;
        BotReachCheck.Forget();
    }

    private static void Survey()
    {
        _stage = Stage.Drawing;
        _next = _clusters.Length - 1;
        FileState = FileState == "not read" ? "drawing from the ground" : FileState;
    }

    private static void Tick()
    {
        var started = Stopwatch.GetTimestamp();
        var deadline = started + (long)(SliceMs * Stopwatch.Frequency / 1000.0);

        Ticks++;

        if (Core.TickCount - _jsonAt >= JsonEveryMs)
        {
            _jsonAt = Core.TickCount;
            BotReachWeb.Build();
        }

        // A solve finished on the worker: said once, and the check told.
        if (_snapshot is { } snap && snap.Version != _seenVersion)
        {
            _seenVersion = snap.Version;
            Solved(snap);
        }

        if (_solveError != null)
        {
            logger.Warning("Reach: the solve failed ({Error}); the last map stands", _solveError);
            _solveError = null;
        }

        switch (_stage)
        {
            case Stage.Loading:
                {
                    if (!_loadDone)
                    {
                        return;
                    }

                    if (_loaded is { } loaded && loaded.Length == _clusters.Length)
                    {
                        _clusters = loaded;
                        _loaded = null;
                        _stage = Stage.Signing;
                        _signNext = _clusters.Length - 1;
                        FileState = "read from disk";

                        var drawn = 0;

                        for (var c = 0; c < _clusters.Length; c++)
                        {
                            drawn += _clusters[c] != null ? 1 : 0;
                        }

                        ClustersDrawn = drawn;

                        logger.Information("Reach: {Drawn} clusters read from {Path}; checking each against the houses and items standing now", drawn, BotReachFile.PathFor(_map));
                    }
                    else
                    {
                        FileState = $"drawn afresh ({_loadWhy ?? "the file did not fit this map"})";
                        _loaded = null;

                        logger.Information("Reach: nothing usable on disk ({Why}); drawing the whole map from the ground", _loadWhy ?? "the file did not fit this map");

                        Survey();
                    }

                    return;
                }
            case Stage.Signing:
                {
                    var work = 0;

                    while (_signNext >= 0)
                    {
                        var c = _signNext--;
                        var cluster = _clusters[c];

                        // Water and rock hold nothing an item or a house could take a step away from.
                        if (cluster == null)
                        {
                            MarkDirty(c);
                        }
                        else if (cluster.Regions > 0 || cluster.Housed)
                        {
                            Signed++;

                            if (_builder.Signature(c) != cluster.Signature)
                            {
                                SignChanged++;
                                MarkDirty(c);
                            }
                        }

                        if ((++work & 15) == 0 && Stopwatch.GetTimestamp() >= deadline)
                        {
                            break;
                        }
                    }

                    SignMs += Spent(started);

                    if (_signNext < 0)
                    {
                        logger.Information(
                            "Reach: {Signed} clusters checked against the houses and items in {Ms:F0}ms of the loop; {Changed} have changed and are drawn again",
                            Signed,
                            SignMs,
                            SignChanged
                        );

                        _stage = Stage.Drawing;

                        // Nothing to draw: the map as read is the map.
                        if (_dirty.Count == 0)
                        {
                            _changed = true;
                        }
                    }

                    return;
                }
            case Stage.Drawing:
                {
                    var work = 0;

                    while (Stopwatch.GetTimestamp() < deadline || work == 0)
                    {
                        int c;

                        if (_next >= 0)
                        {
                            c = _next--;
                        }
                        else if (_dirty.Count > 0)
                        {
                            c = _dirty.Max;
                            _dirty.Remove(c);
                            Redrawn++;
                        }
                        else
                        {
                            break;
                        }

                        if (_clusters[c] == null)
                        {
                            ClustersDrawn++;
                        }

                        _clusters[c] = _builder.Build(c, _clusters);
                        _changed = true;
                        _unsaved = true;
                        Drawn++;
                        work++;
                    }

                    var ms = Spent(started);

                    DrawMs += ms;
                    WorstTickMs = Math.Max(WorstTickMs, ms);

                    if (_next >= 0 || _dirty.Count > 0)
                    {
                        return;
                    }

                    _stage = Stage.Idle;

                    logger.Information(
                        "Reach: {Drawn} clusters drawn ({Redrawn} of them again), {Probes} cells probed, {Items} items read, in {Ms:F0}ms of the loop over {Seconds:F0}s (worst tick {Worst:F1}ms)",
                        Drawn,
                        Redrawn,
                        _builder.Probes,
                        _builder.Terrain.ItemsRead,
                        DrawMs,
                        (Core.TickCount - _began) / 1000.0,
                        WorstTickMs
                    );

                    if (_changed)
                    {
                        Solve();
                    }

                    if (_unsaved)
                    {
                        Keep();
                    }

                    return;
                }
            case Stage.Idle:
                {
                    if (_dirty.Count > 0)
                    {
                        _stage = Stage.Drawing;

                        return;
                    }

                    if (_unsaved && !_writing && !World.Saving)
                    {
                        Keep();
                    }

                    if (_solveAgain && !_solving)
                    {
                        _solveAgain = false;
                        Solve();

                        return;
                    }

                    var now = Core.TickCount;

                    if (now - _idleChecked < IdleCheckMs)
                    {
                        return;
                    }

                    _idleChecked = now;

                    if (_snapshot is { } audited && audited.Version != _docksAudited && BotDocks.Ready)
                    {
                        _docksAudited = audited.Version;
                        AuditDocks(audited);
                    }

                    // The gates are read from the world a little after the boot; home can move. Either is a new solve.
                    if (!_solving && (GateCount() != _gatesSolved || BotPopulation.Where != _homeSolved))
                    {
                        Solve();
                    }

                    return;
                }
        }
    }

    private static double Spent(long started) => (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

    private static int GateCount() => BotGates.Surveyed ? BotGates.All.Count : 0;

    /// <summary>
    /// Hands the clusters, the gates and home to a solve: on a worker when the machine has the cores, inline otherwise. The
    /// clusters array is copied (the references only: the clusters themselves never change).
    /// </summary>
    private static void Solve()
    {
        if (_clusters == null || _map == null)
        {
            return;
        }

        if (_solving)
        {
            _solveAgain = true;

            return;
        }

        var started = Stopwatch.GetTimestamp();

        // The gates are the world's teleporters and moongates: asked for here, once they are read (BotGates surveys on its
        // first question).
        BotGates.Ensure(_map);

        var gates = new List<BotReachGate>();

        if (BotGates.Surveyed)
        {
            var all = BotGates.All;

            for (var i = 0; i < all.Count; i++)
            {
                if (all[i].From != Point3D.Zero && all[i].To != Point3D.Zero)
                {
                    gates.Add(new BotReachGate(all[i].From, all[i].To));
                }
            }
        }

        var input = new BotReachSolveInput
        {
            Map = _map,
            MapName = _map.Name,
            Width = _map.Width,
            Height = _map.Height,
            ClustersX = _builder.ClustersX,
            ClustersY = _builder.ClustersY,
            Clusters = (BotReachCluster[])_clusters.Clone(),
            Gates = gates,
            Home = BotPopulation.Where,
            Version = ++_version
        };

        _gatesSolved = GateCount();
        _homeSolved = input.Home;
        _changed = false;
        _solving = true;
        HandMs += Spent(started);

        if (Environment.ProcessorCount >= OffLoopCores)
        {
            LastSolveInline = false;

            Task.Run(
                () =>
                {
                    try
                    {
                        _snapshot = BotReachSolve.Run(input);
                    }
                    catch (Exception e)
                    {
                        _solveError = e.Message;
                    }
                    finally
                    {
                        _solving = false;
                    }
                }
            );

            return;
        }

        LastSolveInline = true;

        try
        {
            _snapshot = BotReachSolve.Run(input);
        }
        catch (Exception e)
        {
            _solveError = e.Message;
        }
        finally
        {
            _solving = false;
        }
    }

    /// <summary>A solve has landed: counted, said, and the check started the first time.</summary>
    private static void Solved(BotReachSnapshot snap)
    {
        Solves++;
        SolveMs += snap.SolveMs;
        LastSolveMs = snap.SolveMs;

        logger.Information(
            "Reach: solved v{Version} in {Ms:F0}ms {Where}: {Regions} regions, {Edges} one-way steps and gates; {Open} tiles open, {Isle} isle, {Pocket} pocket, {Water} water, {Blocked} blocked{Unknown}; {Lands} lands no walk from home reaches, the largest {Largest}; {Gates} gates, home {Home}{Homed}",
            snap.Version,
            snap.SolveMs,
            LastSolveInline ? "on the loop" : "off the loop",
            snap.Regions,
            snap.Edges,
            snap.Tiles[(int)BotReachClass.Open],
            snap.Tiles[(int)BotReachClass.Isle],
            snap.Tiles[(int)BotReachClass.Pocket],
            snap.Tiles[(int)BotReachClass.Water],
            snap.Tiles[(int)BotReachClass.Blocked],
            snap.Tiles[0] > 0 ? $", {snap.Tiles[0]} not drawn" : "",
            snap.Lands,
            snap.Largest.Length > 0 ? $"{snap.Largest[0].Tiles} tiles at ({snap.Largest[0].X}, {snap.Largest[0].Y})" : "none",
            snap.Gates,
            snap.Home,
            snap.Homed ? "" : " (home stands on no ground the map knows: nothing is open)"
        );

        BotReachCheck.Solved(snap);
    }

    /// <summary>Berths the map calls anything but water, and shores with no ground a walk reaches within two tiles, at the last audit.</summary>
    public static int BerthsAground { get; private set; }

    public static int ShoresUnreached { get; private set; }

    /// <summary>
    /// The docks set against the map (01.10.2026): a ship's berth must be water — the one ground a ship may be on — and the shore
    /// a walker boards from and lands on must be ground a walk reaches (or, at an island's dock, the isle). A dock that fails is
    /// said by name; nothing is changed.
    /// </summary>
    private static void AuditDocks(BotReachSnapshot snap)
    {
        var aground = 0;
        var unreached = 0;
        var said = new List<string>();

        foreach (var dock in BotDocks.All)
        {
            foreach (var berth in dock.Berths)
            {
                if (snap.ClassAt(berth.Berth.X, berth.Berth.Y) != BotReachClass.Water)
                {
                    aground++;
                    said.Add($"{dock.Name}'s berth at ({berth.Berth.X}, {berth.Berth.Y}) is {snap.ClassAt(berth.Berth.X, berth.Berth.Y)}");
                }

                if (!snap.Any(berth.Shore.X, berth.Shore.Y, 2, BotReachClass.Open) && !snap.Any(berth.Shore.X, berth.Shore.Y, 2, BotReachClass.Isle))
                {
                    unreached++;
                    said.Add($"{dock.Name}'s shore at ({berth.Shore.X}, {berth.Shore.Y}) has no ground a walk reaches within 2");
                }
            }
        }

        BerthsAground = aground;
        ShoresUnreached = unreached;

        logger.Information(
            "Reach: {Docks} docks set against map v{Version}: {Aground} berths on ground it calls land, {Unreached} shores with no ground a walk reaches{Said}",
            BotDocks.All.Count,
            snap.Version,
            aground,
            unreached,
            said.Count > 0 ? ": " + string.Join("; ", said) : ""
        );
    }

    /// <summary>The clusters written to disk on a worker, when they have changed and the world is not being saved.</summary>
    private static void Keep()
    {
        if (!Persist || _writing || World.Saving || _clusters == null)
        {
            return;
        }

        _writing = true;
        _unsaved = false;

        var path = BotReachFile.PathFor(_map);
        var id = _map.MapID;
        var fingerprint = _map.Tiles.MapFilesFingerprint;
        var width = _map.Width;
        var height = _map.Height;
        var cx = _builder.ClustersX;
        var cy = _builder.ClustersY;
        var clusters = (BotReachCluster[])_clusters.Clone();

        Task.Run(
            () =>
            {
                try
                {
                    var watch = Stopwatch.StartNew();

                    FileBytes = BotReachFile.Write(path, id, fingerprint, width, height, cx, cy, clusters);
                    FileWrites++;
                    logger.Information("Reach: the map's clusters written to {Path}, {Bytes} bytes in {Ms}ms", path, FileBytes, watch.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    logger.Warning("Reach: the map's clusters could not be written ({Error})", e.Message);
                }

                _writing = false;
            }
        );
    }

    /// <summary>A cluster to draw again, with the four whose links point into it (west, north, north-west, north-east).</summary>
    private static void MarkDirty(int c)
    {
        if (_builder == null || c < 0 || c >= _clusters.Length)
        {
            return;
        }

        var cx = c % _builder.ClustersX;
        var cy = c / _builder.ClustersX;

        _dirty.Add(c);

        for (var dy = -1; dy <= 0; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dy == 0 && dx >= 0)
                {
                    continue;
                }

                var x = cx + dx;
                var y = cy + dy;

                if (x >= 0 && y >= 0 && x < _builder.ClustersX && y < _builder.ClustersY)
                {
                    _dirty.Add(y * _builder.ClustersX + x);
                }
            }
        }
    }

    /// <summary>A house raised, moved or taken down: its clusters and the ring round them are drawn again.</summary>
    private static void OnGroundChanged(Map map, Rectangle2D bounds)
    {
        if (map != _map || _builder == null)
        {
            return;
        }

        var cx1 = Math.Max(0, Math.Min(bounds.Start.X, bounds.End.X) / 16 - 1);
        var cy1 = Math.Max(0, Math.Min(bounds.Start.Y, bounds.End.Y) / 16 - 1);
        var cx2 = Math.Min(_builder.ClustersX - 1, Math.Max(bounds.Start.X, bounds.End.X) / 16 + 1);
        var cy2 = Math.Min(_builder.ClustersY - 1, Math.Max(bounds.Start.Y, bounds.End.Y) / 16 + 1);

        for (var y = cy1; y <= cy2; y++)
        {
            for (var x = cx1; x <= cx2; x++)
            {
                MarkDirty(y * _builder.ClustersX + x);
            }
        }

        GroundChanges++;
    }

    public static long GroundChanges { get; private set; }

    /// <summary>Draws a square of the map again, by hand: <c>reach redraw x y [r]</c> at the door.</summary>
    public static int Redraw(int x, int y, int radius)
    {
        if (_builder == null)
        {
            return 0;
        }

        var marked = 0;

        for (var cy = Math.Max(0, (y - radius) / 16); cy <= Math.Min(_builder.ClustersY - 1, (y + radius) / 16); cy++)
        {
            for (var cx = Math.Max(0, (x - radius) / 16); cx <= Math.Min(_builder.ClustersX - 1, (x + radius) / 16); cx++)
            {
                MarkDirty(cy * _builder.ClustersX + cx);
                marked++;
            }
        }

        return marked;
    }

    // ------------------------------------------------------------------ the questions

    /// <summary>What a tile is, by its best standing place; <see cref="BotReachClass.Unknown"/> before the first solve or on another map.</summary>
    public static BotReachClass Class(Map map, int x, int y) =>
        _snapshot is { } snap && snap.Holds(map) ? snap.ClassAt(x, y) : BotReachClass.Unknown;

    /// <summary>What a tile is at a height: where a tile's standing places differ, the one nearest <paramref name="z"/>.</summary>
    public static BotReachClass Class(Map map, int x, int y, int z) =>
        _snapshot is { } snap && snap.Holds(map) ? snap.ClassAt(x, y, z) : BotReachClass.Unknown;

    /// <summary>
    /// Whether a tile is out of reach: for a walker, anything but open ground; for a walker aboard a ship
    /// (<paramref name="aboard"/>), anything but water — the land stays unreachable to a ship, the water does not. False while
    /// the map cannot say.
    /// </summary>
    public static bool Unreachable(Map map, int x, int y, bool aboard = false)
    {
        var k = Class(map, x, y);

        return k != BotReachClass.Unknown && (aboard ? k != BotReachClass.Water : k != BotReachClass.Open);
    }

    /// <summary>The same for a body: aboard when a ship stands under it.</summary>
    public static bool Unreachable(Mobile walker, int x, int y) =>
        walker?.Map != null && Unreachable(walker.Map, x, y, Afloat(walker.Map, walker.Location));

    /// <summary>
    /// Whether the walkers' own searches leave out every step onto ground the map says the walker cannot reach: from open
    /// ground, anything but open ground; from a deck, anything but water. Patrick's plan v2, point 6 (01.10.2026, build 388).
    /// See <see cref="Standing"/> and <c>BotPath.Find</c>.
    /// </summary>
    public static bool PrunesSearch { get; set; } = true;

    /// <summary>
    /// What ground a walker standing at <paramref name="from"/> can reach by the map: open ground for one on open ground, water
    /// for one aboard a ship. False — nothing can be said — while the map cannot say, during the map's own check
    /// (<see cref="Bypass"/>), and for a walker standing anywhere else (an isle, a pocket it was carried into).
    /// </summary>
    public static bool Standing(Map map, Point3D from, out BotReachClass want)
    {
        want = BotReachClass.Unknown;

        if (!Running || Bypass || _snapshot is not { } snap || !snap.Holds(map) || !snap.Homed)
        {
            return false;
        }

        var here = snap.ClassAt(from.X, from.Y, from.Z);

        if (here == BotReachClass.Open)
        {
            want = BotReachClass.Open;

            return true;
        }

        if (here == BotReachClass.Water && Afloat(map, from))
        {
            want = BotReachClass.Water;

            return true;
        }

        return false;
    }

    /// <summary>Whether the map says a walker at <paramref name="from"/> cannot reach the standing place at <paramref name="to"/>: a point no route, retreat or muster is built on.</summary>
    public static bool RefusesTile(Map map, Point3D from, Point3D to)
    {
        if (!Standing(map, from, out var want))
        {
            return false;
        }

        var kind = Class(map, to.X, to.Y, to.Z);

        return kind != BotReachClass.Unknown && kind != want;
    }

    /// <summary>Whether a ship's deck is under this point.</summary>
    public static bool Afloat(Map map, Point3D at) => map != null && map != Map.Internal && BaseBoat.FindBoatAt(at, map) != null;

    /// <summary>Whether a ship could be where this tile is: water, by the map. See <see cref="BotSeaChart"/> for whether a hull fits.</summary>
    public static bool Sailable(Map map, int x, int y) => Class(map, x, y) == BotReachClass.Water;

    /// <summary>
    /// Whether the map proves a walk from <paramref name="from"/> to within <paramref name="reach"/> of <paramref name="goal"/>
    /// impossible: the walker stands on open ground and no open tile lies within reach of the goal — or the walker is aboard a
    /// ship and no water does. Counted when it refuses. A walker standing anywhere else is never refused: the map says what a
    /// walk from home reaches, and one standing off it is not walking from home.
    /// </summary>
    public static bool Refuses(Map map, Point3D from, Point3D goal, int reach)
    {
        if (!Running || Bypass || _snapshot is not { } snap || !snap.Holds(map) || !snap.Homed)
        {
            return false;
        }

        Asked++;

        reach = Math.Clamp(reach, 0, 24);

        var here = snap.ClassAt(from.X, from.Y, from.Z);

        if (here == BotReachClass.Water)
        {
            // A deck over the water. Anything else standing on a water tile — a swimmer, a pier made of items — gets no answer.
            if (!Afloat(map, from) || snap.Any(goal.X, goal.Y, reach, BotReachClass.Water))
            {
                return false;
            }

            RefusedAboard++;

            return true;
        }

        if (here != BotReachClass.Open || snap.Any(goal.X, goal.Y, reach, BotReachClass.Open))
        {
            return false;
        }

        Refused++;

        return true;
    }

    /// <summary>
    /// Whether the map says a walk joins the two places: the walker stands on open ground and the goal's own standing place, at
    /// its height, is open ground too — both reached from home, over the ground and through every gate (01.10.2026, build next).
    /// The goal's own tile and not its surroundings, so a creature on a crypt's roof beside an open street is not "joined". Read
    /// by <c>BotReach.Ask</c> before the pocket ledger, whose floods do not go through teleporters.
    /// </summary>
    public static bool Joins(Map map, Point3D from, Point3D goal)
    {
        if (!Running || Bypass || _snapshot is not { } snap || !snap.Holds(map) || !snap.Homed)
        {
            return false;
        }

        return snap.ClassAt(from.X, from.Y, from.Z) == BotReachClass.Open && snap.ClassAt(goal.X, goal.Y, goal.Z) == BotReachClass.Open;
    }

    /// <summary>A sampler's place refused by the map. See <see cref="BotPopulation.Reachable"/>.</summary>
    public static void NoteSampler() => SamplerRefused++;

    /// <summary>A route not asked of the planner because the map refused its goal. See <see cref="BotJourney"/>.</summary>
    public static void NotePlanner() => PlannerRefused++;

    /// <summary>
    /// A walker took a step. Stepping from open ground onto ground the map calls anything else is the map wrong about that
    /// tile — or the ground changed under it — and is counted, said the first <see cref="SayContradictions"/> times, and after
    /// <see cref="RedrawAfter"/> in one cluster the cluster is drawn again. The population itself is the map's widest check.
    /// </summary>
    public static void Stepped(Mobile walker, Point3D before, Point3D after)
    {
        if (_snapshot is not { } snap || walker?.Map == null || !snap.Holds(walker.Map))
        {
            return;
        }

        StepsSeen++;

        var a = snap.ClassAt(before.X, before.Y, before.Z);
        var b = snap.ClassAt(after.X, after.Y, after.Z);

        if (a != BotReachClass.Open)
        {
            if (a is BotReachClass.Isle or BotReachClass.Pocket)
            {
                StepsAway++;
            }

            return;
        }

        if (b == BotReachClass.Open || b == BotReachClass.Unknown || Afloat(walker.Map, after))
        {
            return;
        }

        Contradicted++;

        if (Contradicted <= SayContradictions)
        {
            logger.Information(
                "Reach: {Name} stepped from open ground at {From} onto {To}, which the map calls {Class}",
                walker.Name,
                before,
                after,
                b
            );
        }

        var c = (after.Y >> 4) * snap.ClustersX + (after.X >> 4);
        var n = _contradicted.GetValueOrDefault(c) + 1;

        _contradicted[c] = n;

        if (n == RedrawAfter)
        {
            RedrawnForSteps++;
            MarkDirty(c);
        }
    }

    // ------------------------------------------------------------------ the words

    public static string Label(BotReachClass k) =>
        k switch
        {
            BotReachClass.Open => "open ground a walk from home reaches",
            BotReachClass.Isle => "ground no walk reaches, on a shore a ship could land at",
            BotReachClass.Pocket => "ground nothing reaches: a sealed pocket",
            BotReachClass.Water => "water: a ship's, never a walker's",
            BotReachClass.Blocked => "nowhere to stand",
            _ => "not drawn yet"
        };

    public static string State => _stage switch
    {
        Stage.Off => "off",
        Stage.Loading => "reading its file",
        Stage.Signing => $"checking the clusters read against the houses and items ({(_clusters == null ? 0 : _clusters.Length - 1 - _signNext)} of {Clusters})",
        Stage.Drawing => _next >= 0 ? $"drawing ({Clusters - 1 - _next} of {Clusters} clusters)" : $"drawing {_dirty.Count} clusters again",
        _ => _solving ? "solving" : "ready"
    };

    /// <summary>How far the first drawing has got, nought to one.</summary>
    public static double Progress =>
        _clusters == null ? 0.0 : _stage == Stage.Drawing && _next >= 0 ? (Clusters - 1 - _next) / (double)Clusters : _stage == Stage.Loading ? 0.0 : 1.0;

    /// <summary>What a tile is, for the door: its class, its standing places where they differ, its land and the ground round it.</summary>
    public static string TellAt(int x, int y)
    {
        if (_snapshot is not { } snap)
        {
            return $"the reach map is not ready: {State}.";
        }

        var k = snap.ClassAt(x, y);
        var parts = new List<string> { $"({x}, {y}) is {Label(k)}" };
        var c = (y >> 4) * snap.ClustersX + (x >> 4);

        if (snap.Inside(x, y) && snap.LayerClasses[c] is { } layers)
        {
            var cell = ((y & 15) << 4) | (x & 15);

            for (var i = 0; i < layers.Length; i++)
            {
                if ((layers[i] & 0xFF) == cell)
                {
                    parts.Add($"at z {((layers[i] >> 8) & 0xFF) - 128} {(BotReachClass)((layers[i] >> 16) & 0xFF)}");
                }
            }
        }

        var land = snap.LandAt(x, y, out var tiles, out _);

        if (land >= 0 && k != BotReachClass.Open)
        {
            parts.Add($"land #{land} of {tiles} tiles no walk reaches");
        }

        Span<int> near = stackalloc int[6];

        for (var dy = -2; dy <= 2; dy++)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                near[(int)snap.ClassAt(x + dx, y + dy)]++;
            }
        }

        parts.Add($"within 2: {near[1]} open, {near[2]} isle, {near[3]} pocket, {near[4]} water, {near[5]} blocked");
        parts.Add($"to a walker from home {(k == BotReachClass.Open ? "reachable" : "unreachable")}, to a ship {(k == BotReachClass.Water ? "sailable" : "land")}");

        return $"{string.Join("; ", parts)} (map v{snap.Version}).";
    }

    public static string Tell() => $"{Describe()}. {BotReachCheck.Describe()}.";

    public static string Describe()
    {
        if (!Running)
        {
            return "the reach map is off";
        }

        var snap = _snapshot;
        var map = snap == null
            ? $"not solved yet ({State}, {Progress:P0})"
            : $"v{snap.Version} of {snap.MapName}: {snap.Tiles[1]} tiles open, {snap.Tiles[2]} isle, {snap.Tiles[3]} pocket, {snap.Tiles[4]} water, {snap.Tiles[5]} blocked{(snap.Tiles[0] > 0 ? $", {snap.Tiles[0]} not drawn" : "")}; {snap.Lands} lands no walk from home reaches; {snap.OwnArrays} of {snap.Classes.Length} clusters hold a border";

        return $"reach map {map}; {State}; drawn {Drawn} clusters ({Redrawn} again, {Signed} checked at boot and {SignChanged} changed) in {DrawMs + SignMs:F0}ms of the loop (worst tick {WorstTickMs:F1}ms), "
            + $"{Solves} solves ({LastSolveMs:F0}ms the last, {(LastSolveInline ? "inline" : "off the loop")}, {HandMs:F0}ms of the loop to hand them over); file {FileState}, {FileBytes} bytes, written {FileWrites} times; "
            + $"{GroundChanges} ground changes; asked {Asked} times: {Refused} walks refused from open ground, {RefusedAboard} from a deck for land, {SamplerRefused} places refused to the samplers, {PlannerRefused} routes never asked of the planner; "
            + $"{StepsSeen} steps watched: {Contradicted} from open ground onto ground it calls otherwise ({RedrawnForSteps} clusters drawn again for it), {StepsAway} on ground no walk from home reaches; docks: {BerthsAground} berths on ground it calls land, {ShoresUnreached} shores with no ground a walk reaches; "
            + $"no point built on ground it refuses: the walkers' searches left {BotPath.Pruned} steps unexamined for landing on such ground ({BotPath.PrunedSearches} searches{(PrunesSearch ? "" : "; off")}), the planners refused {BotDanger.RefusedNodes} nodes on it, {BotBolt.Unreached} retreats were passed over for landing on it";
    }

    private sealed class TickTimer : Timer
    {
        public TickTimer(TimeSpan interval) : base(interval, interval)
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
                logger.Error(e, "Reach: a tick threw; the map stands as it was");
            }
        }
    }
}
