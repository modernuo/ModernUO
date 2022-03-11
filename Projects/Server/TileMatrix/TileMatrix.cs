using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server
{
    public class TileMatrix
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(TileMatrix));
        private static readonly List<TileMatrix> _instances = new();

        private readonly StaticTile[][][][][] _staticTiles;
        private readonly LandTile[][][] _landTiles;
        private readonly LandTile[] _invalidLandBlock;
        private readonly StaticTile[][][] _emptyStaticBlock;
        private readonly UOPIndex _mapIndex;
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

        private static bool Pre6000ClientSupport;

        public static void Configure()
        {
            // Set to true to support < 6.0.0 clients where map0.mul is both Felucca & Trammel
            Pre6000ClientSupport = ServerConfiguration.GetOrUpdateSetting("maps.enablePre6000Trammel", false);
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
            BlockWidth = width >> 3;
            BlockHeight = height >> 3;

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
                        _mapIndex = new UOPIndex(MapStream);
                    }
                    else
                    {
                        logger.Warning($"map{mapFileIndex}.mul was not found.");
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
                    logger.Warning($"staidx{mapFileIndex}.mul was not found.");
                }

                var staticsPath = Core.FindDataFile($"statics{mapFileIndex}.mul", false);

                if (staticsPath != null)
                {
                    DataStream = new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    logger.Warning($"statics{fileIndex}.mul was not found.");
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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
                lock (_fileShare)
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
                }

                tiles ??= ReadStaticBlock(x, y);

                _staticTiles[x][y] = tiles;
            }

            return tiles;
        }

        public StaticTile[] GetStaticTiles(int x, int y)
        {
            var tiles = GetStaticBlock(x >> 3, y >> 3);

            return tiles[x & 0x7][y & 0x7];
        }

        private readonly TileList m_TilesList = new();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public StaticTile[] GetStaticTiles(int x, int y, bool multis)
        {
            var tiles = GetStaticBlock(x >> 3, y >> 3);

            if (multis)
            {
                var eable = _map.GetMultiTilesAt(x, y);

                if (eable == Map.NullEnumerable<StaticTile[]>.Instance)
                {
                    return tiles[x & 0x7][y & 0x7];
                }

                var any = false;

                foreach (var multiTiles in eable)
                {
                    if (!any)
                    {
                        any = true;
                    }

                    m_TilesList.AddRange(multiTiles);
                }

                eable.Free();

                if (!any)
                {
                    return tiles[x & 0x7][y & 0x7];
                }

                m_TilesList.AddRange(tiles[x & 0x7][y & 0x7]);

                return m_TilesList.ToArray();
            }

            return tiles[x & 0x7][y & 0x7];
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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

            lock (_fileShare)
            {
                for (var i = 0; tiles == null && i < _fileShare.Count; ++i)
                {
                    var shared = _fileShare[i];

                    lock (shared)
                    {
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
                }
            }

            tiles ??= ReadLandBlock(x, y);

            _landTiles[x][y] = tiles;

            return tiles;
        }

        public LandTile GetLandTile(int x, int y)
        {
            var tiles = GetLandBlock(x >> 3, y >> 3);

            return tiles[((y & 0x7) << 3) + (x & 0x7)];
        }

        private TileList[][] m_Lists;

        private StaticTile[] m_TileBuffer = new StaticTile[128];

        [MethodImpl(MethodImplOptions.Synchronized)]
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
                    DataStream.Read(new Span<byte>(pTiles, length));

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
                        lists[pCur->m_X & 0x7][pCur->m_Y & 0x7].Add(pCur->m_ID, pCur->m_Z);

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

        public void Force()
        {
            if (AssemblyHandler.Assemblies?.Length > 0)
            {
                return;
            }

            throw new Exception("No assemblies were loaded, therefore we cannot load TileMatrix.");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private unsafe LandTile[] ReadLandBlock(int x, int y)
        {
            try
            {
                var offset = (x * BlockHeight + y) * 196 + 4;

                if (_mapIndex != null)
                {
                    offset = _mapIndex.Lookup(offset);
                }

                MapStream.Seek(offset, SeekOrigin.Begin);

                var tiles = new LandTile[64];

                fixed (LandTile* pTiles = tiles)
                {
                    MapStream.Read(new Span<byte>(pTiles, 192));
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

        public void Dispose()
        {
            _mapIndex?.Close();
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

    public class UOPIndex
    {
        private class UOPEntry : IComparable<UOPEntry>
        {
            public int m_Offset;
            public readonly int m_Length;
            public int m_Order;

            public UOPEntry(int offset, int length)
            {
                m_Offset = offset;
                m_Length = length;
                m_Order = 0;
            }

            public int CompareTo(UOPEntry other)
            {
                return m_Order.CompareTo(other.m_Order);
            }
        }

        private class OffsetComparer : IComparer<UOPEntry>
        {
            public static readonly IComparer<UOPEntry> Instance = new OffsetComparer();

            public int Compare(UOPEntry x, UOPEntry y) => x!.m_Offset.CompareTo(y!.m_Offset);
        }

        private readonly BinaryReader m_Reader;
        private readonly int m_Length;
        private readonly UOPEntry[] m_Entries;

        public int Version { get; }

        public UOPIndex(FileStream stream)
        {
            m_Reader = new BinaryReader(stream);
            m_Length = (int)stream.Length;

            if (m_Reader.ReadInt32() != 0x50594D)
            {
                throw new ArgumentException("Invalid UOP file.");
            }

            Version = m_Reader.ReadInt32();
            m_Reader.ReadInt32();
            var nextTable = m_Reader.ReadInt32();

            var entries = new List<UOPEntry>();

            do
            {
                stream.Seek(nextTable, SeekOrigin.Begin);
                var count = m_Reader.ReadInt32();
                nextTable = m_Reader.ReadInt32();
                m_Reader.ReadInt32();

                for (var i = 0; i < count; ++i)
                {
                    var offset = m_Reader.ReadInt32();

                    if (offset == 0)
                    {
                        stream.Seek(30, SeekOrigin.Current);
                        continue;
                    }

                    m_Reader.ReadInt64();
                    var length = m_Reader.ReadInt32();

                    entries.Add(new UOPEntry(offset, length));

                    stream.Seek(18, SeekOrigin.Current);
                }
            }
            while (nextTable != 0 && nextTable < m_Length);

            entries.Sort(OffsetComparer.Instance);

            for (var i = 0; i < entries.Count; ++i)
            {
                stream.Seek(entries[i].m_Offset + 2, SeekOrigin.Begin);

                int dataOffset = m_Reader.ReadInt16();
                entries[i].m_Offset += 4 + dataOffset;

                stream.Seek(dataOffset, SeekOrigin.Current);
                entries[i].m_Order = m_Reader.ReadInt32();
            }

            entries.Sort();
            m_Entries = entries.ToArray();
        }

        public int Lookup(int offset)
        {
            var total = 0;

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                var newTotal = total + m_Entries[i].m_Length;

                if (offset < newTotal)
                {
                    return m_Entries[i].m_Offset + (offset - total);
                }

                total = newTotal;
            }

            return m_Length;
        }

        public void Close()
        {
            m_Reader.Close();
        }
    }
}
