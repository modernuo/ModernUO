using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The debuggers' walk round the zones: each goes to the least-watched zone, wakes the ground round it the way a client would,
/// holds it awake long enough for the patrol to move, and goes on.
///
/// <para>
/// <b>Patrick's order of 30.09.2026: "walk the world with Argus and the other debuggers, look at all the mob zones".</b> The
/// world sleeps where no client is (<see cref="BotWake"/>): a creature nobody is near stands where it was spawned, so the only
/// way to learn a patrol a bot has never been near is to wake the ground there and watch. That is what the stop does:
/// <c>Sector.Activate</c> on the sectors round the zone — the five by five a client wakes, widened to the zone's own box up to
/// <see cref="MostSectors"/> — and a pass of the zone's box every <see cref="FocusMs"/> while it holds
/// (<see cref="BotZoneScan.Focus"/>), for <see cref="HoldMs"/>.
/// </para>
///
/// <para>
/// <b>Which zone next: the least watched, and the nearest among equals.</b> Zones are ranked by the seconds their ground has
/// been seen awake, in buckets of <see cref="Bucket"/> seconds, and within a bucket by distance from the walker — so a walker
/// sweeps a region of never-watched zones before crossing the map, and a zone the bots walk through every hour (already awake
/// by <see cref="BotWake"/>) waits for last. A round ends when every zone has been stood in once; the next begins at once.
/// Resumable: the awake seconds and the stops are kept across a restart (<see cref="BotZoneMemory"/>), so a new session picks
/// up with the zones the last one had not reached.
/// </para>
///
/// <para>
/// <b>It never disturbs the bots' own waking.</b> A sector is put back to sleep only if the tour woke it, no bot needs it
/// (<see cref="BotWake.Needed"/>) and no client is near; and <see cref="BotWake"/> in turn leaves alone a sector the tour holds
/// (<see cref="Holds"/>).
/// </para>
///
/// <para>
/// <b>The walkers are the debugger's, reached through two hooks.</b> The debugger lives in the minds' assembly, which references
/// this one and never the reverse; it fills <see cref="Walkers"/> (the watchers free to walk: a body, not witnessing a meeting,
/// not charting the dungeons) and <see cref="Move"/> (its own teleport) when it starts. With no debugger the tour stands still
/// and says so.
/// </para>
///
/// <para>
/// <b>One of them is the zone keeper while the zone watch runs (07.10.2026, Patrick: "one debugger goes wholly onto the
/// zones").</b> <see cref="Keeper"/> names it and <see cref="KeeperHook"/> says whether the squad has given it up to the zones;
/// it walks first, without a pause between stops, and chooses by what the accuracy counter most needs: a zone with no verdict in
/// the window first, then one whose last visit disagreed, then the oldest verdict. Every full hold by anybody is a check for
/// <see cref="BotZoneWatch"/>: a living attacker in the zone's own patrol, or none, against the level the zone is painted. The
/// tour walks only while the watch is on; when the watch ends it stops for every debugger.
/// </para>
/// </summary>
public static class BotZoneTour
{
    /// <summary>Whether the debuggers walk the zones. <c>do zones tour on|off</c> at the door.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often the tour's beat runs.</summary>
    public static int TickMs { get; set; } = 1000;

    /// <summary>How long a walker holds a zone awake. Past a minute so a patrol has moved: creatures wander a step every second or two.</summary>
    public static int HoldMs { get; set; } = 75000;

    /// <summary>How long a sector stays awake after the walker has gone, as the bots' wake keeps its own.</summary>
    public static int GraceMs { get; set; } = 30000;

    /// <summary>Sectors round the stand woken each way: two is the client's five by five.</summary>
    public static int Reach { get; set; } = 2;

    /// <summary>The most sectors one stop wakes, the zone's box and the stand's square together, nearest the stand kept.</summary>
    public static int MostSectors { get; set; } = 81;

    /// <summary>The most debuggers walking at once.</summary>
    public static int MostWalkers { get; set; } = 4;

    /// <summary>How often the held zone's box is passed by the scan while a walker holds it.</summary>
    public static int FocusMs { get; set; } = 2000;

