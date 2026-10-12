using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Engines.Pathing.Tiered;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// <b>The road the planner would draw, judged where the straight line refuses (06.10.2026, road15; Patrick's targets of that
/// night: paths 99 %, work 95 %, Needs 95 %, "money gathers very slowly", and his rule of 03.10: "zones are for understanding
/// danger and who can be met, but the actual presence nearby must not be excluded").</b>
///
/// <para>
/// <b>Measured on build 400 (04.10, 11.3 h, 210 bots).</b> <see cref="Road"/> was asked 64.0 million times and refused 56.6
/// million (88 %): 5.0 million refusals an hour, the first veto printed for weave 579 times an hour, prowl 351, envoy 322,
/// deliver 219, errand 201. The square that refused asked a median 5.2 times what the work brought (9000 against 1511), and
/// 91 % of the printed refusals by the record were for a square nobody had died on in the three hours before: the record of a
/// square is one number moved a quarter by every death, whoever died and whatever killed, kept across restarts and bought
/// back only by crossings the veto itself prevents. And the line is not the road: the planner already charges such a square
/// <see cref="BloodBeyond"/> tiles a node and a deadly patrol <see cref="DeadlyCost"/>, and goes round both.
/// </para>
///
/// <para>
/// <b>The rule (second form, 06.10.2026, road16).</b> The line is asked first, as before, and a road it passes is offered
/// as before. A road it refuses is judged: a search over the navigation graph for a road <em>clear</em> of ground beyond
/// the walker — every node further than <see cref="RoadStep"/> tiles from both ends and outside guarded ground is read as
/// the line's samples were (a patrol crossed at <see cref="ZoneRoadShare"/> of its fight; the record of a square somebody
/// died on within <see cref="RoadFreshMs"/>, or where creatures were counted now, or with dead of an age unknown since the
/// boot, against ×<see cref="RoadDare"/>; an older record past ×<see cref="RoadStaleDare"/>), and a node that fails is not
/// walked through — priced for the rest by the planners' own hook (<see cref="Cost"/>) for a walker of that strength, no
/// further than <see cref="RouteDetour"/> tiles outside the line's own box and <see cref="RouteMost"/> nodes. A clear road
/// found is offered only when the planner would draw it: when its price is within <see cref="RouteSlack"/> tiles of the
/// straight distance, since any road through one node beyond the walker costs the planner at least the straight distance
/// and <see cref="BloodBeyond"/> more; a clear road dearer than that is a detour the walk would not take, and the line's
/// refusal stands. Where no clear road is found inside the box, or the question was not asked (more than
/// <see cref="RoutePerSecond"/> searches or <see cref="RouteMsPerSecond"/> ms this second, ends the graph cannot place),
/// the line's answer stands. A verdict is kept <see cref="RouteKeepMs"/> for its origin square of <see cref="Cell"/> tiles,
/// goal square and band of strength, and a kept verdict answers any walker.
/// </para>
///
/// <para>
/// <b>Why the second form (build 405, 3.5 h).</b> The first form priced the whole road and read it after: with a record past
/// the walker charged 300 tiles a node and 1341 squares of the island so priced, the search's estimate was nothing like the
/// cost, it expanded the whole box before the goal and hit its 3000 nodes: 567 080 of 604 629 judgements were "no road
/// within 240 tiles" (2820 nodes a search), while offline 47–67 % of a sample of 120 had a clear road inside the box. And
/// 16.4 M of 21.6 M line refusals were never judged for a walker that had not stood two seconds: 74 % of the road vetoes
/// printed at the moment a bot took work were those, and the bot had come from elsewhere in the minute before — a bot
/// chooses its next work the moment it stops. The standing rule is gone; a walker holding work (<c>busy</c>) searches only
/// while the second's budget is under <see cref="RouteBusyShare"/>, and reads kept verdicts freely.
/// </para>
///
/// <para>
/// <b>What it costs, read directly.</b> A road offered here that the line refused is remembered for its walker; when the
/// walk draws a leg to it the road is counted walked (<see cref="RouteWalked"/>), and a death within
/// <see cref="RouteDeathMs"/> of such a leg is counted (<see cref="RouteDied"/>): deaths on roads the old rule would have
/// refused. <see cref="RouteJudged"/> off is the old rule.
/// </para>
/// </summary>
public static partial class BotDanger
{
    /// <summary>Whether a road the straight line refuses is judged by the road the planner would draw. Off is the rule of build 388–402.</summary>
    public static bool RouteJudged { get; set; } = true;

