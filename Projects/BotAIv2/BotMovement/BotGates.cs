using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Engines.Pathing.Tiered;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The teleporters of the world read as gates between lands, so that a walk can be drawn through a cave mouth
/// instead of stopping at it.
///
/// <para>
/// <b>Patrick's order of 29.09.2026: "we have cave mouths in the world — entrances to the dungeons. Instead of carrying
/// and teleporting, every cave gets its proper entrance that teleports the bots inside, and the way out is the same:
/// come back to the entrance. Travel there as a party, alone, or alone and gather a party inside."</b> The engine already
/// does the teleporting: a <see cref="Teleporter"/> is an invisible item on a tile, and a body that steps onto it is put
/// down at the item's destination (<c>Teleporter.OnMoveOver</c>). What no bot could do was <i>plan</i> a walk through one,
/// because the dungeon block from x 5120 eastward shares no ground with the island — the navigation graph's components say
/// so, provably — and a search for a road that does not exist answers "no way through" however it is asked. So every
/// delve since 11.09.2026 put its party down by <c>MoveToWorld</c> and lifted it out again (<c>BotDelve.Put</c>,
/// <c>BotDelveParty.Surface</c>).
/// </para>
///
/// <para>
/// <b>A gate is a fact about two components.</b> Every active teleporter on the population's map whose destination is on
/// the same map is read once from the world's items, folded with its neighbours (three in a row across a doorway are one
/// gate), and each end is placed in the graph's component it stands in (<see cref="NavigationService.ComponentOf"/>).
/// A land is a component. A route from one land to another is a shortest path over the gates — a few hundred of them,
/// Dijkstra by straight distance — and what the journey is handed is the first gate on it: walk to that tile exactly,
/// the engine does the rest, and the next beat draws the next leg from wherever the walker has been put down
/// (<c>BotJourney.Draw</c>).
/// </para>
///
/// <para>
/// <b>Read from the world rather than written down, by the rule the dungeons are surveyed by.</b> A table of entrances
/// would be a promise about a map; the items are the map. And the lands are not written down at all: the component is
/// asked of the graph, which is rebuilt when houses go up and read back from disk at boot, so a gate's two ends are as
/// current as the graph is.
/// </para>
///
/// <para>
/// <b>What it will not do.</b> It does not route <i>within</i> a land — that is the tiers' business — and it does not
/// step: a walker on a gate's tile is teleported by the engine or is not (a criminal at a guarded gate, a body mid-fight
/// where the gate checks for it), and a walker that was not is a walker planning straight at a goal in another land,
/// which fails the way it always did and is counted (<see cref="Refused"/>).
/// </para>
/// </summary>
public static class BotGates
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGates));

    /// <summary>One way through: the tile to step on, where it leads, and the land at each end.</summary>
    public sealed class Gate
    {
        /// <summary>The tile a walker aims at. The middle one of the run, where a doorway has three.</summary>
        public Point3D From;

        /// <summary>Where the engine puts a body down.</summary>
        public Point3D To;

        /// <summary>Every teleporter tile folded into this gate.</summary>
        public readonly List<Point3D> Tiles = [];

        public int FromLand = -1;

        public int ToLand = -1;

        /// <summary>"into Despise", "out of the Orc Caves", or "a passage", for the log.</summary>
        public string Name = "a passage";

        /// <summary>Routes that began with this gate.</summary>
        public long Used;

        /// <summary>
        /// A public moongate's way to one of the other moongates (29.09.2026). The engine asks a player which city from a gump;
        /// a bot has no client, so the walker puts it through itself on standing on the gate (<see cref="Travel"/>).
        /// </summary>
        public bool Moongate;

        public override string ToString() => $"{Name} at ({From.X}, {From.Y}) to ({To.X}, {To.Y})";
    }

    /// <summary>Whether gates are read and routed over at all. Off, every land is an island and the delve carries its party as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Teleporters this near one another, leading this near one destination, are one gate.</summary>
    public static int Fold { get; set; } = 4;

    /// <summary>Within this many tiles of a gate's far side, a walker has been through it. See <c>BotJourney.Leg</c>.</summary>
    public static int Through { get; set; } = 6;

    /// <summary>How long a land looked up for a tile is trusted. The graph changes when a house goes up; a minute is generous.</summary>
    public static int LandCacheMs { get; set; } = 60000;

    /// <summary>Tiles whose land is kept at once; past this the keeping starts again. See <see cref="LandOf"/>.</summary>
    public static int LandCacheMost { get; set; } = 50000;

    private static readonly List<Gate> _gates = [];

    private static readonly Dictionary<(int X, int Y, int Z), (int Land, long Tick)> _lands = [];

    // Keyed by the two lands AND where in them the walk begins and ends, by 64-tile squares (build 330): the answer is the
    // shortest way from this point, and one bot's first gate was being handed to every walker of the land for a minute.
    private static readonly Dictionary<(int A, int B, int FX, int FY, int TX, int TY), (Gate First, long Tick)> _routes = [];

    /// <summary>
    /// <b>A dungeon is not a road (29.09.2026, build 330).</b> The gates were weighed by straight distance alone, a cave as cheap
    /// as a meadow, and the moongates read from the whole map include ones standing inside dungeons (a way to Skara Brae from
    /// (5153, 1760)). On 329 Gimrund, sent home to The Crown's new house in Skara Brae, went in by the mouth of Despise, stood
    /// at (5573, 632) for eight minutes and "the walk stopped closing 5014 tiles short"; Edmund the same. A gate that puts the
    /// walker down inside a dungeon other than the one it is bound for costs this many tiles more.
    /// </summary>
    public static int DungeonToll { get; set; } = 3000;

    /// <summary>What passing a gate adds besides its walk. See <see cref="DungeonToll"/>.</summary>
    private static int Toll(Gate gate, Point3D to)
    {
        if (DungeonToll <= 0 || !BotDungeon.Under(gate.To))
        {
            return 0;
        }

        return BotDungeon.Holding(gate.To) is { } deep && ReferenceEquals(deep, BotDungeon.Holding(to)) ? 0 : DungeonToll;
    }

    /// <summary>
    /// The nearest public moongate on the walker's own land that leads onto <paramref name="toLand"/>, within
    /// <paramref name="most"/> tiles; null when there is none. For island dwellers whose work lies over the water (build 332).
    /// </summary>
    public static Gate NearestMoongate(Map map, Point3D from, int toLand, int most)
    {
        if (!Ensure(map) || toLand < 0)
        {
            return null;
        }

        var land = LandOf(map, from);

        if (land < 0 || land == toLand)
        {
            return null;
        }

        Gate best = null;
        var least = int.MaxValue;

        for (var i = 0; i < _gates.Count; i++)
        {
            var gate = _gates[i];

            if (!gate.Moongate || gate.FromLand != land || gate.ToLand != toLand || Away(from, gate.From) > most)
            {
                continue;
            }

            // Among the ways out of one stand, the landing nearest the walker (Jhelom's gate: Trinsic before Minoc).
            var away = Away(from, gate.From) + Away(gate.To, from) / 16;

            if (away < least)
            {
                least = away;
                best = gate;
            }
        }

        return best;
    }

    /// <summary>The whole way over the gates from one point to another, for Argus's <c>gates route</c>; nought when none.</summary>
    public static string Chain(Map map, Point3D from, Point3D to)
    {
        if (!Ensure(map))
        {
            return Describe();
        }

        var la = LandOf(map, from);
        var lb = LandOf(map, to);

        if (la < 0 || lb < 0)
        {
            return $"the graph cannot place {(la < 0 ? "the start" : "the goal")}";
        }

        if (la == lb)
        {
            return $"both stand in land {la}: no gate is needed";
        }

        var chain = new List<Gate>();
        var first = First(la, from, lb, to, chain);

        if (first == null)
        {
            return $"no way over the gates from land {la} to land {lb}";
        }

        var parts = new List<string>();

        for (var i = 0; i < chain.Count; i++)
        {
            parts.Add($"{chain[i]} (land {chain[i].FromLand} to {chain[i].ToLand}{(Toll(chain[i], to) > 0 ? ", into a dungeon" : "")})");
        }

        return $"from land {la} to land {lb} in {chain.Count} gates: {string.Join("; then ", parts)}";
    }

    private static Map _map;

    /// <summary>Whether the world's teleporters have been read.</summary>
    public static bool Surveyed { get; private set; }

    /// <summary>Whether every gate has its two lands: the graph's components were counted and asked.</summary>
    public static bool Landed { get; private set; }

    /// <summary>Whether routes over gates can be drawn.</summary>
    public static bool Ready => Running && Surveyed && Landed;

    /// <summary>The graph's <see cref="NavigationService.ComponentEpoch"/> the gates' lands were asked at.</summary>
    private static int _landEpoch;

    /// <summary>Times the gates' lands were asked again after the graph counted its components afresh, and gates whose lands moved.</summary>
    public static int Relanded { get; private set; }

    public static int Moved { get; private set; }

    /// <summary>Gates read from the world.</summary>
    public static int Gates => _gates.Count;

    /// <summary>
    /// Whether entrances the world lacks are raised from <c>Data/teleporters.json</c>. See <see cref="Raise"/>.
    ///
    /// <para>
    /// <b>The world had 113 of the 438 teleporters its own data describes (build 274, 00:47 on 29.09.2026), and from home
    /// only the Orc Caves could be walked into.</b> Despise's mouth at (1296, 1080), Shame's at (513, 1559), Covetous's
    /// five, Destard's, Wrong's: none stood in the world as an item, though every one is in the file the world was meant
    /// to be decorated from. Patrick's order is that every cave gets its proper entrance; the entrances are the file's.
    /// </para>
    /// </summary>
    public static bool RaiseMissing { get; set; } = true;

    /// <summary>
    /// <b>The public moongates are gates (29.09.2026, Patrick's order: the bots travel by portals).</b> Eight moongates on
    /// Felucca, each leading to all the others: Britain, Moonglow, Jhelom, Yew, Minoc, Trinsic, Skara Brae and Magincia —
    /// four of them islands no road reaches ("Towns: 10 of 18 can be walked to from home"). Each moongate is read as seven
    /// one-way gates to the other seven, so the land-to-land routing that already walks the bots through cave mouths
    /// walks them through the moongates too: roads, travel, residences and trade with the islands follow.
    /// </summary>
    public static bool Moongates { get; set; } = true;

    /// <summary>Whether missing public moongates are raised from the engine's own list (as the MoonGen command does).</summary>
    public static bool RaiseMoongates { get; set; } = true;

    /// <summary>Moongates read, moongates raised, walkers put through one and refused by one.</summary>
    public static int MoongatesRead { get; private set; }

    public static int MoongatesRaised { get; private set; }

    public static long MoonTrips { get; private set; }

    public static long MoonRefused { get; private set; }

    /// <summary>Walkers standing on a teleporter's tile it had not fired for, put through by its own step; and those it still refused.</summary>
    public static long Nudged { get; private set; }

    public static long NudgeRefused { get; private set; }

    private static readonly Dictionary<Serial, long> _nudgedAt = [];

    /// <summary>
    /// <b>A teleporter fires for the step that lands at its height, not for a body standing on its tile (29.09.2026, build
    /// 322).</b> The engine calls <c>OnMoveOver</c> only for an item whose Z equals the mover's new Z, or whose height
    /// spans it (<c>Mobile.Move</c>), and a teleporter has no height. The one on Jhelom's south isle stands at Z 5 at
    /// (1406, 3996); the ground there lies lower, and on 322 Saelihn and Piran stood on its tile for minutes — "homeward is
    /// 163 tiles off, and a walk of 163 from about here closed nothing" — while Odile, Piran, Saelihn and Rafe each ended
    /// "no way through to (1401, 3833)", the seat of The Anchor. A walker whose route ends on a teleporter and stands on
    /// its tile is stepped through by the teleporter's own <c>OnMoveOver</c> — its checks (active, creatures, criminal,
    /// combat) and its delay are the engine's — once per few seconds.
    /// </summary>
    public static bool Nudge(Mobile body, Point3D tile)
    {
        var map = body?.Map;

        if (map == null || map == Map.Internal || body.X != tile.X || body.Y != tile.Y)
        {
            return false;
        }

        var now = Core.TickCount;

        if (_nudgedAt.TryGetValue(body.Serial, out var at) && now - at < NudgeEveryMs)
        {
            return true;
        }

        if (_nudgedAt.Count > 512)
        {
            _nudgedAt.Clear();
        }

        _nudgedAt[body.Serial] = now;

        Teleporter found = null;

        foreach (var gate in map.GetItemsInRange<Teleporter>(new Point3D(tile.X, tile.Y, body.Z), 0))
        {
            if (gate is { Deleted: false, Active: true } && gate.X == tile.X && gate.Y == tile.Y && gate.GetType() == typeof(Teleporter))
            {
                found = gate;

                break;
            }
        }

        if (found == null)
        {
            return false;
        }

        var before = body.Location;

        found.OnMoveOver(body);

        if (body.Location != before || found.Delay > TimeSpan.Zero)
        {
            Nudged++;

            return true;
        }

        NudgeRefused++;

        return false;
    }

    /// <summary>How often one walker is stepped through a teleporter it stands on. See <see cref="Nudge"/>.</summary>
    public static int NudgeEveryMs { get; set; } = 4000;

    /// <summary>Teleporters raised from the file because the world lacked them, and file entries the world already had.</summary>
    public static int Raised { get; private set; }

    public static int Present { get; private set; }

    /// <summary>Teleporters in the world passed over: not the plain kind, inactive, or leading to another map.</summary>
    public static int Odd { get; private set; }

    /// <summary>Teleporter items folded into them.</summary>
    public static int Tiles { get; private set; }

    /// <summary>Gates with an end the graph could not place: on a tile no walker can stand at, or off the graph.</summary>
    public static int Landless { get; private set; }

    /// <summary>Routes over gates asked for.</summary>
    public static long Asked { get; private set; }

    /// <summary>Of those, routes found.</summary>
    public static long Routed { get; private set; }

    /// <summary>Walkers that stood on a gate and were not put through it. See <c>BotJourney</c>.</summary>
    public static long Refused { get; private set; }

    /// <summary>Walkers put through a gate. See <c>BotJourney</c>.</summary>
    public static long Passed { get; private set; }

    public static IReadOnlyList<Gate> All => _gates;

    /// <summary>Reads the gates and, once the graph's components are counted, their lands. Cheap after the first call; asked by everything below.</summary>
    public static bool Ensure(Map map)
    {
        if (!Running || map == null || map == Map.Internal)
        {
            return false;
        }

        if (!Surveyed)
        {
            Survey(map);
        }

        if (Surveyed && !Landed && NavigationService.ComponentsCounted(map))
        {
            Land(map);
        }
        // <b>A land is a root of the graph's union-find, and a recount may name it by another root (29.09.2026, build 319).</b>
        // The gates' lands were asked once, at the first count; a cluster rebuilt later (a house raised, a cave cluster
        // redrawn) recounts every component, the live LandOf answered in the new names and the gates kept the old ones. Gimrund's
        // party of five marched ten minutes to the Orc Caves mouth and the leader went in alone: "the navigation graph says
        // there is no way from (1058, 1336) to (5332, 1365)" — the inner passage (5134, 1946) to (5332, 1379) led to a land
        // no live answer named any more, and every member's station in the cave was "no way from here". Asked again on a new count.
        else if (Landed && map == _map && NavigationService.ComponentsCounted(map) && NavigationService.ComponentEpoch(map) != _landEpoch)
        {
            Reland(map);
        }

        return Ready;
    }

    /// <summary>One pass over the world's items, once per world load.</summary>
    public static void Survey(Map map)
    {
        if (Surveyed || map == null || map == Map.Internal)
        {
            return;
        }

        Surveyed = true;
        _map = map;

        if (RaiseMissing)
        {
            Raise(map);
        }

        var read = 0;

        // The whole map as one spatial query, by the repository's rule (no walk over World.Items): one pass over the sectors.
        foreach (var gate in map.GetItemsInBounds<Teleporter>(new Rectangle2D(0, 0, map.Width, map.Height)))
        {
            if (gate is not { Deleted: false } || gate.Map != map)
            {
                continue;
            }

            // The plain kind only. A keyword, skill, confirmation or condition teleporter wants something of the body
            // that steps on it — a word said, a skill held, a gump answered — and a bot standing on one would wait for
            // ever for a door that does not open by walking.
            if (!gate.Active || gate.GetType() != typeof(Teleporter) || gate.MapDest != null && gate.MapDest != map || gate.PointDest == Point3D.Zero)
            {
                Odd++;

                continue;
            }

            read++;
            Add(gate.Location, gate.PointDest);
        }

        Tiles = read;

        if (Moongates)
        {
            ReadMoongates(map);
        }

        for (var i = 0; i < _gates.Count; i++)
        {
            Settle(_gates[i]);
        }

        logger.Information(
            "Gates: {Tiles} teleporters on {Map} read as {Gates} gates ({Odd} passed over as not the plain kind, inactive or off the map; {Raised} raised from Data/teleporters.json, {Present} of the file's already stood); their lands are asked of the navigation graph once its components are counted",
            read,
            map.Name,
            _gates.Count,
            Odd,
            Raised,
            Present
        );

        // Where each gate puts a walker down, against the floor there. See Alight.
        Gate aloft = null;
        sbyte under = 0;

        for (var i = 0; i < _gates.Count; i++)
        {
            if (!_gates[i].Moongate && Aloft(map, _gates[i].To, out var floor))
            {
                Floating++;

                if (aloft == null)
                {
                    aloft = _gates[i];
                    under = floor;
                }
            }
        }

        if (aloft != null)
        {
            logger.Information(
                "Gates: {Floating} of {Gates} put a walker down above the floor there — the first {Gate}, at height {Z} over a floor at {Floor}; a walker put down so is set on the floor as it lands",
                Floating,
                _gates.Count,
                aloft,
                aloft.To.Z,
                under
            );
        }
    }

    /// <summary>Gates whose far side is above the floor there. See <see cref="Alight"/>.</summary>
    public static int Floating { get; private set; }

    /// <summary>Walkers set on the floor after a gate put them down above it. See <see cref="Alight"/>.</summary>
    public static long Alighted { get; private set; }

    /// <summary>How many of those are said in the log, each session.</summary>
    public static int SayAlighted { get; set; } = 12;

    /// <summary>
    /// Whether a point stands above the floor of its tile — no standable surface within a unit of its height, and one within
    /// <c>BotStep.GroundReach</c> below it — and the floor it ought to mean.
    /// </summary>
    public static bool Aloft(Map map, Point3D at, out sbyte floor)
    {
        floor = 0;

        if (map == null || map == Map.Internal || BotStep.Ground(map, at.X, at.Y, at.Z, 1, out _))
        {
            return false;
        }

        return BotStep.Ground(map, at.X, at.Y, at.Z, BotStep.GroundReach, out floor) && floor < at.Z;
    }

    /// <summary>
    /// <b>A body standing where nothing can stand is put on the nearest floor (01.10.2026, build next).</b>
    ///
    /// <para>
    /// The walker plans from the body's tile and height, and a body can be somewhere no step would have taken it: on a ledge or
    /// a rock a spell or a shove put it on, or on a teleporter's landing the file sets above the floor. On build 384 Lorandil of
    /// Thrain's party stood at (5145, 1969, 5) on the Orc Caves' first level — a tile the reach map calls blocked, nowhere to
    /// stand — and from 10:21 every road it asked, its station, the close-up on Thrain, herbs in Britain, was "no way from
    /// here"; its company left it, and the population's rescue does not carry a bot out of a dungeon by rule. Asked when a road
    /// comes back sealed: a body above a floor is set on it (<see cref="Alight"/>); a body with no floor under it at all is set on
    /// the nearest floor within <see cref="UnwedgeReach"/> that the reach map does not call blocked or water. At most once in
    /// <see cref="UnwedgeMs"/> a body, so it never becomes a way of travelling.
    /// </para>
    /// </summary>
    public static bool Unwedge(Mobile body)
    {
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return false;
        }

        var now = Core.TickCount;

        if (_wedged.TryGetValue(body.Serial, out var last) && now - last < UnwedgeMs)
        {
            return false;
        }

        var at = body.Location;
        var z = (sbyte)Math.Clamp(at.Z, sbyte.MinValue, sbyte.MaxValue);

        // Standing on a floor: not wedged, whatever the road said.
        if (BotStep.Ground(map, at.X, at.Y, z, 1, out _) && Standable(map, at.X, at.Y, z))
        {
            return false;
        }

        _wedged[body.Serial] = now;

        // Above a floor somebody can stand on: set on it, as a gate's landing is. A floor the map calls blocked is no better than
        // the ledge, and the ring below is asked instead.
        if (Aloft(map, at, out var under) && Standable(map, at.X, at.Y, under) && Alight(body))
        {
            Unwedged++;

            return true;
        }

        for (var r = 1; r <= UnwedgeReach; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    var x = at.X + dx;
                    var y = at.Y + dy;

                    if (!BotStep.Ground(map, x, y, z, BotStep.GroundReach, out var floor) || !Standable(map, x, y, floor))
                    {
                        continue;
                    }

                    body.MoveToWorld(new Point3D(x, y, floor), map);
                    Unwedged++;

                    if (Unwedged <= SayAlighted)
                    {
                        logger.Information(
                            "Gates: {Name} stood at {From}, where nothing stands, and has been set on the floor at ({X}, {Y}, {Z})",
                            body.Name,
                            at,
                            x,
                            y,
                            floor
                        );
                    }

                    return true;
                }
            }
        }

        Unwedgeable++;

        return false;
    }

    /// <summary>Whether the reach map lets a body stand here: anything but blocked ground and water; anything at all before it is drawn.</summary>
    private static bool Standable(Map map, int x, int y, int z)
    {
        var k = BotReachMap.Class(map, x, y, z);

        return k is not (BotReachClass.Blocked or BotReachClass.Water);
    }

    /// <summary>How far a wedged body is looked round for a floor. Two tiles.</summary>
    public static int UnwedgeReach { get; set; } = 2;

    /// <summary>The least time between two unwedgings of one body. Ten seconds.</summary>
    public static int UnwedgeMs { get; set; } = 10000;

    /// <summary>Bodies set on a floor by <see cref="Unwedge"/>, and bodies it found no floor for.</summary>
    public static long Unwedged { get; private set; }

    public static long Unwedgeable { get; private set; }

    private static readonly Dictionary<Serial, long> _wedged = [];

    /// <summary>
    /// <b>A gate that puts a walker down above the floor is a walker no road can begin from (30.09.2026, build 338).</b>
    ///
    /// <para>
    /// The engine's teleporter sets a body down at the height written on it (<c>Teleporter.PointDest</c>), and a body is
    /// only brought to the floor by its own first step. The precise search plans from the body's height as it stands
    /// (<c>BotPath.Find</c>), while the look at the far side settles its start on a floor first (<c>BotPath.Enclose</c>,
    /// <c>Footing</c>), and on 336 the two disagreed at the landing of the passage from (2618, 977) to Buccaneer's Den:
    /// Falk led three from the muster at (2591, 1039) toward the Orc Caves at 00:15:32, stepped through, and every road from
    /// (2727, 2134, 5) — the delve's, and a forage twelve tiles off — was "no way from here" in the same second; five
    /// minutes later the stall watch found "the ground it was on is TooBig, its tile allows FF of the eight directions and
    /// the floor there is 0 against the 5 it thought it stood at", and carried him home. The file puts that landing at 5.
    /// </para>
    ///
    /// <para>
    /// A walker a gate has just put down is set on the floor under it, as its first step would set it, before anything is
    /// planned from there. Said the first few times with the place; counted after. Unverified on a live landing: that the
    /// search from the floor walks out where the search from 5 did not is the reading of the two floods' code, not a trial.
    /// </para>
    /// </summary>
    public static bool Alight(Mobile body)
    {
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || !Aloft(map, body.Location, out var floor))
        {
            return false;
        }

        var was = body.Location;

        body.MoveToWorld(new Point3D(was.X, was.Y, floor), map);
        Alighted++;

        if (Alighted <= SayAlighted)
        {
            logger.Information(
                "Gates: {Name} was put down at ({X}, {Y}) at height {Z}, over a floor at {Floor}, and has been set on it",
                body.Name,
                was.X,
                was.Y,
                was.Z,
                floor
            );
        }

        return true;
    }

    /// <summary>
    /// Every teleporter the file describes on this map, from and to this map, that the world lacks is put into it: a plain
    /// <see cref="Teleporter"/> on the file's tile to the file's destination, and its return where the file says so.
    /// Idempotent — an entry whose tile already carries a teleporter to that destination is counted as present.
    /// </summary>
    private static void Raise(Map map)
    {
        var path = Path.Combine(Core.BaseDirectory, "Data", "teleporters.json");

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));

            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (!entry.TryGetProperty("src", out var src) || !entry.TryGetProperty("dst", out var dst))
                {
                    continue;
                }

                if (!Read(src, out var srcMap, out var from) || !Read(dst, out var dstMap, out var to))
                {
                    continue;
                }

                if (!string.Equals(srcMap, map.Name, StringComparison.OrdinalIgnoreCase) || !string.Equals(dstMap, map.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Want(map, from, to);

                if (entry.TryGetProperty("back", out var back) && back.ValueKind == JsonValueKind.True)
                {
                    Want(map, to, from);
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Gates: Data/teleporters.json could not be read; the world's own teleporters are used as they stand");
        }
    }

    private static bool Read(JsonElement end, out string mapName, out Point3D at)
    {
        mapName = null;
        at = Point3D.Zero;

        if (!end.TryGetProperty("map", out var m) || !end.TryGetProperty("loc", out var loc) || loc.ValueKind != JsonValueKind.Array || loc.GetArrayLength() < 2)
        {
            return false;
        }

        mapName = m.GetString();

        var x = loc[0].GetInt32();
        var y = loc[1].GetInt32();
        var z = loc.GetArrayLength() > 2 ? loc[2].GetInt32() : 0;

        at = new Point3D(x, y, z);

        return true;
    }

    /// <summary>A teleporter from <paramref name="from"/> to <paramref name="to"/> stands, or is raised.</summary>
    private static void Want(Map map, Point3D from, Point3D to)
    {
        foreach (var item in map.GetItemsAt(from.X, from.Y))
        {
            if (item is Teleporter { Deleted: false } gate && Away(gate.PointDest, to) <= 1)
            {
                Present++;

                return;
            }
        }

        var made = new Teleporter(to, map);

        made.MoveToWorld(from, map);
        Raised++;
    }

    /// <summary>Folds one teleporter into a gate beside it, or starts a gate.</summary>
    private static void Add(Point3D tile, Point3D dest)
    {
        for (var i = 0; i < _gates.Count; i++)
        {
            var gate = _gates[i];

            if (Away(gate.To, dest) > Fold)
            {
                continue;
            }

            for (var j = 0; j < gate.Tiles.Count; j++)
            {
                if (Away(gate.Tiles[j], tile) <= Fold)
                {
                    gate.Tiles.Add(tile);

                    return;
                }
            }
        }

        var made = new Gate { To = dest };

        made.Tiles.Add(tile);
        _gates.Add(made);
    }

    /// <summary>The middle tile of the run is the one aimed at, and the gate is named after the dungeon it opens on.</summary>
    /// <summary>The town a Felucca moongate entry names, by the entry's cliloc number.</summary>
    private static string MoonTown(int number) =>
        number switch
        {
            1012003 => "Moonglow",
            1012004 => "Britain",
            1012005 => "Jhelom",
            1012006 => "Yew",
            1012007 => "Minoc",
            1012008 => "Trinsic",
            1012009 => "Skara Brae",
            1012010 => "Magincia",
            1019001 => "Buccaneer's Den",
            _ => "a far moongate"
        };

    /// <summary>
    /// Reads the public moongates standing on the map (raising the engine's list when none stand), and adds one gate from each
    /// to every entry of the map's list that is not itself. See <see cref="Moongates"/>.
    /// </summary>
    private static void ReadMoongates(Map map)
    {
        var list = map == Map.Felucca ? PMList.Felucca : map == Map.Trammel ? PMList.Trammel : null;

        if (list == null)
        {
            return;
        }

        List<Point3D> stands = [];

        foreach (var gate in map.GetItemsInBounds<PublicMoongate>(new Rectangle2D(0, 0, map.Width, map.Height)))
        {
            if (gate is { Deleted: false } && gate.Map == map)
            {
                stands.Add(gate.Location);
            }
        }

        // Every entry of the list gets its moongate: on 29.09.2026 one of Felucca's nine stood in the world, so the bots could
        // go to Moonglow and never come back. The engine's MoonGen raises the whole list; this raises only what is missing.
        if (RaiseMoongates)
        {
            foreach (var entry in list.Entries)
            {
                var standing = false;

                for (var s = 0; s < stands.Count; s++)
                {
                    if (Away(stands[s], entry.Location) <= 2)
                    {
                        standing = true;

                        break;
                    }
                }

                if (standing)
                {
                    continue;
                }

                var raised = new PublicMoongate();
                raised.MoveToWorld(entry.Location, map);
                stands.Add(entry.Location);
                MoongatesRaised++;
            }
        }

        MoongatesRead = stands.Count;

        for (var s = 0; s < stands.Count; s++)
        {
            var stand = stands[s];

            foreach (var entry in list.Entries)
            {
                if (Away(stand, entry.Location) <= 2)
                {
                    continue;
                }

                var made = new Gate { To = entry.Location, Moongate = true, Name = $"the moongate to {MoonTown(entry.Number)}" };
                made.Tiles.Add(stand);
                _gates.Add(made);
            }
        }

        logger.Information(
            "Moongates: {Stands} public moongates on {Map} ({Raised} raised from the engine's list), read as ways to {Entries} cities",
            stands.Count,
            map.Name,
            MoongatesRaised,
            list.Entries.Length
        );
    }

    /// <summary>The moongate gate that stands at this tile and leads to that one, or nothing.</summary>
    public static Gate Moon(Point3D from, Point3D to)
    {
        for (var i = 0; i < _gates.Count; i++)
        {
            var gate = _gates[i];

            if (gate.Moongate && gate.From.X == from.X && gate.From.Y == from.Y && Away(gate.To, to) <= 1)
            {
                return gate;
            }
        }

        return null;
    }

    /// <summary>
    /// The public moongate within <paramref name="reach"/> of <paramref name="at"/> whose way through shortens the walk to
    /// <paramref name="toward"/> the most — the walk to the gate and on from its far side, against the walk straight — or null
    /// (01.10.2026, build next). Two moongates on one land (Minoc's and Britain's) are not a gate into another land, so the
    /// route is drawn overland and a member following its leader through one walked the width of the island.
    /// </summary>
    public static Gate MoongateNear(Point3D at, Point3D toward, int reach)
    {
        Gate best = null;
        var bestWay = Away(at, toward);

        for (var i = 0; i < _gates.Count; i++)
        {
            var gate = _gates[i];

            if (!gate.Moongate || Away(at, gate.From) > reach)
            {
                continue;
            }

            var way = Away(at, gate.From) + Away(gate.To, toward);

            if (way < bestWay)
            {
                best = gate;
                bestWay = way;
            }
        }

        return best;
    }

    /// <summary>
    /// The public moongate's way from the tile <paramref name="at"/> whose far side lies nearest <paramref name="toward"/>, and
    /// nearer than <paramref name="at"/> itself; null when no moongate stands there (01.10.2026, build next). One pass over the
    /// gates; asked only of a company member left on the far side of a gate from its leader. See <c>BotEnlist.Holding</c>.
    /// </summary>
    public static Gate MoongateAt(Point3D at, Point3D toward)
    {
        Gate best = null;
        var bestAway = Away(at, toward);

        for (var i = 0; i < _gates.Count; i++)
        {
            var gate = _gates[i];

            if (!gate.Moongate || gate.From.X != at.X || gate.From.Y != at.Y)
            {
                continue;
            }

            var away = Away(gate.To, toward);

            if (away < bestAway)
            {
                best = gate;
                bestAway = away;
            }
        }

        return best;
    }

    /// <summary>
    /// Puts a bot standing on a public moongate through to the gate's city, as the engine's moongate gump does for a player
    /// who picks it: refused to a criminal, a body in a fight, or one casting.
    /// </summary>
    public static bool Travel(Mobile body, Gate gate)
    {
        var map = body?.Map;

        if (map == null || map == Map.Internal || gate is not { Moongate: true })
        {
            return false;
        }

        if (body.Criminal || Server.Spells.SpellHelper.CheckCombat(body) || body.Spell != null)
        {
            MoonRefused++;

            return false;
        }

        Server.Mobiles.BaseCreature.TeleportPets(body, gate.To, map);
        body.Combatant = null;
        body.Warmode = false;
        body.MoveToWorld(gate.To, map);
        Effects.PlaySound(gate.To, map, 0x1FE);
        gate.Used++;
        MoonTrips++;

        return true;
    }

    private static void Settle(Gate gate)
    {
        // A moongate's gate keeps the name of its city and its one tile.
        if (gate.Moongate)
        {
            gate.From = gate.Tiles[0];

            return;
        }

        gate.Tiles.Sort(static (a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        gate.From = gate.Tiles[gate.Tiles.Count / 2];

        var into = Deep(gate.To);
        var from = Deep(gate.From);

        gate.Name = into != null && into != from
            ? $"into {into.Name}"
            : from != null && into != from
                ? $"out of {from.Name}"
                : "a passage";
    }

    private static BotDungeon.Deep Deep(Point3D where)
    {
        var all = BotDungeon.All;

        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Holds(where))
            {
                return all[i];
            }
        }

        return null;
    }

    /// <summary>The gates' lands asked again after a recount; the routes and the tiles' lands kept under the old names are dropped.</summary>
    private static void Reland(Map map)
    {
        _landEpoch = NavigationService.ComponentEpoch(map);
        _lands.Clear();
        _routes.Clear();
        Relanded++;
        Landless = 0;

        var moved = 0;

        for (var i = 0; i < _gates.Count; i++)
        {
            var gate = _gates[i];
            var from = NavigationService.ComponentOf(map, gate.From);
            var to = NavigationService.ComponentOf(map, gate.To);

            if (from != gate.FromLand || to != gate.ToLand)
            {
                moved++;
            }

            gate.FromLand = from;
            gate.ToLand = to;

            if (from < 0 || to < 0)
            {
                Landless++;
            }
        }

        Moved += moved;

        if (moved > 0)
        {
            logger.Information(
                "Gates: the graph counted its lands afresh ({Count} times now) and {Moved} of {Gates} gates had their lands renamed; routes over the gates are drawn anew",
                Relanded,
                moved,
                _gates.Count
            );
        }
    }

    /// <summary>Asks the graph which component each end of each gate stands in.</summary>
    private static void Land(Map map)
    {
        Landed = true;
        Landless = 0;
        _landEpoch = NavigationService.ComponentEpoch(map);

        var lands = new HashSet<int>();

        for (var i = 0; i < _gates.Count; i++)
        {
            var gate = _gates[i];

            gate.FromLand = NavigationService.ComponentOf(map, gate.From);
            gate.ToLand = NavigationService.ComponentOf(map, gate.To);

            if (gate.FromLand < 0 || gate.ToLand < 0)
            {
                Landless++;

                continue;
            }

            lands.Add(gate.FromLand);
            lands.Add(gate.ToLand);
        }

        var home = BotPopulation.Where;
        var homeLand = NavigationService.ComponentOf(map, home);
        var reachable = 0;
        var deeps = 0;

        for (var i = 0; i < BotDungeon.All.Count; i++)
        {
            var deep = BotDungeon.All[i];

            if (First(homeLand, home, deep) is { } gate)
            {
                reachable++;
                deeps++;

                logger.Information("Gates: {Deep} is reached from home through {Gate}", deep.Name, gate);
            }
            else
            {
                deeps++;

                logger.Information("Gates: {Deep} has no way in from home over the gates", deep.Name);
            }
        }

        logger.Information(
            "Gates: {Gates} gates join {Lands} lands ({Landless} gates have an end the graph cannot place); from home (land {Home}) {Reachable} of {Deeps} dungeons can be walked into",
            _gates.Count,
            lands.Count,
            Landless,
            homeLand,
            reachable,
            deeps
        );
    }

    /// <summary>The land a tile stands in, or -1. Cached by tile and height for <see cref="LandCacheMs"/>.</summary>
    public static int LandOf(Map map, Point3D at)
    {
        if (!Ensure(map) || map != _map)
        {
            return -1;
        }

        // <b>By the tile, not by the cluster (29.09.2026, build 320).</b> Keyed by the sixteen-tile cluster, the first point asked
        // named the land for every point of the cluster at that height for a minute; a cluster holds a house, a fenced yard or
        // a hilltop as well as the road beside it, so a walker on the road was handed the pocket's land — no gate leads out
        // of a pocket — and the route through the mouth was "no way" (Gimrund and Liriel, three times each at (1056–1059,
        // 1334–1337) on 318, while the gates' own lands were whole). Kept to a bounded size; it is only a saving.
        var key = (at.X, at.Y, (at.Z + 128) >> 3);
        var now = Core.TickCount;

        if (_lands.Count >= LandCacheMost)
        {
            _lands.Clear();
        }

        if (_lands.TryGetValue(key, out var known) && now - known.Tick < LandCacheMs)
        {
            return known.Land;
        }

        var land = NavigationService.ComponentOf(map, at);

        // Not kept while the graph is between counts: a -1 held a minute read every walk across a gate as landless.
        if (land >= 0)
        {
            _lands[key] = (land, now);
        }

        return land;
    }

    /// <summary>
    /// Whether a walker at <paramref name="a"/> can get to <paramref name="b"/>: over the ground, or over the gates. Until the
    /// gates are ready the old answer stands — the dungeons are another map that happens to share a map.
    /// </summary>
    public static bool Joined(Map map, Point3D a, Point3D b)
    {
        if (!Ensure(map))
        {
            return BotDungeon.Under(a) == BotDungeon.Under(b);
        }

        var la = LandOf(map, a);
        var lb = LandOf(map, b);

        if (la < 0 || lb < 0)
        {
            return BotDungeon.Under(a) == BotDungeon.Under(b);
        }

        if (la == lb)
        {
            return true;
        }

        // A way between two lands the walkers lately found refused is not a way, until it lapses (work17). See WalkRefused.
        if (_refusedWays.Count > 0 && Shut(la, lb))
        {
            JoinsDenied++;

            return false;
        }

        return First(la, a, lb, b) != null;
    }

    // ---- A way the walkers find refused (06.10.2026, work17) ----------------------------------------------------------------
    //
    // <b>The gates said yes and the walker said no, and both were believed in turn.</b> On build 410, 125 of 639 bad endings were
    // bots standing in the Orc Caves, 70 of them "no way": the gates' map of lands joins the cave to the island (Argus 12:58,
    // `gates route 5335 1373 1603 1517`: two gates, out at (5332, 1381) and the passage at (5139, 2016)), the medium graph routes to
    // the gate (`nav 5329 1362 5332 1381`: 819 steps for 19 straight) and the precise walker refuses the gate's own tile ("road":
    // Sealed, the far side TooBig; `tile 5332 1381`: statics at z 0 and z 5 under a teleporter at z 10). So every appraisal
    // (BotAppraisal, BotHomer, BotDelve.Finish) read the cave as joined and offered walks that ended "there is no way from here",
    // 126 times for one leader in two seconds. A walk refused from inside the dungeon block to another land is filed against the
    // pair of lands; RefuseTimes of them within RefuseWindowMs shut the way for RefuseShutMs, and Joined and Next say no: a bot
    // in the cave is then brought up by hand (BotHomer, BotDelve.Finish) instead of being offered walks that do not exist.

    /// <summary>Refused walks from one land to another that shut the way between them. Two.</summary>
    public static int RefuseTimes { get; set; } = 2;

    /// <summary>How close together those refusals must fall. Five minutes.</summary>
    public static int RefuseWindowMs { get; set; } = 300000;

    /// <summary>How long a shut way stays shut. Ten minutes; it opens again by itself, and a walk through it opens it at once.</summary>
    public static int RefuseShutMs { get; set; } = 600000;

    /// <summary>Refused walks filed against a pair of lands, ways shut by them, and Joined answers turned to no while shut.</summary>
    public static long WaysRefused { get; private set; }

    public static long WaysShut { get; private set; }

    public static long JoinsDenied { get; private set; }

    private static readonly Dictionary<(int From, int To), (int Count, long First, long Until)> _refusedWays = [];

    /// <summary>
    /// A walk from <paramref name="from"/> to <paramref name="to"/> was refused by the walker ("there is no way from here"). Filed
    /// only when the walker stands in the dungeon block and the gates joined the two lands; see the note above.
    /// </summary>
    public static void WalkRefused(Map map, Point3D from, Point3D to)
    {
        if (!Running || RefuseTimes <= 0 || from.X < Edge || !Ensure(map) || map != _map)
        {
            return;
        }

        var la = LandOf(map, from);
        var lb = LandOf(map, to);

        if (la < 0 || lb < 0 || la == lb || Shut(la, lb) || First(la, from, lb, to) == null)
        {
            return;
        }

        var now = Core.TickCount;

        if (_refusedWays.Count >= 1024)
        {
            _refusedWays.Clear();
        }

        WaysRefused++;

        var key = (la, lb);
        var (count, first, _) = _refusedWays.TryGetValue(key, out var kept) && now - kept.First < RefuseWindowMs ? kept : (0, now, 0L);

        count++;

        if (count >= Math.Max(1, RefuseTimes))
        {
            WaysShut++;
            _refusedWays[key] = (count, first, now + RefuseShutMs);

            logger.Information(
                "The way from land {From} to land {To} was refused {Count} times in {Seconds}s (last from ({X}, {Y}, {Z}) to ({TX}, {TY}, {TZ})); the gates read it shut for {Minutes} minutes",
                la,
                lb,
                count,
                (now - first) / 1000,
                from.X,
                from.Y,
                from.Z,
                to.X,
                to.Y,
                to.Z,
                RefuseShutMs / 60000
            );

            return;
        }

        _refusedWays[key] = (count, first, 0L);
    }

    /// <summary>A walker was put through a gate out of this land after all: every way out of it filed as refused is open again.</summary>
    public static void WalkedThrough(Map map, Point3D gate)
    {
        if (_refusedWays.Count == 0 || map != _map)
        {
            return;
        }

        var la = LandOf(map, gate);

        if (la < 0)
        {
            return;
        }

        _openedWays.Clear();

        foreach (var key in _refusedWays.Keys)
        {
            if (key.From == la)
            {
                _openedWays.Add(key);
            }
        }

        for (var i = 0; i < _openedWays.Count; i++)
        {
            _refusedWays.Remove(_openedWays[i]);
            WaysOpened++;
        }
    }

    private static readonly List<(int From, int To)> _openedWays = [];

    /// <summary>Refused ways opened again by a walker put through a gate out of the land.</summary>
    public static long WaysOpened { get; private set; }

    private static bool Shut(int la, int lb) =>
        _refusedWays.TryGetValue((la, lb), out var kept) && kept.Until != 0 && Core.TickCount - kept.Until < 0;

    /// <summary>Ways shut now.</summary>
    private static int WaysShutNow()
    {
        var now = Core.TickCount;
        var shut = 0;

        foreach (var kept in _refusedWays.Values)
        {
            if (kept.Until != 0 && now - kept.Until < 0)
            {
                shut++;
            }
        }

        return shut;
    }

    /// <summary>
    /// The first gate on the way from <paramref name="from"/> to <paramref name="to"/>, when they lie in different lands and
    /// the gates join them; null when they share a land or nothing does.
    /// </summary>
    public static bool Next(Map map, Point3D from, Point3D to, out Gate gate)
    {
        gate = null;

        if (!Ensure(map))
        {
            return false;
        }

        var la = LandOf(map, from);
        var lb = LandOf(map, to);

        if (la < 0 || lb < 0 || la == lb)
        {
            return false;
        }

        // Not through a way the walkers lately found refused (work17). See WalkRefused.
        if (_refusedWays.Count > 0 && Shut(la, lb))
        {
            return false;
        }

        gate = First(la, from, lb, to);

        return gate != null;
    }

    /// <summary>The first gate on the way from a land to a dungeon's rooms, or null.</summary>
    public static Gate First(int fromLand, Point3D from, BotDungeon.Deep deep)
    {
        if (deep == null || fromLand < 0 || _map == null)
        {
            return null;
        }

        var room = deep.Rooms.Count > 0 ? deep.Rooms[0] : new Point3D(deep.Middle.X, deep.Middle.Y, 0);
        var land = LandOf(_map, room);

        if (land < 0)
        {
            return null;
        }

        return land == fromLand ? null : First(fromLand, from, land, room);
    }

    /// <summary>Where the island ends and the dungeon block begins: every dungeon lies east of this, with no ground joining the two.</summary>
    public static int Edge { get; set; } = 5120;

    /// <summary>
    /// The gate on the way from <paramref name="from"/> to <paramref name="to"/> that leaves the island for the dungeon block —
    /// a dungeon's mouth — or null when the way has none. A party is counted before it goes through it (<c>BotDelve</c>, 30.09.2026,
    /// build 338). Asked once a march; the chain is not kept.
    /// </summary>
    public static Gate Mouth(Map map, Point3D from, Point3D to)
    {
        if (!Ensure(map))
        {
            return null;
        }

        var la = LandOf(map, from);
        var lb = LandOf(map, to);

        if (la < 0 || lb < 0 || la == lb)
        {
            return null;
        }

        var chain = new List<Gate>();

        if (First(la, from, lb, to, chain) == null)
        {
            return null;
        }

        for (var i = 0; i < chain.Count; i++)
        {
            if (chain[i].From.X < Edge && chain[i].To.X >= Edge)
            {
                return chain[i];
            }
        }

        return null;
    }

    /// <summary>Whether a walker at <paramref name="from"/> can walk into this dungeon at all.</summary>
    public static bool Reaches(Map map, Point3D from, BotDungeon.Deep deep)
    {
        if (!Ensure(map) || deep == null)
        {
            return false;
        }

        var room = deep.Rooms.Count > 0 ? deep.Rooms[0] : new Point3D(deep.Middle.X, deep.Middle.Y, 0);

        return Joined(map, from, room);
    }

    /// <summary>
    /// Dijkstra over the gates, by straight distance: the first gate of the shortest way from one land to another. The
    /// answer for a pair of lands is kept a minute, because it is asked at every appraisal of work across a gate.
    /// </summary>
    private static Gate First(int fromLand, Point3D from, int toLand, Point3D to, List<Gate> chain = null)
    {
        var now = Core.TickCount;
        var key = (fromLand, toLand, from.X >> 6, from.Y >> 6, to.X >> 6, to.Y >> 6);

        if (chain == null && _routes.TryGetValue(key, out var kept) && now - kept.Tick < LandCacheMs)
        {
            return kept.First;
        }

        if (_routes.Count >= 4096)
        {
            _routes.Clear();
        }

        Asked++;

        var n = _gates.Count;
        Span<int> dist = n <= 1024 ? stackalloc int[n] : new int[n];
        Span<int> prev = n <= 1024 ? stackalloc int[n] : new int[n];
        Span<bool> done = n <= 1024 ? stackalloc bool[n] : new bool[n];

        for (var i = 0; i < n; i++)
        {
            var gate = _gates[i];

            dist[i] = gate.FromLand == fromLand && gate.ToLand >= 0 ? Away(from, gate.From) + Toll(gate, to) : int.MaxValue;
            prev[i] = -1;
            done[i] = false;
        }

        var best = int.MaxValue;
        var bestGate = -1;

        while (true)
        {
            var pick = -1;
            var least = int.MaxValue;

            for (var i = 0; i < n; i++)
            {
                if (!done[i] && dist[i] < least)
                {
                    least = dist[i];
                    pick = i;
                }
            }

            if (pick < 0 || least >= best)
            {
                break;
            }

            done[pick] = true;

            var gate = _gates[pick];

            if (gate.ToLand == toLand)
            {
                var total = least + Away(gate.To, to);

                if (total < best)
                {
                    best = total;
                    bestGate = pick;
                }

                continue;
            }

            for (var j = 0; j < n; j++)
            {
                var next = _gates[j];

                if (done[j] || next.FromLand != gate.ToLand || next.ToLand < 0)
                {
                    continue;
                }

                var through = least + Away(gate.To, next.From) + 1 + Toll(next, to);

                if (through < dist[j])
                {
                    dist[j] = through;
                    prev[j] = pick;
                }
            }
        }

        Gate first = null;

        if (bestGate >= 0)
        {
            var at = bestGate;

            while (prev[at] >= 0)
            {
                at = prev[at];
            }

            first = _gates[at];

            if (chain != null)
            {
                for (var step = bestGate; step >= 0; step = prev[step])
                {
                    chain.Insert(0, _gates[step]);
                }

                return first;
            }

            first.Used++;
            Routed++;
        }
        else if (chain != null)
        {
            return null;
        }

        _routes[key] = (first, now);

        return first;
    }

    /// <summary>A walker stood on a gate and stayed where it was. See <c>BotJourney.Leg</c>.</summary>
    public static void NotedRefused() => Refused++;

    /// <summary>A walker was put through a gate. See <c>BotJourney.Leg</c>.</summary>
    public static void NotedPassed() => Passed++;

    private static int Away(Point3D a, Point3D b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    public static string Describe() =>
        !Running
            ? "gates are off: every land is an island"
            : !Surveyed
                ? "the gates have not been read"
                : !Landed
                    ? $"{_gates.Count} gates read from {Tiles} teleporters, waiting for the graph's components"
                    : $"{_gates.Count} gates from {Tiles} teleporters and {MoongatesRead} moongates ({MoongatesRaised} raised; {MoonTrips} walkers put through a moongate, {MoonRefused} refused by one; {Nudged} stepped through a teleporter that had not fired for them, {NudgeRefused} refused by it; {Floating} put a walker down above the floor, {Alighted} walkers set on it) ({Landless} with an end the graph cannot place; lands asked again {Relanded} times, {Moved} gates renamed); {Asked} land-to-land routes asked, {Routed} found; {Passed} walkers put through a gate, {Refused} stood on one and stayed; {WaysRefused} walks out of the dungeon block refused across a gate the lands joined, {WaysShut} ways shut by {Math.Max(1, RefuseTimes)} of them in {RefuseWindowMs / 60000} minutes for {RefuseShutMs / 60000} ({WaysShutNow()} shut now, {WaysOpened} opened again by a walker put through), {JoinsDenied} answers of joined turned to no while shut";

    /// <summary>
    /// The gates standing within <paramref name="radius"/> of a point, each with its kept lands and the graph's live answer for
    /// both ends — Argus's <c>gates</c>, for a party that stops at a mouth.
    /// </summary>
    public static string Near(Map map, Point3D at, int radius)
    {
        if (!Ensure(map))
        {
            return Describe();
        }

        var lines = new List<string>();

        for (var i = 0; i < _gates.Count && lines.Count < 20; i++)
        {
            var gate = _gates[i];

            if (Away(gate.From, at) > radius && Away(gate.To, at) > radius)
            {
                continue;
            }

            lines.Add(
                $"{gate} (lands {gate.FromLand} to {gate.ToLand} kept, {NavigationService.ComponentOf(map, gate.From)} to {NavigationService.ComponentOf(map, gate.To)} live; used {gate.Used})"
            );
        }

        return lines.Count == 0
            ? $"no gate within {radius} tiles of ({at.X}, {at.Y}); the point stands in land {LandOf(map, at)}"
            : $"the point ({at.X}, {at.Y}) stands in land {LandOf(map, at)}; {lines.Count} gates near: {string.Join("; ", lines)}";
    }

    /// <summary>A world reload is a different world, and the gates are items in it.</summary>
    public static void Forget()
    {
        _gates.Clear();
        _lands.Clear();
        _routes.Clear();
        _map = null;
        Surveyed = false;
        Landed = false;
        _landEpoch = 0;
        Tiles = 0;
        Landless = 0;
        Asked = 0;
        Routed = 0;
        Refused = 0;
        Passed = 0;
        Floating = 0;
        Alighted = 0;
        Unwedged = 0;
        Unwedgeable = 0;
        _wedged.Clear();
        _refusedWays.Clear();
        WaysRefused = 0;
        WaysShut = 0;
        WaysOpened = 0;
        JoinsDenied = 0;
    }
}
