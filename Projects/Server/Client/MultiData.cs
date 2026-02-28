/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MultiData.cs                                                    *
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
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Server.Buffers;
using Server.Compression;

namespace Server;

public static class MultiData
{
    public static string HousingUOPPath { get; private set; }
    public static UOPEntry HousingEntry { get; private set; }

    public static void Configure()
    {
        var multiUOPPath = Core.FindDataFile("MultiCollection.uop", false);

        if (File.Exists(multiUOPPath))
        {
            LoadUOP(multiUOPPath);
            return;
        }

        // OSI Client 7.0.9.0+ uses 64bit tiledata flags
        var postHSMulFormat = ServerConfiguration.GetSetting(
            "maps.enablePostHSMultiComponentFormat",
            UOClient.ServerClientVersion == null || UOClient.ServerClientVersion >= ClientVersion.Version7090
        );

        LoadMul(postHSMulFormat);
    }

    private static readonly Dictionary<int, MultiComponentList> _components = new();

    public static MultiComponentList GetComponents(int multiID) =>
        _components.TryGetValue(multiID & 0x3FFF, out var mcl) ? mcl : MultiComponentList.Empty;

    private static void LoadUOP(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        var housingHash = UOPFiles.HashLittle2("build/multicollection/housing.bin");
        var additionalHashes = new Dictionary<ulong, UOPEntry> { [housingHash] = default };

        var uopEntries = UOPFiles.ReadUOPIndexes(stream, ".bin", 0x10000, 4, 6, additionalHashes);

        var compressionBuffer = STArrayPool<byte>.Shared.Rent(0x10000);
        var buffer = STArrayPool<byte>.Shared.Rent(0x10000);

        foreach (var (i, entry) in uopEntries)
        {
            stream.Seek(entry.Offset, SeekOrigin.Begin);

            Span<byte> data;

            if (entry.Compressed)
            {
                if (stream.Read(buffer.AsSpan( 0, entry.CompressedSize)) != entry.CompressedSize)
                {
                    throw new FileLoadException($"Error loading file {stream.Name}.");
                }

                var decompressedSize = entry.Size;
                if (Deflate.Standard.Unpack(compressionBuffer, buffer, out var bytesDecompressed) != LibDeflateResult.Success
                    || decompressedSize != bytesDecompressed)
                {
                    throw new FileLoadException($"Error loading file {stream.Name}. Failed to unpack entry {i}.");
                }

                data = compressionBuffer.AsSpan(0, decompressedSize);
            }
            else
            {
                data = buffer.AsSpan(0, entry.Size);
            }

            var tileList = new List<MultiTileEntry>();

            var reader = new SpanReader(data);

            reader.Seek(4, SeekOrigin.Begin); // Skip the first 4 bytes
            var count = reader.ReadUInt32LE();

            for (uint t = 0; t < count; t++)
            {
                var itemId = reader.ReadUInt16LE();
                var x = reader.ReadInt16LE();
                var y = reader.ReadInt16LE();
                var z = reader.ReadInt16LE();
                var flagValue = reader.ReadUInt16LE();

                var tileFlag = flagValue switch
                {
                    1   => TileFlag.None,
                    257 => TileFlag.Generic,
                    _   => TileFlag.Background // 0
                };

                var clilocsCount = reader.ReadUInt32LE();
                var skip = (int)Math.Min(clilocsCount, int.MaxValue) * 4; // bypass binary block
                reader.Seek(skip, SeekOrigin.Current);

                tileList.Add(new MultiTileEntry(itemId, x, y, z, tileFlag));
            }

            _components[i] = new MultiComponentList(tileList);
        }

        STArrayPool<byte>.Shared.Return(buffer);
        STArrayPool<byte>.Shared.Return(compressionBuffer);

        if (additionalHashes.TryGetValue(housingHash, out var housingEntry) && housingEntry.Size > 0)
        {
            HousingUOPPath = path;
            HousingEntry = housingEntry;
        }
    }

