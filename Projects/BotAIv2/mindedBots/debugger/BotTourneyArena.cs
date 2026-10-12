using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.Mind;

/// <summary>
/// The day's championship at the round arenas east of Papua: the other half of <see cref="BotTourney"/>.
///
/// <para>
/// <b>Patrick, 07.10.2026, ~21:35: "Still NOT ONCE have I seen a championship where a champion is chosen in battle. On the map at
/// about 6562 3165 there are round arenas — hold the skirmishes there. You port 2–3 groups, one group fights, the others get
/// ready, and so on."</b> And ten minutes later: "the championship can be held with a minimum of 50 participants, 25 sparrings, and
/// only once A DAY — human days, not game days." The hourly 1v1 knockout ran 37 championships on 06–07.10 and 22 title bouts, in
/// town rings nobody stood at, and 374 of its calls hit an entrant "with a company": the bracket ran an hour while the entrants went
/// on with their day, and companies took them out of it.
/// </para>
///
/// <para>
/// <b>The arenas are found, not typed.</b> At (6563, 3168) on Felucca's far east the map's own statics hold a lattice of twelve round
/// rings, each a closed double wall of stone (z −20 and 0) about 52 tiles across round a stone floor at z −20, in void the engine
/// calls impassable, with no region on them and no way in on foot. <see cref="ArenaLoad"/> reads the statics round
/// <see cref="ArenaAnchor"/> at the vigil's start, keeps every closed ring whose floor a body fits on, and logs each one's middle,
/// radius, floor and inside tiles; the ring holding the anchor is the main ring, where the referee stands and the title is fought.
/// </para>
///
/// <para>
/// <b>Once a real day, by the wall clock, and only with enough.</b> The calendar holds it from <see cref="StartHourLocal"/> local time
/// on a day none has been held, and tries again every <see cref="RetryMs"/> until midnight if it could not start; the day it was held
/// is a UTC instant in the world save (<see cref="BotTourneyStore"/>, shape 2), so a restart or a save does not hold it twice. It is
/// held only when at least <see cref="MinEntrants"/> free bots qualify — alive, on the map, in no company or party, in no duel, in no
/// cell, no murderer, not the Baron, not the champion — strongest first by <c>BotThreat.Power</c> (fighters, then crafters when
/// fighters alone are short, <see cref="Crafters"/>), up to <see cref="ArenaEntrants"/>. By hand, <c>do arena</c> holds the day's if it
/// has not been held, and <c>do arena force</c> holds one anyway for a test, logged as forced and not spending the day's.
/// </para>
///
/// <para>
/// <b>Batches, one pair to a ring.</b> Three minutes after the call (<see cref="NoticeMs"/>, announced in the world channel) round
/// one's pairs — seed against seed from the far end, the strongest with a bye when the field is odd — are split into batches of as
/// many pairs as there are fighting rings. Up to <see cref="PortedBatches"/> batches are on the arenas at once: one fighting, the
/// others waiting in rings of their own (outside a ring is void, so a waiting place is a ring), where they stand held by
/// <see cref="BotArenaWait"/> — their own kit, their own bandages, nothing handed out — while the referee heals each pair before its
/// bout as it always has. Every move is a moongate's flash at both ends. A bout is <see cref="BotDuel"/>'s: nobody dies, a fighter
/// at <c>BotDuel.YieldAt</c> of its hits yields, the clock decides by the share left. A winner goes to the winners' ring for the next
/// round; a loser is sent back to where it was ported from and takes up its work again. The last one standing challenges the champion
/// in the main ring (the existing title bout, <c>Title</c>), or is crowned to a vacant title by the final.
/// </para>
///
/// <para>
/// <b>Nobody forfeits for a company.</b> Only free bots are drawn; an entrant found busy when its batch is ported is replaced by the
/// strongest free bot of the reserve (<see cref="Replaced"/>), and from the port to the end of its part an entrant is held: the duel's
/// record answers "duelling" for it (<see cref="BotDuel.Held"/>), and the one door into a company (<c>BotSquads.Join</c>) and the one
/// test of a war's quarrel (<c>BotFeud.Quarrel</c>) refuse a duellist.
/// </para>
/// </summary>
public static partial class BotTourney
{
    /// <summary>Whether the championship is the day's at the arenas (true) or the hourly town-ring knockout (false).</summary>
    public static bool Arena { get; set; } = true;

    /// <summary>The fewest free bots the day's championship is held with. Patrick, 07.10.2026: "a minimum of 50 participants, 25 sparrings".</summary>
    public static int MinEntrants { get; set; } = 50;

    /// <summary>The most drawn. Sixty-four: a clean bracket of 32 sparrings when the island has them; fifty when it has fifty.</summary>
    public static int ArenaEntrants { get; set; } = 64;

    /// <summary>Whether crafters are drawn after the fighters when the fighters alone are fewer than <see cref="ArenaEntrants"/>.</summary>
    public static bool Crafters { get; set; } = true;

    /// <summary>The local hour from which the calendar holds the day's championship. Twenty: Patrick's evening.</summary>
    public static int StartHourLocal { get; set; } = 20;

    /// <summary>How long after a calendar call turned away (too few, no rings) the calendar asks again that day. Ten minutes.</summary>
    public static int RetryMs { get; set; } = 600000;

    /// <summary>
    /// The least time between two of the day's championships, whatever the dates say: a championship held by hand at 23:50 does not let
    /// the calendar hold the next at 00:00. Twelve hours.
    /// </summary>
    public static long LeastDayGapMs { get; set; } = 43200000L;

    /// <summary>How long between the call and the first port: the world channel's "begins in 3 minutes".</summary>
    public static int NoticeMs { get; set; } = 180000;

    /// <summary>How many batches stand on the arenas at once: one fighting and the rest waiting in rings of their own.</summary>
    public static int PortedBatches { get; set; } = 3;

    /// <summary>How many rings are fought in at once; 0 is every ring not used for waiting.</summary>
    public static int FightRings { get; set; } = 0;

    /// <summary>The point the arenas are found round: Patrick's "about 6562 3165", the middle of the main ring.</summary>
    public static Point3D ArenaAnchor { get; set; } = new(6563, 3168, -20);

    /// <summary>How far round the anchor the statics are read for rings, either way.</summary>
    public static int ArenaScan { get; set; } = 320;

    /// <summary>The fewest and most wall tiles a ring is made of: the rings here are 204, two walls stacked on each.</summary>
    public static int RingMinWall { get; set; } = 100;

    public static int RingMaxWall { get; set; } = 600;

    /// <summary>The narrowest and widest a ring may be across, in tiles: the rings here are 52.</summary>
    public static int RingMinSpan { get; set; } = 24;

    public static int RingMaxSpan { get; set; } = 90;

    /// <summary>The fewest floor tiles inside a ring for it to be fought in.</summary>
    public static int RingMinInside { get; set; } = 300;

    /// <summary>How far from a waiting ring's middle its entrants are scattered.</summary>
    public static int PenSpread { get; set; } = 9;

    /// <summary>How far north of the main ring's middle the referee stands, so it is beside the bout and not in it.</summary>
    public static int RefereeOff { get; set; } = 6;

    /// <summary>What the arenas are called in a sentence.</summary>
    public static string ArenaName { get; set; } = "the arenas east of Papua";

    /// <summary>How often the held entrants are looked over: alive, held, waiting.</summary>
    public static int KeepMs { get; set; } = 2000;

    /// <summary>How often a line on the arenas goes in the log while they are switched on.</summary>
    public static int ArenaSummaryMs { get; set; } = 300000;

    /// <summary>The day's championships held at the arenas, forced ones included; see <see cref="Forced"/>.</summary>
    public static long ArenaRuns { get; private set; }

    /// <summary>Of those, held by <c>do arena force</c>: tests, which do not spend the day's.</summary>
    public static long Forced { get; private set; }

    /// <summary>Calls turned away for fewer than <see cref="MinEntrants"/> free bots.</summary>
    public static long TooFew { get; private set; }

    /// <summary>Calls turned away because the day's championship was already held.</summary>
    public static long AlreadyToday { get; private set; }

    /// <summary>Calls turned away for coming from anybody but the calendar or the keyboard while the arenas are on.</summary>
    public static long NotTheirs { get; private set; }

    /// <summary>Entrants ported to the arenas.</summary>
    public static long Ported { get; private set; }

    /// <summary>Drawn entrants found busy at the port and replaced from the reserve.</summary>
    public static long Replaced { get; private set; }

    /// <summary>Drawn entrants found busy at the port with nobody free in the reserve: the other side went on.</summary>
    public static long NoReserve { get; private set; }

    /// <summary>Batches called into the rings.</summary>
    public static long Batches { get; private set; }

    /// <summary>Bouts fought at the arenas, the title bout among them.</summary>
    public static long ArenaBouts { get; private set; }

    /// <summary>Of those, decided by a yield, by the clock or the referee, and by a fall.</summary>
    public static long ArenaYields { get; private set; }

    public static long ArenaClock { get; private set; }

    /// <summary>
    /// Pairs put back beside each other in the middle of their ring after both brawls ended without a winner, and the bouts of those
    /// that still did not fight, decided by the rule of the clock but counted apart from it (07.10.2026, reach28). See <see cref="Replace"/>.
    /// </summary>
    public static long Reseated { get; private set; }

    public static long NoFights { get; private set; }

    public static long ArenaFalls { get; private set; }

    /// <summary>Bouts with a side gone before it was called (resting, dead, deleted): the other went on.</summary>
    public static long ArenaWalkovers { get; private set; }

    /// <summary>Entrants sent back to where they came from.</summary>
    public static long SentHome { get; private set; }

    /// <summary>Entrants found dead at the arenas and raised by the referee, their own corpse given back.</summary>
    public static long ArenaRaised { get; private set; }

    /// <summary>Waits pressed again on a held entrant whose wait something else had taken.</summary>
    public static long Rewaited { get; private set; }

    /// <summary>Bots of ours found inside the rings with no championship holding them (after a restart, mostly), sent home.</summary>
    public static long Strays { get; private set; }

