using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Work one bot keeps failing for the same reason is not offered to that bot for a while.
///
/// <para>
/// <b>The defect this project has met more often than any other, closed once rather than one instance at a time.</b>
/// A deed fails and the same deed is offered to the same bot at the next review, because nothing between the failure
/// and the choice remembers it: the proposer does not ask the question the deed fails on, the note is written where no
/// chooser reads it, or the work is unpaid and learns nothing. <c>DECISIONS.md</c> C1 records thirty-five of them since
/// 15.08.2026 - the guild's hire failed 4,365 times in 43 minutes on the night of 14.09.2026 and Hale's restock 8,719
/// times in half an hour that evening - each found by its symptom and each mended inside its own proposer.
/// </para>
///
/// <para>
/// <b>Keyed on the reason as well as the kind.</b> A hunter that loses one skeleton and then another has failed twice
/// for two reasons and is not in a loop; a bot told "the pack would not hold the 32gp it drew to pay with" six times in
/// five minutes is. Reasons are compared with their numbers taken out, so "could not raise 1252gp" and "could not raise
/// 1260gp" are one reason. Replayed over every session log of 14.09.2026 before this was written, six in five minutes
/// stopped every loop of that day - 48,287 of the 54,409 failures of the afternoon session, 9,559 of the 9,937 of the
/// night's hire session - and tripped at most once in a session with no loop in it.
/// </para>
///
/// <para>
/// <b>Only failures that produced nothing, which is the test the ledger already takes.</b> <c>BotWill.Settle</c> writes
/// <c>Beware</c> only when a failure's takings are nought or less, because a great many endings called failures did the
/// work - a tool worn through, a seam emptied, a shelf run out. Counted here regardless, the breaker would rest the one
/// trade a scribe is paid for: Ilsa ended eight inscriptions "nothing to write with" in build 41's first half hour, seven
/// of them carrying scrolls worth up to 449. Replayed over the twenty-four session logs of 14.09.2026 both ways, the loops
/// stopped are the same ones - 48,280 failures prevented against 48,287 in the afternoon, 9,548 against 9,559 in the hire
/// session - and the seven trips that go were on errands that earned something on the way.
/// </para>
///
/// <para>
/// <b>A backstop, not a repair, so it speaks every time it trips.</b> A rule that quietly rests a failing kind of work
/// is a rule that hides the defect under it. Each trip is a log line naming the bot, the work and the reason, and the
/// counts are in the Will line; the loop it stopped still has to be found and mended where it starts.
/// </para>
/// </summary>
public static class BotBreaker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBreaker));

    /// <summary>Whether the breaker trips at all. Off, work is offered exactly as it was before this existed.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Failures of one kind of work, for one reason, that trip the breaker when they fall inside <see cref="WindowMs"/>.</summary>
    public static int Failures { get; set; } = 6;

    /// <summary>How close together those failures have to fall. Five minutes.</summary>
    public static int WindowMs { get; set; } = 300000;

    /// <summary>How long a tripped kind is rested the first time. Doubled at every trip until the bot finishes that work once.</summary>
    public static int RestMs { get; set; } = 300000;

    /// <summary>The longest rest, however many times the breaker has tripped. Forty minutes.</summary>
    public static int LongestRestMs { get; set; } = 2400000;

    /// <summary>Times the breaker tripped.</summary>
    public static long Trips { get; private set; }

    /// <summary>
    /// Failures that trip the breaker for a reason of the "no way / no room" family (<see cref="Hard"/>) when they fall inside
    /// <see cref="HardWindowMs"/>. Two (06.10.2026, work17): such a reason is a fact about the place or the pack, and the next
    /// offer in the same second meets it again. On build 410 16 bursts of three or more failures in two seconds held 78 of the
    /// 598 bad endings (13 %): unload "no way" out of the Orc Caves 19, restock "no room in the pack" 17, scout "only 1 would
    /// come" 12, restock "would not hold the coin" 9. The six the breaker allowed were paid in full each time.
    /// </summary>
    public static int HardFailures { get; set; } = 2;

    /// <summary>How close together the hard failures must fall. One minute: a burst, not a day's bad luck.</summary>
    public static int HardWindowMs { get; set; } = 60000;

    /// <summary>Of <see cref="Trips"/>, those tripped at <see cref="HardFailures"/> for a reason of the hard family.</summary>
    public static long HardTrips { get; private set; }

    /// <summary>Whether a failure's reason says the place or the pack refuses: no way, no room, a pack that would not take or hold, an offer nobody came to.</summary>
    public static bool Hard(string why) =>
        !string.IsNullOrEmpty(why)
        && (why.Contains("no way", StringComparison.Ordinal) || why.Contains("no room", StringComparison.Ordinal)
            || why.Contains("pack would not", StringComparison.Ordinal) || why.Contains("would come", StringComparison.Ordinal));

    /// <summary>Flights that tripped the breaker and were not rested, because running away never is.</summary>
    public static long Spared { get; private set; }

    /// <summary>Offers refused because their kind was resting for that bot. Counts appraisals, not decisions.</summary>
    public static long Refused { get; private set; }

    /// <summary>Rests lifted early because the bot finished that kind of work after all.</summary>
    public static long Forgiven { get; private set; }

    /// <summary>
    /// Failures kept from the breaker because the work had made goods or skill before it failed. Counted by the
    /// caller, <c>BotWill</c>, which is where the takings are; here so that the two numbers print side by side.
    /// </summary>
    public static long Excused { get; set; }

    private sealed class Trail
    {
        public readonly Queue<long> Ticks = new();

        public long Last;
    }

    private sealed class Record
    {
        public string Name;

        public bool Resting;

        public long Until;

        public int Level;

        public string Why;

        public bool Finished;

        public long FinishedTick;
    }

    private static readonly Dictionary<(Serial Bot, string Kind, ulong Reason), Trail> _trails = [];

    private static readonly Dictionary<(Serial Bot, string Kind), Record> _records = [];

    /// <summary>Failures since the alarm last asked, by bot, kind and reason, with a name and a sentence for each. See <see cref="Loudest"/>.</summary>
    private static readonly Dictionary<(Serial Bot, string Kind, ulong Reason), (string Name, string Why, int Count)> _tally = [];

    /// <summary>Every failure counted into the tally since the alarm last asked.</summary>
    private static int _tallied;

    private static readonly List<(Serial Bot, string Kind, ulong Reason)> _staleTrails = [];

    private static readonly List<(Serial Bot, string Kind)> _staleRecords = [];

    private static bool _swept;

    private static long _sweptTick;

    /// <summary>
    /// A deed of this kind has just failed for this bot, for this reason. Trips the breaker on the failure that makes
    /// <see cref="Failures"/> inside <see cref="WindowMs"/> since the bot last finished the same work.
    /// </summary>
    /// <summary>Deaths at one kind of work that rest it, inside <see cref="DeathWindowMs"/>. See <see cref="Fell"/>.</summary>
    public static int DeathsToRest { get; set; } = 2;

    /// <summary>How close together those deaths have to fall. Thirty minutes.</summary>
    public static int DeathWindowMs { get; set; } = 1800000;

    /// <summary>
    /// How long work a bot died at twice is rested the first time; doubled at each trip. Fifteen minutes: the five of a failure
    /// loop let Ulaine take the same errand again at 08:11 on 350 and die at it four more times in thirteen minutes.
    /// </summary>
    public static int DeathRestMs { get; set; } = 900000;

    /// <summary>Times a kind of work was rested for a bot that kept dying at it.</summary>
    public static long DeathTrips { get; private set; }

    private static readonly Dictionary<(Serial Bot, string Kind), Queue<long>> _falls = [];

    /// <summary>
    /// <b>Work a bot keeps dying at is rested as work it keeps failing at is (30.09.2026, build 348).</b> A death is not a
    /// failure here — the breaker counted only failures, six in five minutes — so a bot sent to the same errand across the
    /// same ground died at it again and again: Sylvaris the Healer, 11gp in its purse, took "restock: after 1 Scissors" at
    /// 60–93/min, walked eight minutes and was killed at (1163, 1859) by a troll, at (1283, 1791) by a giant spider, at
    /// (755, 1734) by an orc and at (926, 1064) by a troll, four times in sixteen minutes on 345; Mardred and Liftrasa six
    /// times each at the same errand over 344–346. Two deaths at one kind inside DeathWindowMs rest it for that bot as a
    /// tripped failure does, and the rest doubles at each trip until the bot finishes that work once. Never the work that
    /// keeps a bot alive or with its company: running, mending, hitting back, waiting for a healer, a company's own work.
    /// </summary>
    /// <summary>
    /// The work that keeps a bot alive or with its company, which no rest of the breaker's ever touches: running, mending,
    /// hitting back, waiting for a healer, a company's own work. Shared by <see cref="Fell"/> and the ground rests below.
    /// </summary>
    public static bool Spares(string kind) =>
        kind is "flee" or "mend" or "rescue" or "await" or "enlist" or "band" or "sweep" or "delve";

    public static void Fell(Mobile bot, string kind)
    {
        if (!Running || bot == null || string.IsNullOrEmpty(kind) || Spares(kind))
        {
            return;
        }

        var now = Core.TickCount;
        var key = (bot.Serial, kind);

        if (!_falls.TryGetValue(key, out var falls))
        {
            if (_falls.Count >= 2048)
            {
                _falls.Clear();
            }

            falls = new Queue<long>();
            _falls[key] = falls;
        }

        falls.Enqueue(now);

        while (falls.Count > 0 && now - falls.Peek() > DeathWindowMs)
        {
            falls.Dequeue();
        }

        if (falls.Count < Math.Max(2, DeathsToRest))
        {
            return;
        }

        var minutes = (now - falls.Peek()) / 60000;

        falls.Clear();

        _records.TryGetValue(key, out var record);

        if (record == null)
        {
            record = new Record();
            _records[key] = record;
        }

        var rest = (long)Math.Min(DeathRestMs * Math.Pow(2.0, Math.Min(record.Level, 16)), LongestRestMs);

        record.Name = bot.Name;
        record.Level++;
        record.Resting = true;
        record.Until = now + rest;
        record.Why = "it died at it";

        Trips++;
        DeathTrips++;

        logger.Information(
            "{Name} died at {Kind} {Times} times in {Minutes} minutes, and that work is not offered to it for {Rest} minutes; tripped {Level} times since it last finished any",
            bot.Name,
            kind,
            Math.Max(2, DeathsToRest),
            minutes,
            rest / 60000,
            record.Level
        );
    }

    // ------------------------------------------------------------------------------------------------ ground a bot keeps dying on

    /// <summary>
    /// <b>A bot that keeps dying on one stretch of ground is not sent across it again for a while, whatever the work
    /// (01.10.2026, build 388; Opus dossier of 386, row 8).</b> The rests above are per kind of work: Maud died 42 times on
    /// 386, Thalion 37, Kaelthas 35 — the ten bots that died most carried 44 % of the shard's 701 deaths, one every six to
    /// eight minutes each, all residents of Trinsic — because a prowl rested for dying twice left the restock, the herbs and
    /// the mine to walk the same jungle. The ground is the thing they have in common. Two deaths of one bot inside
    /// <see cref="GroundWindowMs"/> within <see cref="GroundHalf"/> × 2 tiles of each other close the box round them, a
    /// <see cref="GroundHalf"/> each way, to that bot for <see cref="GroundRestMs"/>: nothing whose goal lies in the box, and
    /// nothing whose straight road crosses it, is offered to it alone. A company may still take it there (the appraisal asks
    /// only of work that brings the bot by itself: <see cref="GroundCompany"/>), the town inside the box stays open (guarded
    /// ground never counts), and so does the ground within <see cref="GroundHome"/> tiles of where the bot lives. Said every
    /// time, like every trip of the breaker: a backstop, not a repair.
    /// </summary>
    public static int GroundDeaths { get; set; } = 2;

    /// <summary>How close together those deaths have to fall. Thirty minutes.</summary>
    public static int GroundWindowMs { get; set; } = 1800000;

    /// <summary>Half the side of the box closed round the deaths, in tiles. Sixty-four: two quadrants, and the deaths of one bot on 386 fell 30–150 tiles apart.</summary>
    public static int GroundHalf { get; set; } = 64;

    /// <summary>How long the ground is closed to the bot. Thirty minutes; a death inside a standing box extends it.</summary>
    public static int GroundRestMs { get; set; } = 1800000;

    /// <summary>Tiles between two looks at a road for a closed box. Sixteen: a box is at least 128 across, so no road crosses one unseen.</summary>
    public static int GroundStep { get; set; } = 16;

    /// <summary>Ground this near the bot's home is never closed to it, or the bot has nowhere to work from.</summary>
    public static int GroundHome { get; set; } = 24;

    /// <summary>Work that brings more than this many times the bot's own strength brings a company, and is not kept off its closed ground.</summary>
    public static double GroundCompany { get; set; } = 1.5;

    /// <summary>Boxes closed to a bot, offers refused for lying in or across one, and boxes extended by a further death inside them.</summary>
    public static long GroundTrips { get; private set; }

    public static long GroundRefused { get; private set; }

    public static long GroundExtended { get; private set; }

    private sealed class Fall
    {
        public Map Map;

        public int X;

        public int Y;

        public long Tick;
    }

    private sealed class Ground
    {
        public Map Map;

        public int X1;

        public int Y1;

        public int X2;

        public int Y2;

        public long Until;

        public int Deaths;

        public string Why;

        public bool Holds(int x, int y) => x >= X1 && x <= X2 && y >= Y1 && y <= Y2;
    }

    private static readonly Dictionary<Serial, List<Fall>> _fallsAt = [];

    private static readonly Dictionary<Serial, List<Ground>> _grounds = [];

    /// <summary>This bot has just died here. See <see cref="GroundDeaths"/>.</summary>
    public static void FellAt(Mobile bot, Map map, Point3D where, Mobile killer)
    {
        if (!Running || bot == null || map == null || map == Map.Internal || GroundDeaths <= 0 || GroundHalf <= 0 || GroundRestMs <= 0)
        {
            return;
        }

        var now = Core.TickCount;

        // A death inside a box already closed to this bot keeps it closed longer, and that is all.
        if (_grounds.TryGetValue(bot.Serial, out var grounds))
        {
            for (var i = 0; i < grounds.Count; i++)
            {
                var g = grounds[i];

                if (g.Map == map && g.Until - now > 0 && g.Holds(where.X, where.Y))
                {
                    g.Until = now + GroundRestMs;
                    g.Deaths++;
                    GroundExtended++;

                    return;
                }
            }
        }

        if (!_fallsAt.TryGetValue(bot.Serial, out var falls))
        {
            if (_fallsAt.Count >= 2048)
            {
                _fallsAt.Clear();
            }

            falls = [];
            _fallsAt[bot.Serial] = falls;
        }

        for (var i = falls.Count - 1; i >= 0; i--)
        {
            if (now - falls[i].Tick > GroundWindowMs)
            {
                falls.RemoveAt(i);
            }
        }

        falls.Add(new Fall { Map = map, X = where.X, Y = where.Y, Tick = now });

        var near = 0;
        var x1 = where.X;
        var y1 = where.Y;
        var x2 = where.X;
        var y2 = where.Y;
        var oldest = now;

        for (var i = 0; i < falls.Count; i++)
        {
            var f = falls[i];

            if (f.Map != map || Math.Max(Math.Abs(f.X - where.X), Math.Abs(f.Y - where.Y)) > GroundHalf * 2)
            {
                continue;
            }

            near++;
            x1 = Math.Min(x1, f.X);
            y1 = Math.Min(y1, f.Y);
            x2 = Math.Max(x2, f.X);
            y2 = Math.Max(y2, f.Y);
            oldest = Math.Min(oldest, f.Tick);
        }

        if (near < Math.Max(2, GroundDeaths))
        {
            return;
        }

        // The deaths that closed the box are spent: the next closure wants fresh ones.
        for (var i = falls.Count - 1; i >= 0; i--)
        {
            var f = falls[i];

            if (f.Map == map && Math.Max(Math.Abs(f.X - where.X), Math.Abs(f.Y - where.Y)) <= GroundHalf * 2)
            {
                falls.RemoveAt(i);
            }
        }

        if (grounds == null)
        {
            if (_grounds.Count >= 2048)
            {
                _grounds.Clear();
            }

            grounds = [];
            _grounds[bot.Serial] = grounds;
        }

        var minutes = (now - oldest) / 60000;
        var ground = new Ground
        {
            Map = map,
            X1 = x1 - GroundHalf,
            Y1 = y1 - GroundHalf,
            X2 = x2 + GroundHalf,
            Y2 = y2 + GroundHalf,
            Until = now + GroundRestMs,
            Deaths = near,
            Why = $"{near} deaths in {minutes} minutes round ({(x1 + x2) / 2}, {(y1 + y2) / 2}), the last by {killer?.Name ?? "something"}"
        };

        grounds.Add(ground);
        GroundTrips++;

        logger.Information(
            "{Name} died {Times} times in {Minutes} minutes within ({X1}, {Y1})-({X2}, {Y2}), the last by {Killer}, and nothing that goes into or across that ground is offered to it alone for {Rest} minutes, whatever the work",
            bot.Name,
            near,
            minutes,
            ground.X1,
            ground.Y1,
            ground.X2,
            ground.Y2,
            killer?.Name ?? "something",
            GroundRestMs / 60000
        );
    }

    /// <summary>
    /// Whether work at <paramref name="to"/>, walked from <paramref name="from"/>, lies in or crosses ground closed to this bot:
    /// the goal, then the straight road every <see cref="GroundStep"/> tiles. Guarded ground and the ground round the bot's home
    /// never count. Counted when it refuses.
    /// </summary>
    public static bool GroundResting(Mobile bot, Map map, Point3D from, Point3D to, out string why, out long leftMs)
    {
        why = null;
        leftMs = 0;

        if (!Running || bot == null || map == null || !_grounds.TryGetValue(bot.Serial, out var grounds) || grounds.Count == 0)
        {
            return false;
        }

        var now = Core.TickCount;

        for (var i = grounds.Count - 1; i >= 0; i--)
        {
            if (grounds[i].Until - now <= 0)
            {
                grounds.RemoveAt(i);
            }
        }

        if (grounds.Count == 0)
        {
            return false;
        }

        var home = BotPopulation.HomeOf(bot);

        for (var i = 0; i < grounds.Count; i++)
        {
            var g = grounds[i];

            if (g.Map != map)
            {
                continue;
            }

            if (g.Holds(to.X, to.Y) && Closed(map, to, home))
            {
                GroundRefused++;
                why = g.Why;
                leftMs = g.Until - now;

                return true;
            }

            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Max(Math.Abs(dx), Math.Abs(dy));
            var step = Math.Max(4, GroundStep);

            if (length <= step)
            {
                continue;
            }

            var samples = length / step;

            for (var s = 1; s < samples; s++)
            {
                var at = new Point3D(from.X + dx * s / samples, from.Y + dy * s / samples, 0);

                if (g.Holds(at.X, at.Y) && Closed(map, at, home))
                {
                    GroundRefused++;
                    why = g.Why;
                    leftMs = g.Until - now;

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// <b>Boxes lifted by a company that cleared the ground (02.10.2026).</b> A box is closed to one bot because it died there
    /// twice with nobody dealing with the ground; a company has now walked the ground and emptied it, or walked it for its
    /// whole clock. Every box, of every bot, that holds <paramref name="where"/> or whose middle lies within
    /// <paramref name="spread"/> of it is dropped. Returns how many.
    /// </summary>
    public static int LiftGround(Map map, Point3D where, int spread)
    {
        if (!Running || map == null || map == Map.Internal)
        {
            return 0;
        }

        var lifted = 0;

        foreach (var grounds in _grounds.Values)
        {
            for (var i = grounds.Count - 1; i >= 0; i--)
            {
                var g = grounds[i];

                if (g.Map != map)
                {
                    continue;
                }

                var near = g.Holds(where.X, where.Y)
                           || Math.Max(Math.Abs((g.X1 + g.X2) / 2 - where.X), Math.Abs((g.Y1 + g.Y2) / 2 - where.Y)) <= spread + GroundHalf;

                if (near)
                {
                    grounds.RemoveAt(i);
                    lifted++;
                }
            }
        }

        GroundLifted += lifted;

        return lifted;
    }

    /// <summary>Boxes lifted by a company that cleared their ground. See <see cref="LiftGround"/>.</summary>
    public static long GroundLifted { get; private set; }

    /// <summary>Whether a tile inside a closed box is really closed: not guarded ground, not the ground round the bot's home.</summary>
    private static bool Closed(Map map, Point3D at, Point3D home)
    {
        if (home != Point3D.Zero && Math.Max(Math.Abs(home.X - at.X), Math.Abs(home.Y - at.Y)) <= GroundHome)
        {
            return false;
        }

        return Region.Find(at, map)?.IsPartOf<Server.Regions.GuardedRegion>() != true;
    }

    /// <summary>Boxes closed to bots now. For the Will line.</summary>
    private static int GroundsStanding()
    {
        var now = Core.TickCount;
        var standing = 0;

        foreach (var grounds in _grounds.Values)
        {
            for (var i = 0; i < grounds.Count; i++)
            {
                if (grounds[i].Until - now > 0)
                {
                    standing++;
                }
            }
        }

        return standing;
    }

    public static void Failed(Mobile bot, string kind, string why)
    {
        if (!Running || bot == null || string.IsNullOrEmpty(kind))
        {
            return;
        }

        var now = Core.TickCount;

        Sweep(now);

        var key = (bot.Serial, kind, Shape(why));

        // Counted for the alarm before anything is trimmed: the trail below keeps only the last few failures and is emptied
        // when it trips, so it cannot say which loop was the loudest of a five-minute window. See Loudest. Bounded, in case
        // nothing ever asks.
        if (_tally.Count >= 4096)
        {
            _tally.Clear();
            _tallied = 0;
        }

        _tally[key] = _tally.TryGetValue(key, out var counted)
            ? (counted.Name, counted.Why, counted.Count + 1)
            : (bot.Name, why ?? "no reason given", 1);
        _tallied++;

        if (!_trails.TryGetValue(key, out var trail))
        {
            trail = new Trail();
            _trails[key] = trail;
        }

        _records.TryGetValue((bot.Serial, kind), out var record);

        trail.Last = now;
        trail.Ticks.Enqueue(now);

        var hard = HardFailures >= 2 && HardFailures < Failures && Hard(why);
        var most = hard ? HardFailures : Math.Max(2, Failures);
        var window = hard ? Math.Min(WindowMs, HardWindowMs) : WindowMs;

        // Only failures inside the window, and only failures since this bot last finished the same work: a finish in
        // between proves the work can be done here, and failures on both sides of it are not one loop.
        while (trail.Ticks.Count > 0)
        {
            var oldest = trail.Ticks.Peek();
            var expired = now - oldest > window;
            var beforeFinish = record is { Finished: true } && oldest - record.FinishedTick < 0;

            if (!expired && !beforeFinish && trail.Ticks.Count <= most)
            {
                break;
            }

            trail.Ticks.Dequeue();
        }

        if (trail.Ticks.Count < most)
        {
            return;
        }

        var seconds = (now - trail.Ticks.Peek()) / 1000;

        trail.Ticks.Clear();

        // <b>Running away is never rested.</b> The breaker's cure for a loop is to take the work off the bot for a
        // while; for a bot that is losing, the work is the only thing between it and the corpse. Ilsa the Sage,
        // 07:25 on 22.09.2026 in the Orc Caves: six refused flights in two seconds and the breaker rested "flee"
        // for five minutes with Woghuglat still on her. The loop itself is the flight's to fix (it now bends round
        // the threat and keeps out of the ways refused, see BotBolt.Bend); this only refuses to make it worse.
        if (kind == BotBolt.Trade)
        {
            Spared++;

            return;
        }

        if (record == null)
        {
            record = new Record();
            _records[(bot.Serial, kind)] = record;
        }

        var rest = (long)Math.Min(RestMs * Math.Pow(2.0, Math.Min(record.Level, 16)), LongestRestMs);

        record.Name = bot.Name;
        record.Level++;
        record.Resting = true;
        record.Until = now + rest;
        record.Why = why ?? "no reason given";

        Trips++;

        if (hard)
        {
            HardTrips++;
        }

        logger.Information(
            "{Name} failed at {Kind} {Times} times in {Seconds}s for the same reason ({Why}), and that work is not offered to it for {Minutes} minutes; tripped {Level} times since it last finished any",
            bot.Name,
            kind,
            most,
            seconds,
            record.Why,
            rest / 60000,
            record.Level
        );
    }

    /// <summary>
    /// The bot, kind and reason with the most failures since the last time this was asked, and how many failures there were
    /// in all; empties the count. False when nothing failed.
    ///
    /// <para>
    /// <b>For the alarm, which could not tell one looping bot from the shard.</b> Every failure already passes through here
    /// with the bot, the kind and the sentence, and only the trails were kept, which hold the last few failures of a loop
    /// and are emptied when it trips. See <c>BotSigns.Work</c>; §5 S5, which Fable's order of the structural remedies put
    /// first.
    /// </para>
    /// </summary>
    public static bool Loudest(out string name, out string kind, out string why, out int times, out int failures)
    {
        name = null;
        kind = null;
        why = null;
        times = 0;
        failures = _tallied;

        foreach (var (key, counted) in _tally)
        {
            if (counted.Count > times)
            {
                name = counted.Name;
                kind = key.Kind;
                why = counted.Why;
                times = counted.Count;
            }
        }

        _tally.Clear();
        _tallied = 0;

        return times > 0;
    }

    /// <summary>
    /// Whether this kind of work is resting for this bot, and if so after what failure and for how much longer.
    /// </summary>
    public static bool Resting(Mobile bot, string kind, out string why, out long leftMs)
    {
        why = null;
        leftMs = 0;

        if (!Running || bot == null || _records.Count == 0 || string.IsNullOrEmpty(kind))
        {
            return false;
        }

        if (!_records.TryGetValue((bot.Serial, kind), out var record) || !record.Resting)
        {
            return false;
        }

        leftMs = record.Until - Core.TickCount;

        if (leftMs <= 0)
        {
            record.Resting = false;
            leftMs = 0;

            return false;
        }

        Refused++;
        why = record.Why;

        return true;
    }

    /// <summary>How much longer this kind of work rests for this bot, in milliseconds, without counting it as a refusal.</summary>
    public static long RestLeftMs(Mobile bot, string kind)
    {
        if (bot == null || string.IsNullOrEmpty(kind) || !_records.TryGetValue((bot.Serial, kind), out var record) || !record.Resting)
        {
            return 0;
        }

        return Math.Max(0, record.Until - Core.TickCount);
    }

    /// <summary>This bot has just finished a deed of this kind: whatever the breaker held against it is lifted.</summary>
    public static void Finished(Mobile bot, string kind)
    {
        if (bot == null || string.IsNullOrEmpty(kind))
        {
            return;
        }

        if (!_records.TryGetValue((bot.Serial, kind), out var record))
        {
            record = new Record { Name = bot.Name };
            _records[(bot.Serial, kind)] = record;
        }

        if (record.Resting)
        {
            Forgiven++;
        }

        record.Resting = false;
        record.Level = 0;
        record.Finished = true;
        record.FinishedTick = Core.TickCount;
    }

    /// <summary>A bot that is gone takes its records with it, or the tables grow for the life of the shard.</summary>
    public static void Forget(Mobile bot)
    {
        if (bot == null)
        {
            return;
        }

        var serial = bot.Serial;

        foreach (var key in _trails.Keys)
        {
            if (key.Bot == serial)
            {
                _staleTrails.Add(key);
            }
        }

        foreach (var key in _records.Keys)
        {
            if (key.Bot == serial)
            {
                _staleRecords.Add(key);
            }
        }

        for (var i = 0; i < _staleTrails.Count; i++)
        {
            _trails.Remove(_staleTrails[i]);
        }

        for (var i = 0; i < _staleRecords.Count; i++)
        {
            _records.Remove(_staleRecords[i]);
        }

        _staleTrails.Clear();
        _staleRecords.Clear();
        _fallsAt.Remove(serial);
        _grounds.Remove(serial);
    }

    /// <summary>Trails whose last failure has left the window are dropped, at most once a minute.</summary>
    private static void Sweep(long now)
    {
        if (_swept && now - _sweptTick < 60000)
        {
            return;
        }

        _swept = true;
        _sweptTick = now;

        foreach (var (key, trail) in _trails)
        {
            if (now - trail.Last > WindowMs)
            {
                _staleTrails.Add(key);
            }
        }

        for (var i = 0; i < _staleTrails.Count; i++)
        {
            _trails.Remove(_staleTrails[i]);
        }

        _staleTrails.Clear();
    }

    /// <summary>
    /// A reason with its numbers taken out, as a hash: "could not raise 1252gp" and "could not raise 1260gp" are one
    /// reason, and so are two walks that stopped short of two different coordinates for the same cause.
    /// </summary>
    private static ulong Shape(string why)
    {
        const ulong prime = 1099511628211UL;

        var hash = 14695981039346656037UL;

        if (string.IsNullOrEmpty(why))
        {
            return hash;
        }

        var number = false;

        for (var i = 0; i < why.Length; i++)
        {
            var c = why[i];
            var digit = char.IsAsciiDigit(c) || (number && c == '.' && i + 1 < why.Length && char.IsAsciiDigit(why[i + 1]));

            if (digit)
            {
                if (!number)
                {
                    hash = unchecked((hash ^ 'N') * prime);
                    number = true;
                }

                continue;
            }

            number = false;
            hash = unchecked((hash ^ c) * prime);
        }

        return hash;
    }

    /// <summary>The breaker's counts, for the Will line.</summary>
    public static string Describe()
    {
        var now = Core.TickCount;
        var resting = 0;

        foreach (var record in _records.Values)
        {
            if (record.Resting && record.Until - now > 0)
            {
                resting++;
            }
        }

        return $"{Excused} failures kept from the breaker for having made goods or skill first; {Trips} times one bot's work was rested after failing {Math.Max(2, Failures)} times in {WindowMs / 60000} minutes for the same reason ({HardTrips} of them after {HardFailures} in {HardWindowMs / 1000}s for a reason of the no-way / no-room family; {DeathTrips} of them after dying at it {Math.Max(2, DeathsToRest)} times in {DeathWindowMs / 60000}), {resting} resting now, {Refused} offers refused while resting and {Forgiven} rests lifted because the work was finished after all, {Spared} flights that tripped it and were not rested; "
            + $"{GroundTrips} stretches of ground closed to one bot for {GroundRestMs / 60000} minutes after it died on them {Math.Max(2, GroundDeaths)} times in {GroundWindowMs / 60000} ({GroundExtended} closures extended by another death inside, {GroundsStanding()} standing now, {GroundLifted} lifted by a company that cleared the ground), {GroundRefused} offers refused for a goal or a road inside one";
    }

    /// <summary>Every rest standing now, with the bot, the work, the time left and the failure that tripped it. For the door.</summary>
    public static string List()
    {
        var now = Core.TickCount;
        var parts = new List<string>();

        foreach (var (key, record) in _records)
        {
            if (!record.Resting || record.Until - now <= 0)
            {
                continue;
            }

            parts.Add($"{record.Name}: {key.Kind} for {(record.Until - now) / 1000}s more, trip {record.Level}, after \"{record.Why}\"");
        }

        foreach (var (serial, grounds) in _grounds)
        {
            for (var i = 0; i < grounds.Count; i++)
            {
                var g = grounds[i];

                if (g.Until - now > 0)
                {
                    parts.Add($"{World.FindMobile(serial)?.Name ?? serial.ToString()}: ground ({g.X1}, {g.Y1})-({g.X2}, {g.Y2}) for {(g.Until - now) / 1000}s more, after \"{g.Why}\"");
                }
            }
        }

        return parts.Count == 0
            ? $"nothing is resting. {Describe()}."
            : $"{parts.Count} resting: {string.Join("; ", parts)}. {Describe()}.";
    }
}
