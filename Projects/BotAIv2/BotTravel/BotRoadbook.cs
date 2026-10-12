using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Server.Engines.Pathing.Tiered;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The roads between the towns: how long each is, how dangerous the ground along it reads, and how often the population
/// has walked it.
///
/// <para>
/// <b>The road is the navigation graph's, and the knowledge is the population's.</b> A road from one town to another is
/// drawn by the long tier (<see cref="NavigationService.Route"/>), square to square, a pair a slice on the module's
/// clock; its length is the steps along its waypoints. That much is a fact about the map, and since 29.09.2026 it is kept
/// in the store with the walks and only ever shortened (<see cref="Offer"/>): the boot's draw and every traveller's walk are
/// offered against it, and the shorter wins.
/// What the population knows is the rest: how many times somebody walked the road (<see cref="Road.Walked"/>), which is
/// what makes a road "built" — a traveller prefers a road nobody has walked, and the dashboard draws a road once somebody
/// has — and what the ground along it reads on the quadrant map, which is the same map every bot reads
/// (<see cref="BotQuad.Safety"/>), averaged along the waypoints when asked. The walks are kept in
/// <c>Saves/BotRoadbook</c>, so what the island has learned of its roads outlives a restart and is wiped with the rest.
/// </para>
///
/// <para>
/// <b>What "the ways round" are.</b> The long tier is the way round: it routes over the whole graph and finds the bridge
/// a thousand steps off, and the walker follows it a leg at a time (BotJourney). A road's danger is shared aloud by the
/// traveller that walked it, to its guild (BotTravel) — the sharing Patrick asked for — and it is written nowhere else,
/// because the danger map is one map already.
/// </para>
/// </summary>
public static class BotRoadbook
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRoadbook));

    public sealed class Road
    {
        public string From;

        public string To;

        /// <summary>Steps along the waypoints, square to square. Nought while undrawn; -1 when the graph found no way.</summary>
        public int Steps;

        public readonly List<Point3D> Points = [];

        /// <summary>Times a traveller walked it end to end.</summary>
        public int Walked;

        public DateTime LastWalked;

        /// <summary>Asked of the graph this boot. A road read from the store is drawn again all the same, and kept if no longer.</summary>
        public bool Asked;

        /// <summary>Whether the road held is a traveller's walk rather than the graph's draw.</summary>
        public bool ByWalk;

        public bool Drawn => Steps != 0;

        public override string ToString() => $"{From}–{To}: {(Steps > 0 ? $"{Steps} steps" : Steps < 0 ? "no way" : "undrawn")}, walked {Walked}";
    }

    /// <summary>Every fourth waypoint is enough for a line on the map.</summary>
    public static int Thin { get; set; } = 4;

    private static readonly Dictionary<(string, string), Road> _roads = new();

    private static readonly List<Road> _list = [];

    private static int _cursor;

    private static Map _map;

    public static bool Listed { get; private set; }

    /// <summary>Whether every pair of reachable towns has been asked of the graph.</summary>
    public static bool Drawn { get; private set; }

    public static int Roads => _list.Count;

    public static int Ways { get; private set; }

    public static int NoWay { get; private set; }

    public static long Walks { get; private set; }

    /// <summary>Walks and draws that were shorter than the road held, and took its place; and those that were not.</summary>
    public static long WalksShorter { get; private set; }

    public static long WalksNoShorter { get; private set; }

    public static long DrawsShorter { get; private set; }

    public static long DrawsNoShorter { get; private set; }

    /// <summary>
    /// How far apart a traveller's footprints are kept, in tiles: sixteen, the graph's own cluster, so a walk is measured on the
    /// same scale as a drawn road's one point a cluster and neither wins by being sampled finer.
    /// </summary>
    public static int TrackStep { get; set; } = 16;

    /// <summary>
    /// More than this between two footprints is not a walk — a gate, a death carried home, a resurrection — and the track is
    /// not offered. Forty-eight tiles.
    /// </summary>
    public static int TrackJump { get; set; } = 48;

    private static readonly List<Point3D> _offered = [];

    /// <summary>The roads as JSON for the dashboard's <c>/api/roads</c>. Written on the loop, read by the listener's thread.</summary>
    public static volatile string Json = "{\"roads\":[]}";

    public static IReadOnlyList<Road> All => _list;

    private static (string, string) Key(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

    /// <summary>The road between two towns, or null when none is listed.</summary>
    public static Road Between(string a, string b) =>
        a != null && b != null && _roads.TryGetValue(Key(a, b), out var road) ? road : null;

    /// <summary>Lists every pair of reachable towns once the towns are known. Returns whether the list stands.</summary>
    public static bool List(Map map)
    {
        if (Listed)
        {
            return true;
        }

        if (!BotTowns.Ensure(map))
        {
            return false;
        }

        Listed = true;
        _map = map;

        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            for (var j = i + 1; j < towns.Count; j++)
            {
                if (!towns[i].FromHome || !towns[j].FromHome)
                {
                    continue;
                }

                var key = Key(towns[i].Name, towns[j].Name);

                if (_roads.ContainsKey(key))
                {
                    continue;
                }

                var road = new Road { From = key.Item1, To = key.Item2 };

                // What the store read before the towns were known is laid over the fresh list.
                if (_walkedRead.TryGetValue(key, out var read))
                {
                    road.Walked = read.Walked;
                    road.LastWalked = read.LastWalked;

                    if (read.Steps > 0 && read.Points is { Count: > 1 })
                    {
                        road.Steps = read.Steps;
                        road.Points.AddRange(read.Points);
                        road.ByWalk = read.ByWalk;
                    }
                }

                _roads[key] = road;
                _list.Add(road);
            }
        }

        logger.Information("Roads: {Roads} between {Towns} reachable towns are to be drawn, one a slice", _list.Count, BotTowns.Reachable);

        return true;
    }

    /// <summary>Draws one undrawn road. Returns whether anything is left to draw.</summary>
    public static bool Slice(Map map)
    {
        if (!List(map))
        {
            return true;
        }

        if (Drawn)
        {
            return false;
        }

        for (var looked = 0; looked < _list.Count; looked++)
        {
            var road = _list[_cursor++ % _list.Count];

            if (road.Asked)
            {
                continue;
            }

            var from = BotTowns.Find(road.From);
            var to = BotTowns.Find(road.To);

            if (from == null || to == null)
            {
                road.Asked = true;
                road.Steps = road.Steps > 0 ? road.Steps : -1;

                continue;
            }

            var points = new List<Point3D>();
            var status = NavigationService.Route(map, from.Square, to.Square, points, 8);

            switch (status)
            {
                case NavStatus.Ok:
                case NavStatus.Direct:
                    {
                        road.Asked = true;
                        points.Insert(0, from.Square);
                        points.Add(to.Square);
                        Offer(road, points, false);
                        Ways++;

                        break;
                    }
                case NavStatus.Pending:
                    {
                        // The graph is not whole there yet; asked again next slice.
                        return true;
                    }
                default:
                    {
                        // A road held from the store stands: the graph finding none now is not a shorter one.
                        road.Asked = true;
                        road.Steps = road.Steps > 0 ? road.Steps : -1;
                        NoWay++;

                        break;
                    }
            }

            return true;
        }

        Drawn = true;
        Write();

        logger.Information("Roads: {Ways} drawn and {NoWay} found no way between {Roads} pairs of towns; the longest {Longest}", Ways, NoWay, _list.Count, Longest());

        return false;
    }

    private static string Longest()
    {
        Road longest = null;

        for (var i = 0; i < _list.Count; i++)
        {
            if (_list[i].Steps > 0 && (longest == null || _list[i].Steps > longest.Steps))
            {
                longest = _list[i];
            }
        }

        return longest?.ToString() ?? "none";
    }

    private static int Length(List<Point3D> points)
    {
        var steps = 0;

        for (var i = 1; i < points.Count; i++)
        {
            steps += Math.Max(Math.Abs(points[i].X - points[i - 1].X), Math.Abs(points[i].Y - points[i - 1].Y));
        }

        return steps;
    }

    /// <summary>The safety of the ground along a road, averaged over its waypoints: the quadrant map's reading, -1 to 1.</summary>
    public static double Safety(Road road)
    {
        if (road == null || _map == null || road.Points.Count == 0)
        {
            return BotQuad.Fresh;
        }

        var total = 0.0;

        for (var i = 0; i < road.Points.Count; i++)
        {
            total += BotQuad.Safety(_map, road.Points[i]);
        }

        return total / road.Points.Count;
    }

    /// <summary>
    /// The strongest thing the ground along a road asks of a walker: the quadrant map's <c>Muscle</c> at the worst of its
    /// waypoints — what lives and has killed there — so a traveller can be measured against the road before it sets out.
    /// </summary>
    public static double Asks(Road road)
    {
        if (road == null || _map == null || road.Points.Count == 0)
        {
            return 0.0;
        }

        var worst = 0.0;

        for (var i = 0; i < road.Points.Count; i++)
        {
            worst = Math.Max(worst, BotQuad.Muscle(_map, road.Points[i]));
        }

        return worst;
    }

    /// <summary>
    /// A traveller walked from one town to another, leaving <paramref name="track"/> (its footprints every
    /// <see cref="TrackStep"/> tiles, from where it set out to where it arrived; null when the walk was broken). Returns the
    /// road, or null when none is listed between them.
    /// </summary>
    public static Road Walked(string from, string to, IReadOnlyList<Point3D> track = null)
    {
        var road = Between(from, to);

        if (road == null)
        {
            return null;
        }

        road.Walked++;
        road.LastWalked = Core.Now;
        Walks++;

        var a = BotTowns.Find(road.From);
        var b = BotTowns.Find(road.To);

        if (track is { Count: > 1 } && a != null && b != null)
        {
            // Measured as the road is: square to square, in the road's own direction.
            _offered.Clear();
            _offered.Add(a.Square);

            if (string.Equals(from, road.From, StringComparison.Ordinal))
            {
                for (var i = 0; i < track.Count; i++)
                {
                    _offered.Add(track[i]);
                }
            }
            else
            {
                for (var i = track.Count - 1; i >= 0; i--)
                {
                    _offered.Add(track[i]);
                }
            }

            _offered.Add(b.Square);
            Offer(road, _offered, true);
        }

        Write();

        return road;
    }

    /// <summary>
    /// <b>A road is replaced only by a shorter one (Patrick, 29.09.2026: "paths and graphs must be remembered; they can only
    /// be optimised").</b> The boot's draw and every traveller's walk are measured the same way — the steps along points one
    /// cluster apart, square to square — and take the road's place only when they come out shorter; a road with no length
    /// yet takes whatever is offered. Returns whether it took the place.
    /// </summary>
    private static bool Offer(Road road, List<Point3D> points, bool walked)
    {
        var steps = Math.Max(1, Length(points));

        if (road.Steps > 0 && steps >= road.Steps)
        {
            if (walked)
            {
                WalksNoShorter++;
            }
            else
            {
                DrawsNoShorter++;
            }

            return false;
        }

        if (road.Steps > 0)
        {
            if (walked)
            {
                WalksShorter++;

                logger.Information("Roads: a walk shortened {From}–{To} from {Old} steps to {New}", road.From, road.To, road.Steps, steps);
            }
            else
            {
                DrawsShorter++;
            }
        }

        road.Points.Clear();
        road.Points.AddRange(points);
        road.Steps = steps;
        road.ByWalk = walked;

        return true;
    }

    /// <summary>The dashboard's JSON again, for the danger readings that change under it. Once a minute, from the module.</summary>
    public static void Refresh()
    {
        if (Drawn)
        {
            Write();
        }
    }

    /// <summary>Rewrites the dashboard's JSON. A few kilobytes; on the loop, after a draw or a walk.</summary>
    private static void Write()
    {
        var buffer = new MemoryStream();

        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WritePropertyName("roads");
            w.WriteStartArray();

            for (var i = 0; i < _list.Count; i++)
            {
                var road = _list[i];

                if (road.Steps <= 0)
                {
                    continue;
                }

                var safety = Safety(road);

                w.WriteStartObject();
                w.WriteString("from", road.From);
                w.WriteString("to", road.To);
                w.WriteNumber("steps", road.Steps);
                w.WriteNumber("walked", road.Walked);
                w.WriteNumber("safety", Math.Round(safety, 3));
                w.WriteString("band", BotQuad.Band(safety));
                w.WritePropertyName("points");
                w.WriteStartArray();

                for (var j = 0; j < road.Points.Count; j++)
                {
                    if (j % Math.Max(1, Thin) != 0 && j != road.Points.Count - 1)
                    {
                        continue;
                    }

                    w.WriteStartArray();
                    w.WriteNumberValue(road.Points[j].X);
                    w.WriteNumberValue(road.Points[j].Y);
                    w.WriteEndArray();
                }

                w.WriteEndArray();
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        Json = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    public static string Describe()
    {
        if (!Listed)
        {
            return "the roads are not listed yet";
        }

        var walked = 0;

        for (var i = 0; i < _list.Count; i++)
        {
            if (_list[i].Walked > 0)
            {
                walked++;
            }
        }

        return $"{_list.Count} roads between the towns, {Ways} drawn and {NoWay} without a way; {walked} of them walked, {Walks} walks in all; "
               + $"only ever shortened: {WalksShorter} walks shorter than the road held and {WalksNoShorter} not, {DrawsShorter} draws shorter than the road "
               + $"kept from the store and {DrawsNoShorter} not";
    }

    // ---- the store: what was walked, by town names, read before the towns are known and laid over the list ----

    private static readonly Dictionary<(string, string), (int Walked, DateTime LastWalked, int Steps, List<Point3D> Points, bool ByWalk)> _walkedRead = new();

    /// <summary>Shape 2: every road walked or with a length, and the road itself, so it is only ever shortened across boots.</summary>
    internal static void Save(IGenericWriter writer)
    {
        var many = 0;

        for (var i = 0; i < _list.Count; i++)
        {
            if (_list[i].Walked > 0 || _list[i].Steps > 0)
            {
                many++;
            }
        }

        writer.WriteEncodedInt(many);

        for (var i = 0; i < _list.Count; i++)
        {
            var road = _list[i];

            if (road.Walked <= 0 && road.Steps <= 0)
            {
                continue;
            }

            writer.Write(road.From);
            writer.Write(road.To);
            writer.WriteEncodedInt(road.Walked);
            writer.Write(road.LastWalked);

            var kept = road.Steps > 0 ? road.Points.Count : 0;

            writer.WriteEncodedInt(road.Steps > 0 ? road.Steps : 0);
            writer.Write(road.ByWalk);
            writer.WriteEncodedInt(kept);

            for (var j = 0; j < kept; j++)
            {
                writer.Write(road.Points[j]);
            }
        }
    }

    internal static int Load(IGenericReader reader, int shape)
    {
        _walkedRead.Clear();

        var many = reader.ReadEncodedInt();

        for (var i = 0; i < many; i++)
        {
            var from = reader.ReadString();
            var to = reader.ReadString();
            var walked = reader.ReadEncodedInt();
            var last = reader.ReadDateTime();
            var steps = 0;
            var byWalk = false;
            List<Point3D> points = null;

            if (shape >= 2)
            {
                steps = reader.ReadEncodedInt();
                byWalk = reader.ReadBool();

                var kept = reader.ReadEncodedInt();

                points = new List<Point3D>(kept);

                for (var j = 0; j < kept; j++)
                {
                    points.Add(reader.ReadPoint3D());
                }
            }

            if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            {
                _walkedRead[Key(from, to)] = (walked, last, steps, points, byWalk);
            }
        }

        return _walkedRead.Count;
    }

    public static void Forget()
    {
        _roads.Clear();
        _list.Clear();
        _walkedRead.Clear();
        _cursor = 0;
        _map = null;
        Listed = false;
        Drawn = false;
        Ways = 0;
        NoWay = 0;
        Walks = 0;
        WalksShorter = 0;
        WalksNoShorter = 0;
        DrawsShorter = 0;
        DrawsNoShorter = 0;
        Json = "{\"roads\":[]}";
    }
}

/// <summary>The walks along the roads and the roads themselves, kept between boots. Shape 2 (1: walks only). See <see cref="BotRoadbook"/>.</summary>
public sealed class BotRoadbookStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRoadbookStore));

    private const int Shape = 2;

    private static BotRoadbookStore _store;

    public static void Configure() => _store ??= new BotRoadbookStore();

    public BotRoadbookStore() : base("BotRoadbook", 15)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotRoadbook.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < 1 || shape > Shape)
        {
            logger.Warning("The saved roadbook is shape {Found} and this build reads {Wanted}; the roads start unwalked", shape, Shape);

            return;
        }

        var read = BotRoadbook.Load(reader, shape);

        if (read > 0)
        {
            logger.Information("Roadbook: {Read} walked roads read back from the save", read);
        }
    }
}