    /// <summary>
    /// Whether the planner's road is judged for a road the line passes as well, and refused when it crosses ground beyond the
    /// walker. Off: the line's pass stands, as it always has. The lever to pull before <see cref="RouteJudged"/> if deaths
    /// on the road rise: on build 400, 338 of 532 deaths at work that is not a hunt fell on a square that asked more than
    /// ×1.5 of the dead bot's strength at that moment, with the line having passed the road.
    /// </summary>
    public static bool RouteAlways { get; set; }

    /// <summary>Tiles outside the box of the straight line a judged road may run. Two hundred and forty: eight squares of the record.</summary>
    public static int RouteDetour { get; set; } = 240;

    /// <summary>
    /// Nodes one judged road may expand before it is given up as not found. Four thousand: the box of a 300-tile road holds
    /// about 1500 clusters and 3700 nodes, so a search that finds no clear road reads the whole box and says so, rather than
    /// running out first (build 405's 3000 gave up on 94 % of its judgements).
    /// </summary>
    public static int RouteMost { get; set; } = 4000;

    /// <summary>
    /// Tiles a clear road's price may exceed the straight distance by and still be the road the planner draws: any road through
    /// a node beyond the walker costs the planner at least <see cref="BloodBeyond"/> (300) more than the straight distance. When
    /// a hostile patrol's node was what the search went round, the margin is that patrol's least price for such a walker (90).
    /// </summary>
    public static int RouteSlack { get; set; } = 300;

    /// <summary>
    /// The share of a second's searches a walker holding work may use: beyond it such a walker reads kept verdicts only, and the
    /// rest of the second is for walkers choosing. A third.
    /// </summary>
    public static double RouteBusyShare { get; set; } = 0.34;

    /// <summary>Roads searched in one second of the loop at most; a question past that keeps the line's answer and is asked again later.</summary>
    public static int RoutePerSecond { get; set; } = 12;

    /// <summary>And the most of one second those searches may take, in milliseconds. Fifteen: a hundredth and a half of the loop.</summary>
    public static double RouteMsPerSecond { get; set; } = 15.0;

    /// <summary>How long a verdict stands for its squares and strength. Two minutes: the record moves by deaths, and the walk prices its own route when it begins.</summary>
    public static int RouteKeepMs { get; set; } = 120000;

    /// <summary>
    /// How long after a death a square's record is fresh: read against ×<see cref="RoadDare"/> on a judged road. Three hours,
    /// <see cref="BotQuad.DreadMs"/>: as long as the square takes no credit for crossings. A square where hostile creatures
    /// were counted within <see cref="BotQuad.SightMs"/> is fresh whatever its deaths.
    ///
    /// <para>
    /// <b>And a record whose age the shard cannot know is fresh.</b> When a square's last death fell is not stored
    /// (<c>BotQuad.Quad.DiedTick</c>: "a restart is a fresh look"), so for this long after a boot a square with dead on it and
    /// none seen since the boot may have lost them minutes before the restart: it is read as fresh until the shard has been
    /// up as long as a fresh record lasts. On a night of four restarts in an hour the other reading would have called
    /// every record on the island old.
    /// </para>
    /// </summary>
    public static int RoadFreshMs { get; set; } = 10800000;

    private static long _bootTick;

    private static bool _booted;

    /// <summary>The module has started: the age of what the save brought back is unknown from here. See <see cref="RoadFreshMs"/>.</summary>
    private static void Booted()
    {
        _bootTick = Core.TickCount;
        _booted = true;
    }

    /// <summary>
    /// How many times what the walker brings an older record may ask on a judged road before it refuses. Four and a half:
    /// for the median bot of 1500 that is 6750, so a square one old death below neutral (6000) is the planner's
    /// <see cref="BloodBeyond"/> tiles of detour and no more, and one two deaths below (8500) still refuses. At
    /// <see cref="RoadDare"/> an old record refuses as a fresh one does.
    /// </summary>
    public static double RoadStaleDare { get; set; } = 4.5;

