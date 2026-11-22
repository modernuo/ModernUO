/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
using System.IO.Hashing;
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
    // Indexed by chunk number; a missing chunk has Offset 0.
    private readonly UOPEntry[] _uopMapChunks;
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

    /// <summary>
    /// XxHash3 fingerprint over the bytes of the three on-disk files for this map
    /// (mapX.mul / .uop, staidxX.mul, staticsX.mul). The .mul format has no built-in
    /// CRC and CentredSharp / UOFiddler rewrite these files in place, so this is the
    /// only way to detect "client patched / mapper saved" changes that invalidate
    /// downstream caches keyed on map data. Computed once at construction; never
    /// changes for the lifetime of the TileMatrix.
    /// </summary>
    public ulong MapFilesFingerprint { get; }

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

                    _uopMapChunks = BuildMapChunks(uopEntries, mapPath);
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

            // Stream all three on-disk files through XxHash3 once. Saves + restores
            // each stream's position so this is invisible to the rest of the constructor
            // (UOP path has already read part of MapStream by now). Cost is ~5-10ms per
            // map at boot — paid once, then MapFilesFingerprint is a constant.
            MapFilesFingerprint = ComputeMapFilesFingerprint();
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

    private ulong ComputeMapFilesFingerprint()
    {
        var hasher = HashUtility.CreateXxHash3();
        AppendStreamFromStart(hasher, MapStream);
        AppendStreamFromStart(hasher, IndexStream);
        AppendStreamFromStart(hasher, DataStream);
        return hasher.GetCurrentHashAsUInt64();
    }

    private static void AppendStreamFromStart(XxHash3 hasher, FileStream stream)
    {
        if (stream == null)
        {
            return;
        }
        var originalPosition = stream.Position;
        stream.Position = 0;
        hasher.Append(stream);
        stream.Position = originalPosition;
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
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return _emptyStaticBlock;
        }

        // A map without static files (tests, generated maps) serves only what was injected.
        if (DataStream == null || IndexStream == null)
        {
            return _staticTiles[x]?[y] ?? _emptyStaticBlock;
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
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return _invalidLandBlock;
        }

        // A map without a map file (tests, generated maps) serves only what was injected.
        if (MapStream == null)
        {
            return _landTiles[x]?[y] ?? _invalidLandBlock;
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

            if (count == 0)
            {
                return _emptyStaticBlock;
            }

            DataStream.Seek(lookup, SeekOrigin.Begin);

            if (m_TileBuffer.Length < count)
            {
                m_TileBuffer = new StaticTile[count];
            }

            var staTiles = m_TileBuffer;

            fixed (StaticTile* pTiles = staTiles)
            {
                // count * 7, not length: a length that is not a multiple of 7 would overrun the buffer.
                DataStream.ReadExactly(new Span<byte>(pTiles, count * 7));

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
        catch (EndOfStreamException)
        {
            if (Core.Now >= m_NextStaticWarning)
            {
                logger.Warning("Static EOS for {Map} ({X}, {Y})", _map, x, y);
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
            var block = (long)x * BlockHeight + y;
            long offset;

            if (_uopMapChunks != null)
            {
                // Clients address map UOPs as fixed chunks of 4096 blocks, by chunk number.
                var chunk = block >> UopChunkShift;
                var blockInChunk = (int)(block & (UopBlocksPerChunk - 1));

                if (chunk >= _uopMapChunks.Length)
                {
                    return _invalidLandBlock;
                }

                ref readonly var entry = ref _uopMapChunks[chunk];
                if (entry.Offset == 0 || (blockInChunk + 1) * LandBlockSize > entry.Size)
                {
                    return _invalidLandBlock;
                }

                offset = entry.Offset + blockInChunk * LandBlockSize + 4;
            }
            else
            {
                offset = block * LandBlockSize + 4;
            }

            MapStream.Seek(offset, SeekOrigin.Begin);

            var tiles = new LandTile[64];

            fixed (LandTile* pTiles = tiles)
            {
                MapStream.ReadExactly(new Span<byte>(pTiles, 192));
            }

            return tiles;
        }
        catch (Exception)
        {
            if (Core.Now >= m_NextLandWarning)
            {
                logger.Warning("Land EOS for {Map} ({X}, {Y})", _map, x, y);
                m_NextLandWarning = Core.Now + TimeSpan.FromMinutes(1.0);
            }

            return _invalidLandBlock;
        }
    }

    private const int LandBlockSize = 196;
    private const int UopChunkShift = 12;
    private const int UopBlocksPerChunk = 1 << UopChunkShift;

    /// <summary>
    /// Places a map UOP's chunks by the index in their name (<c>build/map#legacymul/########.dat</c>), as clients do.
    /// The first int32 of a chunk is its first block's header, not an order: EA's files store the block number
    /// there, but files from other tools can repeat it.
    /// </summary>
    internal static UOPEntry[] BuildMapChunks(Dictionary<int, UOPEntry> entries, string path = null)
    {
        var length = 0;
        foreach (var index in entries.Keys)
        {
            length = Math.Max(length, index + 1);
        }

        var chunks = new UOPEntry[length];
        foreach (var (index, entry) in entries)
        {
            if (entry.Compressed)
            {
                logger.Warning("Map chunk {Index} in {File} is compressed and will not load.", index, path);
                continue;
            }

            chunks[index] = entry;
        }

        if (entries.Count != length)
        {
            logger.Warning("{File} is missing {Count} map chunks; those blocks will not load.", path, length - entries.Count);
        }

        return chunks;
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
