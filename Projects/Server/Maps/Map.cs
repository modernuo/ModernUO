/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Map.cs                                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Collections;
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

public sealed partial class Map : IComparable<Map>, ISpanFormattable, ISpanParsable<Map>
{
    public const int SectorSize = 16;
    public const int SectorShift = 4;
    public const int SectorActiveRange = 2;

    private static ILogger logger = LogFactory.GetLogger(typeof(Map));
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
                logger.Warning(
                    $"Internal map name was '{{Name}}'{Environment.NewLine}{{StackTrace}}",
                    m_Name,
                    new StackTrace()
                );
                m_Name = "Internal";
            }

            return m_Name;
        }
        set
        {
            if (this == Internal && value != "Internal")
            {
                logger.Warning(
                    $"Attempted to set internal map name to '{{Value}}'{Environment.NewLine}{{StackTrace}}",
                    value,
                    new StackTrace()
                );
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

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
    {
        if (destination.Length >= Name.Length)
        {
            Name.CopyTo(destination);
            charsWritten = Name.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    public override string ToString() => Name;

    public string ToString(string format, IFormatProvider formatProvider)
    {
        // format and formatProvider are not doing anything right now, so use the
        // default ToString implementation.
        return ToString();
    }

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

    private static void AcquireFixItems(Map map, int x, int y, Item[] pool, out int length)
    {
        length = 0;
        if (map == null || map == Internal || x < 0 || x > map.Width || y < 0 || y > map.Height)
        {
            return;
        }

        var p = new Point3D(x, y, 0);
        foreach (var item in map.GetItemsInRange(p, 0))
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

        foreach (var item in sector.Items)
        {
            if (item is BaseMulti || item.ItemID > TileData.MaxItemValue || !item.AtWorldPoint(p.X, p.Y) ||
                item.Movable)
            {
                continue;
            }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CalculateSectors(
        Rectangle2D bounds,
        out int sectorStartX, out int sectorStartY,
        out int sectorEndX, out int sectorEndY)
    {
        int left = bounds.Start.X;
        int top = bounds.Start.Y;
        int right = bounds.End.X;
        int bottom = bounds.End.Y;

        // Limit the coordinates to inside the valid map region
        Bound(left, top, out left, out top);
        Bound(right - 1, bottom - 1, out right, out bottom);

        // Calculate the top left sector
        sectorStartX = left >> SectorShift;
        sectorStartY = top >> SectorShift;

        // Calculate the bottom right sector.
        sectorEndX = right >> SectorShift;
        sectorEndY = bottom >> SectorShift;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnEnter(Mobile m) => OnEnter(m.Location, m);

    public void OnEnter(Point3D p, Mobile m)
    {
        if (this != Internal)
        {
            GetSector(p).OnEnter(m);
        }
    }

    public void OnEnter(Item item)
    {
        OnEnter(item.Location, item);
    }

    public void OnEnter(Point3D p, Item item)
    {
        if (this == Internal)
        {
            return;
        }

        GetSector(p).OnEnter(item);

        if (item is BaseMulti m)
        {
            var mcl = m.Components;

            var start = GetMultiMinSector(m.Location, mcl);
            var end = GetMultiMaxSector(m.Location, mcl);

            AddMulti(m, start, end);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnLeave(Mobile m) => OnLeave(m.Location, m);

    public void OnLeave(Point3D p, Mobile m)
    {
        if (this != Internal)
        {
            GetSector(p).OnLeave(m);
        }
    }

    public void OnLeave(Item item)
    {
        OnLeave(item.Location, item);
    }

    public void OnLeave(Point3D p, Item item)
    {
        if (this == Internal)
        {
            return;
        }

        GetSector(p).OnLeave(item);

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
            logger.Warning("Duplicate region name '{RegionName}' for map '{MapName}'", regName, Name);
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
            // Calculate the height based on the container, not the item inside.
            var rootParent = item.RootParent;
            if (rootParent != null)
            {
                p = GetPoint(rootParent, eye);
            }
            else
            {
                p = item.GetWorldLocation();
                p.Z += item.ItemData.Height / 2 + 1;
            }
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
            logger.Warning("Warning: Invalid object ({Object}) in line of sight", o);
            p = Point3D.Zero;
        }

        return p;
    }

    public IPooledEnumerable<StaticTile[]> GetMultiTilesAt(int x, int y) =>
        PooledEnumeration.GetMultiTiles(this, new Rectangle2D(x, y, 1, 1));

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
        var mobs = sector.Mobiles;

        foreach (var item in sector.Items)
        {
            if (item is BaseMulti || item.ItemID > TileData.MaxItemValue || !item.AtWorldPoint(x, y))
            {
                continue;
            }

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
                foreach (Item item in GetItemsInRange(point, 0))
                {
                    if (item.Visible)
                    {
                        contains = false;
                        break;
                    }
                }

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

        foreach (var i in GetItemsInBounds(rect))
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

            return false;
        }

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

#pragma warning restore CA1000 // Do not declare static members on generic types
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Map Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Map Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Map result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Map Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        s = s.Trim();

        if (s.Length == 0)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        if (s.InsensitiveEquals("Internal"))
        {
            return Internal;
        }

        if (!int.TryParse(s, provider, out var index))
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

            if (index >= 0 && map.MapIndex == index || s.InsensitiveEquals(map.Name))
            {
                return map;
            }
        }

        throw new FormatException($"The input string '{s}' was not in a correct format.");
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Map result)
    {
        s = s.Trim();

        if (s.Length == 0)
        {
            result = default;
            return false;
        }

        if (s.InsensitiveEquals("Internal"))
        {
            result = Internal;
            return true;
        }

        if (!int.TryParse(s, provider, out var index))
        {
            index = -1;
        }
        else if (index == 127)
        {
            result = Internal;
            return true;
        }

        for (int i = 0; i < Maps.Length; i++)
        {
            var map = Maps[i];
            if (map == null)
            {
                continue;
            }

            if (index >= 0 && map.MapIndex == index || s.InsensitiveEquals(map.Name))
            {
                result = map;
                return true;
            }
        }

        result = default;
        return false;
    }

    public class Sector
    {
        // TODO: Can we avoid this?
        private static readonly List<Mobile> m_DefaultMobileList = new();
        private static readonly List<NetState> m_DefaultClientList = new();
        private static readonly List<BaseMulti> m_DefaultMultiList = new();
        private static readonly List<Region> m_DefaultRectList = new();
        private bool m_Active;
        private List<NetState> _clients;
        private ValueLinkList<Item> _items;
        private List<Mobile> _mobiles;
        private List<BaseMulti> _multis;
        private List<Region> _regions;

        public Sector(int x, int y, Map owner)
        {
            X = x;
            Y = y;
            Owner = owner;
            m_Active = false;
        }

        public List<Region> Regions => _regions ?? m_DefaultRectList;

        public List<BaseMulti> Multis => _multis ?? m_DefaultMultiList;

        public List<Mobile> Mobiles => _mobiles ?? m_DefaultMobileList;

        internal ref readonly ValueLinkList<Item> Items => ref _items;

        public List<NetState> Clients => _clients ?? m_DefaultClientList;

        public bool Active => m_Active && Owner != Internal;

        public Map Owner { get; }

        public int X { get; }

        public int Y { get; }

        public void OnClientChange(NetState oldState, NetState newState)
        {
            Utility.Replace(ref _clients, oldState, newState);
        }

        public void OnEnter(Item item)
        {
            _items.AddLast(item);
        }

        public void OnLeave(Item item)
        {
            _items.Remove(item);
        }

        public void OnEnter(Mobile mob)
        {
            Utility.Add(ref _mobiles, mob);

            if (mob.NetState != null)
            {
                Utility.Add(ref _clients, mob.NetState);

                Owner.ActivateSectors(X, Y);
            }
        }

        public void OnLeave(Mobile mob)
        {
            Utility.Remove(ref _mobiles, mob);

            if (mob.NetState != null)
            {
                Utility.Remove(ref _clients, mob.NetState);

                Owner.DeactivateSectors(X, Y);
            }
        }

        public void OnEnter(Region region, Rectangle3D rect)
        {
            Utility.Add(ref _regions, region);

            _regions.Sort();

            UpdateMobileRegions();
        }

        public void OnLeave(Region region)
        {
            if (_regions != null)
            {
                for (var i = _regions.Count - 1; i >= 0; i--)
                {
                    var r = _regions[i];

                    if (r == region)
                    {
                        _regions.RemoveAt(i);
                        break;
                    }
                }

                if (_regions.Count == 0)
                {
                    _regions = null;
                }
            }

            UpdateMobileRegions();
        }

        private void UpdateMobileRegions()
        {
            if (_mobiles != null)
            {
                using var queue = PooledRefQueue<Mobile>.Create(_mobiles.Count);
                foreach (var mob in _mobiles)
                {
                    queue.Enqueue(mob);
                }

                while (queue.Count > 0)
                {
                    queue.Dequeue().UpdateRegion();
                }
            }
        }

        public void OnMultiEnter(BaseMulti multi)
        {
            Utility.Add(ref _multis, multi);
        }

        public void OnMultiLeave(BaseMulti multi)
        {
            Utility.Remove(ref _multis, multi);
        }

        public void Activate()
        {
            if (!Active)
            {
                foreach (var item in _items)
                {
                    item.OnSectorActivate();
                }

                if (_mobiles != null)
                {
                    foreach (var mob in _mobiles)
                    {
                        mob.OnSectorActivate();
                    }
                }

                m_Active = true;
            }
        }

        public void Deactivate()
        {
            if (Active)
            {
                foreach (var item in _items)
                {
                    item.OnSectorDeactivate();
                }

                if (_mobiles != null)
                {
                    foreach (var mob in _mobiles)
                    {
                        mob.OnSectorDeactivate();
                    }
                }

                m_Active = false;
            }
        }
    }
}
