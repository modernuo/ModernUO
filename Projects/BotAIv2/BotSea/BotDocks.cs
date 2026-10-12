using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// One place a ship is put in the water at a dock: the shore a bot boards from, the berth, and the harbour from it to the
/// open sea. A dock's first berth is the dock's own; the others are where a ship goes in when the first is lying full.
/// </summary>
public sealed class BotBerth
{
    /// <summary>Ground a bot stands on beside the water, joined to the town's square: it boards from here.</summary>
    public Point3D Shore;

    /// <summary>
    /// Where the ship's middle is put down, at sea level, with a plank within <see cref="BotDocks.Gangway"/> of the shore. The
    /// north-facing hull a deed puts down fits here; so does the hull lying as <see cref="East"/> says.
    /// </summary>
    public Point3D Berth;

    /// <summary>Whether the ship lies east–west at the berth and through the harbour; north–south otherwise.</summary>
    public bool East;

    /// <summary>The harbour: the berth first, every turn, and last a tile in an open cell of the open sea.</summary>
    public List<Point3D> Harbour;

    /// <summary>The open cell the harbour leads to.</summary>
    public int Entry;

    public int HarbourTiles;

    /// <summary>
    /// For a berth other than the dock's first: the open cells from its <see cref="Entry"/> to the first berth's, where the
    /// dock's lanes begin. Empty for the first; null until drawn.
    /// </summary>
    public List<int> Link;

    public BotSeaCourse.Ask Linking;

    public long Departures;

    public override string ToString() => $"({Berth.X}, {Berth.Y}) off ({Shore.X}, {Shore.Y})";
}

/// <summary>
/// One town's dock: its berths — where a bot stands to board, where its ship is put down, and the harbour between that
/// berth and the open sea — and where a ship arriving there puts in. The fields below are the first berth's.
/// </summary>
public sealed class BotDock
{
    public BotTowns.Town Town;

    public string Name;

    /// <summary>Ground a bot stands on beside the water, joined to the town's square: it boards from here and lands here.</summary>
    public Point3D Shore;

    /// <summary>Where the ship's middle is put down, at sea level, with a plank within <see cref="BotDocks.Gangway"/> of the shore.</summary>
    public Point3D Berth;

    /// <summary>Whether the ship lies east–west at the berth and through the harbour; north–south otherwise.</summary>
    public bool East;

    /// <summary>The harbour: the berth first, every turn, and last a tile in an open cell of the open sea.</summary>
    public List<Point3D> Harbour;

    /// <summary>The open cell the harbour leads to, where the sea's course begins.</summary>
    public int Entry;

    public int HarbourTiles;

    /// <summary>Every berth, the first — the one the fields above describe — first. See <see cref="BotDocks.MostBerths"/>.</summary>
    public readonly List<BotBerth> Berths = [];

    public BotBerth First => Berths.Count > 0 ? Berths[0] : null;

    public long Departures;

    public long Arrivals;

    public override string ToString() => $"{Name} ({Shore.X}, {Shore.Y})";
}