    /// <summary>Championships called off by hand at the arenas.</summary>
    public static long ArenaStopped { get; private set; }

    /// <summary>Championships closed by the watchdog: too long, or a title stage the arenas do not drive. See <see cref="WatchdogMs"/>.</summary>
    public static long Watchdogs { get; private set; }

    /// <summary>Calls at the arenas refused for coming within <see cref="LeastDayGapMs"/> of the last held (by hand, unforced).</summary>
    public static long TooSoonDay { get; private set; }

    /// <summary>Summoned creatures left in a fighting ring and taken away between batches.</summary>
    public static long Unsummoned { get; private set; }

    /// <summary>Corpses lying in the rings given back to their raised owners, wherever in the ring they lay.</summary>
    public static long Regeared { get; private set; }

    /// <summary>
    /// The longest a championship may hold the arenas before the watchdog closes it and sends everybody home (07.10.2026, arena27
    /// review). Three hours: 64 entrants run about six rounds of a few minutes each and a title wait of a quarter of an hour.
    /// </summary>
    public static long WatchdogMs { get; set; } = 10800000L;

    /// <summary>Whether a championship is running at the arenas now.</summary>
    public static bool ArenaOn => _arenaStage != ArenaStage.Idle;

    private enum ArenaStage
    {
        Idle,
        Notice,
        Bracket,
        Title
    }

    private static ArenaStage _arenaStage = ArenaStage.Idle;

    /// <summary>One ring of the arenas: its middle at floor height, how far its wall stands from it, and how many tiles a body fits inside.</summary>
    private sealed class ArenaRing
    {
        public Point3D Centre;

        public int Radius;

        public int Wall;

        public int Inside;

        public string Region;

        public override string ToString() => $"({Centre.X}, {Centre.Y}) r {Radius} z {Centre.Z}, {Inside} tiles inside";
    }

    private static readonly List<ArenaRing> _rings = [];

    private static Map _arenaMap;

    private static bool _ringsLooked;

    /// <summary>The rings fought in, the main ring first; the waiting rings; and the winners' ring, which is the first waiting ring.</summary>
    private static readonly List<ArenaRing> _fightRings = [];

    private static readonly List<ArenaRing> _pens = [];

    private sealed class Entrant
    {
        public BotMobile Bot;

        public Map FromMap;

        public Point3D From;
    }

    /// <summary>Everybody held at the arenas now, with where it was ported from.</summary>
    private static readonly Dictionary<Serial, Entrant> _arenaHeld = [];

    private sealed class Pair
    {
        public BotMobile A;

        public BotMobile B;

        public ArenaRing Ring;

        public long Began;

        public bool Done;

        public bool Title;

        public BotMobile Winner;

        /// <summary>Put back beside each other once after both brawls lost the other was tried (07.10.2026, reach28). See <see cref="Replace"/>.</summary>
        public bool Replaced;

        /// <summary>And it was done: the pair really stood beside each other again. Only this is said in the no-fight line.</summary>
        public bool Reseated;
    }

    /// <summary>The batches of this round not yet called, in order; each a list of pairs.</summary>
    private static readonly List<List<Pair>> _batches = [];

    /// <summary>Of those, how many have been ported already (round one only: later rounds stand in the winners' ring).</summary>
    private static int _portedAhead;

    /// <summary>The pairs in the rings now.</summary>
    private static readonly List<Pair> _fighting = [];

    /// <summary>Winners of this round, and the bye: the next round.</summary>
    private static readonly List<BotMobile> _arenaNext = [];

    /// <summary>Free bots beyond the drawn, strongest first: who replaces an entrant found busy at its port.</summary>
    private static readonly List<BotMobile> _reserve = [];

    /// <summary>Everybody drawn since the call, replacements included: nobody is drawn twice.</summary>
    private static readonly HashSet<Serial> _drawn = [];

    private static int _batchNo;

    private static int _batchesInRound;

    private static long _arenaTick;

    private static long _keepTick;

    private static long _strayTick;

    private static long _arenaSummaryTick;

    private static bool _forced;

    /// <summary>When the running championship was called, for the watchdog.</summary>
    private static long _arenaBegan;

    /// <summary><see cref="BotTourney.Fights"/> at the call: the day is spent only when it has grown (a bout was fought).</summary>
    private static long _fightsAtCall;

    /// <summary>Who has had a bye in the running championship: the next bye goes to the strongest who has not.</summary>
    private static readonly HashSet<Serial> _byes = [];

    /// <summary>When the day's championship was last held at the arenas, in UTC; <c>DateTime.MinValue</c> for never. Kept by the world save.</summary>
    private static DateTime _dayHeld = DateTime.MinValue;

    /// <summary>When the running championship was called, UTC; what the day is recorded as once it has fought.</summary>
    private static DateTime _calledUtc = DateTime.MinValue;

    /// <summary>The local date the "too few" line was last said in the world channel: once a day, not once a retry.</summary>
    private static DateTime _shortSaid = DateTime.MinValue;

    /// <summary>The last reason the calendar could not hold the day's championship, for the state line.</summary>
    private static string _lastRefusal;

    /// <summary>Whether the day's championship has been held on today's local date.</summary>
    private static bool HeldToday => _dayHeld != DateTime.MinValue && _dayHeld.ToLocalTime().Date == DateTime.Now.Date;

    // ------------------------------------------------------------------ the rings

    /// <summary>
    /// At the vigil's start: the rings found, the hold hooked into the duel's record, and anybody left inside a ring by a restart
    /// sent home. Every boot logs what was found.
    /// </summary>
    private static void ArenaLoad()
    {
        BotDuel.Held = Holding;
        _arenaStage = ArenaStage.Idle;
        _arenaHeld.Clear();
        _fighting.Clear();
        _batches.Clear();
        _arenaNext.Clear();
        _ringsLooked = false;

        EnsureRings();
        StrayHome();
        _strayTick = Core.TickCount;

        logger.Information(
            "Tourney: the championship is {Mode}; the day's was last held {Day}",
            Arena
                ? $"held at {ArenaName} once a day from {StartHourLocal}:00 local, with at least {MinEntrants} entrants"
                : "the hourly town-ring knockout (BotTourney.Arena is off)",
            _dayHeld == DateTime.MinValue ? "never" : $"{_dayHeld.ToLocalTime():yyyy-MM-dd HH:mm} local"
        );
    }

    /// <summary>The hold the duel's record asks of (<see cref="BotDuel.Held"/>): whether this one is held at the arenas.</summary>
    public static bool Holding(Mobile m) => m != null && _arenaHeld.ContainsKey(m.Serial);

    /// <summary>Reads the rings out of the map's statics once a boot; false when none is found.</summary>
    private static bool EnsureRings()
    {
        if (_ringsLooked)
        {
            return _rings.Count > 0;
        }

        var map = Map.Felucca;

        if (map == null || map == Map.Internal || map.Tiles == null)
        {
            return false;
        }

        _ringsLooked = true;
        _arenaMap = map;

        try
        {
            FindRings(map);
        }
        catch (Exception e)
        {
            logger.Warning(e, "Tourney: reading the arenas out of the map's statics threw; the arenas are not used this boot");
            _rings.Clear();
        }

        Assign();

        if (_rings.Count == 0)
        {
            logger.Warning(
                "Tourney: no round arena found within {Scan} tiles of ({X}, {Y}) on {Map}; the day's championship cannot be held this boot",
                ArenaScan,
                ArenaAnchor.X,
                ArenaAnchor.Y,
                map
            );

            return false;
        }

        logger.Information(
            "Tourney: {Count} round arenas found in the statics round ({X}, {Y}) on {Map}: {Rings}; fought in {Fight} at once (the main ring {Main}), waited in {Pens} (the winners' ring {Winners})",
            _rings.Count,
            ArenaAnchor.X,
            ArenaAnchor.Y,
            map,
            string.Join("; ", _rings.ConvertAll(r => $"{r} (wall {r.Wall} tiles, region {r.Region})")),
            _fightRings.Count,
            _fightRings.Count > 0 ? _fightRings[0].ToString() : "none",
            _pens.Count,
            _pens.Count > 0 ? _pens[0].ToString() : "none"
        );

        return _rings.Count > 0;
    }

