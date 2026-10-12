using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Logging;
using Server.Regions;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// Where the watchers hold what they hold: a revel's feast in a town, a hunt's waves and an orc camp in the countryside, the
/// championship's ring — each chosen from the places that fit it, are safe, can be walked to, were not used lately, and lie
/// where the bots actually are.
///
/// <para>
/// <b>Patrick, 02.10.2026: "I always see the debuggers holding feasts and events in one and the same place, which is also very
/// bad."</b> They were in one place by construction. A revel was "called" at the population's home and every hunt's waves stood
/// there — 27 of the 29 hunt revels between the wipe of 01.10.2026 11:32 and 11:00 on 02.10.2026 put every wave at (1440, 1470);
/// the marshal's two camps stood at (1356, 1384) and two tiles from it, the model naming the same spot each time; and all 136
/// championship bouts were fought on the drill ground at (1458, 1500). Nothing chose a place: a constant did.
/// </para>
///
/// <para>
/// <b>Four tests, one order.</b> A candidate must fit its use (a town's square for a feast; outdoors, out of every guarded
/// region, for a field; enough open ground in sight of the middle for a ring), be safe (no hostile or deadly ground at the
/// middle and no deadly ground within <see cref="SafeRadius"/> — <c>BotZones.LevelAt</c>, read only), be reached (open ground on
/// the reach map, or joined by the gates while the map is still being drawn), and not lie within <see cref="RepeatApart"/> of
/// the last <see cref="Recent"/> places of its use. Among those that pass, the draw is weighted by the bots near each: the
/// square root of the count within <see cref="NearReach"/>, so the town most of the island lives in is likeliest without
/// being the only answer. A field is pitched off a bot that is working in the open near the chosen town, which is where a
/// hunter will see a wave.
/// </para>
///
/// <para>
/// <b>Every refusal has its own counter</b>, and a choice that fell back to the old fixed place says so (<see cref="Fallbacks"/>):
/// a venue chooser that quietly returned home would read exactly like the defect it replaces.
/// </para>
/// </summary>
public static class BotVenues
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotVenues));

    /// <summary>What a place is for.</summary>
    public enum Use
    {
        /// <summary>A revel of a trade: called in a town's square.</summary>
        Feast,

        /// <summary>A hunt's waves or an orc camp: in the countryside, out of town.</summary>
        Field,

        /// <summary>The championship's ring.</summary>
        Ring
    }

    /// <summary>One place chosen: what for, a name a person reads, where, and how many bots were near it when it was chosen.</summary>
    public sealed class Venue
    {
        public Use Use { get; init; }

        public string Name { get; init; }

        public string Town { get; init; }

        public Map Map { get; init; }

        public Point3D At { get; set; }

        public int Near { get; init; }

        /// <summary>The old fixed place, because nothing else could be had. See <see cref="Fallbacks"/>.</summary>
        public bool Fallback { get; init; }

        /// <summary>The place in a sentence: "at Minoc", "north-east of Yew", "in Trinsic", "at home".</summary>
        public string Phrase => Use == Use.Field && !Fallback ? Name : $"at {Name}";

        public override string ToString() => $"{Name} ({At.X}, {At.Y})";
    }

    /// <summary>Whether places are chosen at all. Off: every event is held where it always was.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How many of a use's last places a new one may not repeat.</summary>
    public static int Recent { get; set; } = 3;

    /// <summary>How near a recent place a candidate counts as the same place, in tiles.</summary>
    public static int RepeatApart { get; set; } = 40;

    /// <summary>How far round a place the bots near it are counted, in tiles.</summary>
    public static int NearReach { get; set; } = 200;

    /// <summary>The weight of a town nobody is near, for a feast (or the drill ground, for a ring): a quarter of a town with one bot near it.</summary>
    public static double EmptyWeight { get; set; } = 0.25;

    /// <summary>How far from the bot it is pitched off a field lies: near enough for a hunter's look of fifty tiles.</summary>
    public static int FieldNear { get; set; } = 18;

    public static int FieldFar { get; set; } = 42;

    /// <summary>How far round a place no deadly ground may lie.</summary>
    public static int SafeRadius { get; set; } = 12;

    /// <summary>How many open tiles in sight of the middle a ring needs, in the fifteen-by-fifteen round it: a shop's floor is not a ring.</summary>
    public static int RingOpen { get; set; } = 60;

    /// <summary>How long the herald stands at a place it has just called something at, before it goes back to watching.</summary>
    public static int HeraldHoldMs { get; set; } = 120000;

    /// <summary>Places chosen, by use, and in all.</summary>
    public static long Chosen { get; private set; }

    /// <summary>Candidates refused, one bucket per reason. A candidate is counted once, at the first test it fails.</summary>
    public static long Deadly { get; private set; }

    public static long Hostile { get; private set; }

    public static long Unreached { get; private set; }

    public static long Repeats { get; private set; }

    public static long Cramped { get; private set; }

    public static long Walled { get; private set; }

    public static long Unstood { get; private set; }

    /// <summary>Choices that took a recent place again because every other candidate failed a test of its own.</summary>
    public static long Reused { get; private set; }

    /// <summary>Choices that found nothing and held the event at the old fixed place.</summary>
    public static long Fallbacks { get; private set; }

    /// <summary>The marshal's own spot for a camp, kept as it named it or moved to a fitting place.</summary>
    public static long ModelKept { get; private set; }

    public static long ModelMoved { get; private set; }

    /// <summary>Times the herald went and stood at a place to call something there.</summary>
    public static long Heralded { get; private set; }

    private static readonly List<Venue>[] _recent = [[], [], []];

    /// <summary>Where events were held, by use and name, since the boot: the summary's "where".</summary>
    private static readonly Dictionary<string, int>[] _where = [new(), new(), new()];

    /// <summary>Whether a town's square holds a ring, asked once a session: the ground of a square does not change.</summary>
    private static readonly Dictionary<string, bool> _rings = new(StringComparer.OrdinalIgnoreCase);

    private static BotWatcher _herald;

    private static long _heraldUntil;

    private static Map _heraldMap;

    private static Point3D _heraldAt;

    private static string _heraldWhat;

    /// <summary>The watcher standing, shown, as the referee of the arena championship, or null. See <see cref="Referee"/>.</summary>
    private static BotWatcher _referee;

    private static Map _refereeMap;

    private static Point3D _refereeAt;

    private static string _refereeWhat;

    /// <summary>Times a watcher went to the arenas and showed itself as their referee.</summary>
    public static long Refereed { get; private set; }

    /// <summary>
    /// A place for this use. Never null: with nothing that passes, the old fixed place, flagged and counted. For a ring,
    /// <paramref name="who"/> are the entrants, and the weight is theirs; otherwise every bot in the world counts.
    /// </summary>
    public static Venue Choose(Use use, Map map, IReadOnlyList<Mobile> who = null)
    {
        Venue found = null;

        if (Running && map != null && map != Map.Internal && BotTowns.Ensure(map))
        {
            found = use == Use.Field ? Field(map) : Town(use, map, who);
        }

        found ??= Default(use, map);
        Note(found);

        return found;
    }

    /// <summary>
    /// The marshal's own spot for a camp, if it fits: outdoors, safe, reached, not a recent place, and near a bot. Null when it
    /// does not, and the caller chooses one; counted either way.
    /// </summary>
    public static Venue Accept(Map map, Point3D spot)
    {
        if (!Running || map == null || map == Map.Internal || spot == Point3D.Zero)
        {
            return null;
        }

        BotTowns.Ensure(map);

        Nearest(map, spot, out var town, out var near);

        if (near > 0 && Fits(Use.Field, map, ref spot) && !Lately(Use.Field, spot))
        {
            ModelKept++;

            var venue = new Venue
            {
                Use = Use.Field, Name = Bearing(town, spot), Town = town?.Name, Map = map, At = spot, Near = near
            };

            Note(venue);

            return venue;
        }

        ModelMoved++;

        return null;
    }

    /// <summary>
    /// This use's place in one named town, or null when the town has none that fits: the fair's championship stands in the fair's
    /// town (07.10.2026, events17). The same tests as the town's turn in <see cref="Choose"/>, without the turn.
    /// </summary>
    public static Venue InTown(Use use, Map map, string name)
    {
        if (!Running || map == null || map == Map.Internal || string.IsNullOrEmpty(name) || !BotTowns.Ensure(map) || BotTowns.Find(name) is not { } town)
        {
            return null;
        }

        var at = use == Use.Ring && town.Bounds.Contains(BotTourney.Ring) ? BotTourney.Ring : town.Square;

        if (!Safe(map, at) || !Reached(map, at) || use == Use.Ring && !RingFits(map, town.Name, at))
        {
            return null;
        }

        var venue = new Venue { Use = use, Name = town.Name, Town = town.Name, Map = map, At = at, Near = Count(BotPopulation.Bots, map, at) };

        Note(venue);

        return venue;
    }

    /// <summary>A place a person at the door named, taken exactly as named and only recorded.</summary>
    public static Venue Exact(Use use, Map map, Point3D spot)
    {
        BotTowns.Ensure(map);
        Nearest(map, spot, out var town, out var near);

        var venue = new Venue { Use = use, Name = Bearing(town, spot), Town = town?.Name, Map = map, At = spot, Near = near };

        Note(venue);

        return venue;
    }

    /// <summary>The place moved after it was chosen — a camp's footing a few tiles off — so the record keeps where it stood.</summary>
    public static void Moved(Venue venue, Point3D at)
    {
        if (venue != null)
        {
            venue.At = at;
        }
    }

    // ------------------------------------------------------------------ towns: feasts and rings

    private static Venue Town(Use use, Map map, IReadOnlyList<Mobile> who)
    {
        List<(Venue Venue, double Weight)> passed = [];
        List<Venue> recent = [];

        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            var town = towns[i];

            if (!town.FromHome)
            {
                continue;
            }

            // The drill ground is Britain's ring: proven ground, and the school's own. Any town holding the configured ring has it.
            var at = use == Use.Ring && town.Bounds.Contains(BotTourney.Ring) ? BotTourney.Ring : town.Square;

            if (!Safe(map, at) || !Reached(map, at))
            {
                continue;
            }

            var near = who != null ? Count(who, map, at) : Count(BotPopulation.Bots, map, at);

            // A ring nobody of the entrants is near is a walk home across the island for every loser, through whatever lies
            // between: only the drill ground is offered with nobody near it.
            if (use == Use.Ring && near == 0 && at != BotTourney.Ring)
            {
                continue;
            }

            var venue = new Venue { Use = use, Name = town.Name, Town = town.Name, Map = map, At = at, Near = near };

            if (Lately(use, at))
            {
                Repeats++;
                recent.Add(venue);

                continue;
            }

            passed.Add((venue, near > 0 ? Math.Sqrt(near) : EmptyWeight));
        }

        // A ring is asked whether its square holds one only once it is drawn, and a town that does not is drawn again without:
        // the question reads two hundred tiles' sight lines, and is kept for the session once asked.
        while (passed.Count > 0)
        {
            var pick = Draw(passed);

            if (use != Use.Ring || RingFits(map, pick.Name, pick.At))
            {
                return pick;
            }

            passed.RemoveAll(p => ReferenceEquals(p.Venue, pick));
        }

        // Every fitting place was used lately: the one used longest ago is better than the old constant.
        while (recent.Count > 0)
        {
            var pick = Oldest(use, recent);

            if (use != Use.Ring || RingFits(map, pick.Name, pick.At))
            {
                Reused++;

                return pick;
            }

            recent.Remove(pick);
        }

        return null;
    }

    /// <summary>
    /// Whether this square holds a ring: both fighters' marks found as <c>BotTourney.Spot</c> finds them, and at least
    /// <see cref="RingOpen"/> tiles of the fifteen by fifteen round it standable and in sight of the middle. Asked once a town.
    /// </summary>
    private static bool RingFits(Map map, string name, Point3D at)
    {
        if (_rings.TryGetValue(name, out var fits))
        {
            if (!fits)
            {
                Cramped++;
            }

            return fits;
        }

        var open = 0;
        var eye = new Point3D(at.X, at.Y, at.Z + 10);

        for (var dy = -7; dy <= 7; dy++)
        {
            for (var dx = -7; dx <= 7; dx++)
            {
                if (map.CanSpawnMobile(at.X + dx, at.Y + dy, at.Z - 12, at.Z + 12, false, false, out var z)
                    && map.LineOfSight(new Point3D(at.X + dx, at.Y + dy, z + 10), eye))
                {
                    open++;
                }
            }
        }

        fits = open >= RingOpen
               && BotTourney.Spot(map, at, at.X - BotTourney.Apart, at.Y, out _)
               && BotTourney.Spot(map, at, at.X + BotTourney.Apart, at.Y, out _);

        _rings[name] = fits;

        logger.Information(
            "Venues: {Town}'s square at {X},{Y} {Fits} a ring: {Open} of 225 tiles round it open and in sight",
            name,
            at.X,
            at.Y,
            fits ? "holds" : "does not hold",
            open
        );

        if (!fits)
        {
            Cramped++;
        }

        return fits;
    }

    // ------------------------------------------------------------------ the countryside: waves and camps

    /// <summary>
    /// A field: a town drawn by the bots near it, a bot of those working in the open, and a spot <see cref="FieldNear"/> to
    /// <see cref="FieldFar"/> tiles off it that passes every test; the town's own edge when nobody near it is out of doors.
    /// </summary>
    private static Venue Field(Map map)
    {
        List<(BotTowns.Town Town, double Weight, List<BotMobile> Out)> towns = [];
        var all = BotTowns.All;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < all.Count; i++)
        {
            var town = all[i];

            if (!town.FromHome)
            {
                continue;
            }

            var near = 0;
            List<BotMobile> outdoors = [];

            for (var k = 0; k < bots.Count; k++)
            {
                if (bots[k] is not { Deleted: false, Alive: true } bot || bot.Map != map || !Utility.InRange(bot.Location, town.Square, NearReach))
                {
                    continue;
                }

                near++;

                if (!Guarded(map, bot.Location))
                {
                    outdoors.Add(bot);
                }
            }

            if (near > 0)
            {
                towns.Add((town, Math.Sqrt(near), outdoors));
            }
        }

        Venue recent = null;

        for (var attempt = 0; attempt < 24 && towns.Count > 0; attempt++)
        {
            var (town, _, outdoors) = Pick(towns);
            Point3D anchor;

            if (outdoors.Count > 0)
            {
                anchor = outdoors[Utility.Random(outdoors.Count)].Location;
            }
            else if (!Edge(map, town, out anchor))
            {
                continue;
            }

            var bearing = Utility.RandomDouble() * Math.PI * 2.0;
            var span = Utility.RandomMinMax(FieldNear, FieldFar);
            var spot = new Point3D(
                anchor.X + (int)Math.Round(Math.Cos(bearing) * span),
                anchor.Y + (int)Math.Round(Math.Sin(bearing) * span),
                anchor.Z
            );

            if (!Fits(Use.Field, map, ref spot))
            {
                continue;
            }

            var venue = new Venue
            {
                Use = Use.Field, Name = Bearing(town, spot), Town = town.Name, Map = map, At = spot,
                Near = Count(bots, map, spot)
            };

            if (Lately(Use.Field, spot))
            {
                Repeats++;
                recent ??= venue;

                continue;
            }

            return venue;
        }

        if (recent != null)
        {
            Reused++;
        }

        return recent;
    }

    /// <summary>Whether a spot fits its use, safe and reached; a field's spot is settled on the map's own floor first.</summary>
    private static bool Fits(Use use, Map map, ref Point3D spot)
    {
        if (use == Use.Field)
        {
            if (!BotStep.Settle(map, spot.X, spot.Y, out var z))
            {
                Unstood++;

                return false;
            }

            spot = new Point3D(spot.X, spot.Y, z);

            if (Guarded(map, spot))
            {
                Walled++;

                return false;
            }
        }

        return Safe(map, spot) && Reached(map, spot);
    }

    /// <summary>Where a walk from the town's square out along a random bearing leaves its guarded ground.</summary>
    private static bool Edge(Map map, BotTowns.Town town, out Point3D at)
    {
        var bearing = Utility.RandomDouble() * Math.PI * 2.0;

        for (var span = 8; span <= 400; span += 8)
        {
            var x = town.Square.X + (int)Math.Round(Math.Cos(bearing) * span);
            var y = town.Square.Y + (int)Math.Round(Math.Sin(bearing) * span);
            var p = new Point3D(x, y, town.Square.Z);

            if (!Guarded(map, p))
            {
                at = p;

                return true;
            }
        }

        at = Point3D.Zero;

        return false;
    }

    // ------------------------------------------------------------------ the tests

    /// <summary>
    /// No hostile or deadly ground at the middle, and no deadly ground on two rings round it. Read from the zones' published
    /// raster, and only read: the zones are BotZones' and nothing here writes to them.
    /// </summary>
    private static bool Safe(Map map, Point3D at)
    {
        var middle = BotZones.LevelAt(map, at.X, at.Y);

        if (middle == BotZoneLevel.Deadly)
        {
            Deadly++;

            return false;
        }

        if (middle == BotZoneLevel.Hostile)
        {
            Hostile++;

            return false;
        }

        for (var ring = SafeRadius / 2; ring <= SafeRadius; ring += Math.Max(1, SafeRadius / 2))
        {
            for (var dir = 0; dir < 8; dir++)
            {
                var angle = dir * Math.PI / 4.0;
                var x = at.X + (int)Math.Round(Math.Cos(angle) * ring);
                var y = at.Y + (int)Math.Round(Math.Sin(angle) * ring);

                if (BotZones.LevelAt(map, x, y) == BotZoneLevel.Deadly)
                {
                    Deadly++;

                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Open ground on the reach map; while the map cannot say, whether the gates join it to home; with neither, yes.</summary>
    private static bool Reached(Map map, Point3D at)
    {
        var kind = BotReachMap.Class(map, at.X, at.Y, at.Z);

        if (kind != BotReachClass.Unknown)
        {
            if (kind == BotReachClass.Open)
            {
                return true;
            }

            Unreached++;

            return false;
        }

        if (BotGates.Ready && !BotGates.Joined(map, BotPopulation.Where, at))
        {
            Unreached++;

            return false;
        }

        return true;
    }

    private static bool Guarded(Map map, Point3D at) => map != null && Region.Find(at, map)?.IsPartOf<GuardedRegion>() == true;

    /// <summary>Whether a place lies within <see cref="RepeatApart"/> of one of the last <see cref="Recent"/> of its use.</summary>
    private static bool Lately(Use use, Point3D at)
    {
        var list = _recent[(int)use];

        for (var i = 0; i < list.Count; i++)
        {
            if (Utility.InRange(list[i].At, at, RepeatApart))
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------ choosing and counting

    private static Venue Draw(List<(Venue Venue, double Weight)> passed)
    {
        var total = 0.0;

        for (var i = 0; i < passed.Count; i++)
        {
            total += passed[i].Weight;
        }

        var roll = Utility.RandomDouble() * total;

        for (var i = 0; i < passed.Count; i++)
        {
            roll -= passed[i].Weight;

            if (roll < 0.0)
            {
                return passed[i].Venue;
            }
        }

        return passed[^1].Venue;
    }

    private static (BotTowns.Town Town, double Weight, List<BotMobile> Out) Pick(List<(BotTowns.Town Town, double Weight, List<BotMobile> Out)> towns)
    {
        var total = 0.0;

        for (var i = 0; i < towns.Count; i++)
        {
            total += towns[i].Weight;
        }

        var roll = Utility.RandomDouble() * total;

        for (var i = 0; i < towns.Count; i++)
        {
            roll -= towns[i].Weight;

            if (roll < 0.0)
            {
                return towns[i];
            }
        }

        return towns[^1];
    }

    /// <summary>Of candidates every one of which was used lately, the one whose last use is oldest.</summary>
    private static Venue Oldest(Use use, List<Venue> candidates)
    {
        var list = _recent[(int)use];
        Venue best = null;
        var bestAge = -1;

        for (var c = 0; c < candidates.Count; c++)
        {
            // Newest first in the list, so a later index is an older use.
            var age = list.Count;

            for (var i = 0; i < list.Count; i++)
            {
                if (Utility.InRange(list[i].At, candidates[c].At, RepeatApart))
                {
                    age = i;

                    break;
                }
            }

            if (age > bestAge)
            {
                bestAge = age;
                best = candidates[c];
            }
        }

        return best;
    }

    private static int Count<T>(IReadOnlyList<T> who, Map map, Point3D at) where T : Mobile
    {
        var n = 0;

        for (var i = 0; i < who.Count; i++)
        {
            if (who[i] is { Deleted: false, Alive: true } m && m.Map == map && Utility.InRange(m.Location, at, NearReach))
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>The town nearest a spot, and how many bots are within <see cref="NearReach"/> of the spot.</summary>
    private static void Nearest(Map map, Point3D spot, out BotTowns.Town town, out int near)
    {
        town = BotTowns.Surveyed ? BotTowns.Nearest(spot) : null;
        near = Count(BotPopulation.Bots, map, spot);
    }

    /// <summary>"north-east of Minoc", "near Britain", "in Trinsic", or the coordinates when no town has been read.</summary>
    private static string Bearing(BotTowns.Town town, Point3D at)
    {
        if (town == null)
        {
            return $"at {at.X},{at.Y}";
        }

        // Inside the town's box but out of its guarded streets, as a field always is: the edge of the town.
        if (town.Bounds.Contains(at))
        {
            return Guarded(BotPopulation.Home, at) ? $"in {town.Name}" : $"near {town.Name}";
        }

        var dx = at.X - town.Square.X;
        var dy = at.Y - town.Square.Y;
        var ns = Math.Abs(dy) * 2 < Math.Abs(dx) ? "" : dy < 0 ? "north" : "south";
        var ew = Math.Abs(dx) * 2 < Math.Abs(dy) ? "" : dx < 0 ? "west" : "east";
        var way = ns.Length > 0 && ew.Length > 0 ? $"{ns}-{ew}" : ns + ew;

        return $"{(way.Length > 0 ? way : "outside")} of {town.Name}";
    }

    /// <summary>The place every event used to have: the population's home for a feast or a field, the drill ground for a ring.</summary>
    private static Venue Default(Use use, Map map)
    {
        Fallbacks++;

        var at = use == Use.Ring ? BotTourney.Ring : BotPopulation.Where;
        var town = BotTowns.Surveyed ? BotTowns.Of(at)?.Name : null;

        return new Venue
        {
            Use = use,
            Name = use == Use.Ring ? "the drill ground" : town ?? "home",
            Town = town,
            Map = map ?? BotPopulation.Home,
            At = at,
            Fallback = true
        };
    }

    /// <summary>Into the recent list and the tally of where events were held.</summary>
    private static void Note(Venue venue)
    {
        if (venue == null)
        {
            return;
        }

        Chosen++;

        var list = _recent[(int)venue.Use];

        list.Insert(0, venue);

        while (list.Count > Math.Max(1, Recent))
        {
            list.RemoveAt(list.Count - 1);
        }

        var key = venue.Fallback ? $"{venue.Name} (the old fixed place)" : venue.Use == Use.Field ? $"by {venue.Town ?? "nowhere named"}" : venue.Name;
        var tally = _where[(int)venue.Use];

        tally[key] = tally.TryGetValue(key, out var had) ? had + 1 : 1;
    }

    // ------------------------------------------------------------------ the herald

    /// <summary>
    /// The watcher who calls events: the marshal while one stands, else the lead. Null when neither has a body or both are
    /// witnessing a meeting, or the lead is charting the dungeons.
    /// </summary>
    private static BotWatcher Crier()
    {
        // Never the arena's referee (07.10.2026, arena27): it stands at the rings for the whole championship, and a revel's call
        // must not take it off them. See Referee.
        if (BotMarshal.Standing is { Body: { Deleted: false } } marshal && !BotWitness.Busy(marshal) && !BotZoneTour.Holding(marshal.Body)
            && !ReferenceEquals(marshal, _referee))
        {
            return marshal;
        }

        if (BotVigil.Lead is { Body: { Deleted: false } } lead && !BotWitness.Busy(lead) && !BotZoneTour.Holding(lead.Body)
            && (BotHalls.Charted || BotHalls.Probing == Point3D.Zero) && !ReferenceEquals(lead, _referee))
        {
            return lead;
        }

        return null;
    }

    /// <summary>
    /// A watcher goes to the arena and stands there <b>shown</b> as the referee until <see cref="Unreferee"/>: Argus himself when he
    /// is free, else the first of the squad that is. Returns its name, or null when nobody of the squad could go.
    ///
    /// <para>
    /// <b>Patrick, 07.10.2026: "Still NOT ONCE have I seen a championship where a champion is chosen in battle."</b> The herald stood
    /// at a town ring hidden, for two minutes a bout, so a person at a client saw two bots hitting each other with nobody calling it.
    /// The arena's referee stands at the main ring for the whole event and is visible — blessed and above Player, as a meeting's
    /// witness is (<c>BotWitness</c>), so nothing fights it. Held by its own slot rather than the herald's, so the herald's two-minute
    /// holds for revels and camps neither move it nor are blocked by it; <see cref="Holds"/> and <see cref="Keep"/> answer for both.
    /// </para>
    /// </summary>
    public static string Referee(Map map, Point3D at, string what)
    {
        if (map == null || map == Map.Internal)
        {
            return null;
        }

        BotWatcher pick = null;

        for (var i = 0; i < BotVigil.Squad.Count && pick == null; i++)
        {
            var w = BotVigil.Squad[i];

            if (w.Body is not { Deleted: false } || BotWitness.Busy(w) || BotZoneTour.Holding(w.Body))
            {
                continue;
            }

            // The lead charting the dungeons stands in the room being probed; it is not taken off that.
            if (i == 0 && !BotHalls.Charted && BotHalls.Probing != Point3D.Zero)
            {
                continue;
            }

            pick = w;
        }

        if (pick == null)
        {
            return null;
        }

        if (ReferenceEquals(pick, _herald))
        {
            _herald = null;
        }

        _referee = pick;
        _refereeMap = map;
        _refereeAt = at;
        _refereeWhat = what;
        Refereed++;
        ShowReferee();

        return pick.Name;
    }

    /// <summary>The referee put back on its mark and shown, if something moved or hid it. Asked by the arena each beat and by <see cref="Keep"/>.</summary>
    public static void ShowReferee()
    {
        if (_referee?.Body is not { Deleted: false } body || BotWitness.Busy(_referee))
        {
            return;
        }

        if (body.Map != _refereeMap || body.Location != _refereeAt)
        {
            // Hover hides the body again on purpose (BotDebugger.Hover); it is shown below.
            body.Hover(_refereeMap, _refereeAt);
        }

        if (body.Hidden)
        {
            body.Hidden = false;
        }
    }

    /// <summary>The referee's watch is over: hidden again, and back to watching at the vigil's next move.</summary>
    public static void Unreferee()
    {
        if (_referee?.Body is { Deleted: false } body)
        {
            body.Hidden = true;
        }

        _referee = null;
    }

    /// <summary>The referee's name, or null when nobody stands at the arenas.</summary>
    public static string RefereeName => _referee?.Name;

    /// <summary>The body that speaks for the watchers: the herald's, or any of the squad's.</summary>
    public static Mobile Voice
    {
        get
        {
            if (Crier()?.Body is { Deleted: false } body)
            {
                return body;
            }

            for (var i = 0; i < BotVigil.Squad.Count; i++)
            {
                if (BotVigil.Squad[i].Body is { Deleted: false } any && any.Map != null && any.Map != Map.Internal)
                {
                    return any;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The herald goes and stands beside the place for <paramref name="holdMs"/>, hidden as always, so a person at the place
    /// sees who called it; the vigil and the zones' tour leave it there until the hold is over (<see cref="Keep"/>).
    /// </summary>
    public static void Herald(Venue venue, int holdMs)
    {
        var crier = Crier();

        if (venue?.Map == null || venue.Map == Map.Internal || crier?.Body is not { Deleted: false } body)
        {
            return;
        }

        var spot = BotTowns.Place(venue.Map, venue.At.X + 2, venue.At.Y + 2, 3, out var aside) ? aside : venue.At;

        body.Hover(venue.Map, spot);

        _herald = crier;
        _heraldMap = venue.Map;
        _heraldAt = spot;
        _heraldWhat = venue.Name;
        _heraldUntil = Core.TickCount + Math.Max(0, holdMs);
        Heralded++;
    }

    /// <summary>The herald is let go before its hold is up.</summary>
    public static void Release() => _herald = null;

    /// <summary>Whether this watcher stands at a place it called something at. No side effects: asked when listing the tour's walkers.</summary>
    public static bool Holds(BotWatcher w) =>
        w != null && (ReferenceEquals(w, _herald) && Core.TickCount < _heraldUntil || ReferenceEquals(w, _referee));

    /// <summary>Where this watcher stands as herald and for how long yet, or null. For the live window (02.10.2026).</summary>
    public static string HeraldOf(BotWatcher w) =>
        w != null && ReferenceEquals(w, _referee)
            ? $"referee of {_refereeWhat ?? "the arena"} ({_refereeAt.X}, {_refereeAt.Y}), shown"
            : Holds(w)
                ? $"herald at {_heraldWhat ?? "the place"} ({_heraldAt.X}, {_heraldAt.Y}), {Math.Max(0, _heraldUntil - Core.TickCount) / 1000}s more"
                : null;

    /// <summary>
    /// The vigil's question before it moves a watcher on: true while the watcher holds a place, and the body put back there
    /// if something else moved it meanwhile (a meeting it was summoned to and has left).
    /// </summary>
    public static bool Keep(BotWatcher w)
    {
        // The arena's referee: kept at the rings and shown for as long as the championship runs (07.10.2026, arena27).
        if (w != null && ReferenceEquals(w, _referee))
        {
            ShowReferee();

            return true;
        }

        if (!Holds(w))
        {
            if (ReferenceEquals(w, _herald))
            {
                _herald = null;
            }

            return false;
        }

        if (w.Body is { Deleted: false } body && !BotWitness.Busy(w) && (body.Map != _heraldMap || body.Location != _heraldAt))
        {
            body.Hover(_heraldMap, _heraldAt);
        }

        return true;
    }

    /// <summary>
    /// A line to the island, in the world channel the bots' own voices use (<c>BotVoice</c>): shouted over the herald's head,
    /// broadcast to every client, on the dashboard's stream and in the speech log. Overhead only when the voices are off.
    /// </summary>
    public static void Announce(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var voice = Voice;

        logger.Information("Herald: {Line}", line);

        if (voice == null)
        {
            return;
        }

        if (!BotVoice.Say(voice, "world", line, true))
        {
            voice.Say(line.Length > 180 ? line[..180] : line);
        }
    }

    // ------------------------------------------------------------------ record

    /// <summary>The last places of each use, kept across a restart by <see cref="BotTourneyStore"/> so the rotation survives a deploy.</summary>
    public static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_recent.Length);

        for (var u = 0; u < _recent.Length; u++)
        {
            var list = _recent[u];

            writer.WriteEncodedInt(list.Count);

            for (var i = 0; i < list.Count; i++)
            {
                writer.Write(list[i].Name ?? "");
                writer.Write(list[i].Town ?? "");
                writer.Write(list[i].At);
            }
        }
    }

    public static void Load(IGenericReader reader)
    {
        for (var u = 0; u < _recent.Length; u++)
        {
            _recent[u].Clear();
        }

        var uses = reader.ReadEncodedInt();

        for (var u = 0; u < uses; u++)
        {
            var count = reader.ReadEncodedInt();

            for (var i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                var town = reader.ReadString();
                var at = reader.ReadPoint3D();

                if (u < _recent.Length)
                {
                    _recent[u].Add(new Venue { Use = (Use)u, Name = name, Town = town.Length > 0 ? town : null, At = at });
                }
            }
        }
    }

    /// <summary>A new world: no place has been used yet.</summary>
    public static void Forget()
    {
        for (var u = 0; u < _recent.Length; u++)
        {
            _recent[u].Clear();
            _where[u].Clear();
        }

        _rings.Clear();
        _herald = null;
        Unreferee();
    }

    /// <summary>Where events were held since the boot, and every reason a candidate was turned away. For the 5-minute summary.</summary>
    public static string Describe()
    {
        if (Chosen == 0)
        {
            return "venues: none chosen yet";
        }

        var say = ValueStringBuilder.Create(512);

        try
        {
            say.Append($"venues: {Chosen} chosen —");
            Tally(ref say, "feasts", _where[(int)Use.Feast]);
            Tally(ref say, "; fields", _where[(int)Use.Field]);
            Tally(ref say, "; rings", _where[(int)Use.Ring]);
            say.Append(
                $"; candidates refused: {Deadly} deadly ground, {Hostile} hostile, {Unreached} unreached, {Repeats} a recent place, {Cramped} no room for a ring, {Walled} in a town, {Unstood} no floor; {Reused} recent places reused, {Fallbacks} fallbacks to the old fixed place; the marshal's own camp spot kept {ModelKept} and moved {ModelMoved}; the herald went to {Heralded}, the arena's referee stood at {Refereed}"
            );

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    private static void Tally(ref ValueStringBuilder say, string label, Dictionary<string, int> tally)
    {
        say.Append(' ');
        say.Append(label);

        if (tally.Count == 0)
        {
            say.Append(" none");

            return;
        }

        List<(string Name, int N)> rows = [];

        foreach (var (name, n) in tally)
        {
            rows.Add((name, n));
        }

        rows.Sort((a, b) => b.N.CompareTo(a.N));

        for (var i = 0; i < rows.Count; i++)
        {
            say.Append(i == 0 ? " " : ", ");
            say.Append(rows[i].Name);
            say.Append(' ');
            say.Append(rows[i].N.ToString());
        }
    }
}
