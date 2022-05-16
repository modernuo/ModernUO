using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Items;
using Server.Logging;
using Server.Network;
using Server.Targeting;

namespace Server;

[Flags]
public enum MapRules
{
    None = 0x0000,
    Internal = 0x0001,               // Internal map (used for dragging, commodity deeds, etc)
    FreeMovement = 0x0002,           // Anyone can move over anyone else without taking stamina loss
    BeneficialRestrictions = 0x0004, // Disallow performing beneficial actions on criminals/murderers
    HarmfulRestrictions = 0x0008,    // Disallow performing harmful actions on innocents
    TrammelRules = FreeMovement | BeneficialRestrictions | HarmfulRestrictions,
    FeluccaRules = None
}

public interface IPooledEnumerable : IEnumerable
{
    void Free();
}

public interface IPooledEnumerable<T> : IPooledEnumerable, IEnumerable<T>
{
}

public static class PooledEnumeration
{
    public delegate IEnumerable<T> Selector<out T>(Sector sector, Rectangle2D bounds);

    static PooledEnumeration()
    {
        ClientSelector = SelectClients;
        EntitySelector = SelectEntities;
        MobileSelector = SelectMobiles<Mobile>;
        ItemSelector = SelectItems<Item>;
        MultiSelector = SelectMultis;
        MultiTileSelector = SelectMultiTiles;
    }

    public static Selector<NetState> ClientSelector { get; set; }
    public static Selector<IEntity> EntitySelector { get; set; }
    public static Selector<Mobile> MobileSelector { get; set; }
    public static Selector<Item> ItemSelector { get; set; }
    public static Selector<BaseMulti> MultiSelector { get; set; }
    public static Selector<StaticTile[]> MultiTileSelector { get; set; }

    public static IEnumerable<NetState> SelectClients(Sector s, Rectangle2D bounds)
    {
        var clients = new List<NetState>(s.Clients.Count);
        foreach (var client in s.Clients)
        {
            var m = client.Mobile;

            if (m?.Deleted == false && bounds.Contains(m.Location))
            {
                clients.Add(client);
            }
        }

        return clients;
    }

    public static IEnumerable<IEntity> SelectEntities(Sector s, Rectangle2D bounds)
    {
        var entities = new List<IEntity>(s.Mobiles.Count + s.Items.Count);
        for (int i = s.Mobiles.Count - 1, j = s.Items.Count - 1; i >= 0 || j >= 0; --i, --j)
        {
            if (j >= 0)
            {
                Item item = s.Items[j];
                if (item is { Deleted: false, Parent: null } && bounds.Contains(item.Location))
                {
                    entities.Add(item);
                }
            }

            if (i >= 0)
            {
                Mobile mob = s.Mobiles[i];
                if (mob is { Deleted: false } && bounds.Contains(mob.Location))
                {
                    entities.Add(mob);
                }
            }
        }
        return entities;
    }

    public static IEnumerable<T> SelectMobiles<T>(Sector s, Rectangle2D bounds) where T : Mobile
    {
        var entities = new List<T>(s.Mobiles.Count);
        for (int i = s.Mobiles.Count - 1; i >= 0; --i)
        {
            if (s.Mobiles[i] is T { Deleted: false } mob && bounds.Contains(mob.Location))
            {
                entities.Add(mob);
            }
        }
        return entities;
    }

    public static IEnumerable<T> SelectItems<T>(Sector s, Rectangle2D bounds) where T : Item
    {
        var entities = new List<T>(s.Items.Count);
        for (int i = s.Items.Count - 1; i >= 0; --i)
        {
            if (s.Items[i] is T { Deleted: false, Parent: null } item && bounds.Contains(item.Location))
            {
                entities.Add(item);
            }
        }
        return entities;
    }

    public static IEnumerable<BaseMulti> SelectMultis(Sector s, Rectangle2D bounds)
    {
        var entities = new List<BaseMulti>(s.Multis.Count);
        for (int i = s.Multis.Count - 1; i >= 0; --i)
        {
            BaseMulti multi = s.Multis[i];
            if (multi is { Deleted: false } && bounds.Contains(multi.Location))
            {
                entities.Add(multi);
            }
        }
        return entities;
    }

    public static IEnumerable<StaticTile[]> SelectMultiTiles(Sector s, Rectangle2D bounds)
    {
        for (int l = s.Multis.Count - 1; l >= 0; --l)
        {
            BaseMulti o = s.Multis[l];
            if (o?.Deleted != false)
            {
                continue;
            }

            MultiComponentList c = o.Components;

            int x, y, xo, yo;
            StaticTile[] t, r;

            for (x = bounds.Start.X; x < bounds.End.X; x++)
            {
                xo = x - (o.X + c.Min.X);

                if (xo < 0 || xo >= c.Width)
                {
                    continue;
                }

                for (y = bounds.Start.Y; y < bounds.End.Y; y++)
                {
                    yo = y - (o.Y + c.Min.Y);

                    if (yo < 0 || yo >= c.Height)
                    {
                        continue;
                    }

                    t = c.Tiles[xo][yo];

                    if (t.Length <= 0)
                    {
                        continue;
                    }

                    r = new StaticTile[t.Length];

                    for (var i = 0; i < t.Length; i++)
                    {
                        r[i] = t[i];
                        r[i].Z += o.Z;
                    }

                    yield return r;
                }
            }
        }
    }

    public static Map.PooledEnumerable<NetState> GetClients(Map map, Rectangle2D bounds) =>
        Map.PooledEnumerable<NetState>.Instantiate(map, bounds, ClientSelector ?? SelectClients);