/// <summary>
/// Where each town meets the sea, found from the map rather than written down.
///
/// <para>
/// <b>A dock is a shore, a berth and a harbour.</b> From the town's square outward, ring by ring, the first ground a bot can
/// stand on beside the water and walk to from the square (<see cref="BotGates.Joined"/>) is a shore; round it, the berth is
/// where the small ship fits with a plank within <see cref="Gangway"/> tiles of the shore — the engine's own plank puts a
/// walker down up to six tiles from itself (<c>Plank.OnMoveOver</c>) and takes one aboard from eight (<c>Plank.OnDoubleClick</c>);
/// and the harbour is a breadth-first search over the tiles the ship's hull fits on, facing as it lies at the berth, out to
/// the first open cell of the open sea. A shore whose harbour is a pond is passed over, and so is every shore within
/// <see cref="Spread"/> of it.
/// </para>
///
/// <para>
/// <b>Measured with the same rules on map0 (29.09.2026, Python):</b> thirteen of the fourteen charted towns have a dock, the
/// harbours 4 to 39 tiles; Minoc's shore is a bay whose way out is narrower than the small ship within 128 tiles, and Minoc
/// is walked to anyway. Wind, the Heartwood, Delucia and Papua lie beyond the charted sea (x 5120 and east). Where the map
/// is not what the search should pick, <c>Configuration/bot-sea.json</c> may pin where a town's shore is looked for from
/// (<see cref="Pins"/>).
/// </para>
/// </summary>
public static class BotDocks
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDocks));

    /// <summary>How far from a town's square its shore is looked for. Britain's lies 78 tiles out, Trinsic's 76.</summary>
    public static int Reach { get; set; } = 200;

    /// <summary>How far a plank may be from the shore. Six: the engine's plank puts a walker down no further from itself.</summary>
    public static int Gangway { get; set; } = 6;

    /// <summary>How far from the berth the harbour is searched for the open sea, each way. Vesper's is 39 tiles, Britain's 38.</summary>
    public static int HarbourReach { get; set; } = 128;

    /// <summary>How many shores one town may try before it is said to have no dock.</summary>
    public static int Tries { get; set; } = 24;

    /// <summary>A shore within this many tiles of one already tried is not tried again: it is the same pond.</summary>
    public static int Spread { get; set; } = 16;

    /// <summary>Where to look for a town's shore from, by town, instead of its square. From the configuration.</summary>
    public static Dictionary<string, Point2D> Pins { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Berths one dock keeps: the first, and others along the shore for when a ship is lying in it. Three. A ship lying at
    /// anchor in a berth — its captain dead aboard, or its dry-docking refused — stays for the engine's nine days of decay.
    /// </summary>
    public static int MostBerths { get; set; } = 3;

    /// <summary>How far along the shore from the first berth's another may be. Forty-eight tiles: a walk of ten seconds.</summary>
    public static int BerthReach { get; set; } = 48;

    private static readonly List<BotDock> _docks = [];

    private static readonly List<Point3D> _shores = [];

    private static readonly List<Point3D> _failed = [];

    private static int _town;

    private static int _try;

    private static bool _scanned;

    private static BotDock _dock;

    private static bool _linked;

    public static bool Ready { get; private set; }

    /// <summary>Berths passed over because the live world had something on them the map files do not: an item, a building.</summary>
    public static int Occupied { get; private set; }

    /// <summary>Berths besides the docks' first, and those dropped for want of a way from them to their dock's first.</summary>
    public static int Spare { get; private set; }

    public static int Unlinked { get; private set; }

    public static IReadOnlyList<BotDock> All => _docks;

    /// <summary>Towns beyond the charted sea, towns with no shore, and towns whose every shore was a pond.</summary>
    public static int Beyond { get; private set; }

    public static int Shoreless { get; private set; }

    public static int Landlocked { get; private set; }

    /// <summary>
    /// One piece of the survey: a town's shores scanned, or one of its shores tried. True once every town has been
    /// asked. Needs the chart, the towns and the gates.
    /// </summary>
    public static bool Step(Map map)
    {
        if (Ready)
        {
            return true;
        }

        if (!BotSeaChart.Ready || !BotTowns.Ensure(map) || !BotGates.Ensure(map))
        {
            return false;
        }

        var towns = BotTowns.All;

        if (_town >= towns.Count)
        {
            Ready = true;

            logger.Information(
                "Sea: {Docks} of {Towns} towns have a dock ({Beyond} beyond the charted sea, {Shoreless} with no shore within {Reach} tiles, {Landlocked} whose every shore was a pond): {Names}",
                _docks.Count,
                towns.Count,
                Beyond,
                Shoreless,
                Reach,
                Landlocked,
                Names()
            );

            return true;
        }

        var town = towns[_town];

        if (!_scanned)
        {
            Scan(map, town);
            _scanned = true;
            _try = 0;

            return false;
        }

        if (_try < _shores.Count && _try < Tries && (_dock == null || _dock.Berths.Count < MostBerths))
        {
            var shore = _shores[_try++];

            // The other berths are along the shore from the first, not across the town.
            if (_dock != null && Away(shore.X, shore.Y, _dock.Shore) > BerthReach)
            {
                return false;
            }

            var berth = Try(map, shore);

            if (berth == null)
            {
                return false;
            }

            if (_dock == null)
            {
                _dock = new BotDock
                {
                    Town = town,
                    Name = town.Name,
                    Shore = berth.Shore,
                    Berth = berth.Berth,
                    East = berth.East,
                    Harbour = berth.Harbour,
                    HarbourTiles = berth.HarbourTiles,
                    Entry = berth.Entry
                };

                berth.Link = [];
                _dock.Berths.Add(berth);
                _docks.Add(_dock);

                logger.Information(
                    "Sea: {Town} has a dock: ashore at ({X}, {Y}, {Z}), the ship put down at ({BX}, {BY}) lying {Lie}, a harbour of {Tiles} tiles in {Legs} legs to the open sea",
                    town.Name,
                    berth.Shore.X,
                    berth.Shore.Y,
                    berth.Shore.Z,
                    berth.Berth.X,
                    berth.Berth.Y,
                    berth.East ? "east–west" : "north–south",
                    berth.HarbourTiles,
                    berth.Harbour.Count - 1
                );

                return false;
            }

            // Its harbour may reach the open sea in another cell than the first berth's: the way between is drawn with the
            // lanes, and a berth with none is dropped (Link).
            if (berth.Entry == _dock.Entry)
            {
                berth.Link = [];
            }
            else
            {
                berth.Linking = new BotSeaCourse.Ask { From = berth.Entry, To = _dock.Entry, For = $"{town.Name}'s berth {_dock.Berths.Count + 1}" };
                BotSeaCourse.Enqueue(berth.Linking);
            }

            _dock.Berths.Add(berth);
            Spare++;

            logger.Information(
                "Sea: {Town} has another berth: ashore at ({X}, {Y}, {Z}), {Away} tiles along the shore from the first, the ship put down at ({BX}, {BY}) lying {Lie}, a harbour of {Tiles} tiles",
                town.Name,
                berth.Shore.X,
                berth.Shore.Y,
                berth.Shore.Z,
                Away(berth.Shore.X, berth.Shore.Y, _dock.Shore),
                berth.Berth.X,
                berth.Berth.Y,
                berth.East ? "east–west" : "north–south",
                berth.HarbourTiles
            );

            return false;
        }

        if (_dock == null && _shores.Count > 0)
        {
            Landlocked++;

            logger.Information("Sea: {Town} has no dock: {Shores} shores tried and every one a pond or too shallow for a ship", town.Name, Math.Min(_shores.Count, Tries));
        }

        Next();

        return false;
    }

    private static void Next()
    {
        _town++;
        _scanned = false;
        _dock = null;
        _shores.Clear();
        _failed.Clear();
    }

    /// <summary>
    /// Waits for the ways from every other berth to its dock's first, drops a berth that has none, and says what each dock's
    /// berths hold right now. True once done; asked by the lanes before they are drawn.
    /// </summary>
    public static bool Link(Map map)
    {
        if (_linked)
        {
            return true;
        }

        if (!Ready)
        {
            return false;
        }

        for (var i = 0; i < _docks.Count; i++)
        {
            var berths = _docks[i].Berths;

            for (var j = 1; j < berths.Count; j++)
            {
                if (berths[j].Linking is { Done: false })
                {
                    return false;
                }
            }
        }

        _linked = true;

        for (var i = 0; i < _docks.Count; i++)
        {
            var dock = _docks[i];
            var berths = dock.Berths;

            for (var j = berths.Count - 1; j >= 1; j--)
            {
                var ask = berths[j].Linking;

                if (ask == null)
                {
                    continue;
                }

                berths[j].Linking = null;

                if (ask.Found)
                {
                    berths[j].Link = ask.Turns;

                    continue;
                }

                Unlinked++;
                Spare--;
                berths.RemoveAt(j);

                logger.Information("Sea: {Town}'s berth {Berth} is dropped: no way by sea from its harbour to the first berth's", dock.Name, j + 1);
            }

            // What the live world has on each berth now; a ship lying there goes away, anything else is a berth drawn wrong.
            for (var j = 0; j < berths.Count; j++)
            {
                var berth = berths[j];
                var occupant = BotSeaChart.Occupant(map, berth.Berth, false, out _, out _)
                               ?? (berth.East ? BotSeaChart.Occupant(map, berth.Berth, true, out _, out _) : null);

                if (occupant != null)
                {
                    logger.Warning("Sea: {Town}'s berth {Berth} at ({X}, {Y}) cannot be put into now: {Occupant}", dock.Name, j + 1, berth.Berth.X, berth.Berth.Y, occupant);
                }
            }
        }

        logger.Information(
            "Sea: {Berths} berths at {Docks} docks, {Spare} besides the first ({Unlinked} dropped for want of a way to the first's harbour, {Occupied} passed over for something in the world on them)",
            _docks.Count + Spare,
            _docks.Count,
            Spare,
            Unlinked,
            Occupied
        );

        return true;
    }

    private static string Names()
    {
        using var names = Server.Text.ValueStringBuilder.Create(256);

        for (var i = 0; i < _docks.Count; i++)
        {
            names.Append(i > 0 ? ", " : "");
            names.Append(_docks[i].Name);
        }

        return names.ToString();
    }

    /// <summary>Ground beside the water that the town's square can be walked to from, nearest the square first.</summary>
    private static void Scan(Map map, BotTowns.Town town)
    {
        _shores.Clear();

        var origin = Pins.TryGetValue(town.Name, out var pin) ? new Point3D(pin.X, pin.Y, town.Square.Z) : town.Square;
        var extent = BotSeaChart.Width * 8 - 32;

        if (origin.X < 32 || origin.Y < 32 || origin.X >= extent || origin.Y >= BotSeaChart.Height * 8 - 32)
        {
            Beyond++;

            logger.Information("Sea: {Town} lies beyond the charted sea at ({X}, {Y}) and has no dock", town.Name, origin.X, origin.Y);

            return;
        }

        for (var r = 0; r <= Reach && _shores.Count < Tries; r++)
        {
            for (var dx = -r; dx <= r && _shores.Count < Tries; dx++)
            {
                var edge = Math.Abs(dx) == r;

                for (var dy = -r; dy <= r; dy += edge ? 1 : Math.Max(1, 2 * r))
                {
                    var x = origin.X + dx;
                    var y = origin.Y + dy;

                    if (!Coastal(x, y) || Near(_shores, x, y))
                    {
                        continue;
                    }

                    if (!BotStep.Settle(map, x, y, out var z) || !map.CanSpawnMobile(x, y, z))
                    {
                        continue;
                    }

                    var spot = new Point3D(x, y, z);

                    if (!BotGates.Joined(map, spot, town.Square))
                    {
                        continue;
                    }

                    _shores.Add(spot);
                }
            }
        }

        if (_shores.Count == 0)
        {
            Shoreless++;

            logger.Information("Sea: {Town} has no shore within {Reach} tiles of ({X}, {Y})", town.Name, Reach, origin.X, origin.Y);
        }
    }

    /// <summary>Dry, with water a ship floats on beside it.</summary>
    private static bool Coastal(int x, int y) =>
        !BotSeaChart.Wet(x, y) && (BotSeaChart.Wet(x + 1, y) || BotSeaChart.Wet(x - 1, y) || BotSeaChart.Wet(x, y + 1) || BotSeaChart.Wet(x, y - 1));

    private static bool Near(List<Point3D> spots, int x, int y)
    {
        for (var i = 0; i < spots.Count; i++)
        {
            if (Math.Abs(spots[i].X - x) <= Spread && Math.Abs(spots[i].Y - y) <= Spread)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How many of a shore's candidate berths, best first, are asked of the live world before the shore is given up.</summary>
    public static int Candidates { get; set; } = 8;

    private static readonly List<(int Score, int X, int Y, bool East)> _candidates = [];

    /// <summary>
    /// A berth beside this shore and a harbour from it to the open sea, or null.
    ///
    /// <para>
    /// <b>The berth takes the hull a deed puts down, whatever way the ship is to lie after.</b> A deed and a dry-docked ship
    /// both put down a new ship, and a new ship faces north (<c>BaseBoatDeed.OnPlacement</c> asks <c>CanFit</c> of the new
    /// ship's own item id); an east–west berth is turned to after. Build 321 chose Vesper's berth, (3010, 748), for the
    /// east–west hull alone, and the north-facing one there covers (3010, 752) and (3010, 753) — the shore. Every voyage from
    /// Vesper and from Buccaneer's Den (2743, 2160) was refused at the water; measured over map0 with the multi's own 36
    /// tiles, the other three east–west berths (Trinsic, Nujel'm, Magincia) took the north hull by chance.
    /// </para>
    ///
    /// <para>
    /// <b>And what the map files cannot show is asked of the world.</b> Decoration is items, not statics, and a ship's hull
    /// is a multi: the best few candidates are asked of <see cref="BotSeaChart.Occupant"/> in order, and one with something
    /// on it that is not a ship — ships move — is passed over (<see cref="Occupied"/>).
    /// </para>
    /// </summary>
    private static BotBerth Try(Map map, Point3D shore)
    {
        if (Near(_failed, shore.X, shore.Y))
        {
            return null;
        }

        var half = BotSeaChart.Half;

        // A small window for the berth first, so that a shore with no water deep enough beside it costs a few hundred
        // blocks' reading and not the harbour's thousand.
        var near = new BotSeaWindow(map, shore.X, shore.Y, Gangway + half * 2 + 2);
        var reach = Gangway + half;

        _candidates.Clear();

        for (var dx = -reach; dx <= reach; dx++)
        {
            for (var dy = -reach; dy <= reach; dy++)
            {
                var px = shore.X + dx;
                var py = shore.Y + dy;

                if (!near.Places(px, py))
                {
                    continue;
                }

                for (var lie = 0; lie < 2; lie++)
                {
                    var isEast = lie == 1;

                    if (!near.Fits(px, py, isEast))
                    {
                        continue;
                    }

                    // SmallBoat's planks sit two tiles either side of the middle, across the hull.
                    var plank = Math.Min(
                        Away(isEast ? px : px - 2, isEast ? py - 2 : py, shore),
                        Away(isEast ? px : px + 2, isEast ? py + 2 : py, shore)
                    );

                    if (plank > Gangway)
                    {
                        continue;
                    }

                    _candidates.Add((plank * 64 + Away(px, py, shore), px, py, isEast));
                }
            }
        }

        _candidates.Sort((a, b) => a.Score.CompareTo(b.Score));

        for (var i = 0; i < _candidates.Count && i < Candidates; i++)
        {
            var (_, x, y, east) = _candidates[i];
            var berth = new Point3D(x, y, BotSeaChart.SeaLevel);

            var occupant = BotSeaChart.Occupant(map, berth, false, out var block, out _);

            if (occupant == null && east)
            {
                occupant = BotSeaChart.Occupant(map, berth, true, out block, out _);
            }

            if (occupant != null && block != BotBerthBlock.Ship)
            {
                Occupied++;

                logger.Information("Sea: a berth at ({X}, {Y}) off ({SX}, {SY}) is passed over: {Occupant}", x, y, shore.X, shore.Y, occupant);

                continue;
            }

            var window = new BotSeaWindow(map, x, y, HarbourReach + half + 2);
            var harbour = window.Harbour(x, y, east, out var tiles, out var entry);

            if (harbour == null)
            {
                break;
            }

            return new BotBerth
            {
                Shore = shore,
                Berth = berth,
                East = east,
                Harbour = harbour,
                HarbourTiles = tiles,
                Entry = entry
            };
        }

        _failed.Add(shore);

        return null;
    }

    private static int Away(int x, int y, Point3D p) => Math.Max(Math.Abs(x - p.X), Math.Abs(y - p.Y));

    /// <summary>The town's dock, or null.</summary>
    public static BotDock Of(BotTowns.Town town)
    {
        for (var i = 0; i < _docks.Count; i++)
        {
            if (_docks[i].Town == town)
            {
                return _docks[i];
            }
        }

        return null;
    }

    public static BotDock Find(string name)
    {
        for (var i = 0; i < _docks.Count; i++)
        {
            if (string.Equals(_docks[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return _docks[i];
            }
        }

        return null;
    }

    /// <summary>The dock nearest a place as the crow flies, of any land.</summary>
    public static BotDock Nearest(Point3D where)
    {
        BotDock best = null;
        var nearest = int.MaxValue;

        for (var i = 0; i < _docks.Count; i++)
        {
            var away = Away(where.X, where.Y, _docks[i].Shore);

            if (away < nearest)
            {
                nearest = away;
                best = _docks[i];
            }
        }

        return best;
    }

    public static string Describe()
    {
        if (!Ready)
        {
            return $"docks being found: {_docks.Count} so far";
        }

        using var line = Server.Text.ValueStringBuilder.Create(256);

        line.Append($"{_docks.Count} docks:");

        for (var i = 0; i < _docks.Count; i++)
        {
            var dock = _docks[i];

            line.Append($" {dock.Name} ({dock.Berths.Count} berths, {dock.Departures} out, {dock.Arrivals} in)");
        }

        return line.ToString();
    }

    public static void Forget()
    {
        _docks.Clear();
        _shores.Clear();
        _failed.Clear();
        _candidates.Clear();
        _town = 0;
        _try = 0;
        _scanned = false;
        _dock = null;
        _linked = false;
        Ready = false;
        Beyond = 0;
        Shoreless = 0;
        Landlocked = 0;
        Occupied = 0;
        Spare = 0;
        Unlinked = 0;
    }
}

/// <summary>
/// A square of the map read exactly — statics everywhere, the canals and moats laid over ground included — for the harbour:
/// where the small ship fits, and the way from a berth to the open sea.
/// </summary>
internal sealed class BotSeaWindow
{
    private readonly int _x0;

    private readonly int _y0;

    private readonly int _size;

    /// <summary>Dry tiles counted over every rectangle from the corner, so a hull is asked in four reads.</summary>
    private readonly int[] _dry;

    public BotSeaWindow(Map map, int x, int y, int radius)
    {
        _x0 = x - radius;
        _y0 = y - radius;
        _size = radius * 2 + 1;

        var stride = _size + 1;

        _dry = new int[stride * stride];

        var bx0 = Math.Max(0, _x0 >> 3);
        var by0 = Math.Max(0, _y0 >> 3);
        var bx1 = Math.Min(BotSeaChart.Width - 1, (_x0 + _size - 1) >> 3);
        var by1 = Math.Min(BotSeaChart.Height - 1, (_y0 + _size - 1) >> 3);
        var bw = bx1 - bx0 + 1;
        var bh = by1 - by0 + 1;
        var masks = new ulong[Math.Max(0, bw) * Math.Max(0, bh)];

        for (var bx = bx0; bx <= bx1; bx++)
        {
            for (var by = by0; by <= by1; by++)
            {
                masks[(bx - bx0) * bh + by - by0] = BotSeaChart.Mask(map, bx, by, true);
            }
        }

        for (var j = 0; j < _size; j++)
        {
            var row = 0;
            var ty = _y0 + j;

            for (var i = 0; i < _size; i++)
            {
                var tx = _x0 + i;
                var bx = tx >> 3;
                var by = ty >> 3;
                var wet = tx >= 0 && ty >= 0 && bx >= bx0 && bx <= bx1 && by >= by0 && by <= by1
                          && (masks[(bx - bx0) * bh + by - by0] >> (((ty & 7) << 3) | (tx & 7)) & 1) != 0;

                row += wet ? 0 : 1;
                _dry[(j + 1) * stride + i + 1] = _dry[j * stride + i + 1] + row;
            }
        }
    }

    private int Dry(int x1, int y1, int x2, int y2)
    {
        var i1 = x1 - _x0;
        var j1 = y1 - _y0;
        var i2 = x2 - _x0;
        var j2 = y2 - _y0;

        if (i1 < 0 || j1 < 0 || i2 >= _size || j2 >= _size)
        {
            return 1;
        }

        var stride = _size + 1;

        return _dry[(j2 + 1) * stride + i2 + 1] - _dry[j1 * stride + i2 + 1] - _dry[(j2 + 1) * stride + i1] + _dry[j1 * stride + i1];
    }

    /// <summary>Whether the small ship's hull, its whole rectangle, floats with its middle here.</summary>
    public bool Fits(int x, int y, bool east)
    {
        var hull = east ? BotSeaChart.HullEast : BotSeaChart.HullNorth;

        return Dry(x + hull.X, y + hull.Y, x + hull.X + hull.Width - 1, y + hull.Y + hull.Height - 1) == 0;
    }

    /// <summary>
    /// Whether a deed could put the ship down with its middle here: the north-facing hull's own tiles float (the rectangle
    /// when the multi could not be read). See <see cref="BotSeaChart.TilesNorth"/>.
    /// </summary>
    public bool Places(int x, int y)
    {
        var tiles = BotSeaChart.TilesNorth;

        if (tiles.Length == 0)
        {
            return Fits(x, y, false);
        }

        for (var i = 0; i < tiles.Length; i++)
        {
            var tx = x + tiles[i].X;
            var ty = y + tiles[i].Y;

            if (Dry(tx, ty, tx, ty) != 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The way from a berth to the open sea, breadth first over the tiles the hull fits on, lying as it lies: the berth, every
    /// turn, and the first tile in an open cell of the open sea. Null when there is none inside the window.
    /// </summary>
    public List<Point3D> Harbour(int x, int y, bool east, out int tiles, out int entry)
    {
        tiles = 0;
        entry = -1;

        var count = _size * _size;
        var came = new int[count];
        var queue = new int[count];

        Array.Fill(came, -2);

        var start = (y - _y0) * _size + x - _x0;

        if (start < 0 || start >= count)
        {
            return null;
        }

        var head = 0;
        var tail = 0;
        var goal = -1;

        came[start] = -1;
        queue[tail++] = start;

        while (head < tail)
        {
            var at = queue[head++];
            var ax = at % _size + _x0;
            var ay = at / _size + _y0;

            if (BotSeaChart.AtSea(BotSeaChart.CellOf(ax, ay)))
            {
                goal = at;

                break;
            }

            for (var d = 0; d < 8; d++)
            {
                var nx = ax + BotSeaChart.DX(d);
                var ny = ay + BotSeaChart.DY(d);
                var i = nx - _x0;
                var j = ny - _y0;

                if (i < 0 || j < 0 || i >= _size || j >= _size)
                {
                    continue;
                }

                var next = j * _size + i;

                if (came[next] != -2 || !Fits(nx, ny, east))
                {
                    continue;
                }

                came[next] = at;
                queue[tail++] = next;
            }
        }

        if (goal < 0)
        {
            return null;
        }

        List<Point3D> path = [];

        for (var at = goal; at >= 0; at = came[at])
        {
            path.Add(new Point3D(at % _size + _x0, at / _size + _y0, BotSeaChart.SeaLevel));
        }

        path.Reverse();

        tiles = path.Count - 1;
        entry = BotSeaChart.CellOf(path[^1].X, path[^1].Y);

        // Only the turns: a straight run of tiles is one leg, sailed straight.
        List<Point3D> turns = [path[0]];

        for (var i = 1; i < path.Count - 1; i++)
        {
            var before = (path[i].X - path[i - 1].X, path[i].Y - path[i - 1].Y);
            var after = (path[i + 1].X - path[i].X, path[i + 1].Y - path[i].Y);

            if (before != after)
            {
                turns.Add(path[i]);
            }
        }

        if (path.Count > 1)
        {
            turns.Add(path[^1]);
        }

        return turns;
    }
}