    /// <summary>How long after a walk drew a leg to a road offered here a death is counted against it. Two minutes: a fight on the road is shorter.</summary>
    public static int RouteDeathMs { get; set; } = 120000;

    /// <summary>How long an offered road is remembered for its walker, to know the walk when it begins.</summary>
    public static int RouteWatchMs { get; set; } = 1800000;

    /// <summary>The side of the squares a verdict is kept for. Thirty-two: <see cref="RoadStep"/>, inside which an end is not read.</summary>
    public const int Cell = 32;

    /// <summary>
    /// How the last <see cref="Road"/> was answered when it refused, for the veto's sentence: on the planner's road, or on
    /// the straight line and why the road was not judged. Null when the road was not refused.
    /// </summary>
    public static string RoadHow { get; private set; }

    /// <summary>Roads the straight line refused: what <see cref="RoadsRefused"/> counted until 06.10.2026.</summary>
    public static long LineRefused { get; private set; }

    /// <summary>Of those, left refused without a judgement: the second's searches spent (and, of those, by a walker holding work past its share), an end the graph could not place.</summary>
    public static long RouteBudget { get; private set; }

    public static long RouteBusy { get; private set; }

    public static long RouteUnplaced { get; private set; }

    /// <summary>Roads judged (line-refused or, by <see cref="RouteAlways"/>, line-passed), from a kept verdict and by a search.</summary>
    public static long RouteAsked { get; private set; }

    public static long RouteKept { get; private set; }

    public static long RouteSearches { get; private set; }

    public static long RouteExpanded { get; private set; }

    /// <summary>Roads offered that the line had refused: the planner's road read clear.</summary>
    public static long RouteOffered { get; private set; }

    /// <summary>Roads the line had refused with a clear road found that the planner would not draw (dearer than the straight distance by <see cref="RouteSlack"/>): the line's answer stood.</summary>
    public static long RouteDetours { get; private set; }

    /// <summary>Roads the line had refused with no clear road found inside the box and the budget of nodes: the line's answer stood.</summary>
    public static long RouteNone { get; private set; }

    /// <summary>Searches that read the whole box or ran out of nodes without a clear road, and nodes the searches would not walk through.</summary>
    public static long RouteExhausted { get; private set; }

    public static long RouteBlocked { get; private set; }

    /// <summary>Roads the line had passed and the planner's road refused. Nought unless <see cref="RouteAlways"/>.</summary>
    public static long RouteStricter { get; private set; }

    /// <summary>Walks begun on a road offered here that the line had refused, and deaths within <see cref="RouteDeathMs"/> of a leg of one.</summary>
    public static long RouteWalked { get; private set; }

    public static long RouteDied { get; private set; }

    private static long _routeTicks;

    public static double RouteMs => _routeTicks * 1000.0 / Stopwatch.Frequency;

    private sealed class RoadTally
    {
        public long Asked;
        public long Line;
        public long Offered;
        public long Refused;
        public long Unjudged;
        public long Died;
    }

    private static readonly Dictionary<string, RoadTally> _trades = new(StringComparer.OrdinalIgnoreCase);

    private static RoadTally TallyOf(string trade)
    {
        if (!_trades.TryGetValue(trade, out var tally))
        {
            _trades[trade] = tally = new RoadTally();
        }

        return tally;
    }

    private enum RouteVerdict : byte
    {
        /// <summary>No clear road found inside the box and the budget of nodes.</summary>
        None,
        Clear,

        /// <summary>A clear road found, dearer than the planner would draw. See <see cref="RouteSlack"/>.</summary>
        Detour,

        /// <summary>What one node reads when it is not walked through (<see cref="Reads"/>): a fresh record, an old one far past, a patrol.</summary>
        Record,
        Old,
        Zone,

        /// <summary>The graph could not place an end: not drawn, no node in the end's square of it, another piece of ground.</summary>
        Unplaced
    }

    private struct Kept
    {
        public long Tick;
        public RouteVerdict Verdict;
        /// <summary>The clear road's price in tiles, for the record.</summary>
        public float Asked;
    }

    private static readonly Dictionary<long, Kept> _kept = [];

    private sealed class Offer
    {
        public Point3D To;
        public long Tick;
        public string Trade;
        public bool Walked;
    }

