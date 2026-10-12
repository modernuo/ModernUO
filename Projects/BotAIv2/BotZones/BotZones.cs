using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>The dashboard's copy of the zones: the JSON and the tag a browser may send back to be told nothing changed.</summary>
public sealed record BotZonePage(string Json, string ETag);

/// <summary>
/// The danger and safety zones of the bots' home map: where creatures that attack on sight patrol, how far their sight
/// reaches, how strong they are against a bot, and where only passive animals live. The answer the planners, the hunt and the
/// dashboard read.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, evening:</b> walk the world with Argus and the other debuggers, mark every mob zone on the
/// map with how many live there and how strong they are; put the zones on the dashboard as separate areas that live in
/// memory, update, have smoothed corners and grow more precise; where no mobs are, bots may route safe paths through; where
/// mobs are and their patrol or aggro could touch someone, mark the ground potentially dangerous — "maximum precision, it
/// drives safe paths and the quality of the hunting".
/// </para>
///
/// <para>
/// <b>How the precision grows.</b> At boot every spawner is a declared haunt: a disc of its walking range round it
/// (<see cref="BotHaunts"/>). The sweep (<see cref="BotZoneScan"/>) passes every living creature every few seconds and records
/// where attackers are seen <em>awake</em> (<see cref="BotZoneRaster"/>); when a haunt has been watched awake long enough
/// (<see cref="BotHaunt.Confidence"/> at <see cref="ObservedWins"/>) its declared disc shrinks to the farthest its creatures
/// were seen from home and the observed patrol draws the rest. The ground only wakes where a client or a bot is (<see cref="BotWake"/>), so the debuggers walk the
/// least-watched zones and wake them (<see cref="BotZoneTour"/>). Bot deaths are counted in the zone they fall in, and a zone
/// that keeps killing is read one level deadlier than its arithmetic (<see cref="DeathsPromote"/>).
/// </para>
///
/// <para>
/// <b>Cost.</b> On the loop: a strip of the spawner survey a beat, the sweep within <see cref="BotZoneScan.BudgetMs"/> a beat,
/// the tour once a second, and a gather of plain values every <see cref="RefreshMs"/>; every one of them is measured and
/// printed in the <c>Zones:</c> line. The build — painting, labelling, fringes, outlines and the zones' JSON — is pure
/// arithmetic on that gathered copy (<see cref="BotZoneBuild"/>) and runs off the loop when the machine has
/// <see cref="OffLoopCores"/> cores, published through the one volatile reference <see cref="State"/> (CLAUDE.md rules 3, 10).
/// </para>
///
/// <para>
/// <b>The queries are one array read.</b> <see cref="LevelAt(Map, int, int)"/> is asked per node by a path planner, millions of
/// times: it reads the published state once, indexes the raster by <c>(y &gt;&gt; 2) * width + (x &gt;&gt; 2)</c>, and at most
/// reads one zone out of an array. Nothing in it allocates, locks or walks anything.
/// </para>
/// </summary>
public static class BotZones
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotZones));

    // ---------------------------------------------------------------------------------------------------------------------
    // Dials. Every one is read by a build through BotZoneSettings, copied at the gather.
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>Whether the zones are kept at all. Off: every query answers clear and nothing is scanned.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often the zones' beat runs, in milliseconds.</summary>
    public static int TickMs { get; set; } = 250;

    /// <summary>How often the zones are rebuilt from what is known, at most.</summary>
    public static int RefreshMs { get; set; } = 20000;

    /// <summary>How often the dashboard's JSON is rewritten, at most, when anything in it changed.</summary>
    public static int JsonEveryMs { get; set; } = 4000;

    /// <summary>How often the median living bot's strength, the yardstick of <see cref="BotZone.VsBot"/>, is taken again.</summary>
    public static int ReferenceEveryMs { get; set; } = 120000;

    /// <summary>The yardstick before any bot has been measured, and its floor: a lone bot of the first version, 1116.</summary>
    public static double ReferenceFloor { get; set; } = 1116;

    /// <summary>A declared disc wider than this, in tiles, is not painted: its creatures are drawn where they are instead.</summary>
    public static int DeclaredMost { get; set; } = 64;

    /// <summary>Tiles round a living attacker painted as patrol, and round one whose spawner declares more than <see cref="DeclaredMost"/>.</summary>
    public static int LiveRadius { get; set; } = 4;

    public static int LooseRadius { get; set; } = 12;

    /// <summary>Tiles round a living passive animal of a wide-ranging spawner painted as its hunting ground.</summary>
    public static int PassiveRadius { get; set; } = 8;

    /// <summary>Tiles round a living sea creature painted as its water: its patrol, which the planners read.</summary>
    public static int WaterRadius { get; set; } = 8;

    /// <summary>
    /// Tiles within which sea creatures are grouped into one zone, never painted as danger. Wide, so a school at sea is one zone
    /// rather than twenty: on the spawn files, grouping at 24 left the sea as 748 zones and 1.8MB of JSON, at 40 as 286, and at
    /// 64 the oceans fused into 22.
    /// </summary>
    public static int WaterGroup { get; set; } = 40;

    /// <summary>Cells round an observed cell painted with it: four tiles and the next sighting's wander.</summary>
    public static int ObservedMargin { get; set; } = 1;

    /// <summary>The faded weight of awake sightings a cell must hold to be painted as observed patrol.</summary>
    public static double ObservedLeast { get; set; } = 0.5;

    /// <summary>
    /// A haunt at or above this confidence is painted by what was observed: its disc shrinks to the farthest a creature of it was
    /// seen awake from home (plus a margin), and the cells seen patrolled stand beside it.
    /// </summary>
    public static double ObservedWins { get; set; } = 0.8;

    /// <summary>Seconds awake and awake sightings at which a haunt's shape counts as learned. See <see cref="BotHaunt.Confidence"/>.</summary>
    public static int ConfidentSeconds { get; set; } = 600;

    public static int ConfidentSamples { get; set; } = 60;

    /// <summary>Two awake samples of one haunt further apart than this are not one stretch of waking.</summary>
    public static int AwakeGapMs { get; set; } = 45000;

    /// <summary>The longest sight counted, in tiles: most creatures see 16, a few up to 22.</summary>
    public static int PerceptionMost { get; set; } = 20;

    /// <summary>How far onto the land a sea creature reaches: one that bites, from the water's edge; one that casts, as far as its spells.</summary>
    public static int WaterMeleeReach { get; set; } = 3;

    public static int WaterCasterReach { get; set; } = 12;

    /// <summary>How far a creature that cannot move reaches when it only bites.</summary>
    public static int RootedReach { get; set; } = 3;

    /// <summary>Attackers within this many tiles of one another are counted as one fight. Sight and a few steps.</summary>
    public static int ConvergeTiles { get; set; } = 20;

    /// <summary>
    /// <b>How local a zone is (02.10.2026, build 389).</b> Two spawners are one zone when their discs touch within
    /// <see cref="JoinGap"/> tiles and the zone they make is no wider than <see cref="ZoneMostTiles"/> across; a creature farther
    /// from its spawner than its range plus <see cref="StrayBeyond"/> is a wanderer, a zone of its own where it stands; an
    /// observed cell belongs to the nearest spawner whose range plus <see cref="ObservedSlack"/> covers it, and to nobody
    /// otherwise. On 388 the paint joined into one zone of 1,224 × 1,920 tiles west of Trinsic. See <see cref="BotZoneBuild"/>.
    /// </summary>
    public static int JoinGap { get; set; } = 8;

    public static int ZoneMostTiles { get; set; } = 192;

    public static int StrayBeyond { get; set; } = 16;

    public static int ObservedSlack { get; set; } = 8;

    /// <summary>Whether guarded ground (the towns) is kept out of every patrol and fringe: the guards kill what attacks there.</summary>
    public static bool TownsClear { get; set; } = true;

    /// <summary>
    /// <b>A camp's patrol may grow to where its creatures walk (08.10.2026, deaths30).</b> The observed range is clipped at the
    /// declaration, so the map never learned how far a camp walks. Every calm awake sighting of a spawner's creatures is counted
    /// by its distance from home (<see cref="BotHaunt.ReachAt"/>), halved every <see cref="ReachHalfLifeMinutes"/>; once they
    /// weigh <see cref="ReachLeast"/>, the distance <see cref="ReachPercentile"/> of them fall within is the spawner's reach, and
    /// past the declaration the camp claims the observed cells out to it, never more than <see cref="ReachBeyond"/> past the
    /// declaration. Off by default (nought), on the measure of 08.10 (today's spawners, memory and deaths, offline): at 0.95 and
    /// 32 tiles, 102 of 872 land spawners grow, the land's paint by 1.4 %, and one of the 21 deaths on clear ground of 07–08.10
    /// comes under paint of its own killer's kind (Cedric's spectre) — about nothing. The sightings are counted and kept across
    /// restarts while it is off, so switching it on starts from what was seen.
    /// </summary>
    public static int ReachBeyond { get; set; }

    public static double ReachPercentile { get; set; } = 0.95;

    public static int ReachLeast { get; set; } = 20;

    public static int ReachHalfLifeMinutes { get; set; } = 180;

    /// <summary>
    /// How far a caster's spell lands, past its sight, in its aggro reach (08.10.2026): the range the engine targets spells at,
    /// <c>IRangedSpell.TargetRange</c> (ten tiles from T2A on, twelve before), unless set. An archer's is its bow's own range.
    /// </summary>
    public static int SpellReach
    {
        get => _spellReach >= 0 ? _spellReach : Core.T2A ? 10 : 12;
        set => _spellReach = value;
    }

    private static int _spellReach = -1;

    /// <summary>
    /// <b>A wanderer is painted where it walked (08.10.2026).</b> Its places in the last <see cref="WanderMinutes"/>, joined as
    /// the line it walked from where it stands now, at most <see cref="WanderMostCells"/> cells of new paint a wanderer. Nought
    /// minutes: only where it stands, as before.
    /// </summary>
    public static int WanderMinutes { get; set; } = 10;

    public static int WanderMostCells { get; set; } = 64;

    /// <summary>
    /// Whether an event's creatures are a zone of their own while they live (08.10.2026): an invasion's waves round their lair,
    /// a relic lair's warden on its leash, The Shadow's band lying in wait on a quay for a colony's ship. Off: they are
    /// wanderers like any creature without a spawner.
    /// </summary>
    public static bool Events { get; set; } = true;

    /// <summary>At or above this many median bots of threat, a zone is deadly: past what a company of four takes at the margin a bot commits at.</summary>
    public static double DeadlyVsBot { get; set; } = 6.0;

    /// <summary>The share of its declared fight an empty haunt is counted at, until its spawner fills it.</summary>
    public static double EmptyWeight { get; set; } = 0.5;

    /// <summary>Bot deaths in the window that make a hostile zone deadly whatever its arithmetic says. Nought: never.</summary>
    public static int DeathsPromote { get; set; } = 6;

    /// <summary>How far back bot deaths are counted, in hours.</summary>
    public static int DeathsWindowHours { get; set; } = 24;

    /// <summary>A hunting ground with fewer living animals than this is not a zone.</summary>
    public static int ClearLeastLive { get; set; } = 3;

    /// <summary>Points at most in one ring after smoothing, for an attackers' zone and for a sea or hunting-ground zone.</summary>
    public static int MostPoints { get; set; } = 64;

    public static int ClearMostPoints { get; set; } = 32;

    /// <summary>Points at most in one ring of a sea zone: the sea's shapes matter to boats, not to the roads.</summary>
    public static int SeaMostPoints { get; set; } = 16;

    /// <summary>Rings at most per zone per layer, largest first.</summary>
    public static int MostRings { get; set; } = 4;

    /// <summary>Kinds listed per zone; the rest are counted in <c>mobs.others</c>.</summary>
    public static int MostKinds { get; set; } = 8;

    /// <summary>Douglas–Peucker tolerance in tiles, and Chaikin passes after it.</summary>
    public static double SimplifyTiles { get; set; } = 2.0;

    public static int SmoothPasses { get; set; } = 2;

    /// <summary>Whether the build may run off the loop, and the cores the machine must have for it to (CLAUDE.md rule 10).</summary>
    public static bool OffLoop { get; set; } = true;

    public static int OffLoopCores { get; set; } = 4;

    /// <summary>
    /// How often the zones are rebuilt when the build must run on the loop (fewer than <see cref="OffLoopCores"/> cores): a
    /// build of the whole map is 100–450 ms, which the loop can afford every two minutes and not every twenty seconds.
    /// </summary>
    public static int InlineRefreshMs { get; set; } = 120000;

    /// <summary>A build dispatched this long ago that has not published is reported (once).</summary>
    public static int BuildLostMs { get; set; } = 120000;

    // ---------------------------------------------------------------------------------------------------------------------
    // State and counters
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>The zones as last built. The one volatile reference the build publishes through; null until the first build.</summary>
    public static BotZoneState State => _state;

    private static volatile BotZoneState _state;

    private static volatile BotZonePage _page = new(Empty(), "\"z0\"");

    /// <summary>The dashboard's <c>/api/zones</c>, rewritten on the loop at most every <see cref="JsonEveryMs"/>.</summary>
    public static string Json => _page.Json;

    /// <summary>The JSON and its tag together, read once by the listener so the two never disagree.</summary>
    public static BotZonePage Page => _page;

    public static Map Map { get; private set; }

    public static long Builds { get; private set; }

    public static long BuildsOffLoop { get; private set; }

    public static long BuildFailures { get; private set; }

    public static double GatherMs { get; private set; }

    public static double GatherMsTotal { get; private set; }

    public static double BuildMsTotal { get; private set; }

    public static double BuildMsWorst { get; private set; }

    /// <summary>Loop milliseconds a build ran inline, when it could not go off the loop.</summary>
    public static double InlineMsTotal { get; private set; }

    public static long Ticks { get; private set; }

    public static double SpentMs { get; private set; }

    public static double WorstTickMs { get; private set; }

    public static double TourSpentMs { get; private set; }

    public static long JsonWrites { get; private set; }

    public static double JsonMsTotal { get; private set; }

    public static int JsonBytes { get; private set; }

    public static long DeathsHeard { get; private set; }

    /// <summary>The yardstick now: the median living bot's strength.</summary>
    public static double Reference { get; private set; }

    private static BotZoneState _seen;

    private static bool _building;

    private static bool _lostSaid;

    /// <summary>Counts world reloads, so a build gathered before one and finishing after it is dropped rather than published.</summary>
    private static int _generation;

    private static long _dispatchedTick;

    private static long _refreshedTick;

    private static long _jsonTick;

    private static long _referenceTick;

    private static long _touredTick;

    private static long _watchVersion = -1;

    private static long _startedTick;

    private static bool _jsonDirty = true;

    private static long _version;

    private static readonly ushort[][] _buffers = new ushort[2][];

    private static readonly List<(int X, int Y, long Tick)> _deaths = [];

    /// <summary>At most this many deaths are held for the window; the oldest go first.</summary>
    public static int MostDeaths { get; set; } = 20000;

    private static Timer _timer;

    // ---------------------------------------------------------------------------------------------------------------------
    // Queries
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The level of the ground at a tile: clear, edge (an aggro fringe), hostile or deadly. One read of the published raster;
    /// clear before the first build, off the home map, or with the zones switched off.
    /// </summary>
    public static BotZoneLevel LevelAt(Map map, int x, int y)
    {
        var state = _state;

        if (state == null || map != state.Map)
        {
            return BotZoneLevel.Clear;
        }

        var cx = x >> BotZoneState.Shift;
        var cy = y >> BotZoneState.Shift;

        if ((uint)cx >= (uint)state.Width || (uint)cy >= (uint)state.Height)
        {
            return BotZoneLevel.Clear;
        }

        var v = state.Cells[cy * state.Width + cx];

        if (v == 0)
        {
            return BotZoneLevel.Clear;
        }

        return (v & BotZoneState.Fringe) != 0 ? BotZoneLevel.Edge : state.Zones[(v & BotZoneState.IndexMask) - 1].Level;
    }

    /// <summary>
    /// The same, with the fight a walker meets <em>at this tile</em> and that fight over the median bot: the strongest living
    /// attacker within <see cref="ConvergeTiles"/> of the cell in full and the rest within it at the secondary weight, whatever
    /// zone they are of, never below what the cell's own spawners promise at one place (<see cref="BotZoneState.Fight"/>); the
    /// zone's own worst place (<see cref="BotZone.Threat"/>) only where the raster has nothing. Nought for clear ground, a
    /// hunting ground included. Until build 388 this was the zone's worst place everywhere in it — one number over a peninsula.
    /// </summary>
    public static BotZoneLevel LevelAt(Map map, int x, int y, out float vsBot, out float threat)
    {
        vsBot = 0f;
        threat = 0f;

        var state = _state;

        if (state == null || map != state.Map)
        {
            return BotZoneLevel.Clear;
        }

        var cx = x >> BotZoneState.Shift;
        var cy = y >> BotZoneState.Shift;

        if ((uint)cx >= (uint)state.Width || (uint)cy >= (uint)state.Height)
        {
            return BotZoneLevel.Clear;
        }

        var i = cy * state.Width + cx;
        var v = state.Cells[i];

        if (v == 0)
        {
            return BotZoneLevel.Clear;
        }

        var zone = state.Zones[(v & BotZoneState.IndexMask) - 1];
        var level = (v & BotZoneState.Fringe) != 0 ? BotZoneLevel.Edge : zone.Level;

        // A hunting ground's strength is what its animals are worth if set upon, which is the hunt's question and never the
        // road's: a clear answer carries nought, and the hunt asks ZoneAt for the rest.
        if (level != BotZoneLevel.Clear)
        {
            var fight = state.Fight;
            var here = fight != null ? fight[i] * (float)BotZoneState.FightUnit : 0f;

            threat = here > 0f ? here : (float)zone.Threat;
            vsBot = (float)(threat / Math.Max(1.0, state.RefBotPower));
        }

        return level;
    }

    /// <summary>The fight a walker meets at a tile, by the fight raster alone; nought off any zone. See <see cref="LevelAt(Map, int, int, out float, out float)"/>.</summary>
    public static float FightAt(Map map, int x, int y)
    {
        var state = _state;

        if (state?.Fight == null || map != state.Map)
        {
            return 0f;
        }

        var cx = x >> BotZoneState.Shift;
        var cy = y >> BotZoneState.Shift;

        return (uint)cx >= (uint)state.Width || (uint)cy >= (uint)state.Height ? 0f : state.Fight[cy * state.Width + cx] * (float)BotZoneState.FightUnit;
    }

    /// <summary>The zone a tile belongs to, as patrol or as fringe; null for nobody's ground.</summary>
    public static BotZone ZoneAt(Map map, int x, int y)
    {
        var state = _state;

        return state == null || map != state.Map ? null : state.Owner(x, y);
    }

    /// <summary>Whether a tile is a zone's fringe rather than its patrol.</summary>
    public static bool IsFringe(Map map, int x, int y) => LevelAt(map, x, y) == BotZoneLevel.Edge;

    /// <summary>A zone by its number, or null.</summary>
    public static BotZone Find(int id)
    {
        var zones = _state?.Zones;

        if (zones == null)
        {
            return null;
        }

        for (var i = 0; i < zones.Length; i++)
        {
            if (zones[i].Id == id)
            {
                return zones[i];
            }
        }

        return null;
    }

    public static string Word(BotZoneLevel level) =>
        level switch
        {
            BotZoneLevel.Edge => "edge",
            BotZoneLevel.Hostile => "hostile",
            BotZoneLevel.Deadly => "deadly",
            _ => "clear"
        };

    // ---------------------------------------------------------------------------------------------------------------------
    // Evidence from outside
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>A bot was killed here. From <c>BotMobile.OnDeath</c>, with the place taken before anything moved the ghost.</summary>
    public static void Fell(Map map, Point3D at)
    {
        if (!Running || map == null || map != Map)
        {
            return;
        }

        DeathsHeard++;
        _deaths.Add((at.X, at.Y, Core.TickCount));

        if (_deaths.Count > Math.Max(100, MostDeaths))
        {
            _deaths.RemoveRange(0, _deaths.Count - MostDeaths);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // The beat
    // ---------------------------------------------------------------------------------------------------------------------

    public static void Start(Map map)
    {
        Stop();

        if (map == null || map == Map.Internal)
        {
            logger.Warning("Zones: the population has no home map; nothing is kept");

            return;
        }

        var now = Core.TickCount;

        Map = map;
        _startedTick = now;
        _refreshedTick = now - RefreshMs;
        _jsonTick = now - JsonEveryMs;
        _referenceTick = now - ReferenceEveryMs;
        _touredTick = now;
        _dispatchedTick = now;
        _jsonDirty = true;

        BotHaunts.Open(map, now);
        BotZoneRaster.Open(map, now);
        BotZoneScan.Open(map, now);
        BotZoneTour.Open(now);
        BotZoneMemory.Load(map);

        // The zone watch's window and state, once a process (07.10.2026). See BotZoneWatch.
        BotZoneWatch.Load();

        _timer = new ZoneTimer(TimeSpan.FromMilliseconds(Math.Max(50, TickMs)));
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    public static void Tick()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        var began = Stopwatch.GetTimestamp();
        var now = Core.TickCount;

        if (!Running)
        {
            BotZoneTour.Release(now);

            return;
        }

        BotHaunts.Step(now);
        BotHaunts.Measure();

        if (BotHaunts.Surveyed)
        {
            BotZoneScan.Step(now);
            BotZoneMemory.Apply(now);
        }

        if (now - _touredTick >= BotZoneTour.TickMs)
        {
            _touredTick = now;

            var tourBegan = Stopwatch.GetTimestamp();

            if (BotZoneTour.Beat(map, now))
            {
                _jsonDirty = true;
            }

            TourSpentMs += Stopwatch.GetElapsedTime(tourBegan).TotalMilliseconds;
        }

        if (now - _referenceTick >= ReferenceEveryMs)
        {
            _referenceTick = now;
            Reference = Median();
        }

        var state = _state;

        if (!ReferenceEquals(state, _seen) && state != null)
        {
            if (state.Generation == _generation)
            {
                Published(state);
            }
            else
            {
                // Built for the world before a reload: nothing of it describes this one.
                _seen = null;
                _state = null;
                _building = false;
            }
        }

        // A build that never publishes is said once and not replaced: a second build beside a first that is still running would
        // share its scratch arrays, and a stale map is better than a corrupt one.
        if (_building && !_lostSaid && now - _dispatchedTick > BuildLostMs)
        {
            _lostSaid = true;
            BuildFailures++;

            logger.Warning("Zones: a build dispatched {Seconds}s ago has not published; no other will start until it does", (now - _dispatchedTick) / 1000);
        }

        var every = OffLoopNow ? RefreshMs : Math.Max(RefreshMs, InlineRefreshMs);

        if (!_building && now - _refreshedTick >= every && Ready() && !SaveUnderWay())
        {
            _refreshedTick = now;
            Dispatch(map, now);
        }

        if (_jsonDirty && now - _jsonTick >= JsonEveryMs)
        {
            _jsonTick = now;
            _jsonDirty = false;
            WriteJson();
        }

        BotZoneMemory.Beat(now);

        // The accuracy counter's verdict and its file (07.10.2026); a change the zones page has not shown makes it due.
        BotZoneWatch.Beat(now);

        if (BotZoneWatch.Version != _watchVersion)
        {
            _watchVersion = BotZoneWatch.Version;
            _jsonDirty = true;
        }

        var ms = Stopwatch.GetElapsedTime(began).TotalMilliseconds;

        Ticks++;
        SpentMs += ms;

        if (ms > WorstTickMs)
        {
            WorstTickMs = ms;
        }
    }

    /// <summary>A world save is being written or is about to be: work off the loop waits for it (CLAUDE.md rule 3).</summary>
    public static bool SaveUnderWay() => World.WorldState is WorldState.Saving or WorldState.PendingSave;

    /// <summary>Whether a build goes off the loop: switched on, and a machine with the cores for it (CLAUDE.md rule 10).</summary>
    public static bool OffLoopNow => OffLoop && Environment.ProcessorCount >= Math.Max(2, OffLoopCores);

    /// <summary>
    /// The first build waits for one whole sweep, so the zones start from what is alive rather than from spawners alone, and for
    /// every kind to be known (two minutes at most): a kind not yet read counts as passive, and a first map built before would
    /// call an orc camp whose orcs are all dead a hunting ground.
    /// </summary>
    private static bool Ready() =>
        BotHaunts.Surveyed && BotZoneScan.Sweeps > 0 && (BotHaunts.Pending == 0 || Core.TickCount - _startedTick > 120000);

    private static void Published(BotZoneState state)
    {
        _seen = state;
        _building = false;
        _lostSaid = false;
        _jsonDirty = true;
        Builds++;
        BuildMsTotal += state.BuildMs;

        if (state.BuildMs > BuildMsWorst)
        {
            BuildMsWorst = state.BuildMs;
        }

        if (state.OffLoop)
        {
            BuildsOffLoop++;
        }

        if (Builds == 1)
        {
            logger.Information(
                "Zones: the first map is built: {Zones} zones ({Clear} clear, {Hostile} hostile, {Deadly} deadly, {Water} at sea) off {Spawners} spawners and {Creatures} creatures, in {Ms:F1}ms {Where}",
                state.Zones.Length,
                state.Clear,
                state.Hostile,
                state.Deadly,
                state.WaterZones,
                state.Spawners,
                state.Creatures,
                state.BuildMs,
                state.OffLoop ? "off the loop" : "on the loop"
            );
        }
    }

    private static void Dispatch(Map map, long now)
    {
        var began = Stopwatch.GetTimestamp();
        var input = Gather(map, now);

        GatherMs = Stopwatch.GetElapsedTime(began).TotalMilliseconds;
        GatherMsTotal += GatherMs;

        _building = true;
        _dispatchedTick = now;

        if (input.OffLoop)
        {
            _ = Task.Run(
                () =>
                {
                    try
                    {
                        // Published whole through the one volatile reference; the loop notices it on its next beat.
                        _state = BotZoneBuild.Build(input);
                    }
                    catch (Exception e)
                    {
                        Core.LoopContext.Post(
                            () =>
                            {
                                _building = false;
                                BuildFailures++;
                                logger.Error(e, "Zones: a build off the loop threw; the last map stands");
                            }
                        );
                    }
                }
            );

            return;
        }

        var inline = Stopwatch.GetTimestamp();

        try
        {
            _state = BotZoneBuild.Build(input);
        }
        catch (Exception e)
        {
            _building = false;
            BuildFailures++;
            logger.Error(e, "Zones: a build threw; the last map stands");
        }

        InlineMsTotal += Stopwatch.GetElapsedTime(inline).TotalMilliseconds;
    }

    /// <summary>
    /// Everything a build needs, copied into plain values on the loop: the kinds, the haunts, the living where they stand now,
    /// the observed cells, the deaths in the window, the places for the labels, and the back buffer.
    /// </summary>
    private static BotZoneInput Gather(Map map, long now)
    {
        Dictionary<BotZoneKind, int> kindIndex = [];
        List<BotZoneKindIn> kinds = [];

        int IndexOf(BotZoneKind kind)
        {
            if (!kindIndex.TryGetValue(kind, out var index))
            {
                index = kinds.Count;
                kindIndex[kind] = index;
                kinds.Add(
                    new BotZoneKindIn(kind.Name, kind.Power, kind.Aggressive, kind.Caster, kind.Perception, kind.Water, kind.Rooted, kind.Swims, kind.Strike)
                );
            }

            return index;
        }

        var all = BotHaunts.All;
        List<BotZoneHauntIn> haunts = new(all.Count);
        List<BotHauntEntry> entries = [];

        for (var i = 0; i < all.Count; i++)
        {
            var haunt = all[i];
            var weights = 0;

            haunt.Index = -1;
            entries.Clear();

            // The towns' own — a shopkeeper learned to be one after the survey read its spawner — are no zone's.
            for (var e = 0; e < haunt.Entries.Length; e++)
            {
                if (!haunt.Entries[e].Kind.Townsfolk)
                {
                    entries.Add(haunt.Entries[e]);
                    weights += haunt.Entries[e].Probability;
                }
            }

            if (entries.Count == 0)
            {
                continue;
            }

            var kindsOf = new int[entries.Count];
            var expected = new double[entries.Count];

            for (var e = 0; e < entries.Count; e++)
            {
                kindsOf[e] = IndexOf(entries[e].Kind);

                // A spawner keeps Count creatures, each drawn from its entries by probability; an entry never holds more than its own cap.
                expected[e] = Math.Min(entries[e].Max, haunt.Count * entries[e].Probability / (double)Math.Max(1, weights));
            }

            haunt.Index = haunts.Count;

            // Whether the spawner stands in the water, read once per place (07.10.2026): the sweep's own test for a living swimmer.
            if (haunt.WetAt != haunt.At)
            {
                haunt.WetAt = haunt.At;
                haunt.Wet = Wet(map, haunt.At.X, haunt.At.Y);
            }

            haunts.Add(
                new BotZoneHauntIn
                {
                    X = haunt.At.X,
                    Y = haunt.At.Y,
                    Z = haunt.At.Z,
                    Radius = haunt.Radius,
                    Count = haunt.Count,
                    Running = haunt.Running,
                    InTown = haunt.InTown,
                    Wet = haunt.Wet,
                    Kinds = kindsOf,
                    Expected = expected,
                    Confidence = haunt.Confidence,
                    AwakeSeconds = haunt.AwakeSeconds,
                    Samples = haunt.Samples,
                    ObservedRange = haunt.ObservedRange,
                    Reach = haunt.ReachAt(now),
                    LastAwake = haunt.LastAwake
                }
            );
        }

        List<BotZoneLiveIn> living = [];
        List<int> trail = [];
        List<BotZoneEventIn> events = [];

        _eventOf.Clear();

        if (Events)
        {
            StandingEvents(map, events);
        }

        var wander = (long)Math.Max(0, WanderMinutes) * 60000L;

        BotHaunts.Living(
            map,
            now,
            (c, seen) =>
            {
                var ev = _eventOf.Count > 0 && _eventOf.Remove(c, out var index) ? index : -1;
                var from = trail.Count / 2;
                var count = 0;

                // The wanderer's places in the last minutes, newest first (08.10.2026). See BotZoneSeen.Walked.
                if (seen.TrailCount > 0 && wander > 0)
                {
                    var length = seen.TrailX.Length;

                    for (var k = 0; k < seen.TrailCount; k++)
                    {
                        var slot = ((seen.TrailHead - k) % length + length) % length;

                        if (now - seen.TrailTick[slot] > wander)
                        {
                            break;
                        }

                        trail.Add(seen.TrailX[slot]);
                        trail.Add(seen.TrailY[slot]);
                        count++;
                    }
                }

                living.Add(
                    new BotZoneLiveIn(
                        c.X,
                        c.Y,
                        IndexOf(seen.Kind),
                        seen.Haunt?.Index ?? -1,
                        seen.Awake,
                        seen.Power,
                        BotHaunts.AttacksFirst(c.FightMode),
                        c.AI == AIType.AI_Mage,
                        c.RangePerception,

                        // A swimmer that can also walk — a water elemental — is the sea's while it is in the water, and the land's
                        // once it steps ashore: where it stands decides whose zone it is.
                        c.CanSwim && (c.CantWalk || Wet(map, c.X, c.Y)),
                        c.CantWalk && !c.CanSwim,
                        BotHaunts.StrikeOf(c),
                        ev,
                        from,
                        count
                    )
                );
            }
        );

        // An event's own the sweep has not passed yet — a wave that landed since, The Shadow's band, which are bots — where they
        // stand now.
        foreach (var (m, ev) in _eventOf)
        {
            if (m is BaseCreature c)
            {
                var kind = BotHaunts.KindOf(c.GetType());

                if (!kind.FromLife)
                {
                    kind.ReadFrom(c);
                }

                living.Add(
                    new BotZoneLiveIn(
                        c.X,
                        c.Y,
                        IndexOf(kind),
                        -1,
                        c.AIObject?.AITimer.Running == true,
                        (float)BotThreat.Power(c),
                        BotHaunts.AttacksFirst(c.FightMode),
                        c.AI == AIType.AI_Mage,
                        c.RangePerception,
                        c.CanSwim && (c.CantWalk || Wet(map, c.X, c.Y)),
                        c.CantWalk && !c.CanSwim,
                        BotHaunts.StrikeOf(c),
                        ev
                    )
                );
            }
            else
            {
                living.Add(
                    new BotZoneLiveIn(
                        m.X,
                        m.Y,
                        IndexOf(_bandit),
                        -1,
                        true,
                        (float)BotThreat.Power(m),
                        true,
                        false,
                        BaseCreature.DefaultRangePerception,
                        false,
                        false,
                        0,
                        ev
                    )
                );
            }
        }

        _eventOf.Clear();

        List<int> cells = [];
        List<float> weightsOf = [];

        BotZoneRaster.Collect(now, (float)ObservedLeast, cells, weightsOf);

        var window = (long)Math.Max(1, DeathsWindowHours) * 3600000L;
        var firstKept = 0;

        while (firstKept < _deaths.Count && now - _deaths[firstKept].Tick > window)
        {
            firstKept++;
        }

        if (firstKept > 0)
        {
            _deaths.RemoveRange(0, firstKept);
        }

        var deaths = new (int X, int Y)[_deaths.Count];

        for (var i = 0; i < _deaths.Count; i++)
        {
            deaths[i] = (_deaths[i].X, _deaths[i].Y);
        }

        List<BotZonePlaceIn> towns = [];

        foreach (var town in BotTowns.All)
        {
            if (town?.Name != null)
            {
                towns.Add(new BotZonePlaceIn(town.Name, town.Square.X, town.Square.Y, town.Square.X, town.Square.Y));
            }
        }

        List<BotZonePlaceIn> deeps = [];

        foreach (var deep in BotDungeon.All)
        {
            deeps.Add(new BotZonePlaceIn(deep.Name, deep.Bounds.Start.X, deep.Bounds.Start.Y, deep.Bounds.End.X, deep.Bounds.End.Y));
        }

        var width = BotZoneRaster.Width;
        var height = BotZoneRaster.Height;

        return new BotZoneInput
        {
            Map = map,
            Width = width,
            Height = height,
            Settings = Settings(),
            Kinds = kinds.ToArray(),
            Haunts = haunts.ToArray(),
            Live = living.ToArray(),
            Trail = trail.ToArray(),
            Events = events.ToArray(),
            Observed = cells.ToArray(),
            ObservedWeight = weightsOf.ToArray(),
            Deaths = deaths,
            Towns = towns.ToArray(),
            Deeps = deeps.ToArray(),
            RefBotPower = Math.Max(ReferenceFloor, Reference),
            Previous = _state,
            Buffer = BackBuffer(width * height),
            FightBuffer = FightBackBuffer(width * height),
            Guarded = GuardedCells(map, width, height),
            At = DateTime.Now,
            OffLoop = OffLoopNow,
            Generation = _generation
        };
    }

    /// <summary>The event each living event creature belongs to, filled and emptied within one gather.</summary>
    private static readonly Dictionary<Mobile, int> _eventOf = [];

    /// <summary>The Shadow's bandits on a quay as one kind: they are bots, not creatures of any spawner.</summary>
    private static readonly BotZoneKind _bandit = new(typeof(BotMobile), "ShadowBandit") { Aggressive = true, Known = true, FromLife = true };

    /// <summary>
    /// The events standing on the map now whose creatures live (08.10.2026), each with its field, and every living creature of
    /// them in <see cref="_eventOf"/>: the invasion's waves round the lair they hold to (<see cref="BotInvasion.Spread"/>); the
    /// relic lair's warden on its leash; The Shadow's band lying in wait on a quay for a colony's ship, which
    /// <see cref="BotPiracy"/> marks <see cref="BotHighway.Party.Quay"/> (a highway ambush lies ahead of a carrier on its road, and
    /// is left to the road: a carrier routed round a band it cannot see would make every robbery a miss). A rumour's lair has no
    /// guard of its own: its chest is put on ground the zones already read hostile.
    /// </summary>
    private static void StandingEvents(Map map, List<BotZoneEventIn> events)
    {
        if (BotInvasion.Now is { Over: false } raid && raid.Map == map)
        {
            var index = -1;

            for (var i = 0; i < raid.Alive.Count; i++)
            {
                if (raid.Alive[i] is { Deleted: false, Alive: true } c && c.Map == map)
                {
                    if (index < 0)
                    {
                        index = events.Count;
                        events.Add(new BotZoneEventIn($"the invasion out of {raid.From}", raid.Lair.X, raid.Lair.Y, BotInvasion.Spread));
                    }

                    _eventOf[c] = index;
                }
            }
        }

        if (BotRelicLair.Now is { } lair && lair.Warden is { Deleted: false, Alive: true } warden && warden.Map == map)
        {
            _eventOf[warden] = events.Count;
            events.Add(new BotZoneEventIn("the relic's lair", lair.At.X, lair.At.Y, Math.Max(0, warden.RangeHome)));
        }

        if (BotHighway.Now is { Quay: true } party && party.Map == map && party.Spot != Point3D.Zero)
        {
            var index = -1;

            for (var i = 0; i < party.Members.Count; i++)
            {
                if (party.Members[i] is { Deleted: false, Alive: true } bandit && bandit.Map == map)
                {
                    if (index < 0)
                    {
                        index = events.Count;
                        events.Add(new BotZoneEventIn("The Shadow's band on the quay", party.Spot.X, party.Spot.Y, 0));
                    }

                    _eventOf[bandit] = index;
                }
            }
        }
    }

    /// <summary>Whether a tile is water: a water land tile with nothing standing on it, or a water static at the surface.</summary>
    private static bool Wet(Map map, int x, int y)
    {
        var tiles = map.Tiles;

        foreach (var tile in tiles.GetStaticTiles(x, y))
        {
            if (BotSeaChart.WaterStatic(tile.ID))
            {
                return true;
            }
        }

        return BotSeaChart.WaterLand(tiles.GetLandTile(x, y).ID);
    }

    /// <summary>The one of the two raster buffers the published state does not use.</summary>
    private static ushort[] BackBuffer(int length)
    {
        var current = _state?.Cells;

        for (var b = 0; b < _buffers.Length; b++)
        {
            if (_buffers[b] == null || _buffers[b].Length != length)
            {
                _buffers[b] = new ushort[length];
            }

            if (!ReferenceEquals(_buffers[b], current))
            {
                return _buffers[b];
            }
        }

        return new ushort[length];
    }

    /// <summary>The same for the fight raster.</summary>
    private static ushort[] FightBackBuffer(int length)
    {
        var current = _state?.Fight;

        for (var b = 0; b < _fightBuffers.Length; b++)
        {
            if (_fightBuffers[b] == null || _fightBuffers[b].Length != length)
            {
                _fightBuffers[b] = new ushort[length];
            }

            if (!ReferenceEquals(_fightBuffers[b], current))
            {
                return _fightBuffers[b];
            }
        }

        return new ushort[length];
    }

    /// <summary>
    /// The guarded cells of the map, nought or one each: every rectangle of every region that is or lies in a
    /// <see cref="GuardedRegion"/> whose guards are not disabled. Built once and kept while the map and its count of regions
    /// stand; on the loop, as all of the gather is. Null with <see cref="TownsClear"/> off.
    /// </summary>
    private static byte[] GuardedCells(Map map, int width, int height)
    {
        if (!TownsClear || map == null)
        {
            return null;
        }

        var regions = Region.Regions;

        if (_guarded != null && _guardedMap == map && _guardedRegions == regions.Count && _guarded.Length == width * height)
        {
            return _guarded;
        }

        var cells = new byte[width * height];
        var marked = 0;

        for (var r = 0; r < regions.Count; r++)
        {
            var region = regions[r];

            if (region?.Map != map || region.Area == null)
            {
                continue;
            }

            var guarded = region.GetRegion<GuardedRegion>();

            if (guarded == null || guarded.IsDisabled())
            {
                continue;
            }

            for (var a = 0; a < region.Area.Length; a++)
            {
                var box = region.Area[a];
                var x0 = Math.Max(0, box.Start.X >> BotZoneState.Shift);
                var y0 = Math.Max(0, box.Start.Y >> BotZoneState.Shift);
                var x1 = Math.Min(width - 1, (box.End.X - 1) >> BotZoneState.Shift);
                var y1 = Math.Min(height - 1, (box.End.Y - 1) >> BotZoneState.Shift);

                for (var cy = y0; cy <= y1; cy++)
                {
                    for (var cx = x0; cx <= x1; cx++)
                    {
                        if (cells[cy * width + cx] == 0)
                        {
                            cells[cy * width + cx] = 1;
                            marked++;
                        }
                    }
                }
            }
        }

        _guarded = cells;
        _guardedMap = map;
        _guardedRegions = regions.Count;
        GuardedCellCount = marked;

        return cells;
    }

    private static byte[] _guarded;

    private static Map _guardedMap;

    private static int _guardedRegions;

    /// <summary>Cells of guarded ground on the home map, as last rasterised.</summary>
    public static int GuardedCellCount { get; private set; }

    private static readonly ushort[][] _fightBuffers = new ushort[2][];

    private static BotZoneSettings Settings() =>
        new()
        {
            DeclaredMost = DeclaredMost,
            LiveRadius = LiveRadius,
            LooseRadius = LooseRadius,
            PassiveRadius = PassiveRadius,
            WaterRadius = WaterRadius,
            WaterGroup = WaterGroup,
            ObservedMargin = Math.Max(0, ObservedMargin),
            ObservedWins = ObservedWins,
            PerceptionMost = PerceptionMost,
            WaterMeleeReach = WaterMeleeReach,
            WaterCasterReach = WaterCasterReach,
            RootedReach = RootedReach,
            ConvergeTiles = ConvergeTiles,
            JoinGap = JoinGap,
            ZoneMostTiles = ZoneMostTiles,
            StrayBeyond = StrayBeyond,
            ObservedSlack = ObservedSlack,
            ReachBeyond = Math.Max(0, ReachBeyond),
            WanderMostCells = WanderMinutes > 0 ? Math.Max(0, WanderMostCells) : 0,
            Secondary = BotThreat.Secondary,
            DeadlyVsBot = DeadlyVsBot,
            EmptyWeight = EmptyWeight,
            DeathsPromote = DeathsPromote,
            DeathsWindowHours = DeathsWindowHours,
            ConfidentSamples = ConfidentSamples,
            ClearLeastLive = ClearLeastLive,
            MostPoints = MostPoints,
            ClearMostPoints = ClearMostPoints,
            SeaMostPoints = SeaMostPoints,
            MostRings = MostRings,
            MostKinds = MostKinds,
            SimplifyTiles = SimplifyTiles,
            SmoothPasses = SmoothPasses
        };

    /// <summary>The median strength of the living bots in the world, or the floor when there are none.</summary>
    private static double Median()
    {
        List<double> powers = [];
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is { Deleted: false, Alive: true } && bot.Map != null && bot.Map != Map.Internal)
            {
                powers.Add(BotThreat.Power(bot));
            }
        }

        if (powers.Count == 0)
        {
            return Math.Max(ReferenceFloor, Reference);
        }

        powers.Sort();

        return Math.Max(ReferenceFloor, powers[powers.Count / 2]);
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // The dashboard
    // ---------------------------------------------------------------------------------------------------------------------

    private static string Empty() =>
        "{\"at\":null,\"map\":null,\"cell\":4,\"refBotPower\":0,\"counts\":{\"zones\":0,\"clear\":0,\"edge\":0,\"hostile\":0,\"deadly\":0,\"spawners\":0,\"creatures\":0},\"tour\":{\"running\":false,\"visited\":0,\"total\":0,\"walkers\":[]},\"zones\":[]}";

    private static void WriteJson()
    {
        var began = Stopwatch.GetTimestamp();
        var state = _state;
        var buffer = new ArrayBufferWriter<byte>((state?.ZonesUtf8.Length ?? 0) + 8192);

        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();

            if (state == null)
            {
                w.WriteNull("at");
            }
            else
            {
                w.WriteString("at", state.At.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
            }

            w.WriteString("map", Map?.Name);
            w.WriteNumber("cell", BotZoneState.Cell);
            w.WriteNumber("refBotPower", Math.Round(state?.RefBotPower ?? 0));

            w.WriteStartObject("counts");
            w.WriteNumber("zones", state?.Zones.Length ?? 0);
            w.WriteNumber("clear", state?.Clear ?? 0);
            w.WriteNumber("edge", state?.Edge ?? 0);
            w.WriteNumber("hostile", state?.Hostile ?? 0);
            w.WriteNumber("deadly", state?.Deadly ?? 0);
            w.WriteNumber("water", state?.WaterZones ?? 0);
            w.WriteNumber("spawners", BotHaunts.Spawners);
            w.WriteNumber("creatures", BotZoneScan.Creatures);
            w.WriteNumber("observedCells", BotZoneRaster.Touched);
            w.WriteEndObject();

            // How local the map is: for the danger bar and the summary line.
            w.WriteStartObject("accuracy");
            w.WriteNumber("landZones", state?.LandZones ?? 0);
            w.WriteNumber("strayZones", state?.StrayZones ?? 0);
            w.WriteNumber("paintedTiles", (long)(state?.PaintedCells ?? 0) * BotZoneState.Cell * BotZoneState.Cell);
            w.WriteNumber("patrolTiles", (long)(state?.LandCoreCells ?? 0) * BotZoneState.Cell * BotZoneState.Cell);
            w.WriteNumber("attackers", state?.LandAggressive ?? 0);
            w.WriteNumber("townsInside", state?.TownsInside ?? 0);
            w.WriteNumber("guardedKept", state?.GuardedKept ?? 0);

            if (state?.Largest is { } largest)
            {
                w.WriteStartObject("largest");
                w.WriteNumber("id", largest.Id);
                w.WriteString("label", largest.Label);
                w.WriteNumber("width", largest.X2 - largest.X1 + 1);
                w.WriteNumber("height", largest.Y2 - largest.Y1 + 1);
                w.WriteNumber("tiles", (long)largest.CoreCells * BotZoneState.Cell * BotZoneState.Cell);
                w.WriteEndObject();
            }
            else
            {
                w.WriteNull("largest");
            }

            w.WriteEndObject();

            w.WritePropertyName("tour");
            BotZoneTour.Write(w);

            // The zone watch: the accuracy counter both ways, the keeper, the end (07.10.2026). See BotZoneWatch.
            w.WritePropertyName("watch");
            BotZoneWatch.Write(w);

            w.WritePropertyName("zones");

            if (state == null)
            {
                w.WriteStartArray();
                w.WriteEndArray();
            }
            else
            {
                w.WriteRawValue(state.ZonesUtf8, true);
            }

            w.WriteEndObject();
        }

        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);

        _version++;
        _page = new BotZonePage(json, $"\"z{_version}\"");

        JsonWrites++;
        JsonBytes = buffer.WrittenCount;
        JsonMsTotal += Stopwatch.GetElapsedTime(began).TotalMilliseconds;
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // Words
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>The five-minute line: every counter this system keeps.</summary>
    public static string Describe()
    {
        var state = _state;
        var zones = state == null
            ? "no zones built yet"
            : $"{state.Zones.Length} zones ({state.Clear} clear, {state.Hostile} hostile, {state.Deadly} deadly; {state.WaterZones} of them at sea; {Emptied(state)} hostile ones empty now), built {Builds} times ({BuildsOffLoop} off the loop, {BuildFailures} failed), {state.Kept} kept their number and {state.Born} new in the last; the median bot {state.RefBotPower:F0}";

        var deaths = state == null
            ? ""
            : $"; deaths in {DeathsWindowHours}h by where they fell: {state.DeathsDeadly} deadly, {state.DeathsHostile} hostile, {state.DeathsEdge} in a fringe, {state.DeathsClear} on clear ground ({DeathsHeard} heard since the start); {BotZoneWatch.Summary()}";

        var build = Builds == 0
            ? "no build yet"
            : $"gather {GatherMs:F1}ms on the loop (last), build {BuildMsTotal / Builds:F1}ms average and {BuildMsWorst:F1}ms worst ({state?.Phases}, the last); {InlineMsTotal:F0}ms of builds ran inline on the loop";

        return $"{zones}{deaths}. {Accuracy(state)}. {BotHaunts.Describe()}. The sweep: {BotZoneScan.Describe()}. Observed: {BotZoneRaster.Touched} cells hold awake sightings ({BotZoneRaster.Recorded} recorded), {state?.ObservedCells ?? 0} of them patrol. "
            + $"The tour: {BotZoneTour.Describe()}. The loop: {Ticks} beats, {(Ticks == 0 ? 0 : SpentMs / Ticks):F3}ms a beat, {WorstTickMs:F1}ms worst, {SpentMs:F0}ms in all (the tour {TourSpentMs:F0}ms); {build}. "
            + $"JSON {JsonBytes / 1024}KB, written {JsonWrites} times in {(JsonWrites == 0 ? 0 : JsonMsTotal / JsonWrites):F2}ms each. {BotZoneMemory.Describe()} {BotZoneWatch.Describe()}";
    }

    /// <summary>
    /// How local the map is, in one sentence: zones of the land's attackers, tiles painted as their patrol or fringe, the largest
    /// zone, attackers per thousand tiles of patrol, towns under a patrol, and guarded ground kept clear. See <see cref="BotZoneBuild"/>.
    /// </summary>
    public static string Accuracy(BotZoneState state)
    {
        if (state == null)
        {
            return "Accuracy: no map yet";
        }

        var tiles = BotZoneState.Cell * BotZoneState.Cell;
        var painted = (long)state.PaintedCells * tiles;
        var patrol = (long)state.LandCoreCells * tiles;
        var perThousand = patrol > 0 ? state.LandAggressive * 1000.0 / patrol : 0.0;
        var largest = state.Largest == null
            ? "none"
            : $"#{state.Largest.Id} {state.Largest.Label} at {state.Largest.X2 - state.Largest.X1 + 1}×{state.Largest.Y2 - state.Largest.Y1 + 1} tiles ({(long)state.Largest.CoreCells * tiles / 1000}k of patrol, {state.Largest.Haunts} spawners, fight {state.Largest.Threat:F0})";

        return $"Accuracy: {state.LandZones} zones of the land's attackers ({state.StrayZones} of wanderers) paint {painted / 1000}k tiles as patrol or fringe ({patrol / 1000}k of patrol); the largest {largest}; {state.LandAggressive} attackers alive, {perThousand:F2} a thousand tiles of patrol; {state.TownsInside} town squares under a patrol or fringe; {state.GuardedKept} guarded cells kept clear of {GuardedCellCount}; {state.GrownHaunts} spawners' patrols grown past their declaration on what was seen (at most {ReachBeyond} tiles), {state.TrailCells} cells painted along wanderers' trails, {state.EventZones} event zones";
    }

    private static int Emptied(BotZoneState state)
    {
        var n = 0;

        for (var i = 0; i < state.Zones.Length; i++)
        {
            if (state.Zones[i].Empty)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>For the door's <c>zones</c>: the counts and the ten strongest zones.</summary>
    public static string Tell()
    {
        var state = _state;

        if (state == null)
        {
            return $"the zones are not built yet: {BotHaunts.Describe()}; {BotZoneScan.Describe()}.";
        }

        List<BotZone> worst = [];

        for (var i = 0; i < state.Zones.Length; i++)
        {
            if (state.Zones[i].Level >= BotZoneLevel.Hostile)
            {
                worst.Add(state.Zones[i]);
            }
        }

        worst.Sort((a, b) => b.VsBot.CompareTo(a.VsBot));

        var lines = new List<string>();

        for (var i = 0; i < worst.Count && i < 10; i++)
        {
            var z = worst[i];

            lines.Add($"#{z.Id} {Word(z.Level)} ×{z.VsBot:F1} at ({z.CenterX}, {z.CenterY}) — {z.Label}: {z.Aggressive} of {z.Live} alive attack, confidence {z.Confidence:F2}, {z.Deaths} deaths");
        }

        return $"{state.Zones.Length} zones: {state.Clear} clear, {state.Hostile} hostile, {state.Deadly} deadly ({state.WaterZones} at sea); the median bot {state.RefBotPower:F0}. The strongest: {string.Join("; ", lines)}. The tour: {BotZoneTour.Describe()}. {BotZoneWatch.Describe()}";
    }

    /// <summary>For the door's <c>zone x y</c>: what the zones say about one tile, and why.</summary>
    public static string TellAt(int x, int y)
    {
        var map = Map;
        var state = _state;

        if (state == null || map == null)
        {
            return "the zones are not built yet.";
        }

        var level = LevelAt(map, x, y);
        var zone = ZoneAt(map, x, y);
        var seen = BotZoneRaster.WeightAt(x, y, Core.TickCount);

        if (zone == null)
        {
            return $"({x}, {y}) is clear: no zone's patrol or fringe covers it; awake sightings here weigh {seen:F2}.";
        }

        var kinds = new List<string>();

        for (var i = 0; i < zone.Kinds.Length; i++)
        {
            var k = zone.Kinds[i];

            kinds.Add($"{k.Kind} {k.Live}/{k.Max} ({k.Power:F0}{(k.Aggressive ? ", attacks" : "")}{(k.Caster ? ", casts" : "")})");
        }

        var last = zone.LastSeen == DateTime.MinValue ? "never" : zone.LastSeen.ToLocalTime().ToString("HH:mm:ss");
        var here = FightAt(map, x, y);

        return $"({x}, {y}) is {Word(level)}: {(level == BotZoneLevel.Edge ? "in the aggro fringe of" : "inside")} zone #{zone.Id}, {Word(zone.Level)} — {zone.Label}. {zone.Why}. "
            + $"{zone.Live} alive of {zone.Max} kept: {string.Join(", ", kinds)}{(zone.OtherKinds > 0 ? $" and {zone.OtherKinds} more kinds" : "")}. "
            + $"The fight at this tile {here:F0} (what can close on a walker here; the spawners promise {zone.Declared:F0} at one place). "
            + $"Threat {zone.Threat:F0} at the zone's worst place (worst creature {zone.Worst:F0}, all {zone.PowerTotal:F0}) = ×{zone.VsBot:F2} the median bot's {state.RefBotPower:F0}. "
            + $"Patrol declared {zone.DeclaredRange} tiles, observed {zone.ObservedRange}, aggro reach {zone.AggroRange}; {zone.CoreCells} cells of patrol and {zone.AggroCells} of reach. "
            + $"Confidence {zone.Confidence:F2}: {zone.Samples} awake sightings over {zone.AwakeSeconds:F0}s, last {last}; this cell's sightings weigh {seen:F2}. {zone.Deaths} bots killed here in {DeathsWindowHours}h; {zone.Haunts} spawners feed it; the tour stands at ({zone.Stand.X}, {zone.Stand.Y}).";
    }

    /// <summary>After a world reload: nothing known, nothing published, every sector the tour held let go.</summary>
    public static void Forget()
    {
        Stop();
        BotZoneTour.Forget(Core.TickCount);
        BotZoneMemory.Save();

        // The accuracy window is about the map, not the population: kept in memory over the reload, and written now.
        BotZoneWatch.Save();
        BotHaunts.Forget();
        BotZoneRaster.Forget();
        BotZoneScan.Forget();
        _state = null;
        _seen = null;
        _page = new BotZonePage(Empty(), "\"z0\"");
        _buffers[0] = null;
        _buffers[1] = null;
        _fightBuffers[0] = null;
        _fightBuffers[1] = null;
        _guarded = null;
        _guardedMap = null;
        _deaths.Clear();
        Map = null;
        Builds = BuildsOffLoop = BuildFailures = 0;
        GatherMs = GatherMsTotal = BuildMsTotal = BuildMsWorst = InlineMsTotal = 0.0;
        Ticks = 0;
        SpentMs = WorstTickMs = TourSpentMs = 0.0;
        JsonWrites = 0;
        JsonMsTotal = 0.0;
        JsonBytes = 0;
        DeathsHeard = 0;

        // A build still running off the loop keeps its flag: it publishes into the next world, is recognised by its generation
        // and dropped, and only then may another start — two at once would share the builder's scratch.
        _generation++;
    }

    private sealed class ZoneTimer : Timer
    {
        public ZoneTimer(TimeSpan interval) : base(interval, interval)
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
                logger.Error(e, "Zones: the beat threw");
            }
        }
    }
}
