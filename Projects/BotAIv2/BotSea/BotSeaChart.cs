using System;
using System.Diagnostics;
using System.IO;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>What keeps a ship off a berth. Only <see cref="Ship"/> goes away by itself.</summary>
public enum BotBerthBlock
{
    None,

    /// <summary>A hull tile that is not water at sea level: the berth was drawn wrong.</summary>
    Ground,

    /// <summary>A static of the map at or above the water: a pier, a rock.</summary>
    Static,

    /// <summary>Another ship's hull, tillerman, hold or plank.</summary>
    Ship,

    /// <summary>Another multi's tile that is not a ship.</summary>
    Multi,

    /// <summary>A visible item standing on the water inside the hull's rectangle: decoration, something dropped.</summary>
    Item
}

/// <summary>
/// The water of the population's map, charted once: which tiles a ship floats on, and which squares of eight a ship can
/// sail anywhere in, whatever way it faces.
///
/// <para>
/// <b>Patrick's point 7 of 29.09.2026: "teach the bots to travel by sea, on boats."</b> Eight of Felucca's eighteen towns
/// cannot be walked to from home (the boot line of 29.09.2026: "Towns: 10 of 18 can be walked to from home") — Jhelom,
/// Moonglow, Magincia, Nujel'm, Ocllo and Skara Brae are across water. A ship is the engine's own way over it, and a ship
/// moves only where <c>BaseBoat.CanFit</c> lets it: every tile under its hull must be water at the ship's own height and
/// carry nothing else at or above it. This chart asks exactly that question of every tile, with the engine's own tile
/// numbers, so a course drawn over it is a course the engine will sail.
/// </para>
///
/// <para>
/// <b>A cell is one map block, and a cell is open when the ship fits anywhere in it, either way round.</b> The small ship
/// reaches five tiles from its middle (multi 0x0 is 5 × 11), so a cell is open when its eight-by-eight and five tiles
/// round it are all water: a ship whose middle is anywhere in the cell fits there facing any way, may turn there, and
/// sails straight into any open neighbour — diagonally only when both cells beside the corner are open too. Measured on
/// map0 (29.09.2026, a Python pass over the client files): 216,560 of the 327,680 cells are open, and 213,351 of them are
/// one body, the open sea; the rest are harbours and inlets the chart leaves to the harbour pilot (<see cref="BotDocks"/>).
/// </para>
///
/// <para>
/// <b>Read once, kept on disk.</b> Reading every block of the map is a second of the loop the first time — sliced, never
/// more than <see cref="SliceMs"/> a tick — and it leaves the engine's tile cache holding the whole map's land (about 70
/// MB) until the next restart. So the masks are written to <see cref="CachePath"/>, keyed by the engine's fingerprint of the
/// map files (<c>TileMatrix.MapFilesFingerprint</c>), and every later boot reads half a megabyte instead. Statics are read
/// only for blocks with water land in them: a block of dry land cannot float a ship whatever stands on it, and reading the
/// statics of every forest on the map would cost a further hundred megabytes for nothing.
/// </para>
/// </summary>
public static class BotSeaChart
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSeaChart));

    /// <summary>
    /// The height of the sea. Minus five: 14,512,366 of map0's water land tiles lie there and the next height has 1,118
    /// (29.09.2026). <c>BaseBoat.CanFit</c> asks for water at exactly the ship's own Z, and a ship is placed at the height of
    /// the water it is put on.
    /// </summary>
    public static int SeaLevel { get; set; } = -5;

    /// <summary>The small ship facing north (<c>SmallBoat.NorthID</c>). The one ship the population buys.</summary>
    public const int ShipNorth = 0x0;

    /// <summary>The small ship facing east (<c>SmallBoat.EastID</c>).</summary>
    public const int ShipEast = 0x1;

    /// <summary>Blocks across the charted sea: x below 5120, the engine's first wrap rectangle for Britannia.</summary>
    public const int Width = 640;

    /// <summary>Blocks down the charted sea.</summary>
    public const int Height = 512;

    /// <summary>
    /// Cells shut along the edge. The engine wraps a ship that sails past the edge of its rectangle (16 to 5103 by 16 to
    /// 4079) round to the other side, which no course means; three cells keep every hull inside it.
    /// </summary>
    private const int Rim = 3;

    /// <summary>How much of the loop one slice of charting may take, in milliseconds.</summary>
    public static double SliceMs { get; set; } = 6.0;

    /// <summary>Whether the chart is kept on disk between boots.</summary>
    public static bool Cache { get; set; } = true;

    public const string CachePath = "Data/bot-sea-chart.bin";

    private const uint Magic = 0x31414553; // "SEA1"

    private const int Version = 1;

    private static ulong[] _wet;

    private static bool[] _open;

    private static int[] _body;

    private static int[] _queue;

    private static Map _map;

    private static int _next;

    private static int _labelled;

    private static int _stage;

    private static long _began;

    /// <summary>How far the ship reaches from its middle, read from its multi. Five for the small ship.</summary>
    public static int Half { get; private set; } = 5;

    /// <summary>The small ship's hull, facing north and facing east, as offsets from its middle.</summary>
    public static Rectangle2D HullNorth { get; private set; } = new(-2, -5, 5, 11);

    public static Rectangle2D HullEast { get; private set; } = new(-5, -2, 11, 5);

    /// <summary>
    /// The tiles the ship stands on facing north, as offsets from its middle: 36 of the rectangle's 55 (multi 0x0). The
    /// hull a deed puts down, whatever way the dock means the ship to lie — <c>BaseBoatDeed.OnPlacement</c> asks
    /// <c>CanFit</c> of the new ship's own item id, and a new ship faces north.
    /// </summary>
    public static Point2D[] TilesNorth { get; private set; } = [];

    private static Point2D[] Offsets(MultiComponentList hull)
    {
        var count = 0;

        for (var x = 0; x < hull.Width; x++)
        {
            for (var y = 0; y < hull.Height; y++)
            {
                if (hull.Tiles[x][y].Length > 0)
                {
                    count++;
                }
            }
        }

        var tiles = new Point2D[count];
        var i = 0;

        for (var x = 0; x < hull.Width; x++)
        {
            for (var y = 0; y < hull.Height; y++)
            {
                if (hull.Tiles[x][y].Length > 0)
                {
                    tiles[i++] = new Point2D(hull.Min.X + x, hull.Min.Y + y);
                }
            }
        }

        return tiles;
    }

    public static bool Ready { get; private set; }

    /// <summary>Whether the masks came from the file rather than from the map.</summary>
    public static bool FromFile { get; private set; }

    public static int OpenCells { get; private set; }

    public static int Bodies { get; private set; }

    /// <summary>The open sea: the largest body of open cells, and how many cells it has.</summary>
    public static int Sea { get; private set; } = -1;

    public static int SeaCells { get; private set; }

    public static long WetTiles { get; private set; }

    /// <summary>Loop time spent charting, and the worst slice.</summary>
    public static double SpentMs { get; private set; }

    public static double WorstMs { get; private set; }

    public static int Slices { get; private set; }

    public static Map Map => _map;

    /// <summary>Whether a water land tile, by the engine's own numbers in <c>BaseBoat.CanFit</c>.</summary>
    public static bool WaterLand(int id) => id is >= 168 and <= 171 or >= 310 and <= 311;

    /// <summary>Whether a water static, by the same.</summary>
    public static bool WaterStatic(int id) => id is >= 0x1796 and <= 0x17B2;

    /// <summary>
    /// One slice of charting. True once the chart is ready. Everything that needs the map is asked on the loop, a block at
    /// a time, and the slice stops at the deadline.
    /// </summary>
    public static bool Step(Map map)
    {
        if (Ready)
        {
            return true;
        }

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var began = Stopwatch.GetTimestamp();
        var deadline = began + (long)(SliceMs * Stopwatch.Frequency / 1000.0);

        try
        {
            switch (_stage)
            {
                case 0:
                    {
                        Begin(map);

                        break;
                    }
                case 1:
                    {
                        Read(deadline);

                        break;
                    }
                case 2:
                    {
                        Open();

                        break;
                    }
                case 3:
                    {
                        Label(deadline);

                        break;
                    }
            }
        }
        finally
        {
            var ms = (Stopwatch.GetTimestamp() - began) * 1000.0 / Stopwatch.Frequency;

            SpentMs += ms;
            Slices++;

            if (ms > WorstMs)
            {
                WorstMs = ms;
            }
        }

        return Ready;
    }

    private static void Begin(Map map)
    {
        _map = map;
        _wet = new ulong[Width * Height];
        _open = new bool[Width * Height];
        _body = new int[Width * Height];
        _began = Core.TickCount;

        Hull();

        if (Cache && Load(map))
        {
            FromFile = true;
            _stage = 2;

            return;
        }

        _next = 0;
        _stage = 1;
    }

    /// <summary>The ship's reach, from the engine's own multi data, so a different client's ship is charted as it is.</summary>
    private static void Hull()
    {
        var north = MultiData.GetComponents(ShipNorth);
        var east = MultiData.GetComponents(ShipEast);

        if (north.Width <= 0 || east.Width <= 0)
        {
            logger.Warning("Sea: the small ship's multi has no components; the chart keeps a reach of {Half}", Half);

            return;
        }

        HullNorth = new Rectangle2D(north.Min.X, north.Min.Y, north.Width, north.Height);
        HullEast = new Rectangle2D(east.Min.X, east.Min.Y, east.Width, east.Height);
        TilesNorth = Offsets(north);

        var half = Math.Max(Math.Max(-north.Min.X, north.Max.X), Math.Max(-north.Min.Y, north.Max.Y));
        half = Math.Max(half, Math.Max(Math.Max(-east.Min.X, east.Max.X), Math.Max(-east.Min.Y, east.Max.Y)));

        // A reach past a block's width would need the cells two away; the small ship is five, and a bigger one is not bought.
        Half = Math.Clamp(half, 0, 8);
    }

    private static void Read(long deadline)
    {
        var map = _map;
        var total = Width * Height;

        while (_next < total)
        {
            var bx = _next / Height;
            var by = _next % Height;

            _wet[_next] = Mask(map, bx, by, false);
            _next++;

            if ((_next & 63) == 0 && Stopwatch.GetTimestamp() >= deadline)
            {
                return;
            }
        }

        if (Cache)
        {
            Save(map);
        }

        _stage = 2;
    }

    /// <summary>
    /// The water tiles of one block, by the engine's rule: water land at sea level, or a water static at sea level, and no
    /// other static at or above it. Statics are read only where there is water land, unless <paramref name="allStatics"/>
    /// asks for them everywhere (a harbour, where moats and canals are water laid over ground).
    /// </summary>
    public static ulong Mask(Map map, int bx, int by, bool allStatics)
    {
        var land = map.Tiles.GetLandBlock(bx, by);
        var sea = SeaLevel;
        ulong mask = 0;
        var water = false;

        for (var i = 0; i < 64 && i < land.Length; i++)
        {
            var tile = land[i];

            if (!WaterLand(tile.ID))
            {
                continue;
            }

            water = true;

            if (tile.Z == sea)
            {
                mask |= 1UL << i;
            }
        }

        if (!water && !allStatics)
        {
            return 0;
        }

        var statics = map.Tiles.GetStaticBlock(bx, by);

        for (var x = 0; x < 8 && x < statics.Length; x++)
        {
            var column = statics[x];

            for (var y = 0; y < 8 && y < column.Length; y++)
            {
                var tiles = column[y];

                if (tiles.Length == 0)
                {
                    continue;
                }

                var bit = 1UL << ((y << 3) | x);
                var wet = (mask & bit) != 0;
                var blocked = false;

                for (var i = 0; i < tiles.Length; i++)
                {
                    var isWater = WaterStatic(tiles[i].ID);

                    if (tiles[i].Z == sea && isWater)
                    {
                        wet = true;
                    }
                    else if (tiles[i].Z >= sea && !isWater)
                    {
                        blocked = true;

                        break;
                    }
                }

                mask = wet && !blocked ? mask | bit : mask & ~bit;
            }
        }

        return mask;
    }

    private static void Open()
    {
        var open = 0;
        long wet = 0;

        for (var i = 0; i < _wet.Length; i++)
        {
            wet += System.Numerics.BitOperations.PopCount(_wet[i]);
        }

        for (var cx = 0; cx < Width; cx++)
        {
            for (var cy = 0; cy < Height; cy++)
            {
                var index = cx * Height + cy;

                _open[index] = Opens(cx, cy);
                _body[index] = -1;

                if (_open[index])
                {
                    open++;
                }
            }
        }

        WetTiles = wet;
        OpenCells = open;
        _queue = new int[Width * Height];
        _labelled = 0;
        Bodies = 0;
        Sea = -1;
        SeaCells = 0;
        _stage = 3;
    }

    /// <summary>
    /// Whether a ship fits anywhere in this cell, either way round: the cell and <see cref="Half"/> tiles round it are
    /// water. Read from the nine blocks' masks — the columns and rows of each neighbour the reach falls in.
    /// </summary>
    private static bool Opens(int cx, int cy)
    {
        if (cx < Rim || cy < Rim || cx >= Width - Rim || cy >= Height - Rim)
        {
            return false;
        }

        for (var dx = -1; dx <= 1; dx++)
        {
            var cols = Columns(dx) * 0x0101010101010101UL;

            for (var dy = -1; dy <= 1; dy++)
            {
                var need = Rows(dy) & cols;

                if ((_wet[(cx + dx) * Height + cy + dy] & need) != need)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static ulong Columns(int dx) =>
        dx switch
        {
            < 0 => (0xFFUL << (8 - Half)) & 0xFF,
            0   => 0xFF,
            _   => (1UL << Half) - 1
        };

    private static ulong Rows(int dy) =>
        dy switch
        {
            < 0 => Half == 0 ? 0 : ~0UL << ((8 - Half) * 8),
            0   => ~0UL,
            _   => Half >= 8 ? ~0UL : (1UL << (Half * 8)) - 1
        };

    /// <summary>Numbers the bodies of open water, a flood at a time, and names the largest the open sea.</summary>
    private static void Label(long deadline)
    {
        var total = Width * Height;

        while (_labelled < total)
        {
            var start = _labelled++;

            if (!_open[start] || _body[start] >= 0)
            {
                continue;
            }

            var id = Bodies++;
            var size = Flood(start, id);

            if (size > SeaCells)
            {
                SeaCells = size;
                Sea = id;
            }

            if (Stopwatch.GetTimestamp() >= deadline)
            {
                return;
            }
        }

        _queue = null;
        Ready = true;

        logger.Information(
            "Sea: charted the water of {Map}{How}: {Wet} tiles float a ship, {Open} of {Cells} cells of eight are open to it either way round ({Bodies} bodies of water, the open sea {Sea} cells); {Ms:F0}ms of the loop in {Slices} slices over {Seconds:F1}s, the worst {Worst:F1}ms",
            _map.Name,
            FromFile ? $" from {CachePath}" : " from the map",
            WetTiles,
            OpenCells,
            total,
            Bodies,
            SeaCells,
            SpentMs,
            Slices,
            (Core.TickCount - _began) / 1000.0,
            WorstMs
        );
    }

    private static int Flood(int start, int id)
    {
        var head = 0;
        var tail = 0;

        _queue[tail++] = start;
        _body[start] = id;

        while (head < tail)
        {
            var cell = _queue[head++];
            var cx = cell / Height;
            var cy = cell % Height;

            for (var d = 0; d < 8; d++)
            {
                if (!Passable(cx, cy, d, out var next) || _body[next] >= 0)
                {
                    continue;
                }

                _body[next] = id;
                _queue[tail++] = next;
            }
        }

        return tail;
    }

    private static readonly int[] _dx = [0, 1, 1, 1, 0, -1, -1, -1];

    private static readonly int[] _dy = [-1, -1, 0, 1, 1, 1, 0, -1];

    /// <summary>The cell a step in direction <paramref name="d"/> (0 north, clockwise) leads to, when a ship can make it.</summary>
    public static bool Passable(int cx, int cy, int d, out int next)
    {
        next = -1;

        var nx = cx + _dx[d];
        var ny = cy + _dy[d];

        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
        {
            return false;
        }

        var index = nx * Height + ny;

        if (!_open[index])
        {
            return false;
        }

        // Diagonally only past two open corners: a hull turning a corner sweeps both.
        if ((d & 1) == 1 && (!_open[nx * Height + cy] || !_open[cx * Height + ny]))
        {
            return false;
        }

        next = index;

        return true;
    }

    public static int DX(int d) => _dx[d];

    public static int DY(int d) => _dy[d];

    /// <summary>The cell holding a tile, or -1 off the chart.</summary>
    public static int CellOf(int x, int y) =>
        x < 0 || y < 0 || x >= Width * 8 || y >= Height * 8 ? -1 : (x >> 3) * Height + (y >> 3);

    /// <summary>The tile a ship aims for in a cell: its middle.</summary>
    public static Point3D Middle(int cell) => new(cell / Height * 8 + 4, cell % Height * 8 + 4, SeaLevel);

    public static bool IsOpen(int cell) => Ready && cell >= 0 && cell < _open.Length && _open[cell];

    /// <summary>Whether this cell is open and part of the open sea.</summary>
    public static bool AtSea(int cell) => IsOpen(cell) && _body[cell] == Sea;

    /// <summary>Whether a ship floats on this tile, as the chart read it (statics only where there is water land).</summary>
    public static bool Wet(int x, int y)
    {
        var cell = CellOf(x, y);

        return cell >= 0 && _wet != null && (_wet[cell] >> (((y & 7) << 3) | (x & 7)) & 1) != 0;
    }

    /// <summary>
    /// Whether the small ship could be put down here now, facing north or east: <c>BaseBoat.CanFit</c> without a ship to ask
    /// it of. See <see cref="Occupant"/>.
    /// </summary>
    public static bool FitsNow(Map map, Point3D p, bool east) => Occupant(map, p, east, out _, out _) == null;

    /// <summary>
    /// What stops the small ship being put down here now, facing north or east, in words — null when nothing does. The two
    /// loops of <c>BaseBoat.CanFit</c> in their order: every hull tile must be water at sea level with no static or ship's
    /// tile at or above it, and no visible item may stand at or above the water inside the hull's rectangle.
    ///
    /// <para>
    /// <b>Written because "the berth at Vesper is taken" was all the log could say, and it was not taken.</b> Build 322's
    /// first ordered voyage (Rhiannel, Vesper for Nujel'm, 29.09.2026 20:12) waited two minutes at the dock and failed so;
    /// nothing was in the water. The dock lay east–west and the check asked the north-facing hull a deed puts down, which
    /// covers (3010, 752) and (3010, 753) — the shore. Every kind of obstruction now has its name and its own count, and
    /// only a ship is worth waiting for (<see cref="BotBerthBlock"/>).
    /// </para>
    /// </summary>
    public static string Occupant(Map map, Point3D p, bool east, out BotBerthBlock block, out BaseBoat ship)
    {
        block = BotBerthBlock.None;
        ship = null;

        if (map == null || map == Map.Internal)
        {
            block = BotBerthBlock.Ground;

            return "no map";
        }

        var hull = MultiData.GetComponents(east ? ShipEast : ShipNorth);

        if (hull.Width <= 0)
        {
            block = BotBerthBlock.Ground;

            return "the ship's multi has no components";
        }

        for (var hx = 0; hx < hull.Width; hx++)
        {
            for (var hy = 0; hy < hull.Height; hy++)
            {
                if (hull.Tiles[hx][hy].Length == 0)
                {
                    continue;
                }

                var x = p.X + hull.Min.X + hx;
                var y = p.Y + hull.Min.Y + hy;
                var land = map.Tiles.GetLandTile(x, y);
                var water = land.Z == SeaLevel && WaterLand(land.ID);

                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                {
                    var isWater = WaterStatic(tile.ID);

                    if (tile.Z == SeaLevel && isWater)
                    {
                        water = true;
                    }
                    else if (tile.Z >= SeaLevel && !isWater)
                    {
                        block = BotBerthBlock.Static;

                        return $"the static 0x{tile.ID:X4} at ({x}, {y}, {tile.Z})";
                    }
                }

                foreach (var tile in map.Tiles.GetMultiTiles(x, y))
                {
                    if (tile.Z < SeaLevel || WaterStatic(tile.ID))
                    {
                        continue;
                    }

                    ship = BaseBoat.FindBoatAt(new Point3D(x, y, tile.Z), map);
                    block = ship != null ? BotBerthBlock.Ship : BotBerthBlock.Multi;

                    return ship != null ? $"{Whose(ship)} at ({ship.X}, {ship.Y})" : $"a building's tile 0x{tile.ID:X4} at ({x}, {y}, {tile.Z})";
                }

                if (!water)
                {
                    block = BotBerthBlock.Ground;

                    return $"dry ground at ({x}, {y}): land 0x{land.ID:X4} at Z {land.Z}";
                }
            }
        }

        var bounds = new Rectangle2D(p.X + hull.Min.X, p.Y + hull.Min.Y, hull.Width, hull.Height);

        foreach (var item in map.GetItemsInBounds(bounds))
        {
            if (item is Server.Items.BaseMulti || item.ItemID > TileData.MaxItemValue || item.Z < p.Z || !item.Visible)
            {
                continue;
            }

            // A tillerman, a hold or a plank is another ship's, and says so.
            ship = BaseBoat.FindBoatAt(item.Location, map);

            if (ship != null)
            {
                block = BotBerthBlock.Ship;

                return $"{Whose(ship)} at ({ship.X}, {ship.Y})";
            }

            block = BotBerthBlock.Item;

            return $"{item.GetType().Name} 0x{item.ItemID:X4} at ({item.X}, {item.Y}, {item.Z})";
        }

        return null;
    }

    private static string Whose(BaseBoat ship) =>
        ship.Owner is { } owner ? $"{owner.Name}'s ship{(ship.Anchored ? ", at anchor" : ", under way")}" : "a ship with no owner";

    private static string FilePath => Path.Combine(Core.BaseDirectory, CachePath);

    private static bool Load(Map map)
    {
        var path = FilePath;

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version || reader.ReadUInt64() != map.Tiles.MapFilesFingerprint
                || reader.ReadInt32() != Width || reader.ReadInt32() != Height || reader.ReadInt32() != SeaLevel)
            {
                logger.Information("Sea: {Path} was charted from other map files or another sea level; charting again", CachePath);

                return false;
            }

            for (var i = 0; i < _wet.Length; i++)
            {
                _wet[i] = reader.ReadByte() switch
                {
                    0 => 0UL,
                    1 => ~0UL,
                    _ => reader.ReadUInt64()
                };
            }

            return true;
        }
        catch (Exception e)
        {
            logger.Warning(e, "Sea: {Path} could not be read; charting again", CachePath);

            return false;
        }
    }

    private static void Save(Map map)
    {
        try
        {
            var path = FilePath;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);

            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(map.Tiles.MapFilesFingerprint);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(SeaLevel);

            for (var i = 0; i < _wet.Length; i++)
            {
                var mask = _wet[i];

                if (mask == 0UL)
                {
                    writer.Write((byte)0);
                }
                else if (mask == ~0UL)
                {
                    writer.Write((byte)1);
                }
                else
                {
                    writer.Write((byte)2);
                    writer.Write(mask);
                }
            }
        }
        catch (Exception e)
        {
            logger.Warning(e, "Sea: the chart could not be written to {Path}; the next boot charts again", CachePath);
        }
    }

    public static string Describe() =>
        Ready
            ? $"the chart has {OpenCells} open cells, the open sea {SeaCells} of them in {Bodies} bodies{(FromFile ? ", read from the file" : "")}"
            : _stage == 1
                ? $"charting the water: {_next * 100 / (Width * Height)}% of the map read"
                : "the water has not been charted";

    public static void Forget()
    {
        _wet = null;
        _open = null;
        _body = null;
        _queue = null;
        _map = null;
        _next = 0;
        _labelled = 0;
        _stage = 0;
        Ready = false;
        FromFile = false;
        OpenCells = 0;
        Bodies = 0;
        Sea = -1;
        SeaCells = 0;
        WetTiles = 0;
        SpentMs = 0;
        WorstMs = 0;
        Slices = 0;
    }
}
