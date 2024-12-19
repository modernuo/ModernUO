/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TileMatrix.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server;

public class TileMatrix
{
    public const int SectorShift = 3;

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TileMatrix));
    private static readonly List<TileMatrix> _instances = new();

    private readonly StaticTile[][][][][] _staticTiles;
    private readonly LandTile[][][] _landTiles;
    private readonly LandTile[] _invalidLandBlock;
    private readonly StaticTile[][][] _emptyStaticBlock;
    private readonly UOPEntry[] _uopMapEntries;
    private readonly int _fileIndex;
    private readonly Map _map;
    private readonly int[][] _staticPatches;
    private readonly int[][] _landPatches;
    private readonly List<TileMatrix> _fileShare = new();

    public TileMatrixPatch Patch { get; }
    public int BlockWidth { get; }
    public int BlockHeight { get; }
    public FileStream MapStream { get; }
    public FileStream IndexStream { get; }
    public FileStream DataStream { get; }
    public BinaryReader IndexReader { get; }

    public static bool Pre6000ClientSupport { get; private set; }

    public static void Configure()
    {
        // Set to true to support < 6.0.0 clients where map0.mul is both Felucca & Trammel
        var isPre6000Trammel = UOClient.ServerClientVersion != null && UOClient.ServerClientVersion < ClientVersion.Version6000;
        Pre6000ClientSupport = ServerConfiguration.GetSetting("maps.enablePre6000Trammel", isPre6000Trammel);
    }

    public TileMatrix(Map owner, int fileIndex, int mapID, int width, int height)
    {
        lock (_instances)
        {
            for (var i = 0; i < _instances.Count; ++i)
            {
                var tm = _instances[i];

                if (tm._fileIndex == fileIndex)
                {
                    lock (_fileShare)
                    {
                        lock (tm._fileShare)
                        {
                            tm._fileShare.Add(this);
                            _fileShare.Add(tm);
                        }
                    }
                }
            }

            _instances.Add(this);
        }

        _fileIndex = fileIndex;
        BlockWidth = width >> SectorShift;
        BlockHeight = height >> SectorShift;

        _map = owner;

        if (fileIndex != 0x7F)
        {
            var mapFileIndex = Pre6000ClientSupport && mapID == 1 ? 0 : fileIndex;

            var mapPath = Core.FindDataFile($"map{mapFileIndex}.mul", false);

            if (mapPath != null)
            {
                MapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            else
            {
                mapPath = Core.FindDataFile($"map{mapFileIndex}LegacyMUL.uop", false);

                if (mapPath != null)
                {
                    MapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                    var uopEntries = UOPFiles.ReadUOPIndexes(MapStream, ".dat", 0x14000, 5);

                    _uopMapEntries = GC.AllocateUninitializedArray<UOPEntry>(uopEntries.Count);
                    uopEntries.Values.CopyTo(_uopMapEntries, 0);

                    ConvertToMapEntries(MapStream);
                }
                else
                {
                    logger.Warning("{File} was not found.", $"map{mapFileIndex}.mul");
                }
            }

            var indexPath = Core.FindDataFile($"staidx{mapFileIndex}.mul", false);

            if (indexPath != null)
            {
                IndexStream = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                IndexReader = new BinaryReader(IndexStream);
            }
            else
            {
                logger.Warning("{File} was not found.", $"staidx{mapFileIndex}.mul");
            }

            var staticsPath = Core.FindDataFile($"statics{mapFileIndex}.mul", false);

            if (staticsPath != null)
            {
                DataStream = new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            else
            {
                logger.Warning("{File} was not found.", $"statics{fileIndex}.mul");
            }
        }

        _emptyStaticBlock = new StaticTile[8][][];

        for (var i = 0; i < 8; ++i)
        {
            _emptyStaticBlock[i] = new StaticTile[8][];

            for (var j = 0; j < 8; ++j)
            {
                _emptyStaticBlock[i][j] = new StaticTile[0];
            }
        }

        _invalidLandBlock = new LandTile[196];

        _landTiles = new LandTile[BlockWidth][][];
        _staticTiles = new StaticTile[BlockWidth][][][][];
        _staticPatches = new int[BlockWidth][];
        _landPatches = new int[BlockWidth][];

        Patch = new TileMatrixPatch(this, fileIndex);
    }

    public StaticTile[][][] EmptyStaticBlock => _emptyStaticBlock;

    public void SetStaticBlock(int x, int y, StaticTile[][][] value)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return;
        }

        _staticTiles[x] ??= new StaticTile[BlockHeight][][][];
        _staticTiles[x][y] = value;

        _staticPatches[x] ??= new int[(BlockHeight + 31) >> 5];
        _staticPatches[x][y >> 5] |= 1 << (y & 0x1F);
    }

    public StaticTile[][][] GetStaticBlock(int x, int y)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight || DataStream == null || IndexStream == null)
        {
            return _emptyStaticBlock;
        }

        _staticTiles[x] ??= new StaticTile[BlockHeight][][][];

        var tiles = _staticTiles[x][y];

        if (tiles == null)
        {
            for (var i = 0; tiles == null && i < _fileShare.Count; ++i)
            {
                var shared = _fileShare[i];

                lock (shared)
                {
                    if (x < shared.BlockWidth && y < shared.BlockHeight)
                    {
                        var theirTiles = shared._staticTiles[x];

                        if (theirTiles != null)
                        {
                            tiles = theirTiles[y];
                        }

                        if (tiles != null)
                        {
                            var theirBits = shared._staticPatches[x];

                            if (theirBits != null && (theirBits[y >> 5] & (1 << (y & 0x1F))) != 0)
                            {
                                tiles = null;
                            }
                        }
                    }
                }
            }

            tiles ??= ReadStaticBlock(x, y);

            _staticTiles[x][y] = tiles;
        }

        return tiles;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.StaticTileEnumerable GetStaticTiles(int x, int y) => new(_map, new Point2D(x, y), includeMultis: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.StaticTileEnumerable GetStaticAndMultiTiles(int x, int y) => new(_map, new Point2D(x, y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.StaticTileEnumerable GetMultiTiles(int x, int y) => new(_map, new Point2D(x, y), false);

    public void SetLandBlock(int x, int y, LandTile[] value)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return;
        }

        _landTiles[x] ??= new LandTile[BlockHeight][];
        _landTiles[x][y] = value;

        _landPatches[x] ??= new int[(BlockHeight + 31) >> 5];
        _landPatches[x][y >> 5] |= 1 << (y & 0x1F);
    }

    public LandTile[] GetLandBlock(int x, int y)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight || MapStream == null)
        {
            return _invalidLandBlock;
        }

        _landTiles[x] ??= new LandTile[BlockHeight][];

        var tiles = _landTiles[x][y];

        if (tiles != null)
        {
            return tiles;
        }

        for (var i = 0; tiles == null && i < _fileShare.Count; ++i)
        {
            var shared = _fileShare[i];

            if (x < shared.BlockWidth && y < shared.BlockHeight)
            {
                var theirTiles = shared._landTiles[x];

                if (theirTiles != null)
                {
                    tiles = theirTiles[y];
                }

                if (tiles != null)
                {
                    var theirBits = shared._landPatches[x];

                    if (theirBits != null && (theirBits[y >> 5] & (1 << (y & 0x1F))) != 0)
                    {
                        tiles = null;
                    }
                }
            }
        }

        tiles ??= ReadLandBlock(x, y);

        _landTiles[x][y] = tiles;

        return tiles;
    }

    public LandTile GetLandTile(int x, int y)
    {
        var tiles = GetLandBlock(x >> SectorShift, y >> SectorShift);

        return tiles[((y & 0x7) << 3) + (x & 0x7)];
    }

    private TileList[][] m_Lists;

    private StaticTile[] m_TileBuffer = new StaticTile[128];

    private unsafe StaticTile[][][] ReadStaticBlock(int x, int y)
    {
        try
        {
            IndexReader.BaseStream.Seek((x * BlockHeight + y) * 12, SeekOrigin.Begin);

            var lookup = IndexReader.ReadInt32();
            var length = IndexReader.ReadInt32();

            if (lookup < 0 || length <= 0)
            {
                return _emptyStaticBlock;
            }

            var count = length / 7;

            DataStream.Seek(lookup, SeekOrigin.Begin);

            if (m_TileBuffer.Length < count)
            {
                m_TileBuffer = new StaticTile[count];
            }

            var staTiles = m_TileBuffer;

            fixed (StaticTile* pTiles = staTiles)
            {
                _ = DataStream.Read(new Span<byte>(pTiles, length));

                if (m_Lists == null)
                {
                    m_Lists = new TileList[8][];

                    for (var i = 0; i < 8; ++i)
                    {
                        m_Lists[i] = new TileList[8];

                        for (var j = 0; j < 8; ++j)
                        {
                            m_Lists[i][j] = new TileList();
                        }
                    }
                }

                var lists = m_Lists;

                StaticTile* pCur = pTiles, pEnd = pTiles + count;

                while (pCur < pEnd)
                {
                    lists[pCur->m_X & 0x7][pCur->m_Y & 0x7].Add(pCur);

                    pCur = pCur + 1;
                }

                var tiles = new StaticTile[8][][];

                for (var i = 0; i < 8; ++i)
                {
                    tiles[i] = new StaticTile[8][];

                    for (var j = 0; j < 8; ++j)
                    {
                        tiles[i][j] = lists[i][j].ToArray();
                    }
                }

                return tiles;
            }
        }
        catch (EndOfStreamException ex)
        {
            if (Core.Now >= m_NextStaticWarning)
            {
                Console.WriteLine("Warning: Static EOS for {0} ({1}, {2})", _map, x, y);
                m_NextStaticWarning = Core.Now + TimeSpan.FromMinutes(1.0);
            }

            return _emptyStaticBlock;
        }
    }

    private DateTime m_NextStaticWarning;
    private DateTime m_NextLandWarning;

    public static void Force()
    {
        if (AssemblyHandler.Assemblies?.Length > 0)
        {
            return;
        }

        throw new Exception("No assemblies were loaded, therefore we cannot load TileMatrix.");
    }

    private unsafe LandTile[] ReadLandBlock(int x, int y)
    {
        try
        {
            long offset = (x * BlockHeight + y) * 196 + 4;

            if (_uopMapEntries != null)
            {
                offset = FindOffset(offset);
            }

            MapStream.Seek(offset, SeekOrigin.Begin);

            var tiles = new LandTile[64];

            fixed (LandTile* pTiles = tiles)
            {
                _ = MapStream.Read(new Span<byte>(pTiles, 192));
            }

            return tiles;
        }
        catch (Exception ex)
        {
            if (Core.Now >= m_NextLandWarning)
            {
                Console.WriteLine("Warning: Land EOS for {0} ({1}, {2})", _map, x, y);
                m_NextLandWarning = Core.Now + TimeSpan.FromMinutes(1.0);
            }

            return _invalidLandBlock;
        }
    }

    public long FindOffset(long offset)
    {
        var total = 0;

        for (var i = 0; i < _uopMapEntries.Length; ++i)
        {
            var entry = _uopMapEntries[i];
            var newTotal = total + entry.Size;

            if (offset < newTotal)
            {
                return entry.Offset + (offset - total);
            }

            total = newTotal;
        }

        return -1;
    }

    private void ConvertToMapEntries(FileStream stream)
    {
        // Sorting by offset to make seeking faster
        Array.Sort(_uopMapEntries, (a, b) => a.Offset.CompareTo(b.Offset));

        var reader = new BinaryReader(stream);

        for (var i = 0; i < _uopMapEntries.Length; ++i)
        {
            var entry = _uopMapEntries[i];
            stream.Seek(entry.Offset, SeekOrigin.Begin);
            _uopMapEntries[i].Extra = reader.ReadInt32(); // order
        }

        Array.Sort(_uopMapEntries, (a, b) => a.Extra.CompareTo(b.Extra));
    }

    public void Dispose()
    {
        MapStream?.Close();
        DataStream?.Close();
        IndexReader?.Close();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LandTile
{
    internal short m_ID;
    internal sbyte m_Z;

    public int ID => m_ID;

    public int Z { get => m_Z; set => m_Z = (sbyte)value; }

    public int Height => 0;

    public bool Ignored => m_ID is 2 or 0x1DB or >= 0x1AE and <= 0x1B5;

    public LandTile(short id, sbyte z)
    {
        m_ID = id;
        m_Z = z;
    }

    public void Set(short id, sbyte z)
    {
        m_ID = id;
        m_Z = z;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticTile
{
    internal ushort m_ID;
    internal byte m_X;
    internal byte m_Y;
    internal sbyte m_Z;
    internal short m_Hue;

    public int ID => m_ID;

    public int X { get => m_X; set => m_X = (byte)value; }

    public int Y { get => m_Y; set => m_Y = (byte)value; }

    public int Z { get => m_Z; set => m_Z = (sbyte)value; }

    public int Hue { get => m_Hue; set => m_Hue = (short)value; }

    public int Height => TileData.ItemTable[m_ID & TileData.MaxItemValue].Height;

    public StaticTile(ushort id, sbyte z)
    {
        m_ID = id;
        m_Z = z;

        m_X = 0;
        m_Y = 0;
        m_Hue = 0;
    }

    public StaticTile(ushort id, byte x, byte y, sbyte z, short hue)
    {
        m_ID = id;
        m_X = x;
        m_Y = y;
        m_Z = z;
        m_Hue = hue;
    }

    public void Set(ushort id, sbyte z)
    {
        m_ID = id;
        m_Z = z;
    }

    public void Set(ushort id, byte x, byte y, sbyte z, short hue)
    {
        m_ID = id;
        m_X = x;
        m_Y = y;
        m_Z = z;
        m_Hue = hue;
    }
}
