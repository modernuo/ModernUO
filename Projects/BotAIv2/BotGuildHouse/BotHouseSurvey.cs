using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Server.Engines.Spawners;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// The empty buildings of the towns: walls round a floor, a roof over it, a doorway with no door in it — read once from the
/// map's own statics, the way the engine's door generator reads them, and kept on disk.
///
/// <para>
/// <b>Patrick's idea of 29.09.2026, evening: "one or two guilds may settle in each of the other towns, but only in an empty
/// and suitable building; when a guild settles there, a door is put in and a sign hung on the wall."</b> A building in a UO
/// town is not an object anywhere — it is statics on the map, and its doors and shop signs are items the world was decorated
/// with. So a building has to be found the way the engine itself finds one: <c>DoorGenerator</c> (the <c>[DoorGen</c>
/// command) recognises a doorway as a west door-frame static with an east one two or three tiles along the same row, or a
/// north frame with a south one two or three tiles down the same column, and puts a door (or a linked pair) between them.
/// This survey reads the same frames with the same public tests (<c>DoorGenerator.IsWestFrame</c> and the rest) and asks,
/// for each doorway, what lies on its two sides.
/// </para>
///
/// <para>
/// <b>A side is a room when a flood from it stops by itself.</b> The flood walks the floor with the planner's own step model
/// (<see cref="BotStep.Mask"/>, land and statics), four ways, inside a band of a storey round the doorway's height, and treats
/// every doorway it knows as shut; a side that runs past <see cref="MaxArea"/> tiles is the street. A doorway with a room on
/// one side and the street on the other is a way in; one with rooms on both sides joins them into one building. A building
/// is kept when its floor is at least <see cref="MinArea"/> and at most <see cref="MaxArea"/> tiles, at least
/// <see cref="RoofShare"/> of it is under a roof or an upper floor, it has at most <see cref="MostEntrances"/> ways in, and
/// no forge, anvil, oven or hearth stands in its statics — those are workshops the bots already use
/// (<c>BotGround</c>). What the world has put in it since — a door, a spawner, a shopkeeper living there, a house, an inn's
/// region — is asked every boot and before every settling by <see cref="Occupied"/>, because the world changes and the map
/// files do not.
/// </para>
///
/// <para>
/// <b>Why a floor of sixty-four tiles.</b> The small one-room cottages every town is built of have a floor of 42 to 49 tiles
/// (7 by 7 walls, measured from the client's own map on 29.09.2026); they are 40 of Britain's 97 buildings with a way out,
/// 10 of Minoc's 26, 19 of Yew's 56 and 46 of Nujel'm's 63, and they are private homes by the world's design. Sixty-four —
/// eight by eight — is the first size past them: the two-room houses and the halls of 90 to 350 tiles, room for a guild of
/// five to fifteen to be born and rise in (<c>BotGuildHouses.Spread</c> puts a body down within three tiles of the heart)
/// without spilling into the street. This survey, run on 29.09.2026 through the engine itself against the client's
/// Felucca files with the world decorated and the spawn files' spawners laid down (a scratch test host, not the shard),
/// kept 287 buildings of 64 to 400 tiles and found free, after the live checks: 20 in Britain, 2 in Minoc, 9 in Trinsic,
/// 4 in Vesper, 19 in Yew and none in Cove — 127 had a spawner in them (a shop, a bank, a healer), 30 a hearth or forge
/// set down as an item, 18 a door already, and 23 lay in an inn's rooms. 1.7 seconds of the loop in 204 slices, the
/// worst 29ms. The boot line says what the shard itself finds.
/// </para>
///
/// <para>
/// <b>Read once, kept on disk, sliced on the loop.</b> Scanning every tile of eighteen town regions for frames and flooding
/// both sides of some thousand doorways is a few seconds of the loop, so it runs a slice of <see cref="SliceMs"/> a tick
/// and writes what it found to <see cref="CachePath"/> under <c>Distribution</c>, keyed by the engine's fingerprint of the
/// map files (<c>TileMatrix.MapFilesFingerprint</c>) and the numbers above — as <c>BotSeaChart</c> does. Never under
/// <c>Saves</c>, which the engine moves into <c>Backups</c> at every save.
/// </para>
/// </summary>
public static class BotHouseSurvey
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHouseSurvey));

    /// <summary>The least floor a guild's house may have, in tiles. See the class: past the 7 × 7 cottages.</summary>
    public static int MinArea { get; set; } = 64;

    /// <summary>The most floor, in tiles; also how far a flood runs before its side is called the street.</summary>
    public static int MaxArea { get; set; } = 400;

    /// <summary>The least share of the floor under a roof or an upper storey. Three quarters: a walled yard is not a house.</summary>
    public static double RoofShare { get; set; } = 0.75;

    /// <summary>The most ways in a house may have; every one of them is given a door.</summary>
    public static int MostEntrances { get; set; } = 2;

    /// <summary>How much of the loop one slice of surveying may take, in milliseconds.</summary>
    public static double SliceMs { get; set; } = 6.0;

    /// <summary>Whether the survey is kept on disk between boots.</summary>
    public static bool Cache { get; set; } = true;

    public const string CachePath = "Data/bot-guild-houses.bin";

    private const uint Magic = 0x31534847; // "GHS1"

    private const int Version = 1;

    /// <summary>How far below and above the doorway's height the flood walks: a step down to the street, not a storey up.</summary>
    private const int Below = 6;

    private const int Above = 12;

    /// <summary>A way in: the doorway's one or two door tiles at the frame's height, which way its frames run, and the street outside.</summary>
    public sealed class Entrance
    {
        public Point3D[] Doors;

        /// <summary>Frames along the row (a wall running east–west, the generator's west/east pair); false for a column.</summary>
        public bool AlongX;

        /// <summary>A standing tile on the street side, next to the doorway.</summary>
        public Point3D Outside;

        /// <summary>The unit step from the doorway to <see cref="Outside"/>: (0, ±1) when <see cref="AlongX"/>, (±1, 0) otherwise.</summary>
        public int OutX;

        public int OutY;
    }

    /// <summary>One building the survey found and kept.</summary>
    public sealed class Building
    {
        public int Id;

        /// <summary>The town whose region box holds it, by the region's name (<see cref="BotTowns.Town.Name"/>).</summary>
        public string Town;

        /// <summary>A floor tile near its middle: where the guild's seat is, and where members are born and rise.</summary>
        public Point3D Heart;

        public int Area;

        public double Roofed;

        public Rectangle2D Box;

        /// <summary>Its floor, as <see cref="Key"/>s.</summary>
        public HashSet<int> Tiles;

        public Entrance[] Entrances;

        /// <summary>The first door tile of its first way in: what a guild's record names it by, across restarts.</summary>
        public Point3D Door => Entrances[0].Doors[0];

        public bool Holds(int x, int y) => Tiles.Contains(Key(x, y));

        /// <summary>Whether a point lies on its floor or on one of its door tiles.</summary>
        public bool Covers(int x, int y)
        {
            if (Holds(x, y))
            {
                return true;
            }

            for (var i = 0; i < Entrances.Length; i++)
            {
                var doors = Entrances[i].Doors;

                for (var j = 0; j < doors.Length; j++)
                {
                    if (doors[j].X == x && doors[j].Y == y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override string ToString() => $"{Town} ({Door.X}, {Door.Y}): {Area} tiles, {Entrances.Length} way{(Entrances.Length == 1 ? "" : "s")} in";
    }

    /// <summary>What the survey counted in one town, for the boot line.</summary>
    public sealed class Tally
    {
        public int Doorways;

        public int WayOut;

        public int Small;

        public int Large;

        public int Roofless;

        public int Doorful;

        public int Workshop;

        public int Kept;
    }

    public static bool Ready { get; private set; }

    public static bool FromFile { get; private set; }

    public static double SpentMs { get; private set; }

    public static double WorstMs { get; private set; }

    public static int Slices { get; private set; }

    private static readonly List<Building> _buildings = [];

    private static readonly Dictionary<int, Building> _byDoor = [];

    private static readonly Dictionary<string, Tally> _tallies = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<Building> All => _buildings;

    public static IReadOnlyDictionary<string, Tally> Tallies => _tallies;

    /// <summary>A tile as one integer: x in the high half, y in the low. The map is under 65536 both ways.</summary>
    public static int Key(int x, int y) => (x << 16) | (y & 0xFFFF);

    private static int KeyX(int key) => key >> 16;

    private static int KeyY(int key) => key & 0xFFFF;

    /// <summary>
    /// A standing cell as the flood keys it: the tile and the storey, the planner's own cell (<see cref="BotStep.Cell"/>). By
    /// tile alone a ground floor and the room above it are one room, and the first run against the real map filed a door at
    /// fifteen under a room at thirty-five.
    /// </summary>
    private static int Cell(int x, int y, sbyte z) => BotStep.Cell(x, y, z);

    private static int CellX(int cell) => (cell >> 13) & 0x1FFF;

    private static int CellY(int cell) => cell & 0x1FFF;

    /// <summary>The building whose first door stands on this tile, or null.</summary>
    public static Building ByDoor(Point3D door) => _byDoor.GetValueOrDefault(Key(door.X, door.Y));

    // ---- the survey's working state, dropped when it is ready ----

    private sealed class Doorway
    {
        public Point3D[] Doors;

        public bool AlongX;

        public int Z;

        public string Town;
    }

    private sealed class Room
    {
        public int Parent;

        public readonly Dictionary<int, sbyte> Floor = [];

        public readonly List<(Doorway Way, Point3D Outside, int OutX, int OutY)> Ways = [];
    }

    private static Map _map;

    private static int _stage;

    private static int _town;

    private static int _column;

    private static int _next;

    private static List<Doorway> _doorways;

    private static HashSet<int> _doorTiles;

    private static List<Room> _rooms;

    private static Dictionary<int, int> _roomOf;

    /// <summary>
    /// Tiles a flood has already proved to be the street: any later flood that steps onto one is the street too, and stops
    /// there. Without it the street side of every doorway is walked out to <see cref="MaxArea"/> tiles again, which is most of
    /// the survey's cost.
    /// </summary>
    private static HashSet<int> _street;

    private static readonly Queue<(int X, int Y, sbyte Z)> _queue = new();

    private static readonly Dictionary<int, sbyte> _seen = [];

    /// <summary>
    /// One slice of surveying. True once the survey is ready. Asked of the module's clock on the loop, and only once the towns
    /// have been read (<see cref="BotTowns.Surveyed"/>), whose boxes it walks.
    /// </summary>
    public static bool Step(Map map)
    {
        if (Ready)
        {
            return true;
        }

        if (map == null || map == Map.Internal || !BotTowns.Surveyed)
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
                        Scan(deadline);

                        break;
                    }
                case 2:
                    {
                        Flood(deadline);

                        break;
                    }
                case 3:
                    {
                        Build(deadline);

                        break;
                    }
            }
        }
        catch (Exception e)
        {
            // A survey that throws is a survey that never finishes and a module that never settles anybody; said once, and
            // the guild houses stay off for this boot rather than the loop throwing every tick.
            logger.Error(e, "Guild houses: the survey of the towns failed at stage {Stage}; no guild will be housed this boot", _stage);
            Ready = true;
            Drop();
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
        _buildings.Clear();
        _byDoor.Clear();
        _tallies.Clear();

        if (Cache && Load(map))
        {
            FromFile = true;
            Finish();

            return;
        }

        _doorways = [];
        _doorTiles = [];
        _rooms = [];
        _roomOf = [];
        _street = [];
        _town = 0;
        _column = int.MinValue;
        _next = 0;
        _stage = 1;
    }

    /// <summary>Walks the towns' boxes a column at a time, finding doorways by the engine's frame pairs.</summary>
    private static void Scan(long deadline)
    {
        var towns = BotTowns.All;

        while (_town < towns.Count)
        {
            var town = towns[_town];
            var box = town.Bounds;
            var x1 = Math.Max(0, box.X);
            var y1 = Math.Max(0, box.Y);
            var x2 = Math.Min(_map.Width - 4, box.X + box.Width - 1);
            var y2 = Math.Min(_map.Height - 4, box.Y + box.Height - 1);

            if (_column == int.MinValue)
            {
                _column = x1;
                TallyOf(town.Name);
            }

            while (_column <= x2)
            {
                for (var y = y1; y <= y2; y++)
                {
                    Frames(_column, y, town.Name);
                }

                _column++;

                if (Stopwatch.GetTimestamp() >= deadline)
                {
                    return;
                }
            }

            _town++;
            _column = int.MinValue;
        }

        _stage = 2;
        _next = 0;
    }

    /// <summary>
    /// The doorways starting at one tile, read exactly as <c>DoorGenerator.Generate</c> reads them: a west frame with an east
    /// frame two tiles on (one door) or three (a pair), else a north frame with a south one two or three tiles down; the
    /// frames within one of each other's height, the door at the lower. A doorway whose tiles hold no floor at that height is
    /// a frame round a window or a wall, and is passed over.
    /// </summary>
    private static void Frames(int x, int y, string town)
    {
        foreach (var tile in _map.Tiles.GetStaticTiles(x, y))
        {
            var id = tile.ID;
            var z = tile.Z;

            if (DoorGenerator.IsWestFrame(id))
            {
                if (Frame(x + 2, y, z, true, out var nz))
                {
                    Add([new Point3D(x + 1, y, 0)], Math.Min(z, nz), true, town);
                }
                else if (Frame(x + 3, y, z, true, out nz))
                {
                    Add([new Point3D(x + 1, y, 0), new Point3D(x + 2, y, 0)], Math.Min(z, nz), true, town);
                }
            }
            else if (DoorGenerator.IsNorthFrame(id))
            {
                if (Frame(x, y + 2, z, false, out var nz))
                {
                    Add([new Point3D(x, y + 1, 0)], Math.Min(z, nz), false, town);
                }
                else if (Frame(x, y + 3, z, false, out nz))
                {
                    Add([new Point3D(x, y + 1, 0), new Point3D(x, y + 2, 0)], Math.Min(z, nz), false, town);
                }
            }
        }
    }

    /// <summary>The closing frame of a pair, east (along a row) or south (down a column), within one of the opening frame's height.</summary>
    private static bool Frame(int x, int y, int z, bool east, out int newZ)
    {
        foreach (var tile in _map.Tiles.GetStaticTiles(x, y))
        {
            var id = tile.ID;

            if (!(east ? DoorGenerator.IsEastFrame(id) : DoorGenerator.IsSouthFrame(id)))
            {
                continue;
            }

            var delta = tile.Z - z;

            if (delta is >= -1 and <= 1)
            {
                newZ = tile.Z;

                return true;
            }
        }

        newZ = 0;

        return false;
    }

    private static void Add(Point3D[] doors, int z, bool alongX, string town)
    {
        var first = Key(doors[0].X, doors[0].Y);

        if (_doorTiles.Contains(first))
        {
            return;
        }

        // A floor at the frame's height on every door tile, by the land and the statics alone: the engine's generator asks
        // CanFit of the same tile, which reads items as well, and a door the world was decorated with would make that answer
        // no — this survey is of the map, and what stands in a doorway is asked live (Occupied). The door itself goes in at
        // the frames' height, where the generator puts it.
        for (var i = 0; i < doors.Length; i++)
        {
            if (!BotStep.Ground(_map, doors[i].X, doors[i].Y, z, 3, out _))
            {
                return;
            }

            doors[i] = new Point3D(doors[i].X, doors[i].Y, z);
        }

        for (var i = 0; i < doors.Length; i++)
        {
            _doorTiles.Add(Key(doors[i].X, doors[i].Y));
        }

        _doorways.Add(new Doorway { Doors = doors, AlongX = alongX, Z = z, Town = town });
        TallyOf(town).Doorways++;
    }

    /// <summary>Floods the two sides of each doorway in turn, a doorway at a time, and files what it finds as rooms.</summary>
    private static void Flood(long deadline)
    {
        while (_next < _doorways.Count)
        {
            var way = _doorways[_next++];
            var d = way.Doors[0];

            // The two sides are across the frame line: north and south of a door in a row, west and east of one in a column.
            var ax = way.AlongX ? d.X : d.X - 1;
            var ay = way.AlongX ? d.Y - 1 : d.Y;
            var bx = way.AlongX ? d.X : d.X + 1;
            var by = way.AlongX ? d.Y + 1 : d.Y;

            var a = Side(ax, ay, way.Z, out var aOpen, out var aAt);
            var b = Side(bx, by, way.Z, out var bOpen, out var bAt);

            if (a >= 0 && bOpen)
            {
                _rooms[Find(a)].Ways.Add((way, bAt, bx - d.X, by - d.Y));
            }
            else if (b >= 0 && aOpen)
            {
                _rooms[Find(b)].Ways.Add((way, aAt, ax - d.X, ay - d.Y));
            }
            else if (a >= 0 && b >= 0)
            {
                // Rooms on both sides: one building, joined through this doorway.
                Union(a, b);
            }

            if (Stopwatch.GetTimestamp() >= deadline)
            {
                return;
            }
        }

        _stage = 3;
        _next = 0;
    }

    /// <summary>
    /// One side of a doorway: the room it opens into (its index), or −1 with <paramref name="open"/> saying whether that was
    /// because the flood ran out into the street (true) or because there was no floor there at all (false).
    /// </summary>
    private static int Side(int x, int y, int z0, out bool open, out Point3D at)
    {
        open = false;
        at = Point3D.Zero;

        if (!BotStep.Ground(_map, x, y, z0, 8, out var sz))
        {
            return -1;
        }

        at = new Point3D(x, y, sz);

        var key = Cell(x, y, sz);

        // A side already known to be a room is that room, and one already known to be the street is the street.
        if (_roomOf.TryGetValue(key, out var known))
        {
            return Find(known);
        }

        if (_street.Contains(key))
        {
            open = true;

            return -1;
        }

        _seen.Clear();
        _queue.Clear();
        _seen[key] = sz;
        _queue.Enqueue((x, y, sz));

        while (_queue.Count > 0)
        {
            var (cx, cy, cz) = _queue.Dequeue();
            var mask = BotStep.Mask(_map, cx, cy, cz);

            for (var d = 0; d < 8; d += 2)
            {
                if ((mask.WalkMask & (1 << d)) == 0)
                {
                    continue;
                }

                var nx = cx + (d == 2 ? 1 : d == 6 ? -1 : 0);
                var ny = cy + (d == 4 ? 1 : d == 0 ? -1 : 0);
                if (_doorTiles.Contains(Key(nx, ny)))
                {
                    continue;
                }

                var nz = mask.GetWalkZ((Direction)d);

                if (nz < z0 - Below || nz > z0 + Above)
                {
                    continue;
                }

                var nkey = Cell(nx, ny, nz);

                if (_seen.ContainsKey(nkey))
                {
                    continue;
                }

                if (_street.Contains(nkey))
                {
                    Street();
                    open = true;

                    return -1;
                }

                // Into a room already filed: this side is that room, and there is nothing new to learn by walking it again.
                if (_roomOf.TryGetValue(nkey, out var into))
                {
                    Absorb(into);

                    return Find(into);
                }

                _seen[nkey] = nz;

                if (_seen.Count > MaxArea)
                {
                    Street();
                    open = true;

                    return -1;
                }

                _queue.Enqueue((nx, ny, nz));
            }
        }

        var room = new Room { Parent = _rooms.Count };

        _rooms.Add(room);
        Absorb(room.Parent);

        return room.Parent;
    }

    /// <summary>Files the tiles walked so far as the street.</summary>
    private static void Street()
    {
        foreach (var key in _seen.Keys)
        {
            _street.Add(key);
        }

        _seen.Clear();
        _queue.Clear();
    }

    /// <summary>Files the tiles walked so far under a room.</summary>
    private static void Absorb(int index)
    {
        var room = _rooms[Find(index)];

        foreach (var (key, z) in _seen)
        {
            if (_roomOf.TryAdd(key, room.Parent))
            {
                room.Floor[key] = z;
            }
        }

        _seen.Clear();
        _queue.Clear();
    }

    private static int Find(int index)
    {
        while (_rooms[index].Parent != index)
        {
            _rooms[index].Parent = _rooms[_rooms[index].Parent].Parent;
            index = _rooms[index].Parent;
        }

        return index;
    }

    private static void Union(int a, int b)
    {
        var ra = Find(a);
        var rb = Find(b);

        if (ra == rb)
        {
            return;
        }

        var into = _rooms[ra];
        var from = _rooms[rb];

        foreach (var (key, z) in from.Floor)
        {
            into.Floor[key] = z;
            _roomOf[key] = ra;
        }

        into.Ways.AddRange(from.Ways);
        from.Floor.Clear();
        from.Ways.Clear();
        from.Parent = ra;
    }

    /// <summary>Turns the rooms into buildings, keeps the fit ones, and writes the file — a room at a time, to the deadline.</summary>
    private static void Build(long deadline)
    {
        for (; _next < _rooms.Count; _next++)
        {
            if (Stopwatch.GetTimestamp() >= deadline)
            {
                return;
            }

            var i = _next;

            if (Find(i) != i)
            {
                continue;
            }

            var room = _rooms[i];

            if (room.Ways.Count == 0)
            {
                continue;
            }

            var town = room.Ways[0].Way.Town;
            var tally = TallyOf(town);

            tally.WayOut++;

            if (room.Floor.Count < MinArea)
            {
                tally.Small++;

                continue;
            }

            if (room.Floor.Count > MaxArea)
            {
                tally.Large++;

                continue;
            }

            if (room.Ways.Count > MostEntrances)
            {
                tally.Doorful++;

                continue;
            }

            var roofed = 0;
            var workshop = false;
            long sx = 0;
            long sy = 0;
            int x1 = int.MaxValue, y1 = int.MaxValue, x2 = int.MinValue, y2 = int.MinValue;

            foreach (var (key, z) in room.Floor)
            {
                var x = CellX(key);
                var y = CellY(key);

                sx += x;
                sy += y;
                x1 = Math.Min(x1, x);
                y1 = Math.Min(y1, y);
                x2 = Math.Max(x2, x);
                y2 = Math.Max(y2, y);

                var covered = false;

                foreach (var tile in _map.Tiles.GetStaticTiles(x, y))
                {
                    var id = tile.ID & TileData.MaxItemValue;
                    var data = TileData.ItemTable[id];

                    if (BotGround.IsForgeId(id) || BotGround.IsAnvilId(id) || BotGround.IsHearthId(id))
                    {
                        workshop = true;
                    }

                    if (!covered && (data.Flags & TileFlag.Foliage) == 0 && tile.Z >= z + 16 && tile.Z <= z + 64)
                    {
                        covered = true;
                    }
                }

                if (covered)
                {
                    roofed++;
                }
            }

            var share = roofed / (double)room.Floor.Count;

            if (share < RoofShare)
            {
                tally.Roofless++;

                continue;
            }

            if (workshop)
            {
                tally.Workshop++;

                continue;
            }

            // The heart: the floor tile nearest the middle, so the seat is inside and away from the walls where it can be — and on
            // the storey the door opens onto. Rooms joined through an inner doorway can lie a storey apart, and the first run of
            // the survey against the real map put one heart twenty above its door (Britain, 1543,1742 at 35 over a door at 15).
            var mx = (int)(sx / room.Floor.Count);
            var my = (int)(sy / room.Floor.Count);
            var doorZ = room.Ways[0].Way.Z;
            var heart = Point3D.Zero;
            var best = int.MaxValue;

            foreach (var (key, z) in room.Floor)
            {
                var gap = Math.Max(Math.Abs(CellX(key) - mx), Math.Abs(CellY(key) - my));

                // A tile off the door's storey counts as far away as the map is wide: taken only when there is no other.
                if (z < doorZ - 4 || z > doorZ + 8)
                {
                    gap += 1 << 16;
                }

                if (gap < best)
                {
                    best = gap;
                    heart = new Point3D(CellX(key), CellY(key), z);
                }
            }

            var entrances = new Entrance[room.Ways.Count];

            for (var w = 0; w < room.Ways.Count; w++)
            {
                var (way, outside, ox, oy) = room.Ways[w];

                entrances[w] = new Entrance { Doors = way.Doors, AlongX = way.AlongX, Outside = outside, OutX = ox, OutY = oy };
            }

            Keep(
                new Building
                {
                    Town = town,
                    Heart = heart,
                    Area = room.Floor.Count,
                    Roofed = share,
                    Box = new Rectangle2D(x1, y1, x2 - x1 + 1, y2 - y1 + 1),
                    Tiles = Flat(room.Floor.Keys),
                    Entrances = entrances
                }
            );

            tally.Kept++;
        }

        if (Cache)
        {
            Save(_map);
        }

        Finish();
    }

    /// <summary>A floor's cells as tiles: what a building holds, whatever the storey. See <see cref="Building.Holds"/>.</summary>
    private static HashSet<int> Flat(IEnumerable<int> cells)
    {
        var tiles = new HashSet<int>();

        foreach (var cell in cells)
        {
            tiles.Add(Key(CellX(cell), CellY(cell)));
        }

        return tiles;
    }

    private static void Keep(Building building)
    {
        building.Id = _buildings.Count;
        _buildings.Add(building);
        _byDoor[Key(building.Door.X, building.Door.Y)] = building;
    }

    private static void Finish()
    {
        Ready = true;
        Drop();

        using var towns = Server.Text.ValueStringBuilder.Create(512);

        foreach (var (name, tally) in _tallies)
        {
            if (tally.Kept == 0 && tally.WayOut == 0)
            {
                continue;
            }

            towns.Append(towns.Length > 0 ? ", " : "");
            towns.Append($"{name} {tally.Kept} of {tally.WayOut}");
        }

        logger.Information(
            "Guild houses: the towns were surveyed {How} in {Ms:F0}ms of the loop over {Slices} slices (the worst {Worst:F1}ms): {Kept} buildings with a floor of {Least} to {Most} tiles, {Roof:P0} of it roofed and at most {Ways} ways in, kept of those with a way out — {Towns}",
            FromFile ? $"from {CachePath}" : "from the map",
            SpentMs,
            Slices,
            WorstMs,
            _buildings.Count,
            MinArea,
            MaxArea,
            RoofShare,
            MostEntrances,
            towns.ToString()
        );
    }

    private static void Drop()
    {
        _doorways = null;
        _doorTiles = null;
        _rooms = null;
        _roomOf = null;
        _street = null;
        _seen.Clear();
        _queue.Clear();
    }

    private static Tally TallyOf(string town)
    {
        if (!_tallies.TryGetValue(town, out var tally))
        {
            tally = new Tally();
            _tallies[town] = tally;
        }

        return tally;
    }

    /// <summary>
    /// Why this building cannot be a guild's house now, or null when it can. Everything the map files cannot say, asked of the
    /// world with spatial queries round the building's own box:
    /// <list type="bullet">
    ///   <item>a region of its own over any of its floor — an inn's room (<c>NoLogoutDelay</c>), a house's, anything other
    ///   than the town's own region;</item>
    ///   <item>a spawner on its floor or beside it, or a creature whose home is on it — a shopkeeper, a banker, a healer;</item>
    ///   <item>a door already in one of its doorways that is not this guild's, or a house over it;</item>
    ///   <item>a forge, anvil or hearth set down in it as an item;</item>
    ///   <item>its street side not joined to home by the gates, so a member could not walk there.</item>
    /// </list>
    /// </summary>
    public static string Occupied(Building b, string guild = null)
    {
        var map = _map ?? BotPopulation.Home;

        if (b == null || map == null || map == Map.Internal)
        {
            return "no map";
        }

        var own = Region.Find(b.Heart, map);

        if (own is not TownRegion || !string.Equals(own.Name, b.Town, StringComparison.OrdinalIgnoreCase))
        {
            return $"it lies in {Named(own)}, not in {b.Town}'s own streets";
        }

        foreach (var key in b.Tiles)
        {
            var at = Region.Find(new Point3D(KeyX(key), KeyY(key), b.Heart.Z), map);

            if (at != own)
            {
                return $"part of it lies in {Named(at)}";
            }
        }

        if (BaseHouse.FindHouseAt(b.Heart, map, 16) != null)
        {
            return "a house stands over it";
        }

        var box = new Rectangle2D(b.Box.X - 2, b.Box.Y - 2, b.Box.Width + 4, b.Box.Height + 4);

        foreach (var item in map.GetItemsInBounds(box))
        {
            if (item is not { Deleted: false })
            {
                continue;
            }

            if (item is ISpawner && Near(b, item.X, item.Y))
            {
                return $"a spawner stands in it ({Kept(item)})";
            }

            if (item is BaseDoor door && InDoorway(b, door))
            {
                if (door is BotGuildDoor ours && string.Equals(ours.Guild, guild, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return door is BotGuildDoor theirs ? $"{theirs.Guild} lives there" : "a door already hangs in its doorway";
            }

            var id = item.ItemID & TileData.MaxItemValue;

            if (b.Holds(item.X, item.Y) && (BotGround.IsForgeId(id) || BotGround.IsAnvilId(id) || BotGround.IsHearthId(id)))
            {
                return "a workshop the bots use stands in it";
            }
        }

        foreach (var mobile in map.GetMobilesInBounds(box))
        {
            if (mobile is not { Deleted: false } || mobile is BotMobile || mobile.Player)
            {
                continue;
            }

            if (mobile is BaseVendor && b.Holds(mobile.X, mobile.Y)
                || mobile is BaseCreature { Home: var home } && home != Point3D.Zero && b.Holds(home.X, home.Y))
            {
                return $"{mobile.Name ?? mobile.GetType().Name} lives in it";
            }
        }

        if (BotGates.Ready && !BotGates.Joined(map, BotPopulation.Where, b.Entrances[0].Outside))
        {
            return "its street cannot be walked to from home";
        }

        return null;
    }

    private static string Named(Region region) =>
        region switch
        {
            null                                => "no region",
            BaseRegion { NoLogoutDelay: true }  => "an inn's rooms",
            _ when !string.IsNullOrEmpty(region.Name) => region.Name,
            _                                   => $"a {region.GetType().Name}"
        };

    /// <summary>What a spawner keeps, for the reason given: its first entry's name when it has one.</summary>
    private static string Kept(Item spawner) =>
        spawner is BaseSpawner { Entries.Count: > 0 } kept && !string.IsNullOrEmpty(kept.Entries[0].SpawnedName)
            ? kept.Entries[0].SpawnedName
            : spawner.GetType().Name;

    /// <summary>Whether a point is on the building's floor or within a tile of it.</summary>
    private static bool Near(Building b, int x, int y)
    {
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (b.Covers(x + dx, y + dy))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Whether a door stands in one of the building's doorways, closed or swung open beside it.</summary>
    private static bool InDoorway(Building b, BaseDoor door)
    {
        var at = door.Open ? new Point3D(door.X - door.Offset.X, door.Y - door.Offset.Y, door.Z) : door.Location;

        for (var i = 0; i < b.Entrances.Length; i++)
        {
            var doors = b.Entrances[i].Doors;

            for (var j = 0; j < doors.Length; j++)
            {
                if (doors[j].X == at.X && doors[j].Y == at.Y)
                {
                    return true;
                }
            }
        }

        return false;
    }

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
                || reader.ReadInt32() != MinArea || reader.ReadInt32() != MaxArea || reader.ReadInt32() != (int)Math.Round(RoofShare * 1000)
                || reader.ReadInt32() != MostEntrances)
            {
                logger.Information("Guild houses: {Path} was surveyed from other map files or with other numbers; surveying again", CachePath);

                return false;
            }

            var towns = reader.ReadInt32();

            for (var i = 0; i < towns; i++)
            {
                var tally = TallyOf(reader.ReadString());

                tally.Doorways = reader.ReadInt32();
                tally.WayOut = reader.ReadInt32();
                tally.Small = reader.ReadInt32();
                tally.Large = reader.ReadInt32();
                tally.Roofless = reader.ReadInt32();
                tally.Doorful = reader.ReadInt32();
                tally.Workshop = reader.ReadInt32();
                tally.Kept = reader.ReadInt32();
            }

            var count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
            {
                var b = new Building
                {
                    Town = reader.ReadString(),
                    Heart = ReadPoint(reader),
                    Area = reader.ReadInt32(),
                    Roofed = reader.ReadInt32() / 1000.0,
                    Box = new Rectangle2D(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32())
                };

                var tiles = reader.ReadInt32();

                b.Tiles = new HashSet<int>(tiles);

                for (var t = 0; t < tiles; t++)
                {
                    b.Tiles.Add(reader.ReadInt32());
                }

                var ways = reader.ReadInt32();

                b.Entrances = new Entrance[ways];

                for (var w = 0; w < ways; w++)
                {
                    var e = new Entrance { AlongX = reader.ReadBoolean() };
                    var doors = reader.ReadInt32();

                    e.Doors = new Point3D[doors];

                    for (var d = 0; d < doors; d++)
                    {
                        e.Doors[d] = ReadPoint(reader);
                    }

                    e.Outside = ReadPoint(reader);
                    e.OutX = reader.ReadInt32();
                    e.OutY = reader.ReadInt32();
                    b.Entrances[w] = e;
                }

                Keep(b);
            }

            return true;
        }
        catch (Exception e)
        {
            logger.Warning(e, "Guild houses: {Path} could not be read; surveying again", CachePath);
            _buildings.Clear();
            _byDoor.Clear();
            _tallies.Clear();

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
            writer.Write(MinArea);
            writer.Write(MaxArea);
            writer.Write((int)Math.Round(RoofShare * 1000));
            writer.Write(MostEntrances);

            writer.Write(_tallies.Count);

            foreach (var (name, tally) in _tallies)
            {
                writer.Write(name);
                writer.Write(tally.Doorways);
                writer.Write(tally.WayOut);
                writer.Write(tally.Small);
                writer.Write(tally.Large);
                writer.Write(tally.Roofless);
                writer.Write(tally.Doorful);
                writer.Write(tally.Workshop);
                writer.Write(tally.Kept);
            }

            writer.Write(_buildings.Count);

            foreach (var b in _buildings)
            {
                writer.Write(b.Town);
                WritePoint(writer, b.Heart);
                writer.Write(b.Area);
                writer.Write((int)Math.Round(b.Roofed * 1000));
                writer.Write(b.Box.X);
                writer.Write(b.Box.Y);
                writer.Write(b.Box.Width);
                writer.Write(b.Box.Height);
                writer.Write(b.Tiles.Count);

                foreach (var key in b.Tiles)
                {
                    writer.Write(key);
                }

                writer.Write(b.Entrances.Length);

                foreach (var e in b.Entrances)
                {
                    writer.Write(e.AlongX);
                    writer.Write(e.Doors.Length);

                    foreach (var d in e.Doors)
                    {
                        WritePoint(writer, d);
                    }

                    WritePoint(writer, e.Outside);
                    writer.Write(e.OutX);
                    writer.Write(e.OutY);
                }
            }
        }
        catch (Exception e)
        {
            logger.Warning(e, "Guild houses: the survey could not be written to {Path}; the next boot surveys again", CachePath);
        }
    }

    private static Point3D ReadPoint(BinaryReader reader) => new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

    private static void WritePoint(BinaryWriter writer, Point3D p)
    {
        writer.Write(p.X);
        writer.Write(p.Y);
        writer.Write(p.Z);
    }

    public static string Describe() =>
        Ready
            ? $"{_buildings.Count} buildings surveyed{(FromFile ? " (from the file)" : "")}"
            : _stage switch
            {
                0 => "the towns have not been surveyed",
                1 => $"surveying the towns: {_town} of {BotTowns.All.Count} scanned for doorways",
                2 => $"surveying the towns: {_next} of {_doorways?.Count ?? 0} doorways looked through",
                _ => "surveying the towns"
            };

    public static void Forget()
    {
        Ready = false;
        FromFile = false;
        _stage = 0;
        _map = null;
        _buildings.Clear();
        _byDoor.Clear();
        _tallies.Clear();
        SpentMs = 0;
        WorstMs = 0;
        Slices = 0;
        Drop();
    }
}