    /// <summary>
    /// The rings: every closed loop of impassable statics round the anchor of a ring's size, whose inside is floor a body fits on.
    /// Walls are found block by block (the engine's own cache of the statics), joined into loops eight ways, and each loop's inside
    /// filled four ways from its middle: a fill that reaches the loop's box was not closed.
    /// </summary>
    private static void FindRings(Map map)
    {
        _rings.Clear();

        var x0 = Math.Max(0, ArenaAnchor.X - ArenaScan) & ~7;
        var y0 = Math.Max(0, ArenaAnchor.Y - ArenaScan) & ~7;
        var x1 = Math.Min(map.Width - 1, ArenaAnchor.X + ArenaScan) | 7;
        var y1 = Math.Min(map.Height - 1, ArenaAnchor.Y + ArenaScan) | 7;
        var w = x1 - x0 + 1;
        var h = y1 - y0 + 1;
        var wall = new bool[w * h];
        var blocked = new bool[w * h];

        for (var bx = x0 >> 3; bx <= x1 >> 3; bx++)
        {
            for (var by = y0 >> 3; by <= y1 >> 3; by++)
            {
                var statics = map.Tiles.GetStaticBlock(bx, by);
                var land = map.Tiles.GetLandBlock(bx, by);

                for (var i = 0; i < 8; i++)
                {
                    for (var j = 0; j < 8; j++)
                    {
                        var x = (bx << 3) + i - x0;
                        var y = (by << 3) + j - y0;

                        if (x < 0 || y < 0 || x >= w || y >= h)
                        {
                            continue;
                        }

                        var tiles = statics[i][j];

                        for (var k = 0; tiles != null && k < tiles.Length; k++)
                        {
                            if (TileData.ItemTable[tiles[k].ID & TileData.MaxItemValue].Impassable)
                            {
                                wall[x + y * w] = true;

                                break;
                            }
                        }

                        var ground = land[(j << 3) + i];

                        if (wall[x + y * w] || ground.Ignored || (TileData.LandTable[ground.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) != 0)
                        {
                            blocked[x + y * w] = true;
                        }
                    }
                }
            }
        }

        var seen = new bool[w * h];
        var stack = new Stack<int>();
        var part = new List<int>();

        for (var start = 0; start < wall.Length; start++)
        {
            if (!wall[start] || seen[start])
            {
                continue;
            }

            part.Clear();
            stack.Push(start);
            seen[start] = true;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

            while (stack.Count > 0)
            {
                var at = stack.Pop();
                var ax = at % w;
                var ay = at / w;

                part.Add(at);
                minX = Math.Min(minX, ax);
                minY = Math.Min(minY, ay);
                maxX = Math.Max(maxX, ax);
                maxY = Math.Max(maxY, ay);

                // A loop of any size stops being one at RingMaxWall: nothing larger is followed further.
                if (part.Count > RingMaxWall * 4)
                {
                    continue;
                }

                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var nx = ax + dx;
                        var ny = ay + dy;

                        if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                        {
                            continue;
                        }

                        var next = nx + ny * w;

                        if (wall[next] && !seen[next])
                        {
                            seen[next] = true;
                            stack.Push(next);
                        }
                    }
                }
            }

            var spanX = maxX - minX + 1;
            var spanY = maxY - minY + 1;

            if (part.Count < RingMinWall || part.Count > RingMaxWall || spanX < RingMinSpan || spanY < RingMinSpan
                || spanX > RingMaxSpan || spanY > RingMaxSpan || Math.Abs(spanX - spanY) > 4)
            {
                continue;
            }

            var cx = (minX + maxX) / 2;
            var cy = (minY + maxY) / 2;
            var inside = Fill(blocked, w, cx, cy, minX, minY, maxX, maxY, out var floorTiles);

            if (inside < RingMinInside)
            {
                continue;
            }

            // Round, not a square room: a circle leaves its box's corners outside, a room's floor runs into them. The dungeons west of
            // the arenas are full of walled rooms of a ring's size.
            var cut = Math.Max(2, Math.Min(spanX, spanY) / 8);

            if (floorTiles.Contains(minX + cut + (minY + cut) * w) || floorTiles.Contains(maxX - cut + (minY + cut) * w)
                || floorTiles.Contains(minX + cut + (maxY - cut) * w) || floorTiles.Contains(maxX - cut + (maxY - cut) * w))
            {
                continue;
            }

            var gx = cx + x0;
            var gy = cy + y0;
            var floor = map.Tiles.GetLandTile(gx, gy).Z;

            if (!map.CanSpawnMobile(gx, gy, floor - 12, floor + 12, false, false, out var z))
            {
                continue;
            }

            var centre = new Point3D(gx, gy, z);
            var region = Region.Find(centre, map);

            // A guarded region would set the town's guards on every blow; none stands here, and one that did is not a ring to fight in.
            if (region?.IsPartOf<GuardedRegion>() == true)
            {
                continue;
            }

            _rings.Add(
                new ArenaRing
                {
                    Centre = centre,
                    Radius = Math.Min(spanX, spanY) / 2,
                    Wall = part.Count,
                    Inside = inside,
                    Region = string.IsNullOrEmpty(region?.Name) ? "none" : region.Name
                }
            );
        }

        var anchor = ArenaAnchor;