    /// <summary>Whether sea zones are toured. Off: the land's patrols are what the paths need.</summary>
    public static bool Water { get; set; }

    /// <summary>Seconds of awake observation counted alike when choosing the next zone; nearest wins within a bucket.</summary>
    public static int Bucket { get; set; } = 60;

    /// <summary>
    /// The debugger that does nothing but the tour while the zone watch runs (07.10.2026). A name of the squad; a helper, never
    /// the lead or the marshal, whose duties nobody could take over. <c>Configuration/bot-zones.json</c> key <c>TourKeeper</c>.
    /// </summary>
    public static string Keeper { get; set; } = "Heimdall";

    /// <summary>The keeper the squad has actually given up to the zones now, or null. Filled by the debugger (BotVigil.KeeperName).</summary>
    public static Func<string> KeeperHook { get; set; }

    /// <summary>The name of the keeper on the zones now, or null.</summary>
    public static string KeeperNow() => Running && BotZoneWatch.On ? KeeperHook?.Invoke() : null;

    /// <summary>A zone the keeper's move to was refused is not chosen by it again for this long.</summary>
    public static int RefusedRestMs { get; set; } = 600000;

    /// <summary>The watchers free to walk now, with their bodies. Filled by the debugger (BotDebugModule); null without one.</summary>
    public static Func<IReadOnlyList<(string Name, Mobile Body)>> Walkers { get; set; }

    /// <summary>Puts a watcher's body at a place. Filled by the debugger with its own teleport.</summary>
    public static Func<Mobile, Map, Point3D, bool> Move { get; set; }

    /// <summary>Stops finished, stops cut short because the walker was called away, and moves the teleport refused.</summary>
    public static long Stops { get; private set; }

    public static long Abandoned { get; private set; }

    public static long Refused { get; private set; }

    /// <summary>Rounds finished since the start, and the round under way (one-based).</summary>
    public static int Round { get; private set; } = 1;

    /// <summary>Sectors the tour woke, put back to sleep, and left awake because a bot or a client needed them.</summary>
    public static long Woken { get; private set; }

    public static long PutBack { get; private set; }

    public static long LeftAwake { get; private set; }

    /// <summary>Zones stood in this round, and zones to stand in, as of the last beat.</summary>
    public static int Visited { get; private set; }

    public static int Total { get; private set; }

    private sealed class Stop
    {
        public string Name;

        public Mobile Body;

        public int Zone;

        public string Label;

        public Point3D At;

        public Rectangle2D Box;

        public long Began;

        public long Focused;

        /// <summary>Whether the keeper holds it, how many looks found a living attacker in the zone's own patrol, and how many looks.</summary>
        public bool Keeper;

        public int Seen;

        public int Looks;

        public readonly List<(int X, int Y)> Sectors = [];
    }

    private struct Held
    {
        public long Until;

        public bool Woke;
    }

    private static readonly Dictionary<Mobile, Stop> _stops = [];

    private static readonly Dictionary<(int X, int Y), Held> _held = [];

    private static readonly List<(int X, int Y)> _expired = [];

    private static readonly HashSet<int> _visited = [];

    private static readonly HashSet<int> _candidates = [];

    private static readonly List<(string Name, Mobile Body)> _walking = [];

    private static readonly List<Mobile> _dropped = [];

    private static readonly Dictionary<int, long> _refused = [];

    /// <summary>Full holds judged for the zone watch, and of the stops held to the end those the keeper held.</summary>
    public static long Judged { get; private set; }

    public static long KeeperStops { get; private set; }

    private static Map _map;

    public static void Open(long now)
    {
        _stops.Clear();
        _held.Clear();
        _visited.Clear();
        _walking.Clear();
        _refused.Clear();
        _map = null;
    }

    /// <summary>Whether the tour holds this sector awake now. Asked by <see cref="BotWake"/> before it puts one back to sleep.</summary>
    public static bool Holds(Map map, int x, int y) => map != null && map == _map && _held.ContainsKey((x, y));

