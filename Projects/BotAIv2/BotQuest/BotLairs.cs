using System;
using System.Collections.Generic;
using Server.Engines.Spawners;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Where a creature lives on the island, read from the spawners that keep it.
///
/// <para>
/// <b>"Kill three mongbats anywhere" sent its taker nowhere.</b> The errand named no place, so the taker looked round
/// wherever it happened to stand and, finding none, stood there. On build 94, from 20:41 on 16.09.2026, Bryn Ashdown held
/// it beside Britain for 5.4 minutes, "looking for Mongbat, 0 of 3 down", and Nessa Ashdown held it for 7.1 minutes for one
/// kill; nothing on the shard knew where a mongbat lives.
/// </para>
///
/// <para>
/// <b>The engine does.</b> A spawner names what it keeps and stands where the engine put those creatures, which is the
/// reading <see cref="BotDungeon"/> already takes of the dungeons. This reads the island's block of the map — west of
/// <see cref="IslandEdge"/>, where the dungeons begin — once per world load, on the first question, and keeps each kind's
/// spawners by type, so an errand asking for a creature is sent to where one is kept rather than to where its taker was.
/// </para>
///
/// <para>
/// <b>And to where one is alive now, which is a second reading of the same spawner.</b> Build 95's first kill of orcs went
/// to the three nearest orc spawners to Britain and found nothing at any of them — "saw no Orc at the 3 nearest places on
/// the island that keep them", 9.6 minutes, 21:40 on 16.09.2026 — and the door's own look at two of them said "nothing
/// alive within 14 tiles". A population of eighty that hunts round its home empties the spawners nearest it; the spawner
/// is still there and its creatures are not. Each spawner entry keeps the list of what it has put into the world, so a lair
/// is offered only while that list holds a living one (DECISIONS W7: positive knowledge, and of now).
/// </para>
///
/// <para>
/// <b>A lair with no road is passed over, and only where the road map can say so.</b> Inside the square the road flood
/// covers (<see cref="BotRoads"/>) a lair no road reaches is left out; outside it nothing is known and distance is the
/// price (DECISIONS W4). Each lair is asked once and the answer kept, so the road map's own counters are not swamped by
/// every bot that considers an errand.
/// </para>
/// </summary>
public static class BotLairs
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLairs));

    /// <summary>The east edge of the island's block of the map: the dungeons are cut in from here on. See <see cref="BotDungeon"/>.</summary>
    public static int IslandEdge { get; set; } = 5120;

    /// <summary>Spawners read, all told.</summary>
    public static int Spawners { get; private set; }

    /// <summary>Names on spawners that the engine could not build as a creature: scenery, vendors' goods, typos.</summary>
    public static int Unknown { get; private set; }

    /// <summary>Lairs passed over because the road map says no road reaches them.</summary>
    public static long Roadless { get; private set; }

    /// <summary>Lairs passed over because nothing they put into the world is alive now.</summary>
    public static long Emptied { get; private set; }

    /// <summary>Lairs passed over because their spawner has been stopped or deleted.</summary>
    public static long Stopped { get; private set; }

    /// <summary>Questions asked of the lairs: a post, an offer, a hop from an empty lair to the next.</summary>
    public static long Asked { get; private set; }

    private sealed class Lair
    {
        public BaseSpawner Spawner;

        /// <summary>Which of the spawner's entries names the creature.</summary>
        public int Entry;

        public Point3D At;

        /// <summary>How far the spawner lets what it keeps wander.</summary>
        public int Roam;

        /// <summary>Steps of road from home; <see cref="Unasked"/> until the road map is ready to say, <see cref="Unmapped"/> outside it.</summary>
        public int Road = Unasked;
    }

    private const int Unasked = -2;

    private const int NoRoad = -1;

    private const int Unmapped = int.MaxValue;

    private static Map _map;

    private static readonly Dictionary<Type, List<Lair>> _byType = [];

    /// <summary>Lairs of a quarry and of every kind of it, gathered once per quarry asked about.</summary>
    private static readonly Dictionary<Type, List<Lair>> _byQuarry = [];

    private static readonly List<Lair> _none = [];

    /// <summary>Reads the island's spawners, once per map per world load.</summary>
    public static void Survey(Map map)
    {
        if (map == null || map == Map.Internal || _map == map)
        {
            return;
        }

        _map = map;
        _byType.Clear();
        _byQuarry.Clear();
        Spawners = 0;
        Unknown = 0;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        Dictionary<string, Type> named = new(StringComparer.OrdinalIgnoreCase);
        var bounds = new Rectangle2D(0, 0, Math.Min(IslandEdge, map.Width), map.Height);

        foreach (var spawner in map.GetItemsInBounds<BaseSpawner>(bounds))
        {
            if (spawner is not { Deleted: false } || spawner.Entries == null)
            {
                continue;
            }

            Spawners++;

            for (var i = 0; i < spawner.Entries.Count; i++)
            {
                var name = spawner.Entries[i]?.SpawnedName;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!named.TryGetValue(name, out var type))
                {
                    type = AssemblyHandler.FindTypeByName(name.Trim());

                    if (type != null && !typeof(BaseCreature).IsAssignableFrom(type))
                    {
                        type = null;
                    }

                    if (type == null)
                    {
                        Unknown++;
                    }

                    named[name] = type;
                }

                if (type == null)
                {
                    continue;
                }

                if (!_byType.TryGetValue(type, out var lairs))
                {
                    lairs = [];
                    _byType[type] = lairs;
                }

                lairs.Add(new Lair { Spawner = spawner, Entry = i, At = spawner.Location, Roam = Math.Max(0, spawner.WalkingRange) });
            }
        }

        logger.Information(
            "The island's lairs were read off {Spawners} spawners west of x {Edge} in {Ms}ms: {Kinds} kinds of creature kept, {Unknown} names that are no creature",
            Spawners,
            IslandEdge,
            watch.ElapsedMilliseconds,
            _byType.Count,
            Unknown
        );
    }

    /// <summary>How many places on the island keep this creature or a kind of it, alive now or not.</summary>
    public static int Count(Map map, Type quarry) => Of(map, quarry).Count;

    /// <summary>
    /// The fight a bot standing here can expect to meet in what the island's spawners keep alive now: the living, wild
    /// creatures that attack unprovoked of every lair whose ground comes within <paramref name="sight"/> of the place, each
    /// lair's weighed by the share of its ground in sight — a spawner lets what it keeps wander a box its walking range
    /// wide, and a bot meets what is near. Reckoned as a fight is where it is met: the strongest (as likely present as its
    /// kind is numerous in sight) in full, and everything at <see cref="BotThreat.Secondary"/>. How many the lairs keep
    /// comes back in <paramref name="kept"/>, and the same creatures summed over all their ground in <paramref name="wide"/>,
    /// which is what build 103 reckoned. See <see cref="BotKept"/>.
    /// </summary>
    public static double MetFight(Map map, Point3D where, int sight, out int kept, out double wide)
    {
        kept = 0;
        wide = 0.0;

        if (map == null || map == Map.Internal)
        {
            return 0.0;
        }

        Survey(map);

        if (_map != map)
        {
            return 0.0;
        }

        var worst = 0.0;
        var total = 0.0;
        var metWorst = 0.0;
        var metTotal = 0.0;

        foreach (var lairs in _byType.Values)
        {
            for (var i = 0; i < lairs.Count; i++)
            {
                var lair = lairs[i];
                var roam = lair.Roam;
                var reach = roam + sight;

                if (Math.Abs(lair.At.X - where.X) > reach || Math.Abs(lair.At.Y - where.Y) > reach)
                {
                    continue;
                }

                // The part of the lair's ground within sight of the place, as a share of all its ground.
                var across = Math.Min(lair.At.X + roam, where.X + sight) - Math.Max(lair.At.X - roam, where.X - sight) + 1;
                var down = Math.Min(lair.At.Y + roam, where.Y + sight) - Math.Max(lair.At.Y - roam, where.Y - sight) + 1;

                if (across <= 0 || down <= 0)
                {
                    continue;
                }

                var side = 2.0 * roam + 1;
                var share = Math.Min(1.0, across * (double)down / (side * side));

                var spawner = lair.Spawner;

                if (spawner is not { Deleted: false, Running: true } || spawner.Entries == null || lair.Entry >= spawner.Entries.Count)
                {
                    continue;
                }

                var spawned = spawner.Entries[lair.Entry]?.Spawned;

                if (spawned == null)
                {
                    continue;
                }

                var alive = 0;
                var strongest = 0.0;
                var sum = 0.0;

                for (var j = 0; j < spawned.Count; j++)
                {
                    if (spawned[j] is not BaseCreature { Deleted: false, Alive: true, Controlled: false, Summoned: false } creature
                        || creature is BaseVendor || creature.Map != map
                        || creature.FightMode is FightMode.None or FightMode.Aggressor or FightMode.Evil)
                    {
                        continue;
                    }

                    var power = BotThreat.Power(creature);

                    alive++;
                    sum += power;
                    strongest = Math.Max(strongest, power);
                }

                if (alive == 0)
                {
                    continue;
                }

                kept += alive;
                total += sum;
                worst = Math.Max(worst, strongest);
                metTotal += sum * share;
                metWorst = Math.Max(metWorst, strongest * Math.Min(1.0, alive * share));
            }
        }

        wide = worst + (total - worst) * BotThreat.Secondary;

        return (1 - BotThreat.Secondary) * metWorst + BotThreat.Secondary * metTotal;
    }

    /// <summary>
    /// The type names of the creatures the island's spawners keep, less the shopkeepers, sorted; read on first use. What a
    /// kill errand may name: see <c>BotMarshal.Schema</c>.
    /// </summary>
    public static List<string> Kinds(Map map)
    {
        List<string> names = [];

        if (map == null || map == Map.Internal)
        {
            return names;
        }

        Survey(map);

        if (_map != map)
        {
            return names;
        }

        foreach (var type in _byType.Keys)
        {
            if (!typeof(BaseVendor).IsAssignableFrom(type))
            {
                names.Add(type.Name);
            }
        }

        names.Sort(StringComparer.Ordinal);

        return names;
    }

    /// <summary>
    /// The nearest lair of this creature to a place by distance alone, whether or not it holds one now or a road reaches it,
    /// or <see cref="Point3D.Zero"/> when the island keeps none; how far off it lies, less how far the spawner lets what it
    /// keeps wander, comes back in <paramref name="reach"/>. For the board, which asks whether a place is anywhere near the
    /// creature at all.
    /// </summary>
    public static Point3D Closest(Map map, Type quarry, Point3D to, out int reach)
    {
        reach = int.MaxValue;

        var lairs = Of(map, quarry);
        Lair best = null;

        for (var i = 0; i < lairs.Count; i++)
        {
            var lair = lairs[i];
            var away = Math.Max(Math.Abs(lair.At.X - to.X), Math.Abs(lair.At.Y - to.Y)) - lair.Roam;

            if (away < reach)
            {
                reach = away;
                best = lair;
            }
        }

        return best?.At ?? Point3D.Zero;
    }

    /// <summary>
    /// The nearest lair of this creature to a place that holds a living one now, passing over the ones in
    /// <paramref name="passed"/> and the ones no road reaches; <see cref="Point3D.Zero"/> when there is none. The lair's
    /// wandering range comes back in <paramref name="roam"/>.
    /// </summary>
    public static Point3D Nearest(Map map, Type quarry, Point3D from, IReadOnlyList<Point3D> passed, out int roam)
    {
        Asked++;
        roam = 0;

        var lairs = Of(map, quarry);
        Lair best = null;
        var closest = int.MaxValue;

        for (var i = 0; i < lairs.Count; i++)
        {
            var lair = lairs[i];
            var away = Math.Max(Math.Abs(lair.At.X - from.X), Math.Abs(lair.At.Y - from.Y));

            // Distance first: it is two subtractions, and the questions below read the spawner.
            if (away >= closest || Passed(passed, lair.At))
            {
                continue;
            }

            // <b>Nearest by the road, not by the crow.</b> A lair over the river west of Britain is 300 to 450 tiles further
            // round from home than the town and the near bank (BotRoads.Behind, build 210); straight-line nearest sent the
            // takers of Hermes's ogre errand of 24.09.2026 at (971, 1308) and (1361, 522), and three of them stopped 150
            // to 190 tiles short after 600 beats. The roundness is added to the distance, so a lair across the water loses
            // to one on this side a little further off, and still wins when it is the only one.
            var cost = away + Math.Max(0, BotRoads.Behind(map, from, lair.At));

            if (cost >= closest || Unreached(map, lair) || !Holds(lair, quarry))
            {
                continue;
            }

            closest = cost;
            best = lair;
        }

        if (best == null)
        {
            return Point3D.Zero;
        }

        roam = best.Roam;

        return best.At;
    }

    /// <summary>Whether a lair's spawner is running and something of the creature it put into the world is alive now.</summary>
    private static bool Holds(Lair lair, Type quarry)
    {
        var spawner = lair.Spawner;

        if (spawner is not { Deleted: false, Running: true } || spawner.Entries == null || lair.Entry >= spawner.Entries.Count)
        {
            Stopped++;

            return false;
        }

        var spawned = spawner.Entries[lair.Entry]?.Spawned;

        if (spawned != null)
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] is BaseCreature { Deleted: false, Alive: true, Controlled: false } creature && quarry.IsInstanceOfType(creature))
                {
                    return true;
                }
            }
        }

        Emptied++;

        return false;
    }

    private static List<Lair> Of(Map map, Type quarry)
    {
        if (map == null || map == Map.Internal || quarry == null)
        {
            return _none;
        }

        Survey(map);

        if (_map != map)
        {
            return _none;
        }

        if (_byQuarry.TryGetValue(quarry, out var gathered))
        {
            return gathered;
        }

        gathered = [];

        foreach (var (type, lairs) in _byType)
        {
            if (quarry.IsAssignableFrom(type))
            {
                gathered.AddRange(lairs);
            }
        }

        _byQuarry[quarry] = gathered;

        return gathered;
    }

    private static bool Unreached(Map map, Lair lair)
    {
        if (lair.Road == Unasked && BotRoads.Ready)
        {
            lair.Road = BotRoads.Covers(map, lair.At.X, lair.At.Y) ? BotRoads.FromHome(map, lair.At.X, lair.At.Y) : Unmapped;
        }

        if (lair.Road != NoRoad)
        {
            return false;
        }

        Roadless++;

        return true;
    }

    private static bool Passed(IReadOnlyList<Point3D> passed, Point3D at)
    {
        if (passed == null)
        {
            return false;
        }

        for (var i = 0; i < passed.Count; i++)
        {
            if (passed[i] == at)
            {
                return true;
            }
        }

        return false;
    }

    public static string Describe() =>
        _map == null
            ? "the island's lairs have not been read"
            : $"{Spawners} spawners read, {_byType.Count} kinds kept, {Asked} questions; lairs passed over: {Emptied} with nothing of theirs alive, {Stopped} stopped, {Roadless} with no road";

    /// <summary>Forgotten with the world: the next question reads the spawners again.</summary>
    public static void Forget()
    {
        _map = null;
        _byType.Clear();
        _byQuarry.Clear();
        Spawners = 0;
        Unknown = 0;
        Roadless = 0;
        Emptied = 0;
        Stopped = 0;
        Asked = 0;
    }
}