    /// <summary>
    /// Roads offered that the line had refused, by walker: where to, when, for which trade, and whether the walk has been
    /// counted. Matched by nearness (<see cref="OfferNear"/>), because the walk's target is where the buyer or the seam is
    /// when the walk is drawn, not where it was when the work was weighed.
    /// </summary>
    private static readonly Dictionary<uint, List<Offer>> _offered = [];

    /// <summary>How near an offered road's goal a walk's target must be to be that road. Forty-eight: a square and a half.</summary>
    public const int OfferNear = 48;

    /// <summary>The most offered roads remembered for one walker; the oldest gives way.</summary>
    private const int MostOffers = 12;

    private static Offer OfferTo(List<Offer> offers, Point3D to, long now)
    {
        Offer best = null;
        var nearest = OfferNear + 1;

        for (var i = 0; i < offers.Count; i++)
        {
            var offer = offers[i];
            var off = Math.Max(Math.Abs(offer.To.X - to.X), Math.Abs(offer.To.Y - to.Y));

            if (off < nearest && now - offer.Tick < RouteWatchMs)
            {
                nearest = off;
                best = offer;
            }
        }

        return best;
    }

    /// <summary>The walker's last leg drawn to such a road. See <see cref="Walks"/>.</summary>
    private static readonly Dictionary<uint, (long Tick, string Trade)> _walking = [];

    private static long _second;

    private static int _searched;

    private static long _spent;

    /// <summary>Whoever is asking, for the price hook to know a walker is: only its being there is read (<see cref="Cost"/>).</summary>
    private static Mobile _judge;

    private static int[] _g = [];

    private static int[] _parent = [];

    private static int[] _seen = [];

    private static int[] _closed = [];

    private static int _search;

    private static readonly PriorityQueue<int, long> _open = new();

    /// <summary>
    /// The planner's road for a road the line has answered. Returns whether the road is refused; when it judges, the square
    /// that refused replaces the line's. See the class note.
    /// </summary>
    private static bool Judge(
        Mobile body, Map map, Point3D from, Point3D to, double brought, int step, double dare, bool lineRefused, string trade, bool busy, RoadTally tally,
        ref double asked, ref Point3D at, ref string what, ref bool byZone
    )
    {
        if (lineRefused)
        {
            LineRefused++;

            if (tally != null)
            {
                tally.Line++;
            }
        }

        var now = Core.TickCount;
        var serial = body.Serial.Value;

        // The band of strength: steps of a fifth, and the band's lower edge is what is judged, so a kept verdict is never
        // kinder than the walker's own.
        var band = Math.Clamp((int)Math.Floor(Math.Log(Math.Max(1.0, brought)) / Math.Log(1.2)), 0, 255);
        var strength = Math.Pow(1.2, band);
        var key = ((long)map.MapID << 56) | ((long)band << 48) | ((long)(from.Y / Cell) << 36) | ((long)(from.X / Cell) << 24)
                  | ((long)(to.Y / Cell) << 12) | (uint)(to.X / Cell);

        if (!_kept.TryGetValue(key, out var kept) || now - kept.Tick >= RouteKeepMs)
        {
            var second = now / 1000;

            if (second != _second)
            {
                _second = second;
                _searched = 0;
                _spent = 0;
            }

            if (_searched >= RoutePerSecond || _spent * 1000.0 / Stopwatch.Frequency >= RouteMsPerSecond)
            {
                return Unjudged(lineRefused, tally, "on the straight line, the second's searches of the planner's road being spent", 1);
            }

            // A walker holding work is weighing a change of mind; the second's searches are for the walkers choosing first.
            if (busy && (_searched >= RoutePerSecond * RouteBusyShare || _spent * 1000.0 / Stopwatch.Frequency >= RouteMsPerSecond * RouteBusyShare))
            {
                return Unjudged(lineRefused, tally, "on the straight line, the second's searches being kept for walkers choosing", 0);
            }

            _searched++;
            _judge = body;

            var began = Stopwatch.GetTimestamp();

            if (Search(map, from, to, strength, step, dare, out kept))
            {
                RouteSearches++;
            }
            else
            {
                // Kept as any verdict is: an end the graph cannot place now is asked about again when the verdict runs out,
                // not at every review.
                kept.Verdict = RouteVerdict.Unplaced;
            }

            var took = Stopwatch.GetTimestamp() - began;

            _judge = null;
            _routeTicks += took;
            _spent += took;
            kept.Tick = now;

            if (_kept.Count > 16384)
            {
                _kept.Clear();
            }

            _kept[key] = kept;
        }
        else if (kept.Verdict != RouteVerdict.Unplaced)
        {
            RouteKept++;
        }

        if (kept.Verdict == RouteVerdict.Unplaced)
        {
            return Unjudged(lineRefused, tally, "on the straight line, the navigation graph not placing an end of the road", 2);
        }

        RouteAsked++;

        switch (kept.Verdict)
        {
            case RouteVerdict.Clear:
                {
                    if (lineRefused)
                    {
                        RouteOffered++;

                        if (tally != null)
                        {
                            tally.Offered++;
                        }

                        Offered(serial, to, trade, now);
                    }

                    asked = 0.0;
                    at = Point3D.Zero;
                    what = null;
                    byZone = false;
                    RoadHow = null;

                    return false;
                }
            case RouteVerdict.Detour:
                {
                    // A clear road, but one the planner would not draw: the line's answer stands, whichever it was.
                    if (lineRefused)
                    {
                        RouteDetours++;

                        if (tally != null)
                        {
                            tally.Refused++;
                        }

                        RoadHow = "on the straight line, the clear road near it being a detour the planner would not draw";
                    }

                    return lineRefused;
                }
            default:
                {
                    // No clear road inside the box: the line's answer stands, whichever it was.
                    if (lineRefused)
                    {
                        RouteNone++;

                        if (tally != null)
                        {
                            tally.Refused++;
                        }

                        RoadHow = "on the straight line, no clear road being found near it";
                    }

                    return lineRefused;
                }
        }
    }