        _rings.Sort((a, b) => Gap(a.Centre, anchor).CompareTo(Gap(b.Centre, anchor)));
    }

    private static double Gap(Point3D a, Point3D b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    /// <summary>
    /// Floor tiles reached four ways from the middle without crossing a blocked tile, and which they are; -1 when the fill reaches the
    /// loop's box, which is a loop that was not closed.
    /// </summary>
    private static int Fill(bool[] blocked, int w, int cx, int cy, int minX, int minY, int maxX, int maxY, out HashSet<int> seen)
    {
        seen = new HashSet<int> { cx + cy * w };

        if (blocked[cx + cy * w])
        {
            return -1;
        }

        var stack = new Stack<int>();

        stack.Push(cx + cy * w);

        while (stack.Count > 0)
        {
            var at = stack.Pop();
            var x = at % w;
            var y = at / w;

            if (x <= minX || y <= minY || x >= maxX || y >= maxY)
            {
                return -1;
            }

            ReadOnlySpan<int> steps = [1, -1, w, -w];

            for (var s = 0; s < steps.Length; s++)
            {
                var next = at + steps[s];

                if (!blocked[next] && seen.Add(next))
                {
                    stack.Push(next);
                }
            }
        }

        return seen.Count;
    }

    /// <summary>
    /// Which rings are fought in and which are waited in: the farthest <see cref="PortedBatches"/> rings wait (the first of them is the
    /// winners' ring), the rest are fought in, the main ring — the anchor's — first.
    /// </summary>
    private static void Assign()
    {
        _fightRings.Clear();
        _pens.Clear();

        if (_rings.Count == 0)
        {
            return;
        }

        if (_rings.Count == 1)
        {
            // One ring only: fought in, and waited in by the far side of it. Rare enough to be said, not designed for.
            _fightRings.Add(_rings[0]);
            _pens.Add(_rings[0]);

            return;
        }

        var pens = Math.Clamp(PortedBatches, 1, _rings.Count - 1);
        var fight = _rings.Count - pens;

        if (FightRings > 0)
        {
            fight = Math.Min(fight, FightRings);
        }

        for (var i = 0; i < fight; i++)
        {
            _fightRings.Add(_rings[i]);
        }

        for (var i = _rings.Count - pens; i < _rings.Count; i++)
        {
            _pens.Add(_rings[i]);
        }
    }

    /// <summary>Whether this point lies inside one of the rings' walls.</summary>
    private static bool InRings(Map map, Point3D p)
    {
        if (map != _arenaMap)
        {
            return false;
        }

        for (var i = 0; i < _rings.Count; i++)
        {
            var r = _rings[i];

            if (Math.Abs(p.X - r.Centre.X) <= r.Radius + 1 && Math.Abs(p.Y - r.Centre.Y) <= r.Radius + 1)
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------ the calendar and the hand

    /// <summary>Whether the calendar should hold the day's championship now.</summary>
    private static bool ArenaDue(long now) =>
        Running && Arena && !HeldToday && DateTime.Now.Hour >= StartHourLocal
        && (_dayHeld == DateTime.MinValue || (DateTime.UtcNow - _dayHeld).TotalMilliseconds >= LeastDayGapMs)
        && now - _bootTick >= BootGraceMs && now - _turnedTick >= RetryMs;

    /// <summary><c>do arena</c> and <c>do arena force</c>.</summary>
    public static string ArenaByHand(bool force) => ArenaStart(force ? "the keyboard (forced)" : "the keyboard", force);

    /// <summary>
    /// Calls the day's championship: by the calendar, or by the keyboard. Refused when one runs, when today's was held (unless forced),
    /// when the rings are not found, and when fewer than <see cref="MinEntrants"/> free bots qualify. Returns what happened.
    /// </summary>
    private static string ArenaStart(string by, bool force)
    {
        if (!Running)
        {
            return "championships are switched off (BotTourney.Running).";
        }

        if (ArenaOn)
        {
            return $"a championship is already running at {ArenaName}: {ArenaState()}.";
        }

        if (_stage != Stage.Idle)
        {
            return "the hourly championship is still running in a town ring; let it finish or do tourney stop.";
        }

        var keyboard = by != null && by.StartsWith("the keyboard", StringComparison.Ordinal);

        if (!keyboard && by != "the calendar")
        {
            NotTheirs++;

            return $"the championship is held once a day at {ArenaName}, by the calendar from {StartHourLocal}:00 or by hand (do arena); {by} does not call it.";
        }

        // By hand as by the calendar, the twelve hours between two of the day's (07.10.2026, arena27 review): a hand at 00:30 must not
        // hold the next day's four hours after the evening's. Forced tests are not bound by it.
        if (!force && _dayHeld != DateTime.MinValue && (DateTime.UtcNow - _dayHeld).TotalMilliseconds < LeastDayGapMs)
        {
            TooSoonDay++;

            return $"the last championship was held at {_dayHeld.ToLocalTime():dd.MM HH:mm}, less than {LeastDayGapMs / 3600000} hours ago; do arena force holds one for a test.";
        }

        if (!force && HeldToday)
        {
            AlreadyToday++;

            return $"today's championship was held at {_dayHeld.ToLocalTime():HH:mm}; the next is tomorrow from {StartHourLocal}:00. do arena force holds one for a test.";
        }

        if (!EnsureRings())
        {
            _lastRefusal = $"no round arena was found round ({ArenaAnchor.X}, {ArenaAnchor.Y})";

            return $"{_lastRefusal}; the championship cannot be held this boot.";
        }

        var field = ArenaField(_reserve);

        if (field.Count < Math.Max(2, MinEntrants))
        {
            TooFew++;
            _lastRefusal = $"only {field.Count} free bots qualified at {DateTime.Now:HH:mm} of the {MinEntrants} it wants";

            logger.Information(
                "Tourney: the day's championship at {Arena} is not held, called by {By}: {Why}{Again}",
                ArenaName,
                by,
                _lastRefusal,
                keyboard ? "" : $"; the calendar asks again in {RetryMs / 60000} minutes, until midnight"
            );

            if (!keyboard && _shortSaid != DateTime.Now.Date)
            {
                _shortSaid = DateTime.Now.Date;
                BotVenues.Announce($"The championship at {ArenaName} waits: only {field.Count} of the {MinEntrants} fighters it needs are free. It is called again later tonight.");
            }

            return $"not held: {_lastRefusal}.";
        }

        _arenaHeld.Clear();
        _fighting.Clear();
        _batches.Clear();
        _arenaNext.Clear();
        _round.Clear();
        _next.Clear();
        _drawn.Clear();

        for (var i = 0; i < field.Count; i++)
        {
            _drawn.Add(field[i].Serial);
        }

        _round.AddRange(field);
        _forced = force;
        _lastRefusal = null;
        _roundNo = 1;
        _batchNo = 0;
        _entered = field.Count;
        _by = by;
        _title = false;
        _vacantFought = false;
        _challenger = null;
        Called++;
        ArenaRuns++;

        // The day is spent when the championship closes with a bout fought, not here (07.10.2026, arena27 review): a restart, a stop
        // or a field that never fought leaves the evening open for the calendar. See ArenaClose.
        if (force)
        {
            Forced++;
        }

        _fightsAtCall = Fights;
        _calledUtc = DateTime.UtcNow;
        _arenaBegan = Core.TickCount;
        _byes.Clear();

        var main = _fightRings[0];

        Venue = new BotVenues.Venue { Use = BotVenues.Use.Ring, Name = ArenaName, Map = _arenaMap, At = main.Centre, Near = field.Count };

        var refereeAt = Spot(_arenaMap, main.Centre, main.Centre.X, main.Centre.Y - RefereeOff, out var stand) ? stand : main.Centre;
        var referee = BotVenues.Referee(_arenaMap, refereeAt, ArenaName);

        _arenaStage = ArenaStage.Notice;
        _arenaTick = Core.TickCount;

        var guilds = Guilds(field);
        var pairs = field.Count / 2;
        var crafters = field.FindAll(f => f.Class?.Role == BotRole.Producer).Count;

        logger.Information(
            "Tourney: the day's championship is called{Forced} by {By} at {Arena} for {Title}: {Count} entrants ({Crafters} of them crafters) in {Pairs} sparrings, {Reserve} free in reserve, the referee {Referee} at ({X}, {Y}); the strongest first: {Names}",
            force ? " (FORCED, a test: the day's is not spent and no prize is paid)" : "",
            by,
            ArenaName,
            Champion != null ? $"the right to challenge {Champion}" : "the vacant title",
            field.Count,
            crafters,
            pairs,
            _reserve.Count,
            referee ?? "nobody (the squad is busy)",
            refereeAt.X,
            refereeAt.Y,
            string.Join(", ", field.ConvertAll(f => $"{f.Name} ({BotThreat.Power(f):F0})"))
        );

        BotVenues.Announce(
            $"The championship at {ArenaName} begins in {NoticeMs / 60000} minutes: {field.Count} fighters, {pairs} sparrings one against one, from {guilds}{(Champion != null ? $"; the last standing challenges {Champion} for the title" : "; for the vacant title")}."
        );

        BotEvents.Post("arena", field[0], $"the championship at {ArenaName} is called by {by}: {field.Count} entrants, {pairs} sparrings{(force ? " (forced)" : "")}");

        return $"the championship is called at {ArenaName}{(force ? " (forced: a test, today's is not spent and no prize is paid)" : "")}: {field.Count} entrants ({crafters} crafters), {field[0].Name} the strongest at {BotThreat.Power(field[0]):F0}; the first batch is ported in {NoticeMs / 60000} minutes; {referee ?? "no watcher"} referees at the main ring ({main.Centre.X}, {main.Centre.Y}). Watch the log for \"Tourney:\".";
    }

    /// <summary>"The Wolf (12), The Anchor (9) and 4 without a guild": who the field comes from, for the herald.</summary>
    private static string Guilds(List<BotMobile> field)
    {
        Dictionary<string, int> count = new(StringComparer.Ordinal);
        var none = 0;

        for (var i = 0; i < field.Count; i++)
        {
            if (field[i].Guild?.Name is { Length: > 0 } name)
            {
                count[name] = count.GetValueOrDefault(name) + 1;
            }
            else
            {
                none++;
            }
        }

        List<(string Name, int N)> rows = [];

        foreach (var (name, n) in count)
        {
            rows.Add((name, n));
        }

        rows.Sort((a, b) => b.N.CompareTo(a.N));

        var parts = rows.ConvertAll(r => $"{r.Name} ({r.N})");

        if (none > 0)
        {
            parts.Add($"{none} of no guild");
        }

        return parts.Count switch
        {
            0 => "nowhere",
            1 => parts[0],
            _ => $"{string.Join(", ", parts.GetRange(0, parts.Count - 1))} and {parts[^1]}"
        };
    }

    /// <summary>
    /// Why this one cannot be drawn or ported now, or null when it is free: dead, resting, with a company or a party, in a duel, in a
    /// cell, a murderer, the Baron, the champion.
    /// </summary>
    private static string ArenaAway(BotMobile bot)
    {
        if (bot is not { Deleted: false } || bot.Class is not { } klass)
        {
            return "gone";
        }

        if (!bot.Alive || bot.Fallen)
        {
            return "dead";
        }

        if (bot.Map == null || bot.Map == Map.Internal)
        {
            return "resting";
        }

        if (klass.Unpaid)
        {
            return "the Baron";
        }

        if (bot.Squad != null || bot.Resolve?.Deed is BotEnlist)
        {
            return "with a company";
        }

        if (BotDelveParty.Delving(bot) || bot.Resolve?.Deed is BotDelve)
        {
            return "down a dungeon";
        }

        if (BotDuel.InBout(bot))
        {
            return "in another duel";
        }

        // The review's list (07.10.2026, arena27): a tired bot owes itself a rest the hold would keep it from, a trader is neutral in
        // every quarrel, a bot at sea is a ship's company, and a bot in a fight is in one.
        if (bot.Tired)
        {
            return "tired";
        }

        if (BotTrading.Neutral(bot))
        {
            return "a trader";
        }

        if (BotReachMap.Afloat(bot.Map, bot.Location))
        {
            return "at sea";
        }

        if (bot.Resolve?.Standing is BotStanding.Failing or BotStanding.Hunted)
        {
            return "in a fight";
        }

        // Work the hold would throw away rather than put down - another bot's goods in the pack, a company's or a party's errand, a
        // thief at its turn - is the same work a company's call spares (BotSquads.Pressable), and spared here for the same reason.
        if (!BotSquads.Pressable(bot))
        {
            return "at work it cannot put down";
        }

        if (BotOutlaw.Jailed(bot))
        {
            return "in a cell";
        }

        if (bot.Murderer)
        {
            return "a murderer";
        }

        return Champion != null && string.Equals(bot.Name, Champion, StringComparison.OrdinalIgnoreCase) ? "the champion" : null;
    }

    /// <summary>The field, strongest first: fighters, then crafters to fill; and the rest of the free into <paramref name="reserve"/>.</summary>
    private static List<BotMobile> ArenaField(List<BotMobile> reserve)
    {
        reserve.Clear();

        var bots = BotPopulation.Bots;
        List<BotMobile> fighters = [];
        List<BotMobile> crafters = [];

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (ArenaAway(bot) != null || Holding(bot))
            {
                continue;
            }

            (bot.Class.Role == BotRole.Producer ? crafters : fighters).Add(bot);
        }

        fighters.Sort((x, y) => BotThreat.Power(y).CompareTo(BotThreat.Power(x)));
        crafters.Sort((x, y) => BotThreat.Power(y).CompareTo(BotThreat.Power(x)));

        List<BotMobile> all = [..fighters];

        if (Crafters)
        {
            all.AddRange(crafters);
        }

        var take = Math.Min(all.Count, Math.Max(MinEntrants, ArenaEntrants));

        reserve.AddRange(all.GetRange(take, all.Count - take));

        return all.GetRange(0, take);
    }

    // ------------------------------------------------------------------ the beat

    /// <summary>
    /// Every minute from <see cref="Beat"/>, whatever <see cref="Arena"/> says (07.10.2026, arena27 review): bots of ours in the rings
    /// with no championship holding them sent home. A restart mid-event followed by the dial switched off must not leave them sealed in.
    /// </summary>
    private static void StraySweep(long now)
    {
        if (ArenaOn || _rings.Count == 0 || now - _strayTick < 60000)
        {
            return;
        }

        _strayTick = now;
        StrayHome();
    }

    /// <summary>The arenas' beat, from <see cref="Beat"/>: the calendar, the notice, the batches, the title.</summary>
    private static void ArenaBeat(long now)
    {
        // The watchdog (07.10.2026, arena27 review): an event never holds the arenas past WatchdogMs, nor sits in its title stage with
        // the title's own stages somewhere the arenas do not drive (Calling, Pausing), which would hold everybody sealed in for good.
        var tooLong = now - _arenaBegan >= WatchdogMs;

        if (ArenaOn && (tooLong || _arenaStage == ArenaStage.Title && _stage is not (Stage.Title or Stage.Fighting or Stage.Idle)))
        {
            Watchdogs++;
            logger.Warning(
                "Tourney: the watchdog closes the championship at {Arena}: {Why}",
                ArenaName,
                tooLong ? $"it has run {(now - _arenaBegan) / 60000} minutes" : $"the title stage stands at {_stage}"
            );

            if (_stage != Stage.Idle)
            {
                Release();
                _stage = Stage.Idle;
                _challenger = null;
                _title = false;
            }

            ArenaClose("watchdog");

            return;
        }

        if (ArenaOn)
        {
            BotVenues.ShowReferee();

            if (now - _keepTick >= KeepMs)
            {
                _keepTick = now;
                KeepHeld();
            }
        }

        if (Arena && now - _arenaSummaryTick >= ArenaSummaryMs)
        {
            _arenaSummaryTick = now;
            logger.Information("Tourney: {State}; {Tally}", ArenaState(), ArenaTally());
        }

        switch (_arenaStage)
        {
            case ArenaStage.Idle:
                if (Arena && ArenaDue(now))
                {
                    var before = Called;

                    ArenaStart("the calendar", false);

                    if (Called == before)
                    {
                        _turnedTick = now;
                    }
                }

                return;

            case ArenaStage.Notice:
                if (now - _arenaTick >= NoticeMs)
                {
                    Open(now);
                }

                return;

            case ArenaStage.Bracket:
                Bracket(now);

                return;

            case ArenaStage.Title:
                switch (_stage)
                {
                    case Stage.Title:
                        Title(now);
                        break;
                    case Stage.Fighting:
                        Judge(now);
                        break;
                }

                if (_stage == Stage.Idle)
                {
                    ArenaClose("the title is decided");
                }

                return;
        }
    }

    /// <summary>Round one: pairs and batches, the bye and the first batches ported; the referee's pause before the first call.</summary>
    private static void Open(long now)
    {
        _arenaStage = ArenaStage.Bracket;

        BuildRound(new List<BotMobile>(_round), now);

        BotVenues.Announce(
            $"The championship at {ArenaName} begins: round 1, {_batches.Count} {(_batches.Count == 1 ? "batch" : "batches")} of sparrings, one pair to a ring; the referee stands at the main ring."
        );
    }

    /// <summary>
    /// A round from these entrants: strongest first, the strongest with a bye when they are odd, the rest paired from the far end
    /// (the strongest against the weakest), the pairs dealt into batches as even as the fighting rings allow. In round one the
    /// first batches are ported at once, and the bye to the winners' ring.
    /// </summary>
    private static void BuildRound(List<BotMobile> entrants, long now)
    {
        entrants.RemoveAll(m => m is not { Deleted: false });
        entrants.Sort((x, y) => BotThreat.Power(y).CompareTo(BotThreat.Power(x)));

        _batches.Clear();
        _arenaNext.Clear();
        _portedAhead = 0;
        _batchNo = 0;

        if (entrants.Count % 2 == 1)
        {
            // The strongest who has not had a bye yet (07.10.2026, arena27 review): 50 runs 25, 13, 7, 4, and the top seed took three.
            var at = entrants.FindIndex(m => !_byes.Contains(m.Serial));
            var bye = entrants[at < 0 ? 0 : at];

            entrants.Remove(bye);
            _byes.Add(bye.Serial);
            Byes++;

            // Round one's bye is ported to the winners' ring now, or its replacement is when it is busy.
            if (!Holding(bye))
            {
                bye = Replacement(bye);

                if (bye != null)
                {
                    Port(bye, _pens[0]);
                }
            }

            if (bye != null)
            {
                _arenaNext.Add(bye);
                logger.Information("Tourney: {Name} has a bye in round {Round} at {Arena}", bye.Name, _roundNo, ArenaName);
            }
        }

        List<Pair> pairs = [];

        for (var i = 0; i < entrants.Count / 2; i++)
        {
            pairs.Add(new Pair { A = entrants[i], B = entrants[entrants.Count - 1 - i] });
        }

        var final = Champion == null && pairs.Count == 1 && _arenaNext.Count == 0;

        if (final)
        {
            pairs[0].Title = true;
        }

        var per = Math.Max(1, _fightRings.Count);
        var count = (pairs.Count + per - 1) / per;

        for (var b = 0; b < count; b++)
        {
            // Even batches: the first ones take one more each while the remainder lasts.
            var size = pairs.Count / count + (b < pairs.Count % count ? 1 : 0);
            var start = 0;

            for (var k = 0; k < b; k++)
            {
                start += pairs.Count / count + (k < pairs.Count % count ? 1 : 0);
            }

            _batches.Add(pairs.GetRange(start, size));
        }

        _batchesInRound = _batches.Count;

        logger.Information(
            "Tourney: round {Round} at {Arena}: {Pairs} sparrings in {Batches} batches of up to {Per}{Bye}",
            _roundNo,
            ArenaName,
            pairs.Count,
            _batches.Count,
            per,
            _arenaNext.Count > 0 ? $", {_arenaNext[0].Name} with a bye" : ""
        );

        PortAhead();
        _arenaTick = now;
    }

    /// <summary>
    /// Ports the batches not yet on the arenas, as far as the waiting rings allow: <see cref="PortedBatches"/> on the arenas at once,
    /// counting the one fighting. An entrant found busy is replaced from the reserve first (<see cref="Replacement"/>).
    /// </summary>
    private static void PortAhead()
    {
        // Waiting rings, not batches, are the bound: one batch to a waiting ring, and the one in the rings counted against PortedBatches.
        var waiting = Math.Min(Math.Max(1, _pens.Count - 1), Math.Max(1, PortedBatches - 1));

        while (_portedAhead < Math.Min(_batches.Count, waiting))
        {
            var batch = _batches[_portedAhead];
            var pen = WaitingPen(_batchNo + _portedAhead);

            for (var i = 0; i < batch.Count; i++)
            {
                var pair = batch[i];

                pair.A = Replacement(pair.A);
                pair.B = Replacement(pair.B);

                if (pair.A != null && !Holding(pair.A))
                {
                    Port(pair.A, pen);
                }

                if (pair.B != null && !Holding(pair.B))
                {
                    Port(pair.B, pen);
                }
            }

            _portedAhead++;
        }
    }

    /// <summary>
    /// The waiting ring for the round's batch of this number (from 0): the winners' ring holds the next round, the others take the
    /// batches in turn, so a batch is ported into the ring the batch two before it has just left for the rings.
    /// </summary>
    private static ArenaRing WaitingPen(int batch)
    {
        if (_pens.Count <= 1)
        {
            return _pens[0];
        }

        return _pens[1 + batch % (_pens.Count - 1)];
    }

    /// <summary>
    /// The entrant itself when it can be ported, else the strongest free bot of the reserve in its place, else null. Only an entrant
    /// not yet held is asked: one already at the arenas is the referee's.
    /// </summary>
    private static BotMobile Replacement(BotMobile bot)
    {
        if (bot == null || Holding(bot))
        {
            return bot;
        }

        var why = ArenaAway(bot);

        if (why == null)
        {
            return bot;
        }

        for (var i = 0; i < _reserve.Count; i++)
        {
            var other = _reserve[i];

            if (ArenaAway(other) != null || Holding(other) || _drawn.Contains(other.Serial))
            {
                continue;
            }

            _reserve.RemoveAt(i);
            _drawn.Add(other.Serial);
            Replaced++;

            logger.Information(
                "Tourney: {Name} is {Why} at the port and is replaced by {Other} ({Power:F0}) from the reserve; nobody forfeits",
                bot.Name,
                why,
                other.Name,
                BotThreat.Power(other)
            );

            BotEvents.Post("arena", other, $"took {bot.Name}'s place at {ArenaName}: {bot.Name} is {why}");

            return other;
        }

        NoReserve++;

        logger.Information("Tourney: {Name} is {Why} at the port and the reserve has nobody free; its opponent goes on", bot.Name, why);

        return null;
    }

    /// <summary>The bracket: judge the batch in the rings, or pause, or call the next batch, or the next round.</summary>
    private static void Bracket(long now)
    {
        if (_fighting.Count > 0)
        {
            JudgeBatch(now);

            return;
        }

        if (now - _arenaTick < BetweenMs)
        {
            return;
        }

        if (_batches.Count > 0)
        {
            CallBatch(now);

            return;
        }

        NextRound(now);
    }

    /// <summary>The next batch into the rings, one pair to a ring, the main ring first; the batch after it ported into the ring it left.</summary>
    private static void CallBatch(long now)
    {
        Unsummon();

        var batch = _batches[0];

        _batches.RemoveAt(0);
        _portedAhead = Math.Max(0, _portedAhead - 1);
        _batchNo++;

        List<string> billed = [];

        for (var i = 0; i < batch.Count; i++)
        {
            var pair = batch[i];

            // Anybody gone since it was ported — dead past raising, resting, deleted — leaves the other the bout.
            pair.A = Replacement(pair.A);
            pair.B = Replacement(pair.B);

            var goneA = pair.A == null || ArenaGone(pair.A);
            var goneB = pair.B == null || ArenaGone(pair.B);

            if (goneA || goneB)
            {
                ArenaWalkovers++;

                var goes = goneA ? goneB ? null : pair.B : pair.A;

                if (goes != null)
                {
                    ToWinners(goes);
                    logger.Information("Tourney: {Name} goes on at {Arena} without a bout: its opponent is gone", goes.Name, ArenaName);
                }

                // Only somebody the arenas hold is sent anywhere: an entrant never ported is left where it is.
                if (goneA && Holding(pair.A))
                {
                    SendHome(pair.A, "gone before its bout");
                }

                if (goneB && Holding(pair.B))
                {
                    SendHome(pair.B, "gone before its bout");
                }

                continue;
            }

            var ring = _fightRings[i % _fightRings.Count];

            if (!Bring(pair.A, pair.B, _arenaMap, ring.Centre))
            {
                Unset++;
                logger.Warning(
                    "Tourney: {A} and {B} could not be set in the ring at ({X}, {Y}); the first goes on by default",
                    pair.A.Name,
                    pair.B.Name,
                    ring.Centre.X,
                    ring.Centre.Y
                );
                ToWinners(pair.A);
                SendHome(pair.B, "the ring could not be set");

                continue;
            }

            pair.Ring = ring;
            pair.Began = now;
            _fighting.Add(pair);
            Fights++;
            ArenaBouts++;

            if (pair.Title)
            {
                TitleBouts++;
                _title = true;
            }

            billed.Add($"{pair.A.Name} against {pair.B.Name}");

            logger.Information(
                "Tourney: round {Round}, batch {Batch} of {Batches}{Title}: {A} ({PowerA:F0}) against {B} ({PowerB:F0}) in the ring at ({X}, {Y})",
                _roundNo,
                _batchNo,
                _batchesInRound,
                pair.Title ? " — the title bout for the vacant crown" : "",
                pair.A.Name,
                BotThreat.Power(pair.A),
                pair.B.Name,
                BotThreat.Power(pair.B),
                ring.Centre.X,
                ring.Centre.Y
            );

            BotEvents.Post("arena", pair.A, $"round {_roundNo}: {pair.A.Name} against {pair.B.Name} in the ring at ({ring.Centre.X}, {ring.Centre.Y}){(pair.Title ? ", for the vacant crown" : "")}");
        }

        _arenaTick = now;

        if (_fighting.Count == 0)
        {
            return;
        }

        if (_fighting.Count == 1 && _fighting[0].Title)
        {
            BotVenues.Announce($"Title bout at {ArenaName}, for the vacant crown: {Billing(_fighting[0].A)} against {Billing(_fighting[0].B)}!");
        }
        else
        {
            var shown = billed.Count <= 5 ? string.Join(", ", billed) : $"{string.Join(", ", billed.GetRange(0, 5))} and {billed.Count - 5} more";

            BotVenues.Announce($"Round {_roundNo}, batch {_batchNo} of {_batchesInRound} at {ArenaName}: {billed.Count} bouts — {shown}.");
        }

        // The ring this batch waited in is free: the next batch is ported into it and gets ready.
        PortAhead();
    }

    /// <summary>
    /// Summoned creatures standing in the fighting rings, taken away before the next batch (07.10.2026, arena27 review): a mage's
    /// summons outlives its bout and would be in the ring when the next pair is put down.
    /// </summary>
    private static void Unsummon()
    {
        List<BaseCreature> gone = [];

        for (var i = 0; i < _fightRings.Count; i++)
        {
            var ring = _fightRings[i];

            foreach (var m in _arenaMap.GetMobilesInRange<BaseCreature>(ring.Centre, ring.Radius))
            {
                if (m is { Deleted: false, Summoned: true })
                {
                    gone.Add(m);
                }
            }
        }

        for (var i = 0; i < gone.Count; i++)
        {
            gone[i].Delete();
            Unsummoned++;
        }
    }

    /// <summary>
    /// Whether this entrant can no longer fight here: deleted, or off the map (resting), or a ghost the referee could not raise. Only
    /// a body the arenas hold is raised: the referee's hands do not reach a bot that died somewhere else.
    /// </summary>
    private static bool ArenaGone(BotMobile bot) =>
        bot is not { Deleted: false } || bot.Map == null || bot.Map == Map.Internal || !bot.Alive && !(Holding(bot) && Raise(bot));

    /// <summary>Each pair in the rings, decided or called; when the whole batch is decided, its result in the world channel.</summary>
    private static void JudgeBatch(long now)
    {
        var open = 0;

        for (var i = 0; i < _fighting.Count; i++)
        {
            var pair = _fighting[i];

            if (pair.Done)
            {
                continue;
            }

            var winner = BotDuel.Winner(pair.A);
            var how = "yield";

            // <b>Both brawls over and the bout still on the record: nobody is fighting it (07.10.2026, reach28).</b> Each brawl gives its
            // foe up after BotBrawl.LostMs out of reach, and the pair then stood until the referee's clock at 150 s called it on
            // "100% against 100%" — a result read off hits nobody struck. Put back beside each other once and pressed again; a pair
            // that loses each other a second time is called at once, as no fight, by the same rule the clock uses.
            if (winner == null && BotDuel.InBout(pair.A) && !Brawling(pair.A) && !Brawling(pair.B)
                && pair.A is { Alive: true } && pair.B is { Alive: true })
            {
                if (!pair.Replaced)
                {
                    pair.Replaced = true;

                    if (Replace(pair))
                    {
                        pair.Reseated = true;
                        open++;

                        continue;
                    }
                }

                BotDuel.NoFight(pair.A, pair.Reseated ? "put back beside each other once, they lost each other again" : "they could not be put back beside each other");
                winner = BotDuel.Winner(pair.A);
                how = "nofight";
            }

            if (winner == null && now - pair.Began >= BotDuel.CapMs + SlackMs)
            {
                CalledOff++;
                BotDuel.Call(pair.A, "the referee called it");
                winner = BotDuel.Winner(pair.A);
                how = "clock";
            }

            if (winner == null)
            {
                if (BotDuel.InBout(pair.A) || BotDuel.InBout(pair.B))
                {
                    open++;

                    continue;
                }

                winner = pair.A is { Deleted: false, Alive: true } ? pair.A : pair.B;
                how = "fall";
            }

            var loser = ReferenceEquals(winner, pair.A) ? pair.B : pair.A;

            if (loser is not { Alive: true } && how != "fall" && how != "nofight")
            {
                how = "fall";
            }
            else if (how == "yield" && now - pair.Began >= BotDuel.CapMs)
            {
                how = "clock";
            }

            switch (how)
            {
                case "fall":
                    ArenaFalls++;
                    break;
                case "clock":
                    ArenaClock++;
                    break;
                case "nofight":
                    NoFights++;
                    break;
                default:
                    ArenaYields++;
                    break;
            }

            BotDuel.End(pair.A, pair.B);
            pair.Done = true;
            pair.Winner = winner;

            var shareW = winner is { HitsMax: > 0 } ? winner.Hits * 100 / winner.HitsMax : 0;
            var shareL = loser is { Alive: true, HitsMax: > 0 } ? loser.Hits * 100 / loser.HitsMax : 0;

            Heal(pair.A);
            Heal(pair.B);

            if (how == "nofight")
            {
                // Not a win on hits: nobody struck one. The rule of the clock picks who goes on, and the line says that is all it was.
                logger.Information(
                    "Tourney: no fight between {Winner} and {Loser} at {Arena} in round {Round}: {Tried}, and the two still could not reach each other after {Seconds}s; {Goes} goes on by the rule of the clock, not on hits{Title}",
                    winner.Name,
                    loser?.Name,
                    ArenaName,
                    _roundNo,
                    pair.Reseated ? "put back beside each other once" : "not put back",
                    (now - pair.Began) / 1000,
                    winner.Name,
                    pair.Title ? " and takes the vacant crown" : ""
                );
            }
            else
            {
                logger.Information(
                    "Tourney: {Winner} beats {Loser} at {Arena} in round {Round} ({How}, {W}% against {L}% after {Seconds}s){Title}",
                    winner.Name,
                    loser?.Name,
                    ArenaName,
                    _roundNo,
                    How(how),
                    shareW,
                    shareL,
                    (now - pair.Began) / 1000,
                    pair.Title ? " and takes the vacant crown" : ""
                );
            }

            BotEvents.Post(
                "arena",
                winner,
                how == "nofight"
                    ? $"goes on past {loser?.Name} in round {_roundNo} at {ArenaName}: no fight, the two could not reach each other"
                    : $"beat {loser?.Name} in round {_roundNo} at {ArenaName} ({How(how)}, {shareW}% to {shareL}%)"
            );

            if (pair.Title)
            {
                _vacantFought = true;
                _title = false;
            }

            ToWinners(winner);

            if (loser != null)
            {
                SendHome(loser, how == "nofight" ? $"no fight with {winner.Name} in round {_roundNo}" : $"lost to {winner.Name} in round {_roundNo}");
            }
        }

        if (open > 0)
        {
            return;
        }

        var winners = new List<string>();

        for (var i = 0; i < _fighting.Count; i++)
        {
            if (_fighting[i].Winner is { } won)
            {
                winners.Add(won.Name);
            }
        }

        var title = _fighting.Count == 1 && _fighting[0].Title;

        _fighting.Clear();
        _arenaTick = now;
        Batches++;

        if (!title)
        {
            var shown = winners.Count <= 8 ? string.Join(", ", winners) : $"{string.Join(", ", winners.GetRange(0, 8))} and {winners.Count - 8} more";

            BotVenues.Announce($"Round {_roundNo}, batch {_batchNo} of {_batchesInRound} is decided at {ArenaName}: {shown} go on.");
        }
    }

    /// <summary>How a bout was decided, in the words of the result line.</summary>
    private static string How(string how) =>
        how switch
        {
            "fall"    => "the other fell",
            "clock"   => "on the clock",
            "nofight" => "no fight",
            _         => "the other yielded"
        };

    /// <summary>
    /// Whether this one is fighting its bout: the brawl in hand, or put down under something that ends by itself (a wound bound) and
    /// taken up again after it.
    /// </summary>
    private static bool Brawling(BotMobile bot) =>
        bot?.Resolve is { } r && (r.Deed is BotBrawl || r.Paused?.Deed is BotBrawl || r.Under?.Deed is BotBrawl);

    /// <summary>How far apart a pair is put back in the middle of its ring. Two: one step each, or one swing of a spear.</summary>
    public static int ReseatApart { get; set; } = 2;

    /// <summary>
    /// A pair whose brawls both ended without a winner, put back in the middle of its ring <see cref="ReseatApart"/> tiles apart on
    /// standing tiles in sight of each other, and pressed again with the bout's clock left running (07.10.2026, reach28). False when
    /// no such two tiles are found near the middle (nobody is moved) or the brawls cannot be pressed.
    /// </summary>
    private static bool Replace(Pair pair)
    {
        if (pair.Ring == null || _arenaMap == null || !Beside(_arenaMap, pair.Ring.Centre, out var at, out var bt))
        {
            logger.Warning(
                "Tourney: {A} and {B} could not be put back beside each other in the ring at ({X}, {Y})",
                pair.A.Name,
                pair.B.Name,
                pair.Ring?.Centre.X ?? 0,
                pair.Ring?.Centre.Y ?? 0
            );

            return false;
        }

        if (!Put(pair.A, _arenaMap, at) || !Put(pair.B, _arenaMap, bt) || !BotDuel.Again(pair.A, pair.B, Trains(pair.A), Trains(pair.B), true))
        {
            return false;
        }

        Reseated++;

        logger.Information(
            "Tourney: {A} and {B} lost each other in the ring at ({X}, {Y}) and are put back {Apart} tiles apart in its middle to fight it out (re-placed)",
            pair.A.Name,
            pair.B.Name,
            pair.Ring.Centre.X,
            pair.Ring.Centre.Y,
            ReseatApart
        );

        return true;
    }

    /// <summary>Two standing tiles <see cref="ReseatApart"/> apart round the middle of a ring, in sight of each other at eye height.</summary>
    private static bool Beside(Map map, Point3D centre, out Point3D at, out Point3D bt)
    {
        var half = Math.Max(1, ReseatApart / 2);
        var rest = Math.Max(1, ReseatApart - half);

        // Across the middle first, then the rows and columns either side of it: the floor of a ring is open, so the first try is the
        // usual answer and the rest are for a ring with something standing in its middle.
        for (var off = 0; off <= 4; off++)
        {
            for (var sign = 1; sign >= -1; sign -= 2)
            {
                var d = off * sign;

                if (Seats(map, centre, centre.X - half, centre.Y + d, centre.X + rest, centre.Y + d, out at, out bt)
                    || Seats(map, centre, centre.X + d, centre.Y - half, centre.X + d, centre.Y + rest, out at, out bt))
                {
                    return true;
                }

                if (off == 0)
                {
                    break;
                }
            }
        }

        at = bt = Point3D.Zero;

        return false;
    }

    /// <summary>Whether a body can be set down on both tiles, at heights a step apart, each in sight of the other.</summary>
    private static bool Seats(Map map, Point3D centre, int ax, int ay, int bx, int by, out Point3D at, out Point3D bt)
    {
        at = bt = Point3D.Zero;

        if (!map.CanSpawnMobile(ax, ay, centre.Z - 12, centre.Z + 12, false, false, out var az)
            || !map.CanSpawnMobile(bx, by, centre.Z - 12, centre.Z + 12, false, false, out var bz)
            || Math.Abs(az - bz) > 2
            || !map.LineOfSight(new Point3D(ax, ay, az + 14), new Point3D(bx, by, bz + 14)))
        {
            return false;
        }

        at = new Point3D(ax, ay, az);
        bt = new Point3D(bx, by, bz);

        return true;
    }

    /// <summary>The round is over: the next one from the winners, or the last one standing to the title.</summary>
    private static void NextRound(long now)
    {
        _arenaNext.RemoveAll(m => m is not { Deleted: false } || ArenaGone(m));

        if (_arenaNext.Count == 0)
        {
            logger.Information("Tourney: nobody is left standing at {Arena}", ArenaName);
            Finish(Champion != null ? $"nobody was left standing at {ArenaName}; {Champion} keeps the title." : $"nobody was left standing at {ArenaName}; the title stays vacant.", true);
            ArenaClose("nobody was left standing");

            return;
        }

        if (_arenaNext.Count == 1)
        {
            var last = _arenaNext[0];

            _arenaNext.Clear();
            Contender(last, now);

            if (_stage == Stage.Title)
            {
                _arenaStage = ArenaStage.Title;

                return;
            }

            ArenaClose("the vacant title is decided");

            return;
        }

        var next = new List<BotMobile>(_arenaNext);

        _roundNo++;
        BuildRound(next, now);

        BotVenues.Announce($"Round {_roundNo} at {ArenaName}: {next.Count} left standing, {_batches.Count} {(_batches.Count == 1 ? "batch" : "batches")}.");
    }

    // ------------------------------------------------------------------ the bodies

    /// <summary>A moongate's flash and its sound at a place: the engine's own teleport effect.</summary>
    private static void Flash(Map map, Point3D at)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        Effects.SendLocationParticles(EffectItem.Create(at, map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
        Effects.PlaySound(at, map, 0x1FE);
    }

    /// <summary>Held from now, with where it stands written down to send it back to. Nothing is moved.</summary>
    private static void Hold(BotMobile bot)
    {
        if (bot is not { Deleted: false } || _arenaHeld.ContainsKey(bot.Serial))
        {
            return;
        }

        _arenaHeld[bot.Serial] = new Entrant { Bot = bot, FromMap = bot.Map, From = bot.Location };
    }

    /// <summary>Held, lifted into a waiting ring with a flash, and set to wait there.</summary>
    private static void Port(BotMobile bot, ArenaRing pen)
    {
        if (bot is not { Deleted: false, Alive: true })
        {
            return;
        }

        Hold(bot);

        if (ToPen(bot, pen))
        {
            Ported++;
            BotEvents.Post("arena", bot, $"ported to {ArenaName} for the championship, from ({_arenaHeld[bot.Serial].From.X}, {_arenaHeld[bot.Serial].From.Y})");
        }
    }

    /// <summary>Into a ring to wait: a tile round its middle, a flash at both ends, the wait pressed.</summary>
    private static bool ToPen(BotMobile bot, ArenaRing pen)
    {
        var x = pen.Centre.X + Utility.RandomMinMax(-PenSpread, PenSpread);
        var y = pen.Centre.Y + Utility.RandomMinMax(-PenSpread, PenSpread);

        if (!Spot(_arenaMap, pen.Centre, x, y, out var at) && !Spot(_arenaMap, pen.Centre, pen.Centre.X, pen.Centre.Y, out at))
        {
            logger.Warning("Tourney: no tile for {Name} in the waiting ring at ({X}, {Y})", bot.Name, pen.Centre.X, pen.Centre.Y);

            return false;
        }

        Flash(bot.Map, bot.Location);
        bot.MoveToWorld(at, _arenaMap);
        bot.Journey?.Discard();
        bot.Combatant = null;
        bot.Warmode = false;
        Flash(_arenaMap, at);

        // Whole on arrival (07.10.2026, arena27 review): a bot pulled off a hunt hurt reached for its bandage, the wait was pressed back
        // over the bandage, and the bandage was offered again - a mend and a wait taking turns every two seconds.
        Heal(bot);
        Wait(bot);

        return true;
    }

    /// <summary>A winner, or a bye, into the winners' ring for the next round.</summary>
    private static void ToWinners(BotMobile bot)
    {
        if (bot == null)
        {
            return;
        }

        if (!_arenaNext.Contains(bot))
        {
            _arenaNext.Add(bot);
        }

        Hold(bot);
        Heal(bot);
        ToPen(bot, _pens[0]);
    }

    /// <summary>The wait pressed on a held entrant, put down over its own work so the work comes back when the championship lets it go.</summary>
    private static void Wait(BotMobile bot)
    {
        // Nor over a bout's brawl: a winner sent to the winners' ring still holds it for a beat, until it reads the bout's end, and the
        // wait put down beneath it is taken up again by itself then (KeepHeld presses it if not).
        if (bot is not { Deleted: false, Alive: true } || bot.Resolve?.Deed is BotArenaWait or BotBrawl or BotSalve)
        {
            return;
        }

        BotWill.Press(bot, new BotArenaWait(_arenaMap, bot.Location), $"held at {ArenaName} for its bout");
    }

    /// <summary>
    /// Raised where it lies, its own corpse given back to it (<c>Corpse.Open</c>, as a player opening its own body): an entrant dead
    /// at the arenas cannot walk to a healer through void. True when it stands again.
    /// </summary>
    private static bool Raise(BotMobile bot)
    {
        if (bot is not { Deleted: false } || bot.Alive)
        {
            return bot is { Deleted: false, Alive: true };
        }

        bot.Resurrect();

        if (!bot.Alive)
        {
            return false;
        }

        ArenaRaised++;

        // Its corpse wherever it lies in the rings (07.10.2026, arena27 review): a ghost that drifted left its kit in a sealed ring. Brought
        // to its feet first, because the engine opens a corpse only within two tiles of a player.
        if (bot.Corpse is Corpse corpse && !corpse.Deleted && InRings(corpse.Map, corpse.Location))
        {
            if (corpse.Map != bot.Map || !bot.InRange(corpse.Location, 2))
            {
                corpse.MoveToWorld(bot.Location, bot.Map);
            }

            corpse.Open(bot, true);
            Regeared++;
        }

        Heal(bot);

        logger.Information("Tourney: {Name} was found dead at {Arena} and is raised by the referee", bot.Name, ArenaName);

        return true;
    }

    /// <summary>
    /// Out of the championship: let go of the hold, raised if need be, healed, and lifted with a flash back to where it was ported
    /// from — or to where it lives, when that ground no longer takes a body. Its wait ends on its next beat and its own work is taken
    /// up again.
    /// </summary>
    private static void SendHome(BotMobile bot, string why)
    {
        if (bot is not { Deleted: false })
        {
            return;
        }

        _arenaHeld.Remove(bot.Serial, out var held);
        _arenaNext.Remove(bot);

        if (!bot.Alive)
        {
            Raise(bot);
        }

        Heal(bot);

        if (bot.Map == null || bot.Map == Map.Internal)
        {
            return;
        }

        var map = held?.FromMap;
        var at = held?.From ?? Point3D.Zero;

        // Where it was ported from, or the nearest tile to it that takes a body (somebody may stand on it now); else where it lives.
        var back = map != null && map != Map.Internal && !InRings(map, at) && (map.CanSpawnMobile(at) || Lodging(map, at, out at));

        if (!back)
        {
            map = BotPopulation.Home ?? _arenaMap;

            if (!Lodging(map, BotPopulation.HomeOf(bot), out at))
            {
                logger.Warning("Tourney: nowhere round {Name}'s home takes a body; it stays at {Arena} for now", bot.Name, ArenaName);

                return;
            }
        }

        Flash(bot.Map, bot.Location);
        bot.MoveToWorld(at, map);
        bot.Journey?.Discard();
        bot.Combatant = null;
        bot.Warmode = false;
        Flash(map, at);
        SentHome++;

        logger.Information("Tourney: {Name} goes home from {Arena} to ({X}, {Y}): {Why}", bot.Name, ArenaName, at.X, at.Y, why);
    }

    /// <summary>A tile round this place a body fits on, ring by ring outwards: where a bot with no other place is put down.</summary>
    private static bool Lodging(Map map, Point3D where, out Point3D at)
    {
        at = Point3D.Zero;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        for (var ring = 0; ring <= 8; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring)
                    {
                        continue;
                    }

                    if (map.CanSpawnMobile(where.X + dx, where.Y + dy, where.Z - 10, where.Z + 10, false, false, out var z))
                    {
                        at = new Point3D(where.X + dx, where.Y + dy, z);

                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Every held entrant looked over: the dead raised, the deleted dropped, and a wait something else took off a waiting entrant
    /// pressed again — the arenas are sealed, and any errand chosen there is an errand into void.
    /// </summary>
    private static void KeepHeld()
    {
        if (_arenaHeld.Count == 0)
        {
            return;
        }

        List<Entrant> all = [.._arenaHeld.Values];

        for (var i = 0; i < all.Count; i++)
        {
            var bot = all[i].Bot;

            if (bot is not { Deleted: false })
            {
                _arenaHeld.Remove(all[i].Bot.Serial);

                continue;
            }

            if (bot.Map == null || bot.Map == Map.Internal)
            {
                continue;
            }

            if (!bot.Alive)
            {
                // A bout's fall is the duel's to judge first; the referee raises the body once the bout is off the record.
                if (!BotDuel.InBout(bot))
                {
                    Raise(bot);
                }

                continue;
            }

            // Never over a mending (BotSalve): it ends by itself, and the wait put down beneath it is taken up again then.
            if (!BotDuel.InBout(bot) && bot.Resolve?.Deed is not (BotArenaWait or BotBrawl or BotSalve))
            {
                Rewaited++;
                Wait(bot);
            }
        }
    }

    /// <summary>
    /// Bots of ours inside the rings that nothing holds — a restart in the middle of a championship leaves them in a sealed ring —
    /// raised if dead and sent to where they live.
    /// </summary>
    private static void StrayHome()
    {
        if (_rings.Count == 0 || ArenaOn)
        {
            return;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || Holding(bot) || !InRings(bot.Map, bot.Location))
            {
                continue;
            }

            Strays++;
            SendHome(bot, "found inside the arenas with no championship holding it");
        }
    }

    /// <summary>
    /// The championship at the arenas is over: every bout off the record, everybody held sent home, the referee hidden again.
    /// </summary>
    private static void ArenaClose(string why)
    {
        for (var i = 0; i < _fighting.Count; i++)
        {
            BotDuel.Call(_fighting[i].A, why);
            BotDuel.End(_fighting[i].A, _fighting[i].B);
        }

        _fighting.Clear();
        _batches.Clear();
        _arenaNext.Clear();
        _round.Clear();
        _next.Clear();

        List<Entrant> all = [.._arenaHeld.Values];

        for (var i = 0; i < all.Count; i++)
        {
            SendHome(all[i].Bot, $"the championship is over: {why}");
        }

        _arenaHeld.Clear();
        _arenaStage = ArenaStage.Idle;
        _held = DateTime.Now;

        // The day's championship is spent only by one that fought (07.10.2026, arena27 review); a forced test never spends it.
        var spent = !_forced && Fights > _fightsAtCall;

        // The call's instant, not the close's: one called at 23:40 and crowned after midnight spends its own evening, not the
        // next one (07.10.2026, second pass of review A).
        if (spent)
        {
            _dayHeld = _calledUtc == DateTime.MinValue ? DateTime.UtcNow : _calledUtc;
        }

        // An evening left open is asked again by the calendar after RetryMs, not on the next beat: a stop by hand is not a recall.
        _turnedTick = Core.TickCount;

        BotVenues.Unreferee();
        BotVenues.Release();

        logger.Information(
            "Tourney: the championship at {Arena}{Forced} is over ({Why}) after {Rounds} rounds, {Fought} bouts; {Day}; the champion is {Champion}; {Tally}",
            ArenaName,
            _forced ? " (forced)" : "",
            why,
            _roundNo,
            Fights - _fightsAtCall,
            spent ? "the day's championship is spent" : _forced ? "a forced test: the day's is not spent" : "no bout was fought: the day's is still open",
            Champion ?? "nobody",
            ArenaTally()
        );
    }

    /// <summary>Called off: the bouts stopped, the title bout released, everybody home.</summary>
    private static string ArenaStop(string why)
    {
        ArenaStopped++;

        if (_stage != Stage.Idle)
        {
            Release();
            _stage = Stage.Idle;
            _challenger = null;
            _title = false;
        }

        ArenaClose(why);

        return $"the championship at {ArenaName} is called off and everybody sent home.";
    }

    /// <summary>A new world: nothing at the arenas. The day's championship stays held if it was: the day is the wall clock's.</summary>
    private static void ArenaForget()
    {
        if (ArenaOn)
        {
            ArenaClose("the world was reset");
        }

        // The day stays spent: "once a day" is the wall clock's, and a new world on the same evening is the same evening.
        _lastRefusal = null;
        _shortSaid = DateTime.MinValue;
    }

    // ------------------------------------------------------------------ what is said

    /// <summary>Where the championship at the arenas stands, in one clause.</summary>
    private static string ArenaState()
    {
        var day = HeldToday
            ? $"today's held at {_dayHeld.ToLocalTime():HH:mm}"
            : $"today's not held yet{(DateTime.Now.Hour >= StartHourLocal ? "" : $", due from {StartHourLocal}:00")}{(_lastRefusal != null ? $" ({_lastRefusal})" : "")}";

        return _arenaStage switch
        {
            ArenaStage.Notice  => $"called by {_by}{(_forced ? " (forced)" : "")}, {_round.Count} entrants, the first port in {Math.Max(0, NoticeMs - (Core.TickCount - _arenaTick)) / 1000}s; {day}",
            ArenaStage.Bracket => _fighting.Count > 0
                ? $"round {_roundNo}, batch {_batchNo} of {_batchesInRound} in the rings ({_fighting.FindAll(p => !p.Done).Count} of {_fighting.Count} bouts still on), {_arenaHeld.Count} held at the arenas; {day}"
                : $"round {_roundNo}, between batches ({_batches.Count} to come), {_arenaHeld.Count} held at the arenas; {day}",
            ArenaStage.Title => _stage == Stage.Fighting
                ? $"the title bout: {_a?.Name} against {_b?.Name} in the main ring; {day}"
                : $"{_challenger?.Name} waits at the arenas to challenge {Champion}; {day}",
            _ => $"no championship at the arenas now ({_rings.Count} rings found); {day}"
        };
    }

    /// <summary>The arenas' counters, for the summary.</summary>
    private static string ArenaTally() =>
        $"arenas: {_rings.Count} rings ({_fightRings.Count} fought in, {_pens.Count} waited in), {ArenaRuns} held ({Forced} forced), "
        + $"{TooFew} not held for fewer than {MinEntrants} free, {AlreadyToday} refused as already held today, {NotTheirs} calls not theirs; "
        + $"{Ported} ported, {Replaced} replaced at the port, {NoReserve} with no reserve, {Batches} batches, {ArenaBouts} bouts "
        + $"({ArenaYields} yields, {ArenaClock} on the clock, {ArenaFalls} falls, {ArenaWalkovers} walkovers; {Reseated} pairs re-placed after losing each other, {NoFights} bouts with no fight), {SentHome} sent home, "
        + $"{ArenaRaised} raised ({Regeared} corpses given back), {Rewaited} waits pressed again, {Strays} strays, {Unsummoned} summons taken away, {ArenaStopped} called off, {Watchdogs} closed by the watchdog, {TooSoonDay} refused inside {LeastDayGapMs / 3600000}h; {BotSquads.Duellists} company calls refused to the held";

    /// <summary><c>do arena state</c>: the bracket — the round, the batch in the rings with each bout, who waits, who goes on.</summary>
    public static string ArenaBracket()
    {
        if (!ArenaOn)
        {
            return $"{ArenaState()}; {(Champion == null ? "the title is vacant" : $"{Champion} holds the title")}; {ArenaTally()}.";
        }

        List<string> bouts = [];

        for (var i = 0; i < _fighting.Count; i++)
        {
            var p = _fighting[i];
            bouts.Add(
                p.Done
                    ? $"{p.A.Name} v {p.B.Name}: {p.Winner?.Name} won"
                    : $"{p.A.Name} {Share(p.A)}% v {p.B.Name} {Share(p.B)}% at ({p.Ring?.Centre.X}, {p.Ring?.Centre.Y})"
            );
        }

        List<string> coming = [];

        for (var b = 0; b < _batches.Count; b++)
        {
            coming.Add($"batch {_batchNo + b + 1}: {string.Join(", ", _batches[b].ConvertAll(p => $"{p.A?.Name ?? "?"} v {p.B?.Name ?? "?"}"))}");
        }

        return $"{ArenaState()}; in the rings: {(bouts.Count == 0 ? "nobody" : string.Join("; ", bouts))}; to come: {(coming.Count == 0 ? "nothing this round" : string.Join(" | ", coming))}; "
            + $"going on: {(_arenaNext.Count == 0 ? "nobody yet" : string.Join(", ", _arenaNext.ConvertAll(m => m.Name)))}; the referee {BotVenues.RefereeName ?? "nobody"}; "
            + $"{(Champion == null ? "the title is vacant" : $"{Champion} holds the title")}.";
    }

    private static int Share(Mobile m) => m is { Alive: true, HitsMax: > 0 } ? m.Hits * 100 / m.HitsMax : 0;
}

/// <summary>
/// An entrant held at the arenas between its bouts: it stands where the referee put it and does nothing else, until the championship
/// lets it go (07.10.2026, arena27).
///
/// <para>
/// <b>Like a sentence (<c>BotSentence</c>), and for the same reason.</b> The rings are sealed by void; any errand the auction chose
/// there would be a walk nobody can make. So it is committed, still and pressed from outside; it pays nothing and is no work, so it is
/// kept out of the finished share. It is steadfast, so a bout or a call to bind a wound puts it down rather than throws it away, and
/// it is taken up again after. Its end is the hold's end: <see cref="BotTourney.Holding"/>.
/// </para>
/// </summary>
public sealed class BotArenaWait : BotDeed
{
    public const string Trade = "arena";

    private readonly Map _map;

    private readonly Point3D _at;

    public BotArenaWait(Map map, Point3D at)
    {
        _map = map;
        _at = at;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Summons => true;

    /// <summary>The work put down for the wait comes back however long the championship took (07.10.2026, arena27 review). See BotDeed.Shelters.</summary>
    public override bool Shelters => true;

    public override bool Unpaid => true;

    public override bool Still => true;

    public override bool KeptOut => true;

    public override Map Map => _map;

    public override Point3D Where => _at;

    public override double Expects => 1.0;

    public override double Minutes => 30.0;

    public override double Coin => 0.0;

    public override bool Repeats(BotDeed other) => other is BotArenaWait;

    public override string Stage => "waiting for its bout at the arenas";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        return BotTourney.Holding(body) ? BotDoing.Work("waiting for its bout at the arenas") : BotDoing.Done("let go from the arenas");
    }
}