    private static void LoadMul(bool postHSMulFormat)
    {
        var idxPath = Core.FindDataFile("multi.idx");
        var mulPath = Core.FindDataFile("multi.mul");

        using var idx = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var idxReader = new BinaryReader(idx);

        using var stream = new FileStream(mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bin = new BinaryReader(stream);

        var count = (int)(idx.Length / 12);
        for (var i = 0; i < count; i++)
        {
            var lookup = idxReader.ReadInt32();
            var length = idxReader.ReadInt32();
            idx.Seek(4, SeekOrigin.Current); // Extra

            if (lookup < 0 || length <= 0)
            {
                continue;
            }

            bin.BaseStream.Seek(lookup, SeekOrigin.Begin);
            _components[i] = new MultiComponentList(bin, length, postHSMulFormat);
        }
    }
}

public struct MultiTileEntry
{
    public ushort ItemId { get; set; }
    public short OffsetX { get; set; }
    public short OffsetY { get; set; }
    public short OffsetZ { get; set; }
    public TileFlag Flags { get; set; }

    public MultiTileEntry(ushort itemID, short xOffset, short yOffset, short zOffset, TileFlag flags)
    {
        ItemId = itemID;
        OffsetX = xOffset;
        OffsetY = yOffset;
        OffsetZ = zOffset;
        Flags = flags;
    }
}

public sealed class MultiComponentList
{
    public static readonly MultiComponentList Empty = new();

    private Point2D m_Min, m_Max;

    public MultiComponentList(MultiComponentList toCopy)
    {
        m_Min = toCopy.m_Min;
        m_Max = toCopy.m_Max;

        Center = toCopy.Center;

        Width = toCopy.Width;
        Height = toCopy.Height;

        Tiles = new StaticTile[Width][][];

        for (var x = 0; x < Width; ++x)
        {
            Tiles[x] = new StaticTile[Height][];

            for (var y = 0; y < Height; ++y)
            {
                Tiles[x][y] = new StaticTile[toCopy.Tiles[x][y].Length];

                for (var i = 0; i < Tiles[x][y].Length; ++i)
                {
                    Tiles[x][y][i] = toCopy.Tiles[x][y][i];
                }
            }
        }

        List = new MultiTileEntry[toCopy.List.Length];

        for (var i = 0; i < List.Length; ++i)
        {
            List[i] = toCopy.List[i];
        }
    }

    public MultiComponentList(IGenericReader reader)
    {
        var version = reader.ReadInt();

        m_Min = reader.ReadPoint2D();
        m_Max = reader.ReadPoint2D();
        Center = reader.ReadPoint2D();
        Width = reader.ReadInt();
        Height = reader.ReadInt();

        var length = reader.ReadInt();

        var allTiles = List = new MultiTileEntry[length];

        if (version == 0)
        {
            for (var i = 0; i < length; ++i)
            {
                int id = reader.ReadShort();
                if (id >= 0x4000)
                {
                    id -= 0x4000;
                }

                allTiles[i].ItemId = (ushort)id;
                allTiles[i].OffsetX = reader.ReadShort();
                allTiles[i].OffsetY = reader.ReadShort();
                allTiles[i].OffsetZ = reader.ReadShort();
                allTiles[i].Flags = (TileFlag)reader.ReadInt();
            }
        }
        else
        {
            for (var i = 0; i < length; ++i)
            {
                allTiles[i].ItemId = reader.ReadUShort();
                allTiles[i].OffsetX = reader.ReadShort();
                allTiles[i].OffsetY = reader.ReadShort();
                allTiles[i].OffsetZ = reader.ReadShort();
                allTiles[i].Flags = (TileFlag)reader.ReadInt();
            }
        }

        var tiles = new TileList[Width][];
        Tiles = new StaticTile[Width][][];

        for (var x = 0; x < Width; ++x)
        {
            tiles[x] = new TileList[Height];
            Tiles[x] = new StaticTile[Height][];

            for (var y = 0; y < Height; ++y)
            {
                tiles[x][y] = new TileList();
            }
        }

        for (var i = 0; i < allTiles.Length; ++i)
        {
            if (i == 0 || allTiles[i].Flags != 0)
            {
                var xOffset = allTiles[i].OffsetX + Center.m_X;
                var yOffset = allTiles[i].OffsetY + Center.m_Y;

                tiles[xOffset][yOffset].Add(allTiles[i].ItemId, (sbyte)allTiles[i].OffsetZ);
            }
        }

        for (var x = 0; x < Width; ++x)
        {
            for (var y = 0; y < Height; ++y)
            {
                Tiles[x][y] = tiles[x][y].ToArray();
            }
        }
    }

