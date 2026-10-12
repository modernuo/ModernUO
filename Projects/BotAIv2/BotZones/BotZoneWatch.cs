using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The zone watch: how often the danger map is right, counted both ways and in two parts over a rolling window, and the end of
/// the watching once both parts are right often enough.
///
/// <para>
/// <b>Patrick's order of 07.10.2026, evening: "if the debuggers are useful, we don't remove them; one debugger goes wholly onto
/// the zones, its other duties are shared out, we keep a counter of the zones' accuracy. As soon as zone accuracy is 99 percent
/// — we stop watching the zones and give the passed duties back."</b> The keeper is <see cref="BotZoneTour.Keeper"/>; this
/// class is the counter and the clock that ends its posting.
/// </para>
///
/// <para>
/// <b>Deaths</b> (<see cref="Died"/>), one check a death. Only a bot killed by a creature that attacks on sight counts — the
/// killer the engine credits is a <see cref="BaseCreature"/> nobody controls or summoned, not townsfolk, whose fight mode attacks
/// first; a bot's, a guard's, a pet's kill, a passive animal the bot set upon, or nothing named is no evidence and is counted
/// apart. Where it fell is read off the map published at that moment: clear ground (no patrol, no fringe; a hunting ground's
/// own level is clear too) counts against; hostile or deadly patrol counts for; a fringe counts for only while the zone it
/// borders has living attackers in that build, and apart otherwise — a fringe round an empty camp warned of nothing, and
/// painting more fringe must not buy agreement (reviews of 07.10.2026).
/// </para>
///
/// <para>
/// <b>Visits</b> (<see cref="Visited"/>), one vote a zone. Every tour stop held its whole <see cref="BotZoneTour.HoldMs"/>, by
/// the keeper or anyone, looks for a living attacker in the zone's own patrol cells. Hostile or deadly with one: agrees. Hostile
/// or deadly with none: against if every spawner feeding it is full (the creatures exist and are not where the map says),
/// apart as <em>respawning</em> if any is below its count (hunted out and refilling: painting it hostile is right, and the
/// hold proves nothing). Clear (a hunting ground; only the keeper stands in those) with an attacker: against; with none:
/// agrees. A zone's latest verdict replaces its earlier one, so the visit part is "zones whose latest verdict in the window
/// agrees, over zones with a verdict in the window", and re-checking a zone never adds weight to it.
/// </para>
///
/// <para>
/// <b>The end, and no automatic return.</b> Each part is judged on its own sample (<see cref="AccuracyMinDeaths"/> deaths,
/// <see cref="AccuracyMinZones"/> zones); the watch ends only when both are at <see cref="AccuracyTarget"/> or better for
/// <see cref="AccuracyHoldMinutes"/>. Then the tour stops for every debugger, the keeper's duties go back to it, and deaths go
/// on being counted. Afterwards the deaths alone, over <see cref="AlarmWindowHours"/> with <see cref="AlarmMinDeaths"/>, under
/// <see cref="ResumeBelow"/> for <see cref="ResumeAfterMinutes"/> raise an alarm in <c>alerts.ndjson</c> and do nothing else:
/// <c>do zonewatch on</c> restarts it, by hand. The deaths and the zones' verdicts are printed beside the painted patrol and
/// fringe, so a map that grows its painted ground to agree is seen to.
/// </para>
///
/// <para>
/// <b>Kept in <c>Data/</c>, not in <c>Saves/</c></b> (which is moved to a backup at every save), as <see cref="BotZoneMemory"/>
/// keeps the observations: the deaths, each zone's latest verdict and the watch's own state, read and written off the loop and
/// written once more at shutdown or a crash. A file that cannot be read is moved aside to <c>.bad</c> and said at warning.
/// </para>
/// </summary>
public static class BotZoneWatch
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotZoneWatch));

    private const int Shape = 2;

    private const string AlarmKind = "zones-accuracy";

    /// <summary>The rolling window both parts are counted over.</summary>
    public static int AccuracyWindowHours { get; set; } = 6;

    /// <summary>The fewest deaths in the window before the death part is judged.</summary>
    public static int AccuracyMinDeaths { get; set; } = 100;

    /// <summary>The fewest zones with a verdict in the window before the visit part is judged: about half the land's zones.</summary>
    public static int AccuracyMinZones { get; set; } = 200;

    /// <summary>The accuracy at which the zones need no more watching, each part.</summary>
    public static double AccuracyTarget { get; set; } = 0.99;

    /// <summary>How long both parts must hold the target, with their samples, before the watch ends.</summary>
    public static int AccuracyHoldMinutes { get; set; } = 60;

    /// <summary>After the end: the deaths' accuracy under this, held for <see cref="ResumeAfterMinutes"/>, raises an alarm.</summary>
    public static double ResumeBelow { get; set; } = 0.97;

    public static int ResumeAfterMinutes { get; set; } = 60;

    /// <summary>The alarm's own window and sample: deaths alone, the tour having stopped, over a span a quiet night still fills.</summary>
    public static int AlarmWindowHours { get; set; } = 12;

    public static int AlarmMinDeaths { get; set; } = 100;

    /// <summary>
    /// How long an empty hostile zone with a spawner below its count is excused as refilling, at least: past the longer of this
    /// and twice the slowest such spawner's longest delay since it was first held empty, an empty hold counts against (second
    /// reviews of 07.10.2026: zone 4, skeletons SE of Yew, 0 of 10 alive after 83,598 awake seconds, was excused for ever).
    /// </summary>
    public static int RespawnGraceMinutes { get; set; } = 30;

    /// <summary>A zone whose last visit disagreed is not stood in again by the keeper sooner than this.</summary>
    public static int RecheckMinutes { get; set; } = 30;

    /// <summary>The file, under the Distribution folder, and how often it is written.</summary>
    public static string File { get; set; } = "Data/bot-zones-accuracy.json";

    public static int SaveEveryMs { get; set; } = 300000;

    /// <summary>The most deaths held; the oldest go first.</summary>
    public static int MostDeaths { get; set; } = 50000;

    /// <summary>Whether the zones are being watched. Turned by the verdict, or by hand (<see cref="Turn"/>); never a dial.</summary>
    public static bool On { get; private set; } = true;

    /// <summary>When the watch ended (unix seconds, nought while it runs), and by what.</summary>
    public static long EndedAt { get; private set; }

    public static string EndedBy { get; private set; } = "";

    /// <summary>A line for Argus's feed. Filled by the debugger, which writes it into its own log under the lead's name.</summary>
    public static Action<string> Noted { get; set; }

    /// <summary>Changes the dashboard has not been shown yet. See <see cref="BotZones"/>' JSON.</summary>
    public static long Version { get; private set; }

    // ---- Since the start: what was heard and why some of it is no evidence.
    public static long DeathsHeard { get; private set; }

    public static long DeathsNotByCreature { get; private set; }

    public static long DeathsUnmapped { get; private set; }

    /// <summary>Deaths in the fringe of a zone that had no living attacker in the build: counted apart.</summary>
    public static long DeathsFringeOfEmpty { get; private set; }

    public static long VisitsHeard { get; private set; }

    /// <summary>Empty holds in a hostile zone with a spawner below its count: refilling, counted apart.</summary>
    public static long VisitsRespawning { get; private set; }

    public static long Alarms { get; private set; }

    private readonly record struct Death(long At, bool Agree, int X, int Y);

    private static readonly List<Death> _deaths = [];

    /// <summary>Each zone's latest verdict, by its stand (a spawner's tile, steadier than a zone number across restarts).</summary>
    private static readonly Dictionary<long, (long At, bool Agree)> _zones = [];

    /// <summary>Every full hold's time by zone, respawning ones included, so the keeper does not stand in a refilling camp again at once.</summary>
    private static readonly Dictionary<long, long> _visitedAt = [];

    /// <summary>When each hostile zone was first held empty, cleared when an attacker is seen there; and when its last hold was excused.</summary>
    private static readonly Dictionary<long, long> _emptySince = [];

    private static readonly Dictionary<long, long> _excusedAt = [];

    /// <summary>The stands of the zones in the build the verdicts were last counted against, and that build.</summary>
    private static readonly HashSet<long> _current = [];

    private static BotZoneState _currentOf;

    // ---- The parts, recounted when stale: deaths and zones in the accuracy window, deaths in the alarm's.
    private static int _deathsAgree, _deathsAll, _zonesAgree, _zonesAll, _alarmAgree, _alarmAll;

    private static bool _stale = true;

    private static (int, int, int, int, int, int) _parts;

    private static long _aboveSince, _belowSince;

    private static long _judgedTick, _savedTick;

    private static bool _started, _read, _writing, _dirty, _byHand, _hooked;

    private static string _loaded = "nothing read yet";

    private static string PathOf() => System.IO.Path.Combine(Core.BaseDirectory, File);

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private static long Key(Point3D stand) => ((long)stand.X << 20) | (uint)(stand.Y & 0xFFFFF);

    // ---------------------------------------------------------------------------------------------------------------------
    // The parts
    // ---------------------------------------------------------------------------------------------------------------------

    private static void Recount()
    {
        if (!_stale)
        {
            return;
        }

        _stale = false;

        var now = Now();
        var window = now - Math.Max(1, AccuracyWindowHours) * 3600L;
        var alarm = now - Math.Max(1, AlarmWindowHours) * 3600L;

        _deathsAgree = _deathsAll = _zonesAgree = _zonesAll = _alarmAgree = _alarmAll = 0;

        for (var i = 0; i < _deaths.Count; i++)
        {
            var d = _deaths[i];

            if (d.At >= window)
            {
                _deathsAll++;
                _deathsAgree += d.Agree ? 1 : 0;
            }

            if (d.At >= alarm)
            {
                _alarmAll++;
                _alarmAgree += d.Agree ? 1 : 0;
            }
        }

        // Only zones of the build published now: a zone retired by a rebuild (the sea camps once painted as land) leaves the
        // visit part at once rather than when its verdict ages out (second reviews of 07.10.2026).
        var state = BotZones.State;

        if (!ReferenceEquals(state, _currentOf))
        {
            _currentOf = state;
            _current.Clear();

            for (var i = 0; i < (state?.Zones.Length ?? 0); i++)
            {
                _current.Add(Key(state.Zones[i].Stand));
            }
        }

        foreach (var (key, last) in _zones)
        {
            if (last.At >= window && _current.Contains(key))
            {
                _zonesAll++;
                _zonesAgree += last.Agree ? 1 : 0;
            }
        }
    }

    /// <summary>Zones whose latest hold was excused as refilling, within the window: set apart from the visit part now.</summary>
    public static int ExcusedZones
    {
        get
        {
            var window = Now() - Math.Max(1, AccuracyWindowHours) * 3600L;
            var n = 0;

            foreach (var (key, at) in _excusedAt)
            {
                if (at >= window && (!_zones.TryGetValue(key, out var last) || last.At < at))
                {
                    n++;
                }
            }

            return n;
        }
    }

    private static double Share(int agree, int all) => all == 0 ? 0.0 : (double)agree / all;

    /// <summary>The death part: agreements over deaths in the window.</summary>
    public static double DeathAccuracy
    {
        get
        {
            Recount();

            return Share(_deathsAgree, _deathsAll);
        }
    }

    /// <summary>The visit part: zones whose latest verdict in the window agrees, over zones with a verdict in the window.</summary>
    public static double VisitAccuracy
    {
        get
        {
            Recount();

            return Share(_zonesAgree, _zonesAll);
        }
    }

    /// <summary>Both parts as one share, for the eye; the verdict reads the two apart.</summary>
    public static double Accuracy
    {
        get
        {
            Recount();

            return Share(_deathsAgree + _zonesAgree, _deathsAll + _zonesAll);
        }
    }

    /// <summary>
    /// A bot died. From <c>BotMobile.OnDeath</c> beside <see cref="BotZones.Fell"/>, with the place taken before anything moved
    /// the ghost and the killer the engine credits.
    /// </summary>
    public static void Died(Map map, Point3D at, Mobile killer)
    {
        var state = BotZones.State;

        if (!BotZones.Running || map == null || map != BotZones.Map)
        {
            return;
        }

        DeathsHeard++;

        if (!ByCreature(killer))
        {
            DeathsNotByCreature++;

            return;
        }

        if (state == null || state.Map != map)
        {
            DeathsUnmapped++;

            return;
        }

        // The map as it stood when the bot fell: the build this death will promote a zone in comes after it.
        var level = BotZones.LevelAt(map, at.X, at.Y);

        if (level == BotZoneLevel.Edge && state.Owner(at.X, at.Y) is { Aggressive: <= 0 })
        {
            DeathsFringeOfEmpty++;

            return;
        }

        _deaths.Add(new Death(Now(), level != BotZoneLevel.Clear, at.X, at.Y));

        if (_deaths.Count > Math.Max(1000, MostDeaths))
        {
            _deaths.RemoveRange(0, _deaths.Count - Math.Max(1000, MostDeaths));
        }

        _stale = true;
        Version++;
    }

    /// <summary>
    /// Whether a killer is the kind of thing the danger map is about: a creature nobody controls or summoned, not townsfolk,
    /// that attacks on sight. A bear the bot set upon itself, a bot's pet or another bot says nothing about the map.
    /// </summary>
    public static bool ByCreature(Mobile killer) =>
        killer is BaseCreature { Deleted: false } c && !c.Controlled && !c.Summoned && !BotHaunts.IsTownsfolk(c)
        && BotHaunts.AttacksFirst(c.FightMode);

    /// <summary>
    /// A tour stop held its whole time: <paramref name="seen"/> is whether a living attacker stood in the zone's own patrol at any
    /// of its looks; <paramref name="refillMinutes"/> is nought when every spawner feeding it is full (or none runs), else twice
    /// the longest delay of the slowest one below its count. Judged against the zone's level in the build published then; the
    /// zone's latest verdict replaces its earlier one. An empty hostile zone that is refilling is excused only while its emptiness
    /// is younger than the longer of <paramref name="refillMinutes"/> and <see cref="RespawnGraceMinutes"/>.
    /// </summary>
    public static void Visited(BotZone zone, bool seen, double refillMinutes)
    {
        if (zone == null || zone.Stray)
        {
            return;
        }

        var key = Key(zone.Stand);
        var now = Now();

        VisitsHeard++;
        _visitedAt[key] = now;

        bool agree;

        if (zone.Level >= BotZoneLevel.Hostile)
        {
            if (seen)
            {
                _emptySince.Remove(key);
            }
            else
            {
                if (!_emptySince.TryGetValue(key, out var since))
                {
                    _emptySince[key] = since = now;
                }

                var grace = Math.Max(Math.Max(0, RespawnGraceMinutes), refillMinutes) * 60.0;

                if (refillMinutes > 0 && now - since < grace)
                {
                    // Hunted out and refilling, and not for long: no evidence either way, and the zone's last verdict stands.
                    VisitsRespawning++;
                    _excusedAt[key] = now;

                    return;
                }
            }

            agree = seen;
        }
        else
        {
            agree = !seen;
        }

        _zones[key] = (now, agree);
        _excusedAt.Remove(key);
        _stale = true;
        Version++;
    }

    /// <summary>When the zone was last held to the end, verdict or none (unix seconds; nought for never this process). For the keeper's choice.</summary>
    public static long LastVisit(BotZone zone) => zone != null && _visitedAt.TryGetValue(Key(zone.Stand), out var at) ? at : 0;

    /// <summary>The zone's latest visit verdict, if it has one: when (unix seconds) and whether it agreed.</summary>
    public static bool LastCheck(BotZone zone, out long at, out bool agree)
    {
        if (zone != null && _zones.TryGetValue(Key(zone.Stand), out var last))
        {
            at = last.At;
            agree = last.Agree;

            return true;
        }

        at = 0;
        agree = false;

        return false;
    }

    /// <summary>Drops the deaths older than both windows.</summary>
    private static void Prune(long now)
    {
        var from = now - Math.Max(Math.Max(1, AccuracyWindowHours), Math.Max(1, AlarmWindowHours)) * 3600L;
        var first = 0;

        while (first < _deaths.Count && _deaths[first].At < from)
        {
            first++;
        }

        if (first > 0)
        {
            _deaths.RemoveRange(0, first);
        }

        // Counted afresh every judgement: a check leaving the window changes the parts though nothing was added.
        _stale = true;
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // The verdict
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>One beat, from the zones' own: the windows judged every few seconds, the file written when due.</summary>
    public static void Beat(long now)
    {
        if (!_started)
        {
            return;
        }

        if (now - _judgedTick >= 5000)
        {
            _judgedTick = now;
            Judge(Now());
        }

        if ((_dirty || now - _savedTick >= SaveEveryMs) && !BotZones.SaveUnderWay())
        {
            _savedTick = now;
            Save();
        }
    }

    /// <summary>Both parts have their samples and are at the target or better.</summary>
    private static bool BothAtTarget() =>
        _deathsAll >= Math.Max(1, AccuracyMinDeaths) && _zonesAll >= Math.Max(1, AccuracyMinZones)
        && Share(_deathsAgree, _deathsAll) >= AccuracyTarget && Share(_zonesAgree, _zonesAll) >= AccuracyTarget;

    private static void Judge(long now)
    {
        Prune(now);
        Recount();

        // The page is told only when a part moved: a death or a verdict leaving the window changes it with nothing added.
        var parts = (_deathsAgree, _deathsAll, _zonesAgree, _zonesAll, _alarmAgree, _alarmAll);

        if (parts != _parts)
        {
            _parts = parts;
            Version++;
        }

        if (On)
        {
            _belowSince = 0;

            if (BothAtTarget())
            {
                if (_aboveSince == 0)
                {
                    _aboveSince = now;
                    _dirty = true;
                }

                if (now - _aboveSince >= Math.Max(0, AccuracyHoldMinutes) * 60L)
                {
                    End("the verdict", now);
                }
            }
            else if (_aboveSince != 0)
            {
                _aboveSince = 0;
                _dirty = true;
            }

            return;
        }

        // After the end the tour is still and only the deaths speak: judged over their own window and sample.
        _aboveSince = 0;

        var alarm = Share(_alarmAgree, _alarmAll);

        if (_alarmAll >= Math.Max(1, AlarmMinDeaths) && alarm < ResumeBelow)
        {
            if (_belowSince == 0)
            {
                _belowSince = now;
                _dirty = true;
            }

            if (now - _belowSince >= Math.Max(0, ResumeAfterMinutes) * 60L)
            {
                if (!BotAlarm.Up(AlarmKind))
                {
                    Alarms++;
                }

                BotAlarm.Raise(
                    AlarmKind,
                    $"the danger zones warned of {alarm:P1} of the deaths by creatures over {AlarmWindowHours}h ({_alarmAgree}/{_alarmAll}), under {ResumeBelow:P0} for {(now - _belowSince) / 60} minutes since the zone watch ended; say do zonewatch on to put the keeper back on them",
                    _alarmAgree,
                    _alarmAll,
                    $"{AlarmWindowHours}h"
                );
            }
        }
        else
        {
            if (_belowSince != 0)
            {
                _belowSince = 0;
                _dirty = true;
            }

            if (BotAlarm.Up(AlarmKind))
            {
                BotAlarm.Clear(AlarmKind, $"the danger zones warned of {alarm:P1} of the deaths by creatures again ({_alarmAgree}/{_alarmAll})", _alarmAgree, _alarmAll, $"{AlarmWindowHours}h");
            }
        }
    }

    private static void End(string by, long now)
    {
        On = false;
        EndedAt = now;
        EndedBy = by;
        _aboveSince = 0;
        _belowSince = 0;
        _dirty = true;
        Version++;

        var line = $"the zone watch ended at {Clock(now)} by {by}: {Parts()}; the tour stops for every debugger, and {Keeper()}'s duties go back to it. The deaths go on being counted; theirs under {ResumeBelow:P0} over {AlarmWindowHours}h for {ResumeAfterMinutes} minutes raises an alarm, and do zonewatch on starts it again.";

        logger.Information("Zones: {Line}", line);
        Noted?.Invoke(line);
    }

    /// <summary>The door's <c>zonewatch on|off|state</c>.</summary>
    public static string Turn(string word)
    {
        var now = Now();

        if (word.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            if (On)
            {
                return $"the zone watch is already on. {Describe()}";
            }

            On = true;
            EndedAt = 0;
            EndedBy = "";
            _aboveSince = 0;
            _belowSince = 0;
            _byHand = true;
            _dirty = true;
            Version++;

            if (BotAlarm.Up(AlarmKind))
            {
                BotAlarm.Clear(AlarmKind, "the zone watch was started again by hand", _alarmAgree, _alarmAll, $"{AlarmWindowHours}h");
            }

            var line = $"the zone watch is on again by hand at {Clock(now)}: {Keeper()} goes back on the zones, its duties shared out again, and the tour walks.";

            logger.Information("Zones: {Line}", line);
            Noted?.Invoke(line);

            return $"{line} {Describe()}";
        }

        if (word.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            if (!On)
            {
                return $"the zone watch is already off. {Describe()}";
            }

            _byHand = true;
            End("hand", now);

            return Describe();
        }

        return $"{Describe()} Say zonewatch on or zonewatch off.";
    }

    private static string Keeper() => string.IsNullOrWhiteSpace(BotZoneTour.Keeper) ? "the keeper" : BotZoneTour.Keeper.Trim();

    private static string Clock(long unix) => DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string Pct(int agree, int all) => all == 0 ? "–" : $"{agree * 100.0 / all:F1} %";

    /// <summary>The two parts and the combined share, each with its counts and its sample.</summary>
    private static string Parts()
    {
        Recount();

        return $"deaths {Pct(_deathsAgree, _deathsAll)} ({_deathsAgree}/{_deathsAll}{(_deathsAll < AccuracyMinDeaths ? $", under {AccuracyMinDeaths}" : "")}), "
               + $"visits {Pct(_zonesAgree, _zonesAll)} ({_zonesAgree}/{_zonesAll} zones{(_zonesAll < AccuracyMinZones ? $", under {AccuracyMinZones}" : "")}), "
               + $"combined {Pct(_deathsAgree + _zonesAgree, _deathsAll + _zonesAll)}, window {AccuracyWindowHours}h";
    }

    /// <summary>The painted ground the shares were earned on: land patrol and fringe, in tiles.</summary>
    private static string Painted()
    {
        var state = BotZones.State;

        if (state == null)
        {
            return "no map yet";
        }

        var tiles = BotZoneState.Cell * BotZoneState.Cell;
        var patrol = (long)state.LandCoreCells * tiles;
        var fringe = Math.Max(0, (long)state.PaintedCells * tiles - patrol);

        return $"painted {patrol / 1000}k patrol + {fringe / 1000}k fringe tiles";
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // Words and the dashboard
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The clause the <c>Zones:</c> line carries: <c>accuracy: deaths 94.5 % (120/127), visits 93.0 % (186/200 zones), combined
    /// 93.8 %, window 6h; 14 holds respawning, 3 fringe deaths of empty zones apart; painted …; keeper Heimdall on zones</c>, or
    /// <c>watch ended at 23:40</c> in place of the keeper.
    /// </summary>
    public static string Summary()
    {
        var who = On
            ? BotZoneTour.KeeperNow() is { } keeper ? $"keeper {keeper} on zones" : $"watch on, no keeper ({KeeperWhy()})"
            : $"watch ended at {Clock(EndedAt)}";

        var held = On && _aboveSince != 0 ? $"; both at {AccuracyTarget:P0} or better for {(Now() - _aboveSince) / 60} of {AccuracyHoldMinutes} min" : "";

        return $"accuracy: {Parts()}; {ExcusedZones} zones excused as respawning now ({VisitsRespawning} holds), {DeathsFringeOfEmpty} fringe deaths of empty zones apart; {Painted()}{held}; {who}";
    }

    private static string KeeperWhy() =>
        !BotZoneTour.Running ? "the tour is off" : BotZoneTour.Walkers == null ? "no debugger" : $"{Keeper()} is not a helper of the squad with a body";

    /// <summary>The five-minute sentence: the summary, what was heard and set apart, and the clock towards the end or the alarm.</summary>
    public static string Describe()
    {
        Recount();

        var after = On
            ? ""
            : $" Ended by {EndedBy}; the alarm reads deaths over {AlarmWindowHours}h: {Pct(_alarmAgree, _alarmAll)} ({_alarmAgree}/{_alarmAll}{(_alarmAll < AlarmMinDeaths ? $", under the {AlarmMinDeaths} it needs" : "")}), "
              + $"{(_belowSince != 0 ? $"under {ResumeBelow:P0} for {(Now() - _belowSince) / 60} of {ResumeAfterMinutes} min" : $"not under {ResumeBelow:P0}")}{(BotAlarm.Up(AlarmKind) ? ", the alarm standing" : "")}.";

        return $"Zone watch {Summary()}. Since the start: {DeathsHeard} deaths heard ({DeathsNotByCreature} not by an attacking creature, {DeathsUnmapped} before a map, {DeathsFringeOfEmpty} in the fringe of an empty zone), {VisitsHeard} full holds ({VisitsRespawning} respawning); {_zones.Count} zones with a verdict ever; the file {_loaded}.{after}";
    }

    /// <summary>The same as an object: <c>watch</c> in <c>/api/zones</c>, <c>zoneWatch</c> in <c>/api/state</c>. Game loop only.</summary>
    public static void Write(Utf8JsonWriter w)
    {
        Recount();

        var state = BotZones.State;
        var tiles = BotZoneState.Cell * BotZoneState.Cell;

        w.WriteStartObject();
        w.WriteBoolean("on", On);
        w.WriteString("keeper", BotZoneTour.KeeperNow());
        w.WriteNumber("accuracy", Math.Round(Share(_deathsAgree + _zonesAgree, _deathsAll + _zonesAll), 4));
        w.WriteStartObject("deaths");
        w.WriteNumber("accuracy", Math.Round(Share(_deathsAgree, _deathsAll), 4));
        w.WriteNumber("agree", _deathsAgree);
        w.WriteNumber("all", _deathsAll);
        w.WriteNumber("min", AccuracyMinDeaths);
        w.WriteNumber("fringeOfEmpty", DeathsFringeOfEmpty);
        w.WriteNumber("notByCreature", DeathsNotByCreature);
        w.WriteEndObject();
        w.WriteStartObject("visits");
        w.WriteNumber("accuracy", Math.Round(Share(_zonesAgree, _zonesAll), 4));
        w.WriteNumber("agree", _zonesAgree);
        w.WriteNumber("zones", _zonesAll);
        w.WriteNumber("min", AccuracyMinZones);
        w.WriteNumber("respawning", VisitsRespawning);
        w.WriteNumber("excusedZones", ExcusedZones);
        w.WriteNumber("respawnGraceMinutes", RespawnGraceMinutes);
        w.WriteEndObject();
        w.WriteNumber("windowHours", AccuracyWindowHours);
        w.WriteNumber("target", AccuracyTarget);
        w.WriteNumber("holdMinutes", AccuracyHoldMinutes);
        w.WriteNumber("heldMinutes", On && _aboveSince != 0 ? (Now() - _aboveSince) / 60 : 0);
        w.WriteNumber("paintedPatrolTiles", (long)(state?.LandCoreCells ?? 0) * tiles);
        w.WriteNumber("paintedFringeTiles", Math.Max(0, (long)(state?.PaintedCells ?? 0) * tiles - (long)(state?.LandCoreCells ?? 0) * tiles));
        w.WriteStartObject("alarm");
        w.WriteNumber("resumeBelow", ResumeBelow);
        w.WriteNumber("windowHours", AlarmWindowHours);
        w.WriteNumber("agree", _alarmAgree);
        w.WriteNumber("all", _alarmAll);
        w.WriteNumber("min", AlarmMinDeaths);
        w.WriteBoolean("up", BotAlarm.Up(AlarmKind));
        w.WriteEndObject();

        if (EndedAt > 0)
        {
            w.WriteString("endedAt", Clock(EndedAt));
        }
        else
        {
            w.WriteNull("endedAt");
        }

        w.WriteString("endedBy", EndedBy);
        w.WriteString("line", Summary());
        w.WriteEndObject();
    }

    /// <summary>The <c>zoneWatch</c> block of <c>/api/state</c>.</summary>
    public static void Web(Utf8JsonWriter w)
    {
        w.WritePropertyName("zoneWatch");
        Write(w);
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // The file
    // ---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Starts reading the file off the loop, once a process: a world reload keeps the window in memory (it is about the map,
    /// not the population), and a second read would put the old file over what was counted since. Hooks the shutdown and the
    /// crash, which write it once more.
    /// </summary>
    public static void Load()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _savedTick = Core.TickCount;

        if (!_hooked)
        {
            _hooked = true;
            EventSink.Shutdown += SaveNow;
            EventSink.ServerCrashed += _ => SaveNow();
        }

        var path = PathOf();

        _ = Task.Run(
            () =>
            {
                if (!System.IO.File.Exists(path))
                {
                    Core.LoopContext.Post(() => Apply(null, "no file yet"));

                    return;
                }

                try
                {
                    var read = Parse(System.IO.File.ReadAllBytes(path)) ?? throw new InvalidOperationException($"not of shape {Shape}");

                    Core.LoopContext.Post(() => Apply(read, null));
                }
                catch (Exception e)
                {
                    // Kept as evidence rather than written over at the next save: an ended watch read as nothing would start again.
                    var moved = false;

                    try
                    {
                        System.IO.File.Move(path, path + ".bad", true);
                        moved = true;
                    }
                    catch
                    {
                        // Said below; the next save writes over it.
                    }

                    Core.LoopContext.Post(
                        () =>
                        {
                            logger.Warning(
                                "Zones: {File} could not be read ({Message}); {Moved}, and the zone watch starts on with an empty window",
                                File,
                                e.Message,
                                moved ? $"moved aside to {File}.bad" : "it could not be moved aside"
                            );
                            Apply(null, $"unreadable ({e.Message}), {(moved ? "moved to .bad" : "not moved")}");
                        }
                    );
                }
            }
        );
    }

    private sealed class Read
    {
        public bool On = true;

        public long SavedAt;

        public long EndedAt;

        public string EndedBy = "";

        public long AboveSince;

        public long BelowSince;

        public readonly List<Death> Deaths = [];

        public readonly List<(long Key, long At, bool Agree)> Zones = [];

        public readonly List<(long Key, long Since)> Empty = [];
    }

    private static Read Parse(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;

        if (!root.TryGetProperty("shape", out var shape) || shape.GetInt32() != Shape)
        {
            return null;
        }

        var read = new Read
        {
            On = root.GetProperty("on").GetBoolean(),
            SavedAt = root.GetProperty("savedAt").GetInt64(),
            EndedAt = root.GetProperty("endedAt").GetInt64(),
            EndedBy = root.GetProperty("endedBy").GetString() ?? "",
            AboveSince = root.GetProperty("aboveSince").GetInt64(),
            BelowSince = root.GetProperty("belowSince").GetInt64()
        };

        foreach (var d in root.GetProperty("deaths").EnumerateArray())
        {
            read.Deaths.Add(new Death(d[0].GetInt64(), d[1].GetInt32() != 0, d[2].GetInt32(), d[3].GetInt32()));
        }

        foreach (var z in root.GetProperty("zones").EnumerateArray())
        {
            read.Zones.Add((z[0].GetInt64(), z[1].GetInt64(), z[2].GetInt32() != 0));
        }

        if (root.TryGetProperty("empty", out var empty))
        {
            foreach (var e in empty.EnumerateArray())
            {
                read.Empty.Add((e[0].GetInt64(), e[1].GetInt64()));
            }
        }

        return read;
    }

    /// <summary>What was read, merged under what this session has counted already; the watch's state taken unless a hand turned it first.</summary>
    private static void Apply(Read read, string why)
    {
        _read = true;

        if (read == null)
        {
            _loaded = why;
            logger.Information("Zones: the zone watch starts {State} with an empty window ({Why})", On ? "on" : "off", why);

            return;
        }

        _deaths.InsertRange(0, read.Deaths);
        _deaths.Sort((a, b) => a.At.CompareTo(b.At));

        for (var i = 0; i < read.Zones.Count; i++)
        {
            var (key, at, agree) = read.Zones[i];

            if (!_zones.TryGetValue(key, out var had) || had.At < at)
            {
                _zones[key] = (at, agree);
            }
        }

        for (var i = 0; i < read.Empty.Count; i++)
        {
            var (key, since) = read.Empty[i];

            if (!_emptySince.TryGetValue(key, out var had) || since < had)
            {
                _emptySince[key] = since;
            }
        }

        if (!_byHand)
        {
            // The clocks towards the end and towards the alarm count minutes the shard was up and measuring: moved on by the time
            // since the file was written, so a restart neither breaks a hold nor counts the downtime as held.
            var down = Math.Max(0, Now() - read.SavedAt);

            On = read.On;
            EndedAt = read.EndedAt;
            EndedBy = read.EndedBy;
            _aboveSince = read.On && read.AboveSince > 0 ? read.AboveSince + down : 0;
            _belowSince = !read.On && read.BelowSince > 0 ? read.BelowSince + down : 0;
        }

        Prune(Now());

        _loaded = $"read {read.Deaths.Count} deaths and {read.Zones.Count} zone verdicts";

        logger.Information(
            "Zones: the zone watch is {State}; the window is back from {File}: {Deaths} deaths and {Zones} zone verdicts. {Summary}",
            On ? "on" : $"ended (at {Clock(EndedAt)} by {EndedBy})",
            File,
            read.Deaths.Count,
            read.Zones.Count,
            Summary()
        );
    }

    private sealed record Snapshot(
        bool On, long EndedAt, string EndedBy, long Above, long Below, Death[] Deaths, (long Key, long At, bool Agree)[] Zones,
        (long Key, long Since)[] Empty
    );

    private static Snapshot Take()
    {
        var zones = new (long, long, bool)[_zones.Count];
        var i = 0;

        foreach (var (key, last) in _zones)
        {
            zones[i++] = (key, last.At, last.Agree);
        }

        var empty = new (long, long)[_emptySince.Count];

        i = 0;

        foreach (var (key, since) in _emptySince)
        {
            empty[i++] = (key, since);
        }

        return new Snapshot(On, EndedAt, EndedBy, _aboveSince, _belowSince, _deaths.ToArray(), zones, empty);
    }

    /// <summary>Writes a snapshot beside the file and moves it over, so a stop mid-write leaves the last good one. Off the loop or at shutdown.</summary>
    private static void WriteFile(Snapshot s, string path, string temp)
    {
        var buffer = new ArrayBufferWriter<byte>(64 * 1024);

        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteNumber("shape", Shape);
            w.WriteNumber("savedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            w.WriteBoolean("on", s.On);
            w.WriteNumber("endedAt", s.EndedAt);
            w.WriteString("endedBy", s.EndedBy);
            w.WriteNumber("aboveSince", s.Above);
            w.WriteNumber("belowSince", s.Below);
            w.WriteStartArray("deaths");

            for (var i = 0; i < s.Deaths.Length; i++)
            {
                var d = s.Deaths[i];

                w.WriteStartArray();
                w.WriteNumberValue(d.At);
                w.WriteNumberValue(d.Agree ? 1 : 0);
                w.WriteNumberValue(d.X);
                w.WriteNumberValue(d.Y);
                w.WriteEndArray();
            }

            w.WriteEndArray();
            w.WriteStartArray("zones");

            for (var i = 0; i < s.Zones.Length; i++)
            {
                var (key, at, agree) = s.Zones[i];

                w.WriteStartArray();
                w.WriteNumberValue(key);
                w.WriteNumberValue(at);
                w.WriteNumberValue(agree ? 1 : 0);
                w.WriteEndArray();
            }

            w.WriteEndArray();
            w.WriteStartArray("empty");

            for (var i = 0; i < s.Empty.Length; i++)
            {
                w.WriteStartArray();
                w.WriteNumberValue(s.Empty[i].Key);
                w.WriteNumberValue(s.Empty[i].Since);
                w.WriteEndArray();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        System.IO.File.WriteAllBytes(temp, buffer.WrittenSpan.ToArray());
        System.IO.File.Move(temp, path, true);
    }

    /// <summary>Copies the window on the loop; writes the JSON and the file off it. Never before the read.</summary>
    public static void Save()
    {
        if (!_started || !_read || _writing)
        {
            return;
        }

        _dirty = false;

        var snapshot = Take();
        var path = PathOf();

        _writing = true;

        _ = Task.Run(
            () =>
            {
                try
                {
                    WriteFile(snapshot, path, path + ".tmp");
                    Core.LoopContext.Post(() => _writing = false);
                }
                catch (Exception e)
                {
                    Core.LoopContext.Post(
                        () =>
                        {
                            // Not marked dirty: the next write is the clock's, not the next beat's, so a full disk is said every few minutes.
                            _writing = false;
                            logger.Warning("Zones: {File} could not be written: {Message}", File, e.Message);
                        }
                    );
                }
            }
        );
    }

    /// <summary>
    /// The shutdown's and the crash's write, on the calling thread: the loop has stopped and posts no more. Beside its own temporary
    /// file, so a periodic write still in flight cannot share one; never before the read, which would put nothing over the window.
    /// </summary>
    private static void SaveNow()
    {
        if (!_started || !_read)
        {
            return;
        }

        try
        {
            var path = PathOf();

            WriteFile(Take(), path, path + ".last.tmp");
        }
        catch (Exception e)
        {
            logger.Warning("Zones: {File} could not be written at shutdown: {Message}", File, e.Message);
        }
    }
}