    private static void Offered(uint serial, Point3D to, string trade, long now)
    {
        if (!_offered.TryGetValue(serial, out var offers))
        {
            if (_offered.Count > 4096)
            {
                _offered.Clear();
            }

            _offered[serial] = offers = [];
        }

        var offer = OfferTo(offers, to, now);

        if (offer != null && Math.Max(Math.Abs(offer.To.X - to.X), Math.Abs(offer.To.Y - to.Y)) <= Cell / 2)
        {
            offer.Tick = now;
            offer.Trade = trade ?? offer.Trade;

            return;
        }

        if (offers.Count >= MostOffers)
        {
            var oldest = 0;

            for (var i = 1; i < offers.Count; i++)
            {
                if (offers[i].Tick - offers[oldest].Tick < 0)
                {
                    oldest = i;
                }
            }

            offer = offers[oldest];
            offer.Walked = false;
        }
        else
        {
            offers.Add(offer = new Offer());
        }

        offer.To = to;
        offer.Tick = now;
        offer.Trade = trade;
    }

    private static bool Unjudged(bool lineRefused, RoadTally tally, string how, int why)
    {
        if (!lineRefused)
        {
            return false;
        }

        switch (why)
        {
            case 0:
                RouteBudget++;
                RouteBusy++;
                break;
            case 1:
                RouteBudget++;
                break;
            default:
                RouteUnplaced++;
                break;
        }

        if (tally != null)
        {
            tally.Unjudged++;
        }

        RoadHow = how;

        return true;
    }