    public static Map.PooledEnumerable<IEntity> GetEntities(Map map, Rectangle2D bounds) =>
        Map.PooledEnumerable<IEntity>.Instantiate(map, bounds, EntitySelector ?? SelectEntities);

    public static Map.PooledEnumerable<Mobile> GetMobiles(Map map, Rectangle2D bounds) =>
        GetMobiles<Mobile>(map, bounds);

    public static Map.PooledEnumerable<T> GetMobiles<T>(Map map, Rectangle2D bounds) where T : Mobile =>
        Map.PooledEnumerable<T>.Instantiate(map, bounds, SelectMobiles<T>);

    public static Map.PooledEnumerable<T> GetItems<T>(Map map, Rectangle2D bounds) where T : Item =>
        Map.PooledEnumerable<T>.Instantiate(map, bounds, SelectItems<T>);

    public static Map.PooledEnumerable<BaseMulti> GetMultis(Map map, Rectangle2D bounds) =>
        Map.PooledEnumerable<BaseMulti>.Instantiate(map, bounds, MultiSelector ?? SelectMultis);

    public static Map.PooledEnumerable<StaticTile[]> GetMultiTiles(Map map, Rectangle2D bounds) =>
        Map.PooledEnumerable<StaticTile[]>.Instantiate(map, bounds, MultiTileSelector ?? SelectMultiTiles);

    public static IEnumerable<Sector> EnumerateSectors(Map map, Rectangle2D bounds)
    {
        if (map == null || map == Map.Internal)
        {
            yield break;
        }

        var x1 = bounds.Start.X;
        var y1 = bounds.Start.Y;
        var x2 = bounds.End.X;
        var y2 = bounds.End.Y;

        if (!Bound(map, ref x1, ref y1, ref x2, ref y2, out var xSector, out var ySector))
        {
            yield break;
        }

        var index = 0;

        while (NextSector(map, x1, y1, x2, y2, ref index, ref xSector, ref ySector, out var s))
        {
            yield return s;
        }
    }

    public static bool Bound(
        Map map,
        ref int x1,
        ref int y1,
        ref int x2,
        ref int y2,
        out int xSector,
        out int ySector
    )
    {
        if (map == null || map == Map.Internal)
        {
            xSector = ySector = 0;
            return false;
        }

        map.Bound(x1, y1, out x1, out y1);
        map.Bound(x2 - 1, y2 - 1, out x2, out y2);

        x1 >>= Map.SectorShift;
        y1 >>= Map.SectorShift;
        x2 >>= Map.SectorShift;
        y2 >>= Map.SectorShift;

        xSector = x1;
        ySector = y1;

        return true;
    }

    private static bool NextSector(
        Map map,
        int x1,
        int y1,
        int x2,
        int y2,
        ref int index,
        ref int xSector,
        ref int ySector,
        out Sector s
    )
    {
        if (map == null)
        {
            s = null;
            xSector = ySector = 0;
            return false;
        }

        if (map == Map.Internal)
        {
            s = map.InvalidSector;
            xSector = ySector = 0;
            return false;
        }

        if (index++ > 0)
        {
            if (++ySector > y2)
            {
                ySector = y1;

                if (++xSector > x2)
                {
                    xSector = x1;

                    s = map.InvalidSector;
                    return false;
                }
            }
        }

        s = map.GetRealSector(xSector, ySector);
        return true;
    }
}

[Parsable]
public sealed class Map : IComparable<Map>
{
    public const int SectorSize = 16;
    public const int SectorShift = 4;
    public const int SectorActiveRange = 2;

    private static ILogger _logger;
    private static ILogger Logger => _logger ??= LogFactory.GetLogger(typeof(Map));

    private readonly int m_FileIndex;
    private readonly Sector[][] m_Sectors;
    private readonly int m_SectorsHeight;

    private readonly int m_SectorsWidth;

    private readonly object tileLock = new();
    private Region m_DefaultRegion;

    private string m_Name;

    private TileMatrix m_Tiles;