    /// <summary>
    /// The bots' wake is letting go of a sector the tour still holds: the tour takes it over, and puts it back to sleep itself
    /// when its own grace runs out. False when the tour does not hold it.
    /// </summary>
    public static bool Adopt(Map map, int x, int y)
    {
        if (map == null || map != _map || !_held.TryGetValue((x, y), out var held))
        {
            return false;
        }

        _held[(x, y)] = new Held { Until = held.Until, Woke = true };

        return true;
    }

    /// <summary>Whether this body is on a stop now, and so is not to be moved on by the debugger's own vigil.</summary>
    public static bool Holding(Mobile body) => body != null && _stops.ContainsKey(body);

    /// <summary>The stop this body holds, in the words <see cref="Write"/> gives the zones page, or null. For the live window (02.10.2026).</summary>
    public static string StopOf(Mobile body) =>
        body != null && _stops.TryGetValue(body, out var stop)
            ? $"holding {Math.Min(HoldMs, Core.TickCount - stop.Began) / 1000}/{HoldMs / 1000} s at zone {stop.Zone} ({stop.Label})"
            : null;

    /// <summary>One beat. True when anything the dashboard shows changed.</summary>
    public static bool Beat(Map map, long now)
    {
        _map = map;

        var state = BotZones.State;
        // The tour is the zone watch's: when the watch has ended, nobody walks (07.10.2026). See BotZoneWatch.
        var walkers = Running && BotZoneWatch.On ? Walkers?.Invoke() : null;

        _walking.Clear();

        if (walkers == null || Move == null || state == null || state.Map != map)
        {
            var had = _stops.Count > 0;

            EndAll();
            LetGo(map, now);

            return had;
        }

        for (var i = 0; i < walkers.Count && _walking.Count < Math.Max(0, MostWalkers); i++)
        {
            if (walkers[i].Body is { Deleted: false })
            {
                _walking.Add(walkers[i]);
            }
        }

        // A walker the debugger no longer offers — called to witness a meeting, or charting — is let go where it stands.
        _dropped.Clear();

        foreach (var (body, _) in _stops)
        {
            if (!Offered(body))
            {
                _dropped.Add(body);
            }
        }

        for (var i = 0; i < _dropped.Count; i++)
        {
            _stops.Remove(_dropped[i]);
            Abandoned++;
        }

        Count(state);

        for (var i = 0; i < _walking.Count; i++)
        {
            var (name, body) = _walking[i];

            if (_stops.TryGetValue(body, out var stop))
            {
                if (now - stop.Began < HoldMs)
                {
                    Keep(map, stop, now);

                    continue;
                }

                Finish(stop, now);
                _stops.Remove(body);
            }

            Begin(map, state, name, body, now);
        }

        LetGo(map, now);

        return _walking.Count > 0 || _dropped.Count > 0;
    }