    /// <summary>
    /// The cheapest road from <paramref name="from"/> to <paramref name="to"/> over the navigation graph clear of ground beyond
    /// a walker of <paramref name="strength"/> (<see cref="Reads"/>), at the planners' own prices for the rest, inside the box.
    /// False when the graph cannot place an end (not drawn, no node in the end's square of the graph, another piece of
    /// ground); otherwise the verdict: clear, a detour the planner would not draw (<see cref="RouteSlack"/>), or none found
    /// inside the box and the budget.
    /// </summary>
    private static bool Search(Map map, Point3D from, Point3D to, double strength, int step, double dare, out Kept kept)
    {
        kept = default;

        var graph = NavigationService.Graph(map);

        if (graph == null || !graph.Contains(from.X, from.Y) || !graph.Contains(to.X, to.Y))
        {
            return false;
        }

        var sc = graph.ClusterOf(from.X, from.Y);
        var gc = graph.ClusterOf(to.X, to.Y);
        var starts = graph.NodesOf(sc);
        var goals = graph.NodesOf(gc);

        if (starts == null || starts.Count == 0 || goals == null || goals.Count == 0 || sc == gc)
        {
            return false;
        }

        // The pieces of ground the ends stand on: a road between two is a gate's, which this does not judge.
        var piece = NavigationService.ComponentOf(map, from);
        var pieceTo = NavigationService.ComponentOf(map, to);

        if (piece >= 0 && pieceTo >= 0 && piece != pieceTo)
        {
            return false;
        }

        if (piece < 0)
        {
            piece = pieceTo;
        }

        var size = graph.Capacity + 1;

        if (_g.Length < size)
        {
            var n = Math.Max(size, _g.Length * 2);

            _g = new int[n];
            _parent = new int[n];
            _seen = new int[n];
            _closed = new int[n];
            _search = 0;
        }

        if (++_search == int.MaxValue)
        {
            Array.Clear(_seen);
            Array.Clear(_closed);
            _search = 1;
        }

        // Priced for a walker of this strength standing here, as the walk's own draw would be (For), and the planners' tallies
        // left as they were: these are questions, not routes. What one walker has in sight is left out: the verdict is kept
        // for every walker of that strength in the square, and the walk prices its own sight when it is drawn.
        var walker = _walker;
        var walkerMap = _walkerMap;
        var walkerStrength = _strength;
        var standsOpen = _standsOpen;
        var afloat = _afloat;
        var sighted = _sighted;
        var nodes = Asked;
        var bloodied = Bloodied;
        var barred = Barred;
        var fringe = InFringe;
        var hostile = InHostile;
        var deadly = InDeadly;
        var cut = CutThrough;
        var beyond = BeyondWalker;
        var refusedNodes = RefusedNodes;
        var inSight = InSight;

        _walker = ByWalker ? _judge : null;
        _walkerMap = map;
        _strength = Math.Max(1.0, strength);
        _standsOpen = BotReachMap.Standing(map, from, out var want) && want == BotReachClass.Open;
        _afloat = want == BotReachClass.Water;
        _sighted = null;

        var found = -1;
        var bar = Math.Max(1.0, strength) * dare;
        var stale = Math.Max(bar, Math.Max(1.0, strength) * RoadStaleDare);
        var standing = BotZones.ZoneAt(map, from.X, from.Y)?.Id ?? 0;
        var ending = BotZones.ZoneAt(map, to.X, to.Y)?.Id ?? 0;
        var slack = RouteSlack;
        var expanded = 0;

        try
        {
            var x1 = Math.Min(from.X, to.X) - RouteDetour;
            var y1 = Math.Min(from.Y, to.Y) - RouteDetour;
            var x2 = Math.Max(from.X, to.X) + RouteDetour;
            var y2 = Math.Max(from.Y, to.Y) + RouteDetour;

            _open.Clear();

            for (var i = 0; i < starts.Count; i++)
            {
                var a = starts[i];

                if (piece >= 0 && graph.Component(a) >= 0 && graph.Component(a) != piece)
                {
                    continue;
                }

                Relax(a, NavCost.Estimate(from.X, from.Y, graph.X(a), graph.Y(a)), -1, NavCost.Estimate(graph.X(a), graph.Y(a), to.X, to.Y));
            }

            while (_open.TryDequeue(out var node, out _))
            {
                if (_closed[node] == _search)
                {
                    continue;
                }

                _closed[node] = _search;

                if (graph.ClusterOfNode(node) == gc)
                {
                    found = node;

                    break;
                }

                if (++expanded > RouteMost)
                {
                    break;
                }

                if (!graph.EdgesOf(node, out var next, out var weights, out var first, out var until))
                {
                    continue;
                }

                var cost = _g[node];

                for (var e = first; e < until; e++)
                {
                    var b = next[e];

                    if (_closed[b] == _search)
                    {
                        continue;
                    }

                    var x = graph.X(b);
                    var y = graph.Y(b);

                    if (x < x1 || x > x2 || y < y1 || y > y2)
                    {
                        continue;
                    }

                    // Not walked through: ground beyond the walker, past the ends. Read once a node, as the search closes it after.
                    if (Math.Max(Math.Abs(x - from.X), Math.Abs(y - from.Y)) > step && Math.Max(Math.Abs(x - to.X), Math.Abs(y - to.Y)) > step)
                    {
                        var reads = Reads(map, x, y, bar, stale, standing, ending, out _, out var zone);

                        if (reads != RouteVerdict.Clear)
                        {
                            _closed[b] = _search;
                            RouteBlocked++;

                            if (zone != 0)
                            {
                                slack = Math.Min(slack, Math.Clamp((int)(HostilePerBot * (bar / Math.Max(1.0, strength)) / Math.Max(0.01, ZoneRoadShare)), HostileLeast, HostileMost));
                            }

                            continue;
                        }
                    }

                    Relax(b, cost + weights[e] + Cost(x, y), node, NavCost.Estimate(x, y, to.X, to.Y));
                }
            }

            RouteExpanded += expanded;

            if (found < 0)
            {
                RouteExhausted++;
            }
        }
        finally
        {
            _walker = walker;
            _walkerMap = walkerMap;
            _strength = walkerStrength;
            _standsOpen = standsOpen;
            _afloat = afloat;
            _sighted = sighted;
            Asked = nodes;
            Bloodied = bloodied;
            Barred = barred;
            InFringe = fringe;
            InHostile = hostile;
            InDeadly = deadly;
            CutThrough = cut;
            BeyondWalker = beyond;
            RefusedNodes = refusedNodes;
            InSight = inSight;
        }

        if (found < 0)
        {
            kept.Verdict = RouteVerdict.None;

            return true;
        }

        // The road the planner would draw, or a detour it would not: any road through one node beyond the walker costs it at
        // least the straight distance and the blood's price more.
        var straight = NavCost.Estimate(from.X, from.Y, to.X, to.Y);

        kept.Verdict = _g[found] <= straight + slack * NavCost.Step ? RouteVerdict.Clear : RouteVerdict.Detour;
        kept.Asked = (float)(_g[found] / (double)NavCost.Step);

        return true;
    }

