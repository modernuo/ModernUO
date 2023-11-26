/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

namespace Server;

public static class MultiData
{
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

    private static Dictionary<int, MultiComponentList> _components = new();

    public static MultiComponentList GetComponents(int multiID) =>
        _components.TryGetValue(multiID & 0x3FFF, out var mcl) ? mcl : MultiComponentList.Empty;

    private static void LoadUOP(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var streamReader = new BinaryReader(stream);

        // Head Information Start
        if (streamReader.ReadInt32() != 0x0050594D) // Not a UOP File
        {
            return;
        }

        if (streamReader.ReadInt32() > 5) // Bad Version
        {
            return;
        }

        UOPHash.BuildChunkIDs(out var chunkIds);

        streamReader.ReadUInt32(); // format timestamp? 0xFD23EC43
        var startAddress = streamReader.ReadInt64();

        stream.Seek(startAddress, SeekOrigin.Begin); // End of head block

        long nextBlock;

        do
        {
            var blockFileCount = streamReader.ReadInt32();
            nextBlock = streamReader.ReadInt64();

            var index = 0;

            do
            {
                var offset = streamReader.ReadInt64();

                var headerSize = streamReader.ReadInt32();       // header length
                var compressedSize = streamReader.ReadInt32();   // compressed size
                var decompressedSize = streamReader.ReadInt32(); // decompressed size

                var filehash = streamReader.ReadUInt64(); // filename hash (HashLittle2)
                streamReader.ReadUInt32();
                var compressionMethod = streamReader.ReadInt16(); // compression method (0 = none, 1 = zlib)

                index++;

                if (offset == 0 || decompressedSize == 0 || filehash == 0x126D1E99DDEDEE0A) // Exclude housing.bin
                {
                    continue;
                }

                chunkIds.TryGetValue(filehash, out var chunkID);

                var position = stream.Position; // save current position

                stream.Seek(offset + headerSize, SeekOrigin.Begin);

                Span<byte> sourceData = GC.AllocateUninitializedArray<byte>(compressedSize);

                if (stream.Read(sourceData) != compressedSize)
                {
                    continue;
                }

                Span<byte> data;

                if (compressionMethod == 1)
                {
                    data = GC.AllocateUninitializedArray<byte>(decompressedSize);
                    Zlib.Unpack(data, ref decompressedSize, sourceData, compressedSize);
                }
                else
                {
                    data = sourceData;
                }

                var tileList = new List<MultiTileEntry>();

                var reader = new SpanReader(data);
                reader.Seek(4, SeekOrigin.Begin);
                var count = reader.ReadUInt32LE();

                for (uint i = 0; i < count; i++)
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

                _components[chunkID] = new MultiComponentList(tileList);

                stream.Seek(position, SeekOrigin.Begin); // back to position
            } while (index < blockFileCount);
        } while (stream.Seek(nextBlock, SeekOrigin.Begin) != 0);

        streamReader.Close();
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

public static class UOPHash
{
    public static void BuildChunkIDs(out Dictionary<ulong, int> chunkIds)
    {
        const int maxId = 0x10000;

        chunkIds = new Dictionary<ulong, int>();

        for (var i = 0; i < maxId; ++i)
        {
            chunkIds[HashLittle2($"build/multicollection/{i:000000}.bin")] = i;
        }
    }

    public static ulong HashLittle2(ReadOnlySpan<char> s)
    {
        var length = s.Length;

        uint b, c;
        var a = b = c = 0xDEADBEEF + (uint)length;

        var k = 0;

        while (length > 12)
        {
            a += s[k];
            a += (uint)s[k + 1] << 8;
            a += (uint)s[k + 2] << 16;
            a += (uint)s[k + 3] << 24;
            b += s[k + 4];
            b += (uint)s[k + 5] << 8;
            b += (uint)s[k + 6] << 16;
            b += (uint)s[k + 7] << 24;
            c += s[k + 8];
            c += (uint)s[k + 9] << 8;
            c += (uint)s[k + 10] << 16;
            c += (uint)s[k + 11] << 24;

            a -= c;
            a ^= (c << 4) | (c >> 28);
            c += b;
            b -= a;
            b ^= (a << 6) | (a >> 26);
            a += c;
            c -= b;
            c ^= (b << 8) | (b >> 24);
            b += a;
            a -= c;
            a ^= (c << 16) | (c >> 16);
            c += b;
            b -= a;
            b ^= (a << 19) | (a >> 13);
            a += c;
            c -= b;
            c ^= (b << 4) | (b >> 28);
            b += a;

            length -= 12;
            k += 12;
        }

        if (length != 0)
        {
            switch (length)
            {
                case 12:
                    {
                        c += (uint)s[k + 11] << 24;
                        goto case 11;
                    }
                case 11:
                    {
                        c += (uint)s[k + 10] << 16;
                        goto case 10;
                    }
                case 10:
                    {
                        c += (uint)s[k + 9] << 8;
                        goto case 9;
                    }
                case 9:
                    {
                        c += s[k + 8];
                        goto case 8;
                    }
                case 8:
                    {
                        b += (uint)s[k + 7] << 24;
                        goto case 7;
                    }
                case 7:
                    {
                        b += (uint)s[k + 6] << 16;
                        goto case 6;
                    }
                case 6:
                    {
                        b += (uint)s[k + 5] << 8;
                        goto case 5;
                    }
                case 5:
                    {
                        b += s[k + 4];
                        goto case 4;
                    }
                case 4:
                    {
                        a += (uint)s[k + 3] << 24;
                        goto case 3;
                    }
                case 3:
                    {
                        a += (uint)s[k + 2] << 16;
                        goto case 2;
                    }
                case 2:
                    {
                        a += (uint)s[k + 1] << 8;
                        goto case 1;
                    }
                case 1:
                    {
                        a += s[k];
                        break;
                    }
            }

            c ^= b;
            c -= (b << 14) | (b >> 18);
            a ^= c;
            a -= (c << 11) | (c >> 21);
            b ^= a;
            b -= (a << 25) | (a >> 7);
            c ^= b;
            c -= (b << 16) | (b >> 16);
            a ^= c;
            a -= (c << 4) | (c >> 28);
            b ^= a;
            b -= (a << 14) | (a >> 18);
            c ^= b;
            c -= (b << 24) | (b >> 8);
        }

        return ((ulong)b << 32) | c;
    }
}