    private static bool Offered(Mobile body)
    {
        for (var i = 0; i < _walking.Count; i++)
        {
            if (_walking[i].Body == body)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A land patrol, or the sea's when toured; never a wanderers' zone, which has no spawner to learn and moves with them.</summary>
    private static bool Eligible(BotZone zone) => zone.Level >= BotZoneLevel.Hostile && !zone.Stray && (Water || !zone.Water);

    private static void Count(BotZoneState state)
    {
        _candidates.Clear();

        for (var i = 0; i < state.Zones.Length; i++)
        {
            if (Eligible(state.Zones[i]))
            {
                _candidates.Add(state.Zones[i].Id);
            }
        }

        var visited = 0;

        foreach (var id in _visited)
        {
            if (_candidates.Contains(id))
            {
                visited++;
            }
        }

        Total = _candidates.Count;
        Visited = visited;
    }

    private static void Begin(Map map, BotZoneState state, string name, Mobile body, long now)
    {
        var keeper = name != null && string.Equals(name, KeeperNow(), StringComparison.OrdinalIgnoreCase);
        var zone = keeper ? PickForKeeper(state, body, now) : Pick(state, body);

        if (zone == null && Total > 0 && Visited >= Total)
        {
            // Every zone stood in once: the round is over, and the next starts with the least watched again.
            Round++;
            _visited.Clear();
            Visited = 0;
            zone = Pick(state, body);
        }

        if (zone == null)
        {
            return;
        }

        var at = zone.Stand;

        if (!zone.StandIsReal)
        {
            at = new Point3D(at.X, at.Y, map.GetAverageZ(at.X, at.Y));
        }

        if (!Move(body, map, at) && (body.Map != map || body.Location != at))
        {
            // Not worth a second try this round: the next beat picks another.
            Refused++;
            _visited.Add(zone.Id);
            _refused[zone.Id] = now;

            return;
        }

        var stop = new Stop
        {
            Name = name,
            Body = body,
            Zone = zone.Id,
            Label = zone.Label,
            At = at,
            Began = now,
            Focused = now,
            Keeper = keeper
        };

        Sectors(map, zone, at, stop.Sectors);

        // The focus pass covers the woken ground and nothing else: a sleeping sector beyond it shows only creatures standing still.
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;

        for (var i = 0; i < stop.Sectors.Count; i++)
        {
            var (sx, sy) = stop.Sectors[i];

            x0 = Math.Min(x0, sx);
            y0 = Math.Min(y0, sy);
            x1 = Math.Max(x1, sx);
            y1 = Math.Max(y1, sy);
        }

        var side = 1 << Map.SectorShift;

        stop.Box = new Rectangle2D(x0 * side, y0 * side, (x1 - x0 + 1) * side, (y1 - y0 + 1) * side);
        _stops[body] = stop;
        Keep(map, stop, now);
    }

    /// <summary>The least-watched zone nobody else is standing in and nobody has stood in this round, nearest among equals.</summary>
    private static BotZone Pick(BotZoneState state, Mobile body)
    {
        BotZone best = null;
        var bestBucket = long.MaxValue;
        var bestDistance = long.MaxValue;
        var bucket = Math.Max(1, Bucket);

        for (var i = 0; i < state.Zones.Length; i++)
        {
            var zone = state.Zones[i];

            if (!Eligible(zone) || _visited.Contains(zone.Id) || Taken(zone.Id))
            {
                continue;
            }

            var b = (long)(zone.AwakeSeconds / bucket);
            var d = body.Map == state.Map ? Math.Max(Math.Abs(body.X - zone.CenterX), Math.Abs(body.Y - zone.CenterY)) : int.MaxValue;

            if (b < bestBucket || b == bestBucket && d < bestDistance)
            {
                best = zone;
                bestBucket = b;
                bestDistance = d;
            }
        }

        return best;
    }

    /// <summary>
    /// The keeper's choice (07.10.2026): what the accuracy counter most needs. Any land zone that is not wanderers', clear ones
    /// (hunting grounds) included, because "clear" is a claim too; nobody else's stop, and no zone whose move was refused lately.
    /// First a zone with no verdict inside the window, nearest first; then one whose last verdict disagreed, nearest first; then
    /// the longest unvisited. A zone held to the end in the last <see cref="BotZoneWatch.RecheckMinutes"/> — a refilling camp that
    /// gave no verdict among them — comes after all of those, so the keeper never stands in the same camp twice running.
    /// </summary>
    private static BotZone PickForKeeper(BotZoneState state, Mobile body, long now)
    {
        BotZone best = null;
        var bestTier = int.MaxValue;
        var bestOrder = long.MaxValue;
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var window = unix - Math.Max(1, BotZoneWatch.AccuracyWindowHours) * 3600L;
        var recheck = unix - Math.Max(0, BotZoneWatch.RecheckMinutes) * 60L;

        for (var i = 0; i < state.Zones.Length; i++)
        {
            var zone = state.Zones[i];

            if (zone.Stray || !Water && zone.Water || Taken(zone.Id)
                || _refused.TryGetValue(zone.Id, out var refused) && now - refused < RefusedRestMs)
            {
                continue;
            }

            long d = body.Map == state.Map ? Math.Max(Math.Abs(body.X - zone.CenterX), Math.Abs(body.Y - zone.CenterY)) : int.MaxValue;
            int tier;
            long order;

            var visited = BotZoneWatch.LastVisit(zone);

            if (visited > recheck)
            {
                tier = 3;
                order = visited;
            }
            else if (!BotZoneWatch.LastCheck(zone, out var at, out var agree) || at < window)
            {
                tier = 0;
                order = d;
            }
            else if (!agree)
            {
                tier = 1;
                order = d;
            }
            else
            {
                tier = 2;
                order = Math.Max(at, visited);
            }

            if (tier < bestTier || tier == bestTier && order < bestOrder)
            {
                best = zone;
                bestTier = tier;
                bestOrder = order;
            }
        }

        return best;
    }

    private static bool Taken(int id)
    {
        foreach (var (_, stop) in _stops)
        {
            if (stop.Zone == id)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The sectors of the stand's square and of the zone's box, nearest the stand first, at most <see cref="MostSectors"/>.</summary>
    private static void Sectors(Map map, BotZone zone, Point3D at, List<(int X, int Y)> into)
    {
        var width = map.Width >> Map.SectorShift;
        var height = map.Height >> Map.SectorShift;
        var cx = at.X >> Map.SectorShift;
        var cy = at.Y >> Map.SectorShift;
        var x0 = Math.Min(cx - Reach, (zone.X1 - zone.AggroRange) >> Map.SectorShift);
        var y0 = Math.Min(cy - Reach, (zone.Y1 - zone.AggroRange) >> Map.SectorShift);
        var x1 = Math.Max(cx + Reach, (zone.X2 + zone.AggroRange) >> Map.SectorShift);
        var y1 = Math.Max(cy + Reach, (zone.Y2 + zone.AggroRange) >> Map.SectorShift);
        List<(int X, int Y, int D)> all = [];

        for (var y = Math.Max(0, y0); y <= Math.Min(height - 1, y1); y++)
        {
            for (var x = Math.Max(0, x0); x <= Math.Min(width - 1, x1); x++)
            {
                all.Add((x, y, Math.Max(Math.Abs(x - cx), Math.Abs(y - cy))));
            }
        }

        all.Sort((a, b) => a.D.CompareTo(b.D));

        for (var i = 0; i < all.Count && i < Math.Max(1, MostSectors); i++)
        {
            into.Add((all[i].X, all[i].Y));
        }
    }

    /// <summary>Holds the stop's sectors awake for another grace and, when due, passes its box.</summary>
    private static void Keep(Map map, Stop stop, long now)
    {
        var until = now + GraceMs;

        for (var i = 0; i < stop.Sectors.Count; i++)
        {
            var (x, y) = stop.Sectors[i];
            var sector = map.GetRealSector(x, y);
            var woke = false;

            if (!sector.Active)
            {
                sector.Activate();
                woke = true;
                Woken++;
            }

            _held[(x, y)] = _held.TryGetValue((x, y), out var held)
                ? new Held { Until = until, Woke = held.Woke || woke }
                : new Held { Until = until, Woke = woke };
        }

        if (now - stop.Focused >= FocusMs)
        {
            stop.Focused = now;
            BotZoneScan.Focus(map, stop.Box, now);
            Look(map, stop);
        }
    }

    /// <summary>
    /// One look for the zone watch (07.10.2026): whether a living attacker stands in the zone's own patrol cells (not its fringe)
    /// on the map published now. Asleep or awake alike — a creature in a sleeping sector is there, only standing still. The
    /// same creatures the sweep counts as attackers: nobody's pet or summons, not townsfolk, attacking on sight. Once one is found
    /// the hold's answer is known and the looks stop.
    /// </summary>
    private static void Look(Map map, Stop stop)
    {
        var state = BotZones.State;

        if (stop.Seen > 0 || state == null || state.Map != map)
        {
            return;
        }

        stop.Looks++;

        var zone = BotZones.Find(stop.Zone);

        if (zone == null)
        {
            return;
        }

        foreach (var c in map.GetMobilesInBounds<BaseCreature>(new Rectangle2D(zone.X1, zone.Y1, zone.X2 - zone.X1 + 1, zone.Y2 - zone.Y1 + 1)))
        {
            if (c is not { Deleted: false, Alive: true } || c.Controlled || c.Summoned || c.Blessed || c.IsDeadBondedPet
                || !BotHaunts.AttacksFirst(c.FightMode) || BotHaunts.IsTownsfolk(c))
            {
                continue;
            }

            var cx = c.X >> BotZoneState.Shift;
            var cy = c.Y >> BotZoneState.Shift;

            if ((uint)cx >= (uint)state.Width || (uint)cy >= (uint)state.Height)
            {
                continue;
            }

            var v = state.Cells[cy * state.Width + cx];

            if (v != 0 && (v & BotZoneState.Fringe) == 0 && state.Zones[(v & BotZoneState.IndexMask) - 1].Id == stop.Zone)
            {
                stop.Seen++;

                return;
            }
        }
    }

    /// <summary>A stop held its time: the zone counts as stood in this round, and its spawners as toured.</summary>
    private static void Finish(Stop stop, long now)
    {
        Stops++;
        _visited.Add(stop.Zone);

        if (stop.Keeper)
        {
            KeeperStops++;
        }

        var zone = BotZones.Find(stop.Zone);

        if (zone == null)
        {
            return;
        }

        // The whole hold is the zone watch's check: the painted level against what stood in the patrol (07.10.2026). A hold in
        // which the map was never looked at (none published) is no check. See BotZoneWatch.Visited.
        if (stop.Looks > 0)
        {
            Judged++;
            BotZoneWatch.Visited(zone, stop.Seen > 0, stop.Seen == 0 ? RefillMinutes(zone) : 0.0);
        }

        var haunts = BotHaunts.All;
        var at = DateTime.UtcNow;

        for (var i = 0; i < haunts.Count; i++)
        {
            var haunt = haunts[i];

            if (haunt.At.X >= zone.X1 && haunt.At.X <= zone.X2 && haunt.At.Y >= zone.Y1 && haunt.At.Y <= zone.Y2)
            {
                haunt.Toured++;
                haunt.TouredAt = at;
            }
        }
    }

    /// <summary>
    /// Whether a spawner feeding the zone — standing on its ground — is running and below its count now (reviews of 07.10.2026),
    /// as twice the longest delay of the slowest such spawner in minutes, or nought: a camp hunted out and refilling, whose empty
    /// hold is no evidence against its colour while that long has not passed (see <see cref="BotZoneWatch.Visited"/>). With
    /// every spawner full, or none running, an empty hold is evidence: the creatures exist and are not where the map paints them.
    /// </summary>
    private static double RefillMinutes(BotZone zone)
    {
        var minutes = 0.0;
        var state = BotZones.State;
        var haunts = BotHaunts.All;

        for (var i = 0; i < haunts.Count; i++)
        {
            var haunt = haunts[i];

            if (haunt.At.X < zone.X1 || haunt.At.X > zone.X2 || haunt.At.Y < zone.Y1 || haunt.At.Y > zone.Y2
                || state?.Owner(haunt.At.X, haunt.At.Y)?.Id != zone.Id)
            {
                continue;
            }

            if (haunt.Spawner is { Deleted: false, Running: true } spawner && !spawner.IsFull)
            {
                minutes = Math.Max(minutes, Math.Max(1.0, 2.0 * spawner.MaxDelay.TotalMinutes));
            }
        }

        return minutes;
    }

    /// <summary>
    /// Every held sector whose grace has run out is let go, and put back to sleep only if the tour woke it, no bot needs it and
    /// no client is near: the bots' own waking is never undone from here.
    /// </summary>
    private static void LetGo(Map map, long now)
    {
        if (_held.Count == 0)
        {
            return;
        }

        _expired.Clear();

        foreach (var (key, held) in _held)
        {
            if (now - held.Until >= 0)
            {
                _expired.Add(key);
            }
        }

        for (var i = 0; i < _expired.Count; i++)
        {
            var key = _expired[i];
            var held = _held[key];

            _held.Remove(key);

            if (!held.Woke || map == null)
            {
                continue;
            }

            if (BotWake.Needed(map, key.X, key.Y) || BotWake.ClientNear(map, key.X, key.Y))
            {
                LeftAwake++;

                continue;
            }

            var sector = map.GetRealSector(key.X, key.Y);

            if (sector.Active)
            {
                sector.Deactivate();
                PutBack++;
            }
        }

        _expired.Clear();
    }

    private static void EndAll()
    {
        Abandoned += _stops.Count;
        _stops.Clear();
    }

    /// <summary>The zones are switched off: the walkers go back to the vigil and the held ground sleeps after its grace.</summary>
    public static void Release(long now)
    {
        EndAll();
        LetGo(_map, now);
    }

    /// <summary>For the dashboard: the round, the progress and every walker with where it stands and what it is doing.</summary>
    public static void Write(Utf8JsonWriter w)
    {
        var now = Core.TickCount;

        w.WriteStartObject();
        w.WriteBoolean("running", Running && BotZoneWatch.On && Walkers != null);
        w.WriteString("keeper", KeeperNow());
        w.WriteNumber("round", Round);
        w.WriteNumber("visited", Visited);
        w.WriteNumber("total", Total);
        w.WriteNumber("stops", Stops);
        w.WriteStartArray("walkers");

        for (var i = 0; i < _walking.Count; i++)
        {
            var (name, body) = _walking[i];

            if (body is not { Deleted: false })
            {
                continue;
            }

            w.WriteStartObject();
            w.WriteString("name", name);
            w.WriteStartArray("at");
            w.WriteNumberValue(body.X);
            w.WriteNumberValue(body.Y);
            w.WriteEndArray();

            if (_stops.TryGetValue(body, out var stop))
            {
                w.WriteStartArray("target");
                w.WriteNumberValue(stop.At.X);
                w.WriteNumberValue(stop.At.Y);
                w.WriteEndArray();
                w.WriteNumber("zone", stop.Zone);
                w.WriteString("state", $"{(stop.Keeper ? "zone keeper, " : "")}holding {Math.Min(HoldMs, now - stop.Began) / 1000}/{HoldMs / 1000} s at zone {stop.Zone} ({stop.Label})");
            }
            else
            {
                w.WriteNull("target");
                w.WriteNull("zone");
                w.WriteString("state", !Running ? "the tour is off" : !BotZoneWatch.On ? "the zone watch has ended" : "choosing the next zone");
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    public static string Describe()
    {
        var who = Walkers == null ? "no debugger to walk it" : Move == null ? "no way to move the debuggers" : $"{_walking.Count} walking";
        var now = Core.TickCount;
        var stops = new List<string>();

        foreach (var (_, stop) in _stops)
        {
            stops.Add($"{stop.Name} at zone {stop.Zone} {(now - stop.Began) / 1000}/{HoldMs / 1000}s{(stop.Seen > 0 ? " (an attacker seen)" : "")}");
        }

        return $"{(!Running ? "off" : !BotZoneWatch.On ? "stopped, the zone watch having ended" : "on")}, keeper {KeeperNow() ?? "none"} ({KeeperStops} of its stops held), round {Round}: {Visited} of {Total} zones stood in this round, {Stops} stops in all ({Abandoned} cut short, {Refused} refused moves, {Judged} judged for the zone watch); {who}{(stops.Count > 0 ? $" ({string.Join(", ", stops)})" : "")}; "
            + $"{_held.Count} sectors held now, {Woken} woken, {PutBack} put back to sleep, {LeftAwake} left awake for a bot or a client";
    }

    /// <summary>After a reload: every sector the tour woke is put back at once, unless a bot or a client needs it.</summary>
    public static void Forget(long now)
    {
        EndAll();

        foreach (var key in new List<(int X, int Y)>(_held.Keys))
        {
            _held[key] = new Held { Until = now, Woke = _held[key].Woke };
        }

        LetGo(_map, now);
        _held.Clear();
        _visited.Clear();
        _walking.Clear();
        _refused.Clear();
        Stops = Abandoned = Refused = 0;
        Judged = KeeperStops = 0;
        Woken = PutBack = LeftAwake = 0;
        Round = 1;
        Visited = Total = 0;
        _map = null;
    }
}