    public MultiComponentList(BinaryReader reader, int length, bool postHSFormat)
    {
        var count = length / (postHSFormat ? 16 : 12);
        var allTiles = List = new MultiTileEntry[count];

        for (var i = 0; i < count; ++i)
        {
            allTiles[i].ItemId = reader.ReadUInt16();
            allTiles[i].OffsetX = reader.ReadInt16();
            allTiles[i].OffsetY = reader.ReadInt16();
            allTiles[i].OffsetZ = reader.ReadInt16();
            allTiles[i].Flags = postHSFormat ? (TileFlag)reader.ReadUInt64() : (TileFlag)reader.ReadUInt32();

            var e = allTiles[i];

            if (i == 0 || e.Flags != 0)
            {
                if (e.OffsetX < m_Min.m_X)
                {
                    m_Min.m_X = e.OffsetX;
                }

                if (e.OffsetY < m_Min.m_Y)
                {
                    m_Min.m_Y = e.OffsetY;
                }

                if (e.OffsetX > m_Max.m_X)
                {
                    m_Max.m_X = e.OffsetX;
                }

                if (e.OffsetY > m_Max.m_Y)
                {
                    m_Max.m_Y = e.OffsetY;
                }
            }
        }

        Center = new Point2D(-m_Min.m_X, -m_Min.m_Y);
        Width = m_Max.m_X - m_Min.m_X + 1;
        Height = m_Max.m_Y - m_Min.m_Y + 1;

        var tiles = new TileList[Width][];
        Tiles = new StaticTile[Width][][];

        for (var i = 0; i < allTiles.Length; ++i)
        {
            if (i == 0 || allTiles[i].Flags != 0)
            {
                var xOffset = allTiles[i].OffsetX + Center.m_X;
                var yOffset = allTiles[i].OffsetY + Center.m_Y;

                tiles[xOffset] ??= new TileList[Height];
                Tiles[xOffset] ??= new StaticTile[Height][];

                tiles[xOffset][yOffset] ??= new TileList();
                tiles[xOffset][yOffset].Add(allTiles[i].ItemId, (sbyte)allTiles[i].OffsetZ);
            }
        }

        for (var x = 0; x < Width; ++x)
        {
            Tiles[x] ??= new StaticTile[Height][];
            for (var y = 0; y < Height; ++y)
            {
                var tileList = tiles[x]?[y];
                Tiles[x][y] = tileList?.ToArray() ?? Array.Empty<StaticTile>();
            }
        }
    }

    public MultiComponentList(List<MultiTileEntry> list)
    {
        var allTiles = List = new MultiTileEntry[list.Count];

        for (var i = 0; i < list.Count; ++i)
        {
            allTiles[i].ItemId = list[i].ItemId;
            allTiles[i].OffsetX = list[i].OffsetX;
            allTiles[i].OffsetY = list[i].OffsetY;
            allTiles[i].OffsetZ = list[i].OffsetZ;

            allTiles[i].Flags = list[i].Flags;

            var e = allTiles[i];

            if (i == 0 || e.Flags != 0)
            {
                if (e.OffsetX < m_Min.m_X)
                {
                    m_Min.m_X = e.OffsetX;
                }

                if (e.OffsetY < m_Min.m_Y)
                {
                    m_Min.m_Y = e.OffsetY;
                }

                if (e.OffsetX > m_Max.m_X)
                {
                    m_Max.m_X = e.OffsetX;
                }

                if (e.OffsetY > m_Max.m_Y)
                {
                    m_Max.m_Y = e.OffsetY;
                }
            }
        }

        Center = new Point2D(-m_Min.m_X, -m_Min.m_Y);
        Width = m_Max.m_X - m_Min.m_X + 1;
        Height = m_Max.m_Y - m_Min.m_Y + 1;

        var tiles = new TileList[Width][];
        Tiles = new StaticTile[Width][][];

        for (var x = 0; x < Width; ++x)
        {
            tiles[x] = new TileList[Height];
            Tiles[x] = new StaticTile[Height][];

            for (var y = 0; y < Height; ++y)
            {
                tiles[x][y] = new TileList();
            }
        }

        for (var i = 0; i < allTiles.Length; ++i)
        {
            if (i == 0 || allTiles[i].Flags != 0)
            {
                var xOffset = allTiles[i].OffsetX + Center.m_X;
                var yOffset = allTiles[i].OffsetY + Center.m_Y;
                var itemID = (allTiles[i].ItemId & TileData.MaxItemValue) | 0x10000;

                tiles[xOffset][yOffset].Add((ushort)itemID, (sbyte)allTiles[i].OffsetZ);
            }
        }

        for (var x = 0; x < Width; ++x)
        {
            for (var y = 0; y < Height; ++y)
            {
                Tiles[x][y] = tiles[x][y].ToArray();
            }
        }
    }

