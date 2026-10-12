using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Ground another guild holds, and what may not be put on it.
///
/// <para>
/// <b>Patrick, 06.10.2026 ~10:00: "The Ash has put its residence on The Wolf's lands: how could that happen? It is
/// forbidden."</b> It happened at 01:16:38 that night. The steward's plot search (<c>BotPlot</c>) asked whether a candidate
/// was another guild's ground of its centre tile alone; The Ash's hall went up at 1470,1412, in a square nobody held, and its
/// seven-by-seven walls and its steps (x 1467–1473, y 1409–1416) stood three columns deep in The Wolf's square at 1440,1410
/// and one row deep in its square at 1440,1380, held since before 04.10 22:50. Raising it wrote the hall's square into the
/// claims as The Ash's (<c>BotClaim.Seat</c>), which would have overwritten another guild's holding without a word had the
/// centre been on one; and the seat followed the hall there. Nothing ever looked again when ground round a standing hall
/// changed hands: The Wolf took the square north of it at 07:21:28 and the hall stood on The Wolf's ground on two sides.
/// </para>
///
/// <para>
/// <b>The rule.</b> A guild building — a first hall, a bigger one, an outpost, a hall carried anywhere — stands wholly on
/// ground its guild will hold: the square of its centre must be open or the guild's own, and every other square under its
/// walls and steps must be the guild's own (<see cref="Covers"/>, strict). A seat, a guild's town house and a bare seat
/// are points, and the point must not be another guild's (<see cref="Foreign"/>). A bot's own house may not stand on another
/// guild's ground unless the two guilds are allied (lenient, allies welcome). A hall's yard never swallows a square another
/// guild holds (<c>BotClaim.Seat</c>), and a claim on held ground is only ever the seizure, which is a war
/// (<c>BotSeizure</c>). Held ground is what <see cref="BotLand.Holder"/> answers: claimed squares, halls' squares and
/// outposts' squares.
/// </para>
///
/// <para>
/// <b>And when ground changes hands, the look that was missing.</b> Every <see cref="LookMs"/> each standing hall is asked
/// whether any square under it is now another guild's; one that is owes a move, which the remover carries to the nearest
/// ground of its own or open ground (<c>BotRemover</c>, the errand that carries a hall to its seat), and the seat follows
/// the hall. A bare seat on another guild's ground is moved to the nearest point that is not, within <see cref="SeatReach"/>;
/// a guild's town house on it is left (<c>BotGuildHouses.Leave</c>, the path a seat set at the door takes) and the guild
/// settles again in a building that is not. Nothing within reach: the hall stands and waits, counted every look, said once.
/// </para>
/// </summary>
public static class BotBounds
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBounds));

    /// <summary>Whether the rule is kept. Off, and placements ask the centre tile alone, as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often the standing halls, seats and houses are looked at for ground that changed hands.</summary>
    public static int LookMs { get; set; } = 30000;

    /// <summary>How far round a bare seat on another guild's ground a point of its own or open ground is looked for.</summary>
    public static int SeatReach { get; set; } = 90;

    /// <summary>Why a building may not stand where it was asked to.</summary>
    public enum Breach
    {
        None,

        /// <summary>A square under it is another guild's.</summary>
        Foreign,

        /// <summary>Its walls reach out of its own square onto open ground, which a hall would then hold without claiming.</summary>
        Straddling
    }

    /// <summary>Placements refused at the moment of building, by what was being built.</summary>
    public enum Placing
    {
        Hall,
        Bigger,
        Outpost,
        Carry,
        Abode
    }

    private static readonly long[] _refused = new long[5];

    /// <summary>Seats refused (set at the door, chosen at founding, re-seated by a town) for lying on another guild's ground.</summary>
    public static long SeatsRefused { get; private set; }

    /// <summary>Town buildings passed over for a guild because their heart lies on another guild's ground. Counts looks.</summary>
    public static long HousesRefused { get; private set; }

    /// <summary>Halls raised or adopted in a square another guild holds, whose yard was not written over that guild's holding.</summary>
    public static long YardsKept { get; private set; }

    /// <summary>Open squares another guild's hall stands partly in, passed over by a staker. Counts looks.</summary>
    public static long Spared { get; private set; }

    /// <summary>Halls found standing on another guild's ground (a new finding, not every look), halls carried off it, looks that found no ground to carry one to.</summary>
    public static long HallsFound { get; private set; }

    public static long HallsMoved { get; private set; }

    public static long HallsOffered { get; private set; }

    public static long HallsWaiting { get; private set; }

    /// <summary>Bare seats moved off another guild's ground, and looks that found nowhere to move one.</summary>
    public static long SeatsMoved { get; private set; }

    public static long SeatsWaiting { get; private set; }

    /// <summary>Guild town houses left for standing on another guild's ground.</summary>
    public static long HousesLeft { get; private set; }

    /// <summary>Bots' own houses standing on the ground of a guild not their own nor allied, at the last look. Not moved: see <see cref="Look"/>.</summary>
    public static int AbodesAbroad { get; private set; }

    /// <summary>
    /// A hall's move off another guild's ground, step by step (06.10.2026, build 410). <b>The first cut carried The Ash 112 tiles away
    /// and dropped its square.</b> Its hall at 1470,1412 stood in its own square with only its west and north walls on The Wolf's;
    /// the remover asked <c>BotPlot.Find</c>, whose spiral begins <c>BotPlot.Near</c> (14) tiles out and whose engine check counts
    /// the standing hall as an obstacle, so the shift of a few tiles that would have fitted was never put to anything: 256 offers
    /// found nothing in 17 minutes, the next found 1522,1300, and the square, no longer reaching the hall, fell (10:39:39).
    /// Now: a shift inside the hall's own square with the standing hall's tiles free (<c>BotShift</c>), then the guild's other
    /// squares near it; then open ground within <see cref="OpenReach"/>; then the hall stands, its foreign part giving it nothing.
    /// </summary>
    private sealed class Owing
    {
        public string Whose;

        /// <summary>Whether the shift within the guild's own ground has been tried and failed this stretch.</summary>
        public bool ShiftTried;

        /// <summary>Offers that looked for open ground within reach and found none.</summary>
        public int OpenMisses;

        /// <summary>Until when the hall is let stand (<see cref="TolerateMs"/>); nought while it owes.</summary>
        public long StandUntil;
    }

    /// <summary>Who owes a move off whose ground.</summary>
    private static readonly Dictionary<string, Owing> _owed = [];

    /// <summary>How many candidate plots a shift puts to the engine in one go, nearest first.</summary>
    public static int ShiftChecks { get; set; } = 150;

    /// <summary>How many squares round the hall's own (Chebyshev) the guild's other held squares are searched for a shift.</summary>
    public static int ShiftSquares { get; set; } = 2;

    /// <summary>How far from the hall open ground may be and still be carried to.</summary>
    public static int OpenReach { get; set; } = 90;

    /// <summary>Offers that may find no open ground within reach before the hall is let stand.</summary>
    public static int OpenLooks { get; set; } = 40;

    /// <summary>How long a hall nothing could be found for is let stand before the move is tried again; said once a stand.</summary>
    public static int TolerateMs { get; set; } = 3600000;

    /// <summary>Halls shifted within their own square, and onto the guild's other squares; shifts that found nothing and put the hall back where it stood.</summary>
    public static long ShiftedOwn { get; private set; }

    public static long ShiftedHeld { get; private set; }

    public static long ShiftsFailed { get; private set; }

    /// <summary>Open ground found but further than <see cref="OpenReach"/>; halls let stand for want of anything (once a stand).</summary>
    public static long OpenTooFar { get; private set; }

    public static long Stood { get; private set; }

    /// <summary>Seats said to be waiting, so the waiting is said once a stretch.</summary>
    private static readonly HashSet<string> _waitingSaid = [];

    private static long _looked;

    /// <summary>Whether this point lies on ground a guild other than <paramref name="guild"/> holds, and whose.</summary>
    public static bool Foreign(string guild, Map map, Point3D at, out string whose)
    {
        whose = null;

        if (!Running || map == null || map == Map.Internal)
        {
            return false;
        }

        var holder = BotLand.Holder(map, at);

        if (holder == null || holder == guild)
        {
            return false;
        }

        whose = holder;

        return true;
    }

    /// <summary>The same for a building of <paramref name="multi"/> centred on <paramref name="centre"/>. See <see cref="Covers(string, Map, Point3D, Point2D, Point2D, bool, bool, out string)"/>.</summary>
    public static Breach Covers(string guild, Map map, Point3D centre, int multi, bool strict, bool alliesWelcome, out string whose)
    {
        var mcl = MultiData.GetComponents(multi);

        return Covers(guild, map, centre, mcl.Min, mcl.Max, strict, alliesWelcome, out whose);
    }

    /// <summary>
    /// Whether a building whose tiles run from <paramref name="min"/> to <paramref name="max"/> about <paramref name="centre"/>
    /// would stand on ground it may not. Every square its bounds touch is asked once (four for a small hall at most).
    /// <paramref name="strict"/>, for a guild building: the centre's square open or the guild's own, and every other square the
    /// guild's own. Not strict, for a hall already standing or a bot's house: no square another guild's, allies' ground welcome
    /// when <paramref name="alliesWelcome"/>.
    /// </summary>
    public static Breach Covers(string guild, Map map, Point3D centre, Point2D min, Point2D max, bool strict, bool alliesWelcome, out string whose)
    {
        whose = null;

        if (!Running || map == null || map == Map.Internal)
        {
            return Breach.None;
        }

        var home = BotQuad.Key(map, centre);
        var low = BotQuad.Key(map, new Point3D(centre.X + min.X, centre.Y + min.Y, 0));
        var high = BotQuad.Key(map, new Point3D(centre.X + max.X, centre.Y + max.Y, 0));

        for (var qx = low.X; qx <= high.X; qx++)
        {
            for (var qy = low.Y; qy <= high.Y; qy++)
            {
                var holder = BotLand.Holder(map, new Point3D(qx * BotQuad.Side + BotQuad.Side / 2, qy * BotQuad.Side + BotQuad.Side / 2, 0));

                if (holder == guild && guild != null)
                {
                    continue;
                }

                if (holder == null)
                {
                    if (strict && (qx != home.X || qy != home.Y))
                    {
                        whose = "open ground";

                        return Breach.Straddling;
                    }

                    continue;
                }

                if (alliesWelcome && BotRegard.AreAllied(guild, holder))
                {
                    continue;
                }

                whose = holder;

                return Breach.Foreign;
            }
        }

        return Breach.None;
    }

    /// <summary>
    /// The building about to go up on <paramref name="plot"/> may not, because the ground under it changed while the bot walked
    /// there. Asked by each building errand at arrival, before the engine is asked and before anything is paid or knocked
    /// down. True when refused; counted by what was being built and said.
    /// </summary>
    public static bool Refuse(Placing what, string guild, Map map, Point3D plot, int multi, out string why)
    {
        why = null;

        var abode = what == Placing.Abode;
        var breach = Covers(guild, map, plot, multi, !abode, abode, out var whose);

        if (breach == Breach.None)
        {
            return false;
        }

        _refused[(int)what]++;

        why = breach == Breach.Foreign
            ? $"the ground at {plot.X},{plot.Y} lies partly on {whose}'s held ground"
            : $"the walls at {plot.X},{plot.Y} would reach out of their own square onto open ground";

        logger.Information("{Guild}: no {What} goes up at {X},{Y}: {Why}", guild ?? "a bot of no guild", Name(what), plot.X, plot.Y, why);

        return true;
    }

    private static string Name(Placing what) =>
        what switch
        {
            Placing.Hall => "hall",
            Placing.Bigger => "bigger hall",
            Placing.Outpost => "outpost",
            Placing.Carry => "hall carried",
            _ => "house of its own"
        };

    /// <summary>A seat refused for lying on another guild's ground. Said by the caller where it has something to say.</summary>
    public static void SeatRefused() => SeatsRefused++;

    /// <summary>A town building passed over for its heart lying on another guild's ground.</summary>
    public static void HouseRefused() => HousesRefused++;

    /// <summary>An open square another guild's hall stands partly in, passed over by a staker.</summary>
    public static void Spare() => Spared++;

    /// <summary>
    /// A hall was raised or adopted in a square <paramref name="holder"/> holds: the square stays <paramref name="holder"/>'s, and the
    /// hall owes a move off it, which the next look files. See <c>BotClaim.Seat</c>.
    /// </summary>
    public static void YardKept(string guild, string holder, Point3D hall)
    {
        YardsKept++;

        logger.Warning(
            "The hall of {Guild} at {X},{Y} stands in a square {Holder} holds; the square stays {Holder}'s, and the hall is to be carried off it",
            guild,
            hall.X,
            hall.Y,
            holder,
            holder
        );
    }

    /// <summary>
    /// The guild of the hall that stands partly in this square, other than <paramref name="guild"/>'s, or nothing. A staker does
    /// not claim open ground another guild's hall stands in: taking it would carry the hall off without a war.
    /// </summary>
    public static string HallIn(string guild, Map map, (int Map, int X, int Y) key)
    {
        if (!Running || map == null)
        {
            return null;
        }

        foreach (var (name, hall) in BotEstate.Held)
        {
            if (name == guild || hall is not { Deleted: false } || hall.Map != map || hall.Components == null)
            {
                continue;
            }

            var low = BotQuad.Key(map, new Point3D(hall.X + hall.Components.Min.X, hall.Y + hall.Components.Min.Y, 0));
            var high = BotQuad.Key(map, new Point3D(hall.X + hall.Components.Max.X, hall.Y + hall.Components.Max.Y, 0));

            if (key.X >= low.X && key.X <= high.X && key.Y >= low.Y && key.Y <= high.Y)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether this guild's hall stands on another guild's ground and owes a move now, and the hall and whose ground. A hall let
    /// stand (<see cref="TolerateMs"/>) owes nothing until its stand runs out; then the steps begin again from the shift.
    /// </summary>
    public static bool Owes(Guild guild, out BaseHouse hall, out string whose)
    {
        hall = null;
        whose = null;

        if (!Running || guild == null || !_owed.TryGetValue(guild.Name, out var owing))
        {
            return false;
        }

        hall = BotEstate.Hall(guild);

        if (hall is not { Deleted: false })
        {
            _owed.Remove(guild.Name);

            return false;
        }

        if (owing.StandUntil != 0)
        {
            if (Core.TickCount - owing.StandUntil < 0)
            {
                return false;
            }

            owing.StandUntil = 0;
            owing.ShiftTried = false;
            owing.OpenMisses = 0;
        }

        whose = owing.Whose;

        return true;
    }

    /// <summary>Whether the next step for this guild's hall is the shift within its own ground (true) or open ground (false).</summary>
    public static bool Shifting(string guild) => guild != null && _owed.TryGetValue(guild, out var owing) && !owing.ShiftTried;

    /// <summary>The remover was sent to carry this guild's hall off.</summary>
    public static void Offered() => HallsOffered++;

    /// <summary>
    /// The candidates for a shift of <paramref name="old"/> (a building of <paramref name="multi"/>), nearest first: its own square,
    /// every tile; then the guild's other squares within <see cref="ShiftSquares"/>, every other tile. Each stands wholly on the
    /// guild's ground (<see cref="Covers(string, Map, Point3D, int, bool, bool, out string)"/>, strict) and passes our own tests
    /// (<c>BotPlot.Suits</c>) with the standing hall not counted. The engine is asked by the shift itself, with the hall down.
    /// </summary>
    public static List<(Point3D At, bool Own)> Shifts(string guild, Map map, BaseHouse old, int multi)
    {
        var found = new List<(Point3D At, bool Own, int Gap)>();

        if (guild == null || map == null || old == null)
        {
            return [];
        }

        var home = BotQuad.Key(map, old.Location);

        foreach (var (holder, facet, qx, qy, _) in BotClaim.Holdings())
        {
            if (holder != guild || facet != map.MapID || Math.Max(Math.Abs(qx - home.X), Math.Abs(qy - home.Y)) > ShiftSquares)
            {
                continue;
            }

            var own = qx == home.X && qy == home.Y;
            var step = own ? 1 : 2;

            for (var dx = 0; dx < BotQuad.Side; dx += step)
            {
                for (var dy = 0; dy < BotQuad.Side; dy += step)
                {
                    var x = qx * BotQuad.Side + dx;
                    var y = qy * BotQuad.Side + dy;

                    if (x == old.X && y == old.Y)
                    {
                        continue;
                    }

                    if (Covers(guild, map, new Point3D(x, y, 0), multi, true, false, out _) != Breach.None)
                    {
                        continue;
                    }

                    map.GetAverageZ(x, y, out _, out var avg, out _);

                    var at = new Point3D(x, y, avg);

                    if (!BotPlot.Suits(map, at, multi, old))
                    {
                        continue;
                    }

                    found.Add((at, own, Math.Max(Math.Abs(x - old.X), Math.Abs(y - old.Y))));
                }
            }
        }

        found.Sort((a, b) => a.Own != b.Own ? (a.Own ? -1 : 1) : a.Gap.CompareTo(b.Gap));

        var list = new List<(Point3D At, bool Own)>(found.Count);

        for (var i = 0; i < found.Count; i++)
        {
            list.Add((found[i].At, found[i].Own));
        }

        return list;
    }

    /// <summary>The shift found no plot the engine would take; the hall was put back where it stood. Next: open ground.</summary>
    public static void ShiftFailed(string guild, string why)
    {
        ShiftsFailed++;

        if (guild != null && _owed.TryGetValue(guild, out var owing))
        {
            owing.ShiftTried = true;
        }

        logger.Warning("The hall of {Guild} could not be shifted onto ground of its own: {Why}; open ground within {Reach} tiles is looked for next", guild, why, OpenReach);
    }

    /// <summary>The hall has been shifted onto its own ground: within its own square (<paramref name="own"/>) or onto another square the guild holds.</summary>
    public static void Shifted(string guild, Point3D from, Point3D at, bool own)
    {
        if (own)
        {
            ShiftedOwn++;
        }
        else
        {
            ShiftedHeld++;
        }

        Owing owing = null;

        if (guild != null)
        {
            _owed.Remove(guild, out owing);
            _waitingSaid.Remove(guild);
        }

        logger.Warning(
            "The hall of {Guild} has been shifted off {Whose}'s ground from {FX},{FY} to {X},{Y}, {Where}; its seat follows it",
            guild,
            owing?.Whose ?? "another guild",
            from.X,
            from.Y,
            at.X,
            at.Y,
            own ? "within its own square" : "onto another square it holds"
        );
    }

    /// <summary>
    /// The remover's look for open ground found none, or found it further than <see cref="OpenReach"/>. After <see cref="OpenLooks"/>
    /// misses, or at once for ground too far, the hall is let stand for <see cref="TolerateMs"/>, said once a stand.
    /// </summary>
    public static void Missed(string guild, string whose, bool tooFar)
    {
        HallsWaiting++;

        if (tooFar)
        {
            OpenTooFar++;
        }

        if (guild == null || !_owed.TryGetValue(guild, out var owing))
        {
            return;
        }

        if (!tooFar && ++owing.OpenMisses < OpenLooks)
        {
            return;
        }

        var rest = Math.Max(60000, TolerateMs);

        owing.StandUntil = Core.TickCount + rest;
        Stood++;

        logger.Warning(
            "The hall of {Guild} stands with part of its walls on {Whose}'s ground: no ground of its own fits it and no open ground lies within {Reach} tiles; it stands, the part on {Whose}'s ground giving it no yard, no land and no toll, and the move is tried again in {Minutes} minutes",
            guild,
            whose,
            OpenReach,
            whose,
            rest / 60000
        );
    }

    /// <summary>The hall has been carried off another guild's ground to <paramref name="at"/>.</summary>
    public static void Paid(string guild, Point3D at)
    {
        if (guild == null || !_owed.Remove(guild, out var owing))
        {
            return;
        }

        HallsMoved++;
        _waitingSaid.Remove(guild);

        logger.Warning("The hall of {Guild} has been carried off {Whose}'s ground to {X},{Y}; its seat follows it", guild, owing.Whose, at.X, at.Y);
    }

    /// <summary>
    /// The look: halls on ground that is now another guild's are filed for the remover; bare seats are moved off it; town
    /// houses on it are left. From <c>BotClaim.Look</c>, on its own clock.
    /// </summary>
    public static void Look()
    {
        if (!Running || !BotEstate.HallsAdopted)
        {
            return;
        }

        var now = Core.TickCount;

        if (now - (_looked + LookMs) < 0)
        {
            return;
        }

        _looked = now;

        LookHalls();
        LookSeats();
        LookAbodes();
    }

    private static readonly List<string> _cleared = [];

    private static void LookHalls()
    {
        _cleared.Clear();

        foreach (var name in _owed.Keys)
        {
            if (!BotEstate.Held.TryGetValue(name, out var gone) || gone is not { Deleted: false })
            {
                _cleared.Add(name);
            }
        }

        foreach (var (name, hall) in BotEstate.Held)
        {
            if (hall is not { Deleted: false } || hall.Map == null || hall.Map == Map.Internal || hall.Components == null)
            {
                continue;
            }

            var breach = Covers(name, hall.Map, hall.Location, hall.Components.Min, hall.Components.Max, false, false, out var whose);

            if (breach == Breach.Foreign)
            {
                if (_owed.TryAdd(name, new Owing { Whose = whose }))
                {
                    HallsFound++;

                    logger.Warning(
                        "The hall of {Guild} at {X},{Y} stands on {Whose}'s held ground; it is to be carried to the nearest ground of its own or open ground",
                        name,
                        hall.X,
                        hall.Y,
                        whose
                    );
                }
                else
                {
                    _owed[name].Whose = whose;
                }

                continue;
            }

            if (_owed.ContainsKey(name))
            {
                _cleared.Add(name);
            }
        }

        for (var i = 0; i < _cleared.Count; i++)
        {
            if (_owed.Remove(_cleared[i]))
            {
                _waitingSaid.Remove(_cleared[i]);
                logger.Information("The hall of {Guild} no longer stands on another guild's ground; it owes no move", _cleared[i]);
            }
        }
    }

    private static readonly List<Guild> _guilds = [];

    private static void LookSeats()
    {
        var map = BotPopulation.Home;

        if (map == null)
        {
            return;
        }

        _guilds.Clear();
        _guilds.AddRange(BotGuilds.Standing);

        for (var i = 0; i < _guilds.Count; i++)
        {
            var name = _guilds[i]?.Name;

            if (name == null)
            {
                continue;
            }

            // A guild's house in a town: left, and the settler finds it a building that is not on anybody's ground.
            if (BotGuildHouses.Of(name) is { } house && Foreign(name, map, house.Heart, out var owner))
            {
                if (BotGuildHouses.Leave(name, $"its house stands on {owner}'s held ground"))
                {
                    HousesLeft++;

                    logger.Warning("{Guild} leaves its house in {Town} at {X},{Y}: it stands on {Whose}'s held ground", name, house.Town, house.Heart.X, house.Heart.Y, owner);
                }

                continue;
            }

            var bare = BotSeat.Bare(name);

            if (bare == Point3D.Zero || !Foreign(name, map, bare, out var whose))
            {
                _waitingSaid.Remove("seat:" + name);

                continue;
            }

            // A guild with a hall owes its move to the remover, and the seat follows the hall when it is carried; a seat
            // moved apart from its hall now would carry the hall towards it a second time.
            if (_owed.ContainsKey(name))
            {
                continue;
            }

            if (Nearest(name, map, bare, out var at))
            {
                SeatsMoved++;
                _waitingSaid.Remove("seat:" + name);
                BotSeat.Shift(name, at, $"its seat at {bare.X},{bare.Y} lay on {whose}'s held ground");

                continue;
            }

            SeatsWaiting++;

            if (_waitingSaid.Add("seat:" + name))
            {
                logger.Warning(
                    "The seat of {Guild} at {X},{Y} lies on {Whose}'s held ground and no ground of its own or open ground lies within {Reach} tiles; it waits",
                    name,
                    bare.X,
                    bare.Y,
                    whose,
                    SeatReach
                );
            }
        }
    }

    /// <summary>The nearest point to <paramref name="from"/> a body can stand on that is not another guild's ground, ring by ring.</summary>
    private static bool Nearest(string guild, Map map, Point3D from, out Point3D at)
    {
        at = Point3D.Zero;

        for (var ring = 2; ring <= SeatReach; ring += 2)
        {
            for (var dx = -ring; dx <= ring; dx += 2)
            {
                for (var dy = -ring; dy <= ring; dy += 2)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring)
                    {
                        continue;
                    }

                    var x = from.X + dx;
                    var y = from.Y + dy;

                    if (Foreign(guild, map, new Point3D(x, y, 0), out _) || !BotStep.Settle(map, x, y, out var z))
                    {
                        continue;
                    }

                    at = new Point3D(x, y, z);

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Bots' own houses on the ground of a guild neither their own nor allied, counted and not moved. Decided: a house a bot
    /// bought is its own, the engine has no way to move one but to knock it down and pay again, and nothing about a guild's
    /// ground is worth that; the rule is kept where it can be, at the buying (<c>BotPlot.FindHome</c>, <c>BotAbode</c>).
    /// </summary>
    private static void LookAbodes()
    {
        var abroad = 0;

        foreach (var house in BotAbode.Houses)
        {
            if (house is not { Deleted: false } || house.Map == null || house.Components == null)
            {
                continue;
            }

            var guild = (house.Owner?.Guild as Guild)?.Name;

            if (Covers(guild, house.Map, house.Location, house.Components.Min, house.Components.Max, false, true, out _) != Breach.None)
            {
                abroad++;
            }
        }

        AbodesAbroad = abroad;
    }

    /// <summary>Part of the estate's line.</summary>
    public static string Describe() =>
        !Running
            ? "ground another guild holds is not kept from placement"
            : $"held ground kept: a guild building stands wholly on its own square or ground it holds, a seat or a house never on another guild's ground, a bot's house only on its own or an ally's; "
            + $"refused at the building: {_refused[(int)Placing.Hall]} halls, {_refused[(int)Placing.Bigger]} bigger halls, {_refused[(int)Placing.Outpost]} outposts, {_refused[(int)Placing.Carry]} halls carried, {_refused[(int)Placing.Abode]} bots' houses; "
            + $"{SeatsRefused} seats refused, {HousesRefused} looks passed over a town building on another guild's ground, {YardsKept} yards not written over another guild's square, {Spared} open squares not claimed for another guild's hall standing in them; "
            + $"ground changing hands: {HallsFound} halls found on another guild's ground ({_owed.Count} owing a move now), {ShiftedOwn} shifted within their own square, {ShiftedHeld} onto another square of their guild's, {ShiftsFailed} shifts that found nothing and put the hall back, {HallsOffered} carries offered, {HallsMoved} carried to open ground, {HallsWaiting} looks found no open ground within {OpenReach} tiles ({OpenTooFar} found it further), {Stood} halls let stand for {TolerateMs / 60000} minutes with their foreign part giving nothing; "
            + $"{SeatsMoved} seats moved off, {SeatsWaiting} looks found nowhere for one; {HousesLeft} town houses left; {AbodesAbroad} bots' houses on a guild's ground not their own nor allied (kept, not moved)";

    public static void Forget()
    {
        Array.Clear(_refused);
        SeatsRefused = 0;
        HousesRefused = 0;
        YardsKept = 0;
        Spared = 0;
        HallsFound = 0;
        HallsMoved = 0;
        HallsOffered = 0;
        HallsWaiting = 0;
        ShiftedOwn = 0;
        ShiftedHeld = 0;
        ShiftsFailed = 0;
        OpenTooFar = 0;
        Stood = 0;
        SeatsMoved = 0;
        SeatsWaiting = 0;
        HousesLeft = 0;
        AbodesAbroad = 0;
        _owed.Clear();
        _waitingSaid.Clear();
        _looked = 0;
    }
}