    public Map(int mapID, int mapIndex, int fileIndex, int width, int height, int season, string name, MapRules rules)
    {
        MapID = mapID;
        MapIndex = mapIndex;
        m_FileIndex = fileIndex;
        Width = width;
        Height = height;
        Season = season;
        m_Name = name;
        Rules = rules;
        Regions = new Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase);
        InvalidSector = new Sector(0, 0, this);
        m_SectorsWidth = width >> SectorShift;
        m_SectorsHeight = height >> SectorShift;
        m_Sectors = new Sector[m_SectorsWidth][];
    }

    public static Map[] Maps { get; } = new Map[0x100];

    public static Map Felucca => Maps[0];
    public static Map Trammel => Maps[1];
    public static Map Ilshenar => Maps[2];
    public static Map Malas => Maps[3];
    public static Map Tokuno => Maps[4];
    public static Map TerMur => Maps[5];
    public static Map Internal => Maps[0x7F];

    public static List<Map> AllMaps { get; } = new();

    public int Season { get; set; }

    public TileMatrix Tiles
    {
        get
        {
            if (m_Tiles == null)
            {
                lock (tileLock)
                {
                    m_Tiles = new TileMatrix(this, m_FileIndex, MapID, Width, Height);
                }
            }

            return m_Tiles;
        }
    }

    public int MapID { get; }

    public int MapIndex { get; }

    public int Width { get; }

    public int Height { get; }

    public Dictionary<string, Region> Regions { get; }

    public Region DefaultRegion
    {
        get => m_DefaultRegion ??= new Region(null, this, 0, Array.Empty<Rectangle3D>());
        set => m_DefaultRegion = value;
    }

    public MapRules Rules { get; set; }

    public Sector InvalidSector { get; }

    public string Name
    {
        get
        {
            if (this == Internal && m_Name != "Internal")
            {
                Logger.Warning($"Internal map name was '{m_Name}'\n{new StackTrace()}");
                m_Name = "Internal";
            }

            return m_Name;
        }
        set
        {
            if (this == Internal && value != "Internal")
            {
                Logger.Warning($"Attempted to set internal map name to '{value}'\n{new StackTrace()}");

                value = "Internal";
            }

            m_Name = value;
        }
    }

    public static int[] InvalidLandTiles { get; set; } = { 0x244 };

    public static int MaxLOSDistance { get; set; } = 25;

    public int CompareTo(Map other) => other == null ? -1 : MapID.CompareTo(other.MapID);

    public static string[] GetMapNames()
    {
        var mapCount = 0;
        for (var i = 0; i < Maps.Length; i++)
        {
            var map = Maps[i];
            if (map != null)
            {
                mapCount++;
            }
        }

        var mapNames = new string[mapCount];
        for (int i = 0, mIndex = 0; i < Maps.Length; i++)
        {
            var map = Maps[i];
            if (map != null)
            {
                mapNames[mIndex++] = map.Name;
            }
        }

        return mapNames;
    }

    public static Map[] GetMapValues()
    {
        var mapCount = 0;
        for (var i = 0; i < Maps.Length; i++)
        {
            var map = Maps[i];
            if (map != null)
            {
                mapCount++;
            }
        }

        var mapValues = new Map[mapCount];
        for (int i = 0, mIndex = 0; i < Maps.Length; i++)
        {
            var map = Maps[i];
            if (map != null)
            {
                mapValues[mIndex++] = map;
            }
        }

        return mapValues;
    }

    // Handles null checks
    public static Map Parse(string value) => Parse(value ?? ReadOnlySpan<char>.Empty);

    public static Map Parse(ReadOnlySpan<char> value)
    {
        value = value.Trim();

        if (value.Length == 0)
        {
            return null;
        }

        if (value.InsensitiveEquals("Internal"))
        {
            return Internal;
        }

        if (!int.TryParse(value, out var index))
        {
            index = -1;
        }
        else if (index == 127)
        {
            return Internal;
        }

        for (int i = 0; i < Maps.Length; i++)
        {
            var map = Maps[i];
            if (map == null)
            {
                continue;
            }

            if (index >= 0 && map.MapIndex == index || value.InsensitiveEquals(map.Name))
            {
                return map;
            }
        }

        return null;
    }

    public override string ToString() => Name;

    public int GetAverageZ(int x, int y)
    {
        GetAverageZ(x, y, out _, out var avg, out _);
        return avg;
    }

    public void GetAverageZ(int x, int y, out int z, out int avg, out int top)
    {
        var zTop = Tiles.GetLandTile(x, y).Z;
        var zLeft = Tiles.GetLandTile(x, y + 1).Z;
        var zRight = Tiles.GetLandTile(x + 1, y).Z;
        var zBottom = Tiles.GetLandTile(x + 1, y + 1).Z;

        z = zTop;
        if (zLeft < z)
        {
            z = zLeft;
        }

        if (zRight < z)
        {
            z = zRight;
        }

        if (zBottom < z)
        {
            z = zBottom;
        }

        top = zTop;
        if (zLeft > top)
        {
            top = zLeft;
        }

        if (zRight > top)
        {
            top = zRight;
        }

        if (zBottom > top)
        {
            top = zBottom;
        }

        avg = (zTop - zBottom).Abs() > (zLeft - zRight).Abs()
            ? FloorAverage(zLeft, zRight)
            : FloorAverage(zTop, zBottom);
    }

    private static int FloorAverage(int a, int b)
    {
        var v = a + b;

        if (v < 0)
        {
            --v;
        }

        return v / 2;
    }

    public IPooledEnumerable<StaticTile[]> GetMultiTilesAt(int x, int y) =>
        PooledEnumeration.GetMultiTiles(this, new Rectangle2D(x, y, 1, 1));

    private static void AcquireFixItems(Map map, int x, int y, Item[] pool, out int length)
    {
        length = 0;
        if (map == null || map == Internal || x < 0 || x > map.Width || y < 0 || y > map.Height)
        {
            return;
        }

        var eable = map.GetItemsInRange(new Point3D(x, y, 0), 0);
        foreach (var item in eable)
        {
            if (item is not BaseMulti && item.ItemID <= TileData.MaxItemValue)
            {
                if (length == 128)
                {
                    break;
                }

                pool[length++] = item;
            }
        }

        eable.Free();

        Array.Sort(pool, 0, length, ZComparer.Default);
    }

    public void FixColumn(int x, int y)
    {
        var landTile = Tiles.GetLandTile(x, y);
        var tiles = Tiles.GetStaticTiles(x, y, true);

        GetAverageZ(x, y, out _, out var landAvg, out _);

        var items = STArrayPool<Item>.Shared.Rent(128);
        AcquireFixItems(this, x, y, items, out var length);

        for (var i = 0; i < length; i++)
        {
            var toFix = items[i];

            if (!toFix.Movable)
            {
                continue;
            }

            var z = int.MinValue;
            var currentZ = toFix.Z;

            if (!landTile.Ignored && landAvg <= currentZ)
            {
                z = landAvg;
            }

            foreach (var tile in tiles)
            {
                var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                var checkZ = tile.Z;
                var checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                {
                    ++checkTop;
                }

                if (checkTop > z && checkTop <= currentZ)
                {
                    z = checkTop;
                }
            }

            for (var j = 0; j < length; ++j)
            {
                if (j == i)
                {
                    continue;
                }

                var item = items[j];
                var id = item.ItemData;

                var checkZ = item.Z;
                var checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                {
                    ++checkTop;
                }

                if (checkTop > z && checkTop <= currentZ)
                {
                    z = checkTop;
                }
            }

            if (z != int.MinValue)
            {
                toFix.Location = new Point3D(toFix.X, toFix.Y, z);
            }
        }

        STArrayPool<Item>.Shared.Return(items, true);
    }

    /* This could probably be re-implemented if necessary (perhaps via an ITile interface?).
    public List<Tile> GetTilesAt( Point2D p, bool items, bool land, bool statics )
    {
      List<Tile> list = new List<Tile>();

      if (this == Internal)
        return list;

      if (land)
        list.Add( Tiles.GetLandTile( p.m_X, p.m_Y ) );

      if (statics)
        list.AddRange( Tiles.GetStaticTiles( p.m_X, p.m_Y, true ) );

      if (items)
      {
        Sector sector = GetSector( p );

        foreach ( Item item in sector.Items )
          if (item.AtWorldPoint( p.m_X, p.m_Y ))
            list.Add( new StaticTile( (ushort)item.ItemID, (sbyte) item.Z ) );
      }

      return list;
    }
    */

    /// <summary>
    ///     Gets the highest surface that is lower than <paramref name="p" />.
    /// </summary>
    /// <param name="p">The reference point.</param>
    /// <returns>A surface <typeparamref name="Tile" /> or <typeparamref name="Item" />.</returns>
    public object GetTopSurface(Point3D p)
    {
        if (this == Internal)
        {
            return null;
        }

        object surface = null;
        var surfaceZ = int.MinValue;

        var lt = Tiles.GetLandTile(p.X, p.Y);

        if (!lt.Ignored)
        {
            var avgZ = GetAverageZ(p.X, p.Y);

            if (avgZ <= p.Z)
            {
                surface = lt;
                surfaceZ = avgZ;

                if (surfaceZ == p.Z)
                {
                    return surface;
                }
            }
        }

        var staticTiles = Tiles.GetStaticTiles(p.X, p.Y, true);

        for (var i = 0; i < staticTiles.Length; i++)
        {
            var tile = staticTiles[i];
            var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            if (id.Surface || id.Wet)
            {
                var tileZ = tile.Z + id.CalcHeight;

                if (tileZ > surfaceZ && tileZ <= p.Z)
                {
                    surface = tile;
                    surfaceZ = tileZ;

                    if (surfaceZ == p.Z)
                    {
                        return surface;
                    }
                }
            }
        }

        var sector = GetSector(p.X, p.Y);

        for (var i = 0; i < sector.Items.Count; i++)
        {
            var item = sector.Items[i];

            if (item is not BaseMulti && item.ItemID <= TileData.MaxItemValue && item.AtWorldPoint(p.X, p.Y) &&
                !item.Movable)
            {
                var id = item.ItemData;

                if (id.Surface || id.Wet)
                {
                    var itemZ = item.Z + id.CalcHeight;

                    if (itemZ > surfaceZ && itemZ <= p.Z)
                    {
                        surface = item;
                        surfaceZ = itemZ;

                        if (surfaceZ == p.Z)
                        {
                            return surface;
                        }
                    }
                }
            }
        }

        return surface;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bound(int x, int y, out int newX, out int newY)
    {
        newX = Math.Clamp(x, 0, Width - 1);
        newY = Math.Clamp(y, 0, Height - 1);
    }

    public Point2D Bound(Point3D p)
    {
        Bound(p.m_X, p.m_Y, out var x, out var y);
        return new Point2D(x, y);
    }

    public Point2D Bound(Point2D p)
    {
        Bound(p.m_X, p.m_Y, out var x, out var y);
        return new Point2D(x, y);
    }

    public void ActivateSectors(int cx, int cy)
    {
        for (var x = cx - SectorActiveRange; x <= cx + SectorActiveRange; ++x)
        {
            for (var y = cy - SectorActiveRange; y <= cy + SectorActiveRange; ++y)
            {
                var sect = GetRealSector(x, y);
                if (sect != InvalidSector)
                {
                    sect.Activate();
                }
            }
        }
    }

    public void DeactivateSectors(int cx, int cy)
    {
        for (var x = cx - SectorActiveRange; x <= cx + SectorActiveRange; ++x)
        {
            for (var y = cy - SectorActiveRange; y <= cy + SectorActiveRange; ++y)
            {
                var sect = GetRealSector(x, y);
                if (sect != InvalidSector && !PlayersInRange(sect, SectorActiveRange))
                {
                    sect.Deactivate();
                }
            }
        }
    }

    private bool PlayersInRange(Sector sect, int range)
    {
        for (var x = sect.X - range; x <= sect.X + range; ++x)
        {
            for (var y = sect.Y - range; y <= sect.Y + range; ++y)
            {
                var check = GetRealSector(x, y);
                if (check != InvalidSector && check.Clients.Count > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void OnClientChange(NetState oldState, NetState newState, Mobile m)
    {
        if (this != Internal)
        {
            GetSector(m.Location).OnClientChange(oldState, newState);
        }
    }

    public void OnEnter(Mobile m)
    {
        if (this != Internal)
        {
            GetSector(m.Location).OnEnter(m);
        }
    }

    public void OnEnter(Item item)
    {
        if (this == Internal)
        {
            return;
        }

        GetSector(item.Location).OnEnter(item);

        if (item is BaseMulti m)
        {
            var mcl = m.Components;

            var start = GetMultiMinSector(m.Location, mcl);
            var end = GetMultiMaxSector(m.Location, mcl);

            AddMulti(m, start, end);
        }
    }

    public void OnLeave(Mobile m)
    {
        if (this != Internal)
        {
            GetSector(m.Location).OnLeave(m);
        }
    }

    public void OnLeave(Item item)
    {
        if (this == Internal)
        {
            return;
        }

        GetSector(item.Location).OnLeave(item);

        if (item is BaseMulti m)
        {
            var mcl = m.Components;

            var start = GetMultiMinSector(m.Location, mcl);
            var end = GetMultiMaxSector(m.Location, mcl);

            RemoveMulti(m, start, end);
        }
    }

    public void RemoveMulti(BaseMulti m, Sector start, Sector end)
    {
        if (this == Internal)
        {
            return;
        }

        for (var x = start.X; x <= end.X; ++x)
        {
            for (var y = start.Y; y <= end.Y; ++y)
            {
                InternalGetSector(x, y).OnMultiLeave(m);
            }
        }
    }

    public void AddMulti(BaseMulti m, Sector start, Sector end)
    {
        if (this == Internal)
        {
            return;
        }

        for (var x = start.X; x <= end.X; ++x)
        {
            for (var y = start.Y; y <= end.Y; ++y)
            {
                InternalGetSector(x, y).OnMultiEnter(m);
            }
        }
    }

    public Sector GetMultiMinSector(Point3D loc, MultiComponentList mcl) =>
        GetSector(Bound(new Point2D(loc.m_X + mcl.Min.m_X, loc.m_Y + mcl.Min.m_Y)));

    public Sector GetMultiMaxSector(Point3D loc, MultiComponentList mcl) =>
        GetSector(Bound(new Point2D(loc.m_X + mcl.Max.m_X, loc.m_Y + mcl.Max.m_Y)));

    public void OnMove(Point3D oldLocation, Mobile m)
    {
        if (this == Internal)
        {
            return;
        }

        var oldSector = GetSector(oldLocation);
        var newSector = GetSector(m.Location);

        if (oldSector != newSector)
        {
            oldSector.OnLeave(m);
            newSector.OnEnter(m);
        }
    }

    public void OnMove(Point3D oldLocation, Item item)
    {
        if (this == Internal)
        {
            return;
        }

        var oldSector = GetSector(oldLocation);
        var newSector = GetSector(item.Location);

        if (oldSector != newSector)
        {
            oldSector.OnLeave(item);
            newSector.OnEnter(item);
        }

        if (item is BaseMulti m)
        {
            var mcl = m.Components;

            var start = GetMultiMinSector(m.Location, mcl);
            var end = GetMultiMaxSector(m.Location, mcl);

            var oldStart = GetMultiMinSector(oldLocation, mcl);
            var oldEnd = GetMultiMaxSector(oldLocation, mcl);

            if (oldStart != start || oldEnd != end)
            {
                RemoveMulti(m, oldStart, oldEnd);
                AddMulti(m, start, end);
            }
        }
    }

    public void RegisterRegion(Region reg)
    {
        var regName = reg.Name;

        if (regName == null)
        {
            return;
        }

        if (Regions.ContainsKey(regName))
        {
            Logger.Warning($"Duplicate region name '{regName}' for map '{Name}'");
        }
        else
        {
            Regions[regName] = reg;
        }
    }

    public void UnregisterRegion(Region reg)
    {
        var regName = reg.Name;

        if (regName != null)
        {
            Regions.Remove(regName);
        }
    }

    public Point3D GetPoint(object o, bool eye)
    {
        Point3D p;

        if (o is Mobile mobile)
        {
            p = mobile.Location;
            p.Z += 14; // eye ? 15 : 10;
        }
        else if (o is Item item)
        {
            p = item.GetWorldLocation();
            p.Z += item.ItemData.Height / 2 + 1;
        }
        else if (o is Point3D point3D)
        {
            p = point3D;
        }
        else if (o is LandTarget target)
        {
            p = target.Location;

            GetAverageZ(p.X, p.Y, out _, out _, out var top);

            p.Z = top + 1;
        }
        else if (o is StaticTarget st)
        {
            var id = TileData.ItemTable[st.ItemID & TileData.MaxItemValue];

            p = new Point3D(st.X, st.Y, st.Z - id.CalcHeight + id.Height / 2 + 1);
        }
        else if (o is IPoint3D d)
        {
            p = new Point3D(d.X, d.Y, d.Z);
        }
        else
        {
            Logger.Warning($"Warning: Invalid object ({o}) in line of sight");
            p = Point3D.Zero;
        }

        return p;
    }

    public IPooledEnumerable<IEntity> GetObjectsInRange(Point3D p) => GetObjectsInRange(p, Core.GlobalMaxUpdateRange);

    public IPooledEnumerable<IEntity> GetObjectsInRange(Point3D p, int range) =>
        GetObjectsInBounds(new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1));

    public IPooledEnumerable<IEntity> GetObjectsInBounds(Rectangle2D bounds) =>
        PooledEnumeration.GetEntities(this, bounds);

    public IPooledEnumerable<NetState> GetClientsInRange(Point3D p) => GetClientsInRange(p, Core.GlobalMaxUpdateRange);

    public IPooledEnumerable<NetState> GetClientsInRange(Point3D p, int range) =>
        GetClientsInBounds(new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1));

    public IPooledEnumerable<NetState> GetClientsInBounds(Rectangle2D bounds) =>
        PooledEnumeration.GetClients(this, bounds);

    public IPooledEnumerable<Item> GetItemsInRange(Point3D p) => GetItemsInRange(p, Core.GlobalMaxUpdateRange);

    public IPooledEnumerable<Item> GetItemsInRange(Point3D p, int range) => GetItemsInRange<Item>(p, range);

    public IPooledEnumerable<T> GetItemsInRange<T>(Point3D p, int range) where T : Item =>
        GetItemsInBounds<T>(new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1));

    public IPooledEnumerable<Item> GetItemsInBounds(Rectangle2D bounds) => GetItemsInBounds<Item>(bounds);

    public IPooledEnumerable<T> GetItemsInBounds<T>(Rectangle2D bounds) where T : Item =>
        PooledEnumeration.GetItems<T>(this, bounds);

    public IPooledEnumerable<Mobile> GetMobilesInRange(Point3D p) => GetMobilesInRange(p, Core.GlobalMaxUpdateRange);

    public IPooledEnumerable<Mobile> GetMobilesInRange(Point3D p, int range) => GetMobilesInRange<Mobile>(p, range);

    public IPooledEnumerable<T> GetMobilesInRange<T>(Point3D p, int range) where T : Mobile =>
        GetMobilesInBounds<T>(new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1));

    public IPooledEnumerable<Mobile> GetMobilesInBounds(Rectangle2D bounds) => GetMobilesInBounds<Mobile>(bounds);

    public IPooledEnumerable<T> GetMobilesInBounds<T>(Rectangle2D bounds) where T : Mobile =>
        PooledEnumeration.GetMobiles<T>(this, bounds);

    public bool CanFit(
        Point3D p, int height, bool checkBlocksFit = false, bool checkMobiles = true,
        bool requireSurface = true
    ) =>
        CanFit(p.m_X, p.m_Y, p.m_Z, height, checkBlocksFit, checkMobiles, requireSurface);

    public bool CanFit(
        Point2D p, int z, int height, bool checkBlocksFit = false, bool checkMobiles = true,
        bool requireSurface = true
    ) =>
        CanFit(p.m_X, p.m_Y, z, height, checkBlocksFit, checkMobiles, requireSurface);

    public bool CanFit(
        int x, int y, int z, int height, bool checkBlocksFit = false, bool checkMobiles = true,
        bool requireSurface = true
    )
    {
        if (this == Internal)
        {
            return false;
        }

        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return false;
        }

        var hasSurface = false;

        var lt = Tiles.GetLandTile(x, y);
        GetAverageZ(x, y, out var lowZ, out var avgZ, out _);
        var landFlags = TileData.LandTable[lt.ID & TileData.MaxLandValue].Flags;

        if ((landFlags & TileFlag.Impassable) != 0 && avgZ > z && z + height > lowZ)
        {
            return false;
        }

        if ((landFlags & TileFlag.Impassable) == 0 && z == avgZ && !lt.Ignored)
        {
            hasSurface = true;
        }

        var staticTiles = Tiles.GetStaticTiles(x, y, true);

        bool surface, impassable;

        for (var i = 0; i < staticTiles.Length; ++i)
        {
            var id = TileData.ItemTable[staticTiles[i].ID & TileData.MaxItemValue];
            surface = id.Surface;
            impassable = id.Impassable;

            if ((surface || impassable) && staticTiles[i].Z + id.CalcHeight > z && z + height > staticTiles[i].Z)
            {
                return false;
            }

            if (surface && !impassable && z == staticTiles[i].Z + id.CalcHeight)
            {
                hasSurface = true;
            }
        }

        var sector = GetSector(x, y);
        var items = sector.Items;
        var mobs = sector.Mobiles;

        for (var i = 0; i < items.Count; ++i)
        {
            var item = items[i];

            if (item is not BaseMulti && item.ItemID <= TileData.MaxItemValue && item.AtWorldPoint(x, y))
            {
                var id = item.ItemData;
                surface = id.Surface;
                impassable = id.Impassable;

                if ((surface || impassable || checkBlocksFit && item.BlocksFit) && item.Z + id.CalcHeight > z &&
                    z + height > item.Z)
                {
                    return false;
                }

                if (surface && !impassable && !item.Movable && z == item.Z + id.CalcHeight)
                {
                    hasSurface = true;
                }
            }
        }

        if (checkMobiles)
        {
            for (var i = 0; i < mobs.Count; ++i)
            {
                var m = mobs[i];

                if (m.Location.m_X == x && m.Location.m_Y == y && (m.AccessLevel == AccessLevel.Player || !m.Hidden) &&
                    m.Z + 16 > z && z + height > m.Z)
                {
                    return false;
                }
            }
        }

        return !requireSurface || hasSurface;
    }

    public bool CanSpawnMobile(Point3D p) => CanSpawnMobile(p.m_X, p.m_Y, p.m_Z);

    public bool CanSpawnMobile(Point2D p, int z) => CanSpawnMobile(p.m_X, p.m_Y, z);

    public bool CanSpawnMobile(int x, int y, int z) =>
        Region.Find(new Point3D(x, y, z), this).AllowSpawn() && CanFit(x, y, z, 16);

    private class ZComparer : IComparer<Item>
    {
        public static readonly ZComparer Default = new();

        public int Compare(Item x, Item y) => x!.Z.CompareTo(y!.Z);
    }

    public Sector GetSector(Point3D p) => InternalGetSector(p.m_X >> SectorShift, p.m_Y >> SectorShift);

    public Sector GetSector(Point2D p) => InternalGetSector(p.m_X >> SectorShift, p.m_Y >> SectorShift);

    // public Sector GetSector(IPoint2D p) => InternalGetSector(p.X >> SectorShift, p.Y >> SectorShift);

    public Sector GetSector(int x, int y) => InternalGetSector(x >> SectorShift, y >> SectorShift);

    public Sector GetRealSector(int x, int y) => InternalGetSector(x, y);

    private Sector InternalGetSector(int x, int y)
    {
        if (x >= 0 && x < m_SectorsWidth && y >= 0 && y < m_SectorsHeight)
        {
            var xSectors = m_Sectors[x];

            if (xSectors == null)
            {
                m_Sectors[x] = xSectors = new Sector[m_SectorsHeight];
            }

            var sec = xSectors[y];

            if (sec == null)
            {
                xSectors[y] = sec = new Sector(x, y, this);
            }

            return sec;
        }

        return InvalidSector;
    }

    public bool LineOfSight(Point3D org, Point3D dest)
    {
        if (this == Internal)
        {
            return false;
        }

        if (!Utility.InRange(org, dest, MaxLOSDistance))
        {
            return false;
        }

        var end = dest;

        if (org.X > dest.X || org.X == dest.X && org.Y > dest.Y || org.X == dest.X && org.Y == dest.Y && org.Z > dest.Z)
        {
            (org, dest) = (dest, org);
        }

        int height;
        Point3D p;
        var path = new Point3DList();
        TileFlag flags;

        if (org == dest)
        {
            return true;
        }

        if (path.Count > 0)
        {
            path.Clear();
        }

        var xd = dest.m_X - org.m_X;
        var yd = dest.m_Y - org.m_Y;
        var zd = dest.m_Z - org.m_Z;
        var zslp = Math.Sqrt(xd * xd + yd * yd);
        var sq3d = zd != 0 ? Math.Sqrt(zslp * zslp + zd * zd) : zslp;

        var rise = yd / sq3d;
        var run = xd / sq3d;
        zslp = zd / sq3d;

        double y = org.m_Y;
        double z = org.m_Z;
        double x = org.m_X;
        while (Utility.NumberBetween(x, dest.m_X, org.m_X, 0.5) && Utility.NumberBetween(y, dest.m_Y, org.m_Y, 0.5) &&
               Utility.NumberBetween(z, dest.m_Z, org.m_Z, 0.5))
        {
            var ix = (int)Math.Round(x);
            var iy = (int)Math.Round(y);
            var iz = (int)Math.Round(z);
            if (path.Count > 0)
            {
                p = path.Last;

                if (p.m_X != ix || p.m_Y != iy || p.m_Z != iz)
                {
                    path.Add(ix, iy, iz);
                }
            }
            else
            {
                path.Add(ix, iy, iz);
            }

            x += run;
            y += rise;
            z += zslp;
        }

        if (path.Count == 0)
        {
            return true; // <--should never happen, but to be safe.
        }

        p = path.Last;

        if (p != dest)
        {
            path.Add(dest);
        }

        Point3D pTop = org, pBottom = dest;
        Utility.FixPoints(ref pTop, ref pBottom);

        var pathCount = path.Count;
        var endTop = end.m_Z + 1;

        for (var i = 0; i < pathCount; ++i)
        {
            var point = path[i];
            var pointTop = point.m_Z + 1;

            var landTile = Tiles.GetLandTile(point.X, point.Y);
            GetAverageZ(point.m_X, point.m_Y, out var landZ, out _, out var landTop);

            if (landZ <= pointTop && landTop >= point.m_Z &&
                (point.m_X != end.m_X || point.m_Y != end.m_Y || landZ > endTop || landTop < end.m_Z) &&
                !landTile.Ignored)
            {
                return false;
            }

            /* --Do land tiles need to be checked?  There is never land between two people, always statics.--
            LandTile landTile = Tiles.GetLandTile( point.X, point.Y );
            if (landTile.Z-1 >= point.Z && landTile.Z+1 <= point.Z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) != 0)
              return false;
            */

            var statics = Tiles.GetStaticTiles(point.m_X, point.m_Y, true);

            var contains = false;
            var ltID = landTile.ID;

            for (var j = 0; !contains && j < InvalidLandTiles.Length; ++j)
            {
                contains = ltID == InvalidLandTiles[j];
            }

            if (contains && statics.Length == 0)
            {
                var eable = GetItemsInRange(point, 0);

                foreach (Item item in eable)
                {
                    if (item.Visible)
                    {
                        contains = false;
                        break;
                    }
                }

                eable.Free();

                if (contains)
                {
                    return false;
                }
            }

            for (var j = 0; j < statics.Length; ++j)
            {
                var t = statics[j];

                var id = TileData.ItemTable[t.ID & TileData.MaxItemValue];

                flags = id.Flags;
                height = id.CalcHeight;

                if (t.Z <= pointTop && t.Z + height >= point.Z && (flags & (TileFlag.Window | TileFlag.NoShoot)) != 0)
                {
                    if (point.m_X == end.m_X && point.m_Y == end.m_Y && t.Z <= endTop && t.Z + height >= end.m_Z)
                    {
                        continue;
                    }

                    return false;
                }
            }
        }

        var rect = new Rectangle2D(pTop.m_X, pTop.m_Y, pBottom.m_X - pTop.m_X + 1, pBottom.m_Y - pTop.m_Y + 1);

        var area = GetItemsInBounds(rect);

        foreach (var i in area)
        {
            if (!i.Visible)
            {
                continue;
            }

            if (i is BaseMulti || i.ItemID > TileData.MaxItemValue)
            {
                continue;
            }

            var id = i.ItemData;
            flags = id.Flags;

            if ((flags & (TileFlag.Window | TileFlag.NoShoot)) == 0)
            {
                continue;
            }

            height = id.CalcHeight;

            var found = false;

            var count = path.Count;

            for (var j = 0; j < count; ++j)
            {
                var point = path[j];
                var pointTop = point.m_Z + 1;
                var loc = i.Location;

                // if (t.Z <= point.Z && t.Z+height >= point.Z && ( height != 0 || ( t.Z == dest.Z && zd != 0 ) ))
                if (loc.m_X == point.m_X && loc.m_Y == point.m_Y && loc.m_Z <= pointTop && loc.m_Z + height >= point.m_Z)
                {
                    if (loc.m_X != end.m_X || loc.m_Y != end.m_Y || loc.m_Z > endTop || loc.m_Z + height < end.m_Z)
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                continue;
            }

            area.Free();
            return false;
        }

        area.Free();
        return true;
    }

    public bool LineOfSight(object from, object dest) =>
        from == dest || (from as Mobile)?.AccessLevel > AccessLevel.Player ||
        (dest as Item)?.RootParent == from || LineOfSight(GetPoint(from, true), GetPoint(dest, false));

    public bool LineOfSight(Mobile from, Point3D target)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        var eye = from.Location;

        eye.Z += 14;

        return LineOfSight(eye, target);
    }

    public bool LineOfSight(Mobile from, Mobile to)
    {
        if (from == to || from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        var eye = from.Location;
        var target = to.Location;

        eye.Z += 14;
        target.Z += 14; // 10;

        return LineOfSight(eye, target);
    }

    public Point3D GetRandomNearbyLocation(
        Point3D loc, int maxRange = 2, int minRange = 0, int retryCount = 10,
        int height = 16, bool checkBlocksFit = false,
        bool checkMobiles = false
    )
    {
        var j = 0;
        var range = maxRange - minRange;
        var locs = range <= 10 ? new bool[range + 1, range + 1] : null;

        do
        {
            var xRand = Utility.Random(range);
            var yRand = Utility.Random(range);

            if (locs?[xRand, yRand] != true)
            {
                var x = loc.X + xRand + minRange;
                var y = loc.Y + yRand + minRange;

                if (CanFit(x, y, loc.Z, height, checkBlocksFit, checkMobiles))
                {
                    loc = new Point3D(x, y, loc.Z);
                    break;
                }

                var z = GetAverageZ(x, y);

                if (CanFit(x, y, z, height, checkBlocksFit, checkMobiles))
                {
                    loc = new Point3D(x, y, z);
                    break;
                }

                if (locs != null)
                {
                    locs[xRand, yRand] = true;
                }
            }

            j++;
        } while (j < retryCount);

        return loc;
    }

    public class NullEnumerable<T> : IPooledEnumerable<T>
    {
        public static readonly NullEnumerable<T> Instance = new();

        private readonly IEnumerable<T> m_Empty = Enumerable.Empty<T>();

        IEnumerator IEnumerable.GetEnumerator() => m_Empty.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => m_Empty.GetEnumerator();

        public void Free()
        {
        }
    }

    public sealed class PooledEnumerable<T> : IPooledEnumerable<T>, IDisposable
    {
        private static readonly Queue<PooledEnumerable<T>> _Buffer = new(0x400);

        private bool m_IsDisposed;

        private List<T> m_Pool = new(0x40);

        public PooledEnumerable(IEnumerable<T> pool)
        {
            m_Pool.AddRange(pool);
        }

        public void Dispose()
        {
            m_IsDisposed = true;

            m_Pool.Clear();
            m_Pool.TrimExcess();
            m_Pool = null;
        }

        IEnumerator IEnumerable.GetEnumerator() => m_Pool.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => m_Pool.GetEnumerator();

        public void Free()
        {
            if (m_IsDisposed)
            {
                return;
            }

            m_Pool.Clear();
            m_Pool.Capacity = Math.Max(m_Pool.Capacity, 0x100);

            lock (((ICollection)_Buffer).SyncRoot)
            {
                _Buffer.Enqueue(this);
            }
        }
#pragma warning disable CA1000 // Do not declare static members on generic types
        public static PooledEnumerable<T> Instantiate(
            Map map, Rectangle2D bounds, PooledEnumeration.Selector<T> selector
        )
        {
            PooledEnumerable<T> e = null;

            lock (((ICollection)_Buffer).SyncRoot)
            {
                if (_Buffer.Count > 0)
                {
                    e = _Buffer.Dequeue();
                }
            }

            var pool = PooledEnumeration.EnumerateSectors(map, bounds).SelectMany(s => selector(s, bounds));

            if (e == null)
            {
                return new PooledEnumerable<T>(pool);
            }

            e.m_Pool.AddRange(pool);
            return e;
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