    private MultiComponentList()
    {
        Tiles = Array.Empty<StaticTile[][]>();
        List = Array.Empty<MultiTileEntry>();
    }

    public Point2D Min => m_Min;
    public Point2D Max => m_Max;

    public Point2D Center { get; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public StaticTile[][][] Tiles { get; private set; }

    public MultiTileEntry[] List { get; private set; }

    public void Add(int itemID, int x, int y, int z)
    {
        var vx = x + Center.m_X;
        var vy = y + Center.m_Y;

        if (vx >= 0 && vx < Width && vy >= 0 && vy < Height)
        {
            var oldTiles = Tiles[vx][vy];

            for (var i = oldTiles.Length - 1; i >= 0; --i)
            {
                var data = TileData.ItemTable[itemID & TileData.MaxItemValue];

                if (oldTiles[i].Z == z && oldTiles[i].Height > 0 == data.Height > 0)
                {
                    var newIsRoof = (data.Flags & TileFlag.Roof) != 0;
                    var oldIsRoof =
                        (TileData.ItemTable[oldTiles[i].ID & TileData.MaxItemValue].Flags & TileFlag.Roof) != 0;

                    if (newIsRoof == oldIsRoof)
                    {
                        Remove(oldTiles[i].ID, x, y, z);
                    }
                }
            }

            oldTiles = Tiles[vx][vy];

            var newTiles = new StaticTile[oldTiles.Length + 1];
            Array.Copy(oldTiles, newTiles, oldTiles.Length);

            newTiles[^1] = new StaticTile((ushort)itemID, (sbyte)z);

            Tiles[vx][vy] = newTiles;

            var oldList = List;
            var newList = new MultiTileEntry[oldList.Length + 1];

            for (var i = 0; i < oldList.Length; ++i)
            {
                newList[i] = oldList[i];
            }

            newList[oldList.Length] = new MultiTileEntry(
                (ushort)itemID,
                (short)x,
                (short)y,
                (short)z,
                TileFlag.Background
            );

            List = newList;

            if (x < m_Min.m_X)
            {
                m_Min.m_X = x;
            }

            if (y < m_Min.m_Y)
            {
                m_Min.m_Y = y;
            }

            if (x > m_Max.m_X)
            {
                m_Max.m_X = x;
            }

            if (y > m_Max.m_Y)
            {
                m_Max.m_Y = y;
            }
        }
    }

    public void RemoveXYZH(int x, int y, int z, int minHeight)
    {
        var vx = x + Center.m_X;
        var vy = y + Center.m_Y;

        if (vx >= 0 && vx < Width && vy >= 0 && vy < Height)
        {
            var oldTiles = Tiles[vx][vy];

            for (var i = 0; i < oldTiles.Length; ++i)
            {
                var tile = oldTiles[i];

                if (tile.Z == z && tile.Height >= minHeight)
                {
                    var newTiles = new StaticTile[oldTiles.Length - 1];
                    Array.Copy(oldTiles, newTiles, i);
                    Array.Copy(oldTiles, i + 1, newTiles, i, oldTiles.Length - i - 1);

                    Tiles[vx][vy] = newTiles;

                    break;
                }
            }

            var oldList = List;

            for (var i = 0; i < oldList.Length; ++i)
            {
                var tile = oldList[i];

                if (tile.OffsetX == (short)x && tile.OffsetY == (short)y && tile.OffsetZ == (short)z &&
                    TileData.ItemTable[tile.ItemId & TileData.MaxItemValue].Height >= minHeight)
                {
                    var newList = new MultiTileEntry[oldList.Length - 1];
                    Array.Copy(oldList, newList, i);
                    Array.Copy(oldList, i + 1, newList, i, oldList.Length - i - 1);

                    List = newList;

                    break;
                }
            }
        }
    }

    public void Remove(int itemID, int x, int y, int z)
    {
        var vx = x + Center.m_X;
        var vy = y + Center.m_Y;

        if (vx >= 0 && vx < Width && vy >= 0 && vy < Height)
        {
            var oldTiles = Tiles[vx][vy];

            for (var i = 0; i < oldTiles.Length; ++i)
            {
                var tile = oldTiles[i];

                if (tile.ID == itemID && tile.Z == z)
                {
                    var newTiles = new StaticTile[oldTiles.Length - 1];
                    Array.Copy(oldTiles, newTiles, i);
                    Array.Copy(oldTiles, i + 1, newTiles, i, oldTiles.Length - i - 1);

                    Tiles[vx][vy] = newTiles;

                    break;
                }
            }

            var oldList = List;

            for (var i = 0; i < oldList.Length; ++i)
            {
                var tile = oldList[i];

                if (tile.ItemId == itemID && tile.OffsetX == (short)x && tile.OffsetY == (short)y &&
                    tile.OffsetZ == (short)z)
                {
                    var newList = new MultiTileEntry[oldList.Length - 1];
                    Array.Copy(oldList, newList, i);
                    Array.Copy(oldList, i + 1, newList, i, oldList.Length - i - 1);

                    List = newList;

                    break;
                }
            }
        }
    }

    public void Resize(int newWidth, int newHeight)
    {
        int oldWidth = Width, oldHeight = Height;
        var oldTiles = Tiles;

        var totalLength = 0;

        var newTiles = new StaticTile[newWidth][][];

        for (var x = 0; x < newWidth; ++x)
        {
            newTiles[x] = new StaticTile[newHeight][];

            for (var y = 0; y < newHeight; ++y)
            {
                if (x < oldWidth && y < oldHeight)
                {
                    newTiles[x][y] = oldTiles[x][y];
                }
                else
                {
                    newTiles[x][y] = Array.Empty<StaticTile>();
                }

                totalLength += newTiles[x][y].Length;
            }
        }

        Tiles = newTiles;
        List = new MultiTileEntry[totalLength];
        Width = newWidth;
        Height = newHeight;

        m_Min = Point2D.Zero;
        m_Max = Point2D.Zero;

        var index = 0;

        for (var x = 0; x < newWidth; ++x)
        {
            for (var y = 0; y < newHeight; ++y)
            {
                var tiles = newTiles[x][y];

                for (var i = 0; i < tiles.Length; ++i)
                {
                    var tile = tiles[i];

                    var vx = x - Center.X;
                    var vy = y - Center.Y;

                    if (vx < m_Min.m_X)
                    {
                        m_Min.m_X = vx;
                    }

                    if (vy < m_Min.m_Y)
                    {
                        m_Min.m_Y = vy;
                    }

                    if (vx > m_Max.m_X)
                    {
                        m_Max.m_X = vx;
                    }

                    if (vy > m_Max.m_Y)
                    {
                        m_Max.m_Y = vy;
                    }

                    List[index++] = new MultiTileEntry(
                        (ushort)tile.ID,
                        (short)vx,
                        (short)vy,
                        (short)tile.Z,
                        TileFlag.Background
                    );
                }
            }
        }
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(1); // version;

        writer.Write(m_Min);
        writer.Write(m_Max);
        writer.Write(Center);

        writer.Write(Width);
        writer.Write(Height);

        writer.Write(List.Length);

        for (var i = 0; i < List.Length; ++i)
        {
            var ent = List[i];

            writer.Write(ent.ItemId);
            writer.Write(ent.OffsetX);
            writer.Write(ent.OffsetY);
            writer.Write(ent.OffsetZ);
            writer.Write((int)ent.Flags);
        }
    }
}