    private static void Relax(int node, int cost, int parent, int estimate)
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

        // f first; at equal f the larger g, and the estimate leant a thousandth towards the goal, as the medium tier's is.
        _open.Enqueue(node, ((long)(cost + estimate + estimate / 1000) << 32) | (uint)(int.MaxValue - cost));
    }

    /// <summary>
    /// What one node of a judged road reads against the walker: as a sample of the line did, but for the age of the record.
    /// See the class note.
    /// </summary>
    private static RouteVerdict Reads(Map map, int x, int y, double bar, double stale, int standing, int ending, out double asks, out int zone)
    {
        asks = 0.0;
        zone = 0;

        var here = new Point3D(x, y, 0);
        var record = BotQuad.Muscle(map, here);

        // The town is the town: guarded ground on the way asks nothing, whatever a zone's paint says of it. Asked only of a
        // node something would refuse (the region lookup is the dear part, and the search asks this of every node).
        if (record > bar && Region.Find(here, map)?.IsPartOf<GuardedRegion>() == true)
        {
            return RouteVerdict.Clear;
        }

        if (record > bar)
        {
            var quad = BotQuad.Known(map, here);
            var now = Core.TickCount;
            var fresh = quad != null
                        && (quad.DiedTick != 0 && now - quad.DiedTick < RoadFreshMs
                            || quad.Mobs > 0 && now - quad.Sighted < BotQuad.SightMs
                            || quad.DiedTick == 0 && quad.Deaths > 0 && (!_booted || now - _bootTick < RoadFreshMs));

            if (fresh || record > stale)
            {
                asks = record;

                return fresh ? RouteVerdict.Record : RouteVerdict.Old;
            }
        }

        if (ZoneRoadShare <= 0.0)
        {
            return RouteVerdict.Clear;
        }

        var level = BotZones.LevelAt(map, x, y, out _, out var threat);

        if (level is not (BotZoneLevel.Deadly or BotZoneLevel.Hostile))
        {
            return RouteVerdict.Clear;
        }

        var id = BotZones.ZoneAt(map, x, y)?.Id ?? 0;

        if (id == 0 || id == standing || id == ending)
        {
            return RouteVerdict.Clear;
        }

        var fight = threat * ZoneRoadShare;

        if (fight > bar && Region.Find(here, map)?.IsPartOf<GuardedRegion>() != true)
        {
            asks = fight;
            zone = id;

            return RouteVerdict.Zone;
        }

        return RouteVerdict.Clear;
    }

    /// <summary>
    /// The walk is drawing a leg towards <paramref name="target"/>: if that is a road offered to this walker that the line had
    /// refused, the walk is counted once and the leg's time kept for <see cref="Fell"/>. Called by the walk beside
    /// <see cref="For"/>.
    /// </summary>
    public static void Walks(Mobile walker, Point3D target)
    {
        if (_offered.Count == 0 || walker == null || target == Point3D.Zero || !_offered.TryGetValue(walker.Serial.Value, out var offers))
        {
            return;
        }

        var now = Core.TickCount;
        var offer = OfferTo(offers, target, now);

        if (offer == null)
        {
            return;
        }

        if (!offer.Walked)
        {
            offer.Walked = true;
            RouteWalked++;
        }

        _walking[walker.Serial.Value] = (now, offer.Trade);
    }

    /// <summary>A bot has died: counted when its walk drew a leg to a road offered here within <see cref="RouteDeathMs"/>. Called by <c>BotKept.Fell</c>.</summary>
    public static void Fell(Mobile bot)
    {
        if (_walking.Count == 0 || bot == null || !_walking.Remove(bot.Serial.Value, out var walking))
        {
            return;
        }

        if (Core.TickCount - walking.Tick >= RouteDeathMs)
        {
            return;
        }

        RouteDied++;

        if (walking.Trade != null)
        {
            TallyOf(walking.Trade).Died++;
        }
    }

    private static string DescribeRoutes()
    {
        if (!RouteJudged)
        {
            return $"the planner's road is not judged where the line refuses (off): {LineRefused} refused by the line";
        }

        var trades = "";

        if (_trades.Count > 0)
        {
            var rows = new List<(string Trade, RoadTally Tally)>(_trades.Count);

            foreach (var (trade, tally) in _trades)
            {
                if (tally.Line > 0)
                {
                    rows.Add((trade, tally));
                }
            }

            rows.Sort((a, b) => b.Tally.Line.CompareTo(a.Tally.Line));

            for (var i = 0; i < rows.Count && i < 14; i++)
            {
                var t = rows[i].Tally;

                trades += $"{(i == 0 ? "" : ", ")}{rows[i].Trade} {t.Line} → {t.Offered} offered, {t.Refused} still refused, {t.Unjudged} not judged, {t.Died} died";
            }
        }

        return $"a clear road judged where the line refused: {LineRefused} refused by the line, {RouteBudget} of them left so past {RoutePerSecond} searches or {RouteMsPerSecond:F0}ms a second "
               + $"({RouteBusy} by a walker holding work past its {RouteBusyShare:P0}) and {RouteUnplaced} for an end the graph could not place; {RouteAsked} judged "
               + $"({RouteKept} from a verdict kept {RouteKeepMs / 1000}s, {RouteSearches} searched in {RouteMs:F0}ms over {RouteExpanded} nodes, {RouteBlocked} not walked through, {RouteExhausted} searches finding no clear road): "
               + $"{RouteOffered} offered on a clear road the planner would draw, {RouteDetours} with a clear road dearer than the straight distance by {RouteSlack} tiles, "
               + $"{RouteNone} with no clear road within {RouteDetour} tiles of the line and {RouteMost} nodes"
               + (RouteAlways ? $"; {RouteStricter} roads the line passed refused on the planner's road" : "")
               + $"; {RouteWalked} such roads walked and {RouteDied} deaths within {RouteDeathMs / 1000}s of a leg on one"
               + (trades.Length > 0 ? $"; by trade, of the appraisal's asks (refused by the line → …): {trades}" : "");
    }

    private static void ForgetRoutes()
    {
        LineRefused = 0;
        RouteBudget = 0;
        RouteUnplaced = 0;
        RouteAsked = 0;
        RouteKept = 0;
        RouteSearches = 0;
        RouteExpanded = 0;
        RouteOffered = 0;
        RouteDetours = 0;
        RouteExhausted = 0;
        RouteBlocked = 0;
        RouteBusy = 0;
        RouteNone = 0;
        RouteStricter = 0;
        RouteWalked = 0;
        RouteDied = 0;
        _routeTicks = 0;
        _trades.Clear();
        _kept.Clear();
        _offered.Clear();
        _walking.Clear();
    }
}
