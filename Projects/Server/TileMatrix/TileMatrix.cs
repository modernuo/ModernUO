using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server
{
    public class TileMatrix : IDisposable
    {
        private static readonly List<TileMatrix> _instances = new();

        private readonly int _fileIndex;
        private readonly List<TileMatrix> _fileShare = new();

        private readonly BinaryReader _indexReader;

        private readonly LandTile[] _invalidLandBlock;
        private readonly int[][] _landPatches;
        private readonly LandTile[][][] _landTiles;

        private readonly FileStream _mapStream;
        private readonly UOPIndex _mapIndex;

        private readonly Map _owner;

        private readonly int[][] _staticPatches;
        private readonly StaticTile[][][][][] _staticTiles;

        private readonly TileList _tilesList = new();

        private TileList[][] _lists;
        private DateTime _nextLandWarning;
        private DateTime _nextStaticWarning;

        private StaticTile[] _tileBuffer = new StaticTile[128];

        public FileStream IndexStream { get; }
        public FileStream DataStream { get; }
        public TileMatrixPatch Patch { get; }
        public int BlockWidth { get; }
        public int BlockHeight { get; }
        public StaticTile[][][] EmptyStaticBlock { get; }

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

            _owner = owner;

            if (fileIndex != 0x7F)
            {
                var mapPath = Core.FindDataFile($"map{fileIndex}LegacyMUL.uop", false);

                if (mapPath != null)
                {
                    _mapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _mapIndex = new UOPIndex(_mapStream);
                }
                else
                {
                    mapPath = Core.FindDataFile($"map{fileIndex}.mul", false, true);

                    if (mapPath != null)
                    {
                        _mapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    }
                }

                var indexPath = Core.FindDataFile($"staidx{fileIndex}.mul", false, true);

                if (indexPath != null)
                {
                    IndexStream = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _indexReader = new BinaryReader(IndexStream);
                }

                var staticsPath = Core.FindDataFile($"statics{fileIndex}.mul", false, true);

                if (staticsPath != null)
                {
                    DataStream = new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
            }

            EmptyStaticBlock = new StaticTile[8][][];

            for (var i = 0; i < 8; ++i)
            {
                EmptyStaticBlock[i] = new StaticTile[8][];

                for (var j = 0; j < 8; ++j)
                {
                    EmptyStaticBlock[i][j] = Array.Empty<StaticTile>();
                }
            }

            _invalidLandBlock = new LandTile[196];

            _landTiles = new LandTile[BlockWidth][][];
            _staticTiles = new StaticTile[BlockWidth][][][][];
            _staticPatches = new int[BlockWidth][];
            _landPatches = new int[BlockWidth][];

            Patch = new TileMatrixPatch(this, mapID);
        }

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
                return EmptyStaticBlock;
            }

            _staticTiles[x] ??= new StaticTile[BlockHeight][][][];

            var tiles = _staticTiles[x][y];

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

            return _staticTiles[x][y] = tiles;
        }

        public StaticTile[] GetStaticTiles(int x, int y) => GetStaticBlock(x >> 3, y >> 3)[x & 0x7][y & 0x7];

        [MethodImpl(MethodImplOptions.Synchronized)]
        public StaticTile[] GetStaticTiles(int x, int y, bool multis)
        {
            var tiles = GetStaticBlock(x >> 3, y >> 3);

            if (!multis)
            {
                return tiles[x & 0x7][y & 0x7];
            }

            var eable = _owner.GetMultiTilesAt(x, y);

            if (eable == Map.NullEnumerable<StaticTile[]>.Instance)
            {
                return tiles[x & 0x7][y & 0x7];
            }

            var any = false;

            foreach (StaticTile[] multiTiles in eable)
            {
                any = true;
                _tilesList.AddRange(multiTiles);
            }

            eable.Free();

            if (!any)
            {
                return tiles[x & 0x7][y & 0x7];
            }

            _tilesList.AddRange(tiles[x & 0x7][y & 0x7]);

            return _tilesList.ToArray();
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
            if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight || _mapStream == null)
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

            return _landTiles[x][y] = tiles ?? ReadLandBlock(x, y);
        }

        public LandTile GetLandTile(int x, int y) => GetLandBlock(x >> 3, y >> 3)[((y & 0x7) << 3) + (x & 0x7)];

        [MethodImpl(MethodImplOptions.Synchronized)]
        private unsafe StaticTile[][][] ReadStaticBlock(int x, int y)
        {
            try
            {
                _indexReader.BaseStream.Seek((x * BlockHeight + y) * 12, SeekOrigin.Begin);

                var lookup = _indexReader.ReadInt32();
                var length = _indexReader.ReadInt32();

                if (lookup < 0 || length <= 0)
                {
                    return EmptyStaticBlock;
                }

                var count = length / 7;

                DataStream.Seek(lookup, SeekOrigin.Begin);

                if (_tileBuffer.Length < count)
                {
                    _tileBuffer = new StaticTile[count];
                }

                var staTiles = _tileBuffer; // new StaticTile[tileCount];

                fixed (StaticTile* pTiles = staTiles)
                {
                    var ptr = DataStream.SafeFileHandle?.DangerousGetHandle();
                    if (ptr == null)
                    {
                        throw new Exception($"Cannot open {DataStream.Name}");
                    }
                    NativeReader.Read(ptr.Value, pTiles, length);

                    if (_lists == null)
                    {
                        _lists = new TileList[8][];

                        for (var i = 0; i < 8; ++i)
                        {
                            _lists[i] = new TileList[8];

                            for (var j = 0; j < 8; ++j)
                            {
                                _lists[i][j] = new TileList();
                            }
                        }
                    }

                    var lists = _lists;

                    StaticTile* pCur = pTiles, pEnd = pTiles + count;

                    while (pCur < pEnd)
                    {
                        lists[pCur->m_X & 0x7][pCur->m_Y & 0x7].Add(pCur->m_ID, pCur->m_Z);
                        pCur += 1;
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
                if (DateTime.UtcNow >= _nextStaticWarning)
                {
                    Console.WriteLine("Warning: Static EOS for {0} ({1}, {2})", _owner, x, y);
                    _nextStaticWarning = DateTime.UtcNow + TimeSpan.FromMinutes(1.0);
                }

                return EmptyStaticBlock;
            }
        }

        public void Force()
        {
            if ((AssemblyHandler.Assemblies?.Length ?? 0) == 0)
            {
                throw new Exception();
            }
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

                _mapStream.Seek(offset, SeekOrigin.Begin);

                var tiles = new LandTile[64];

                fixed (LandTile* pTiles = tiles)
                {
                    var ptr = _mapStream.SafeFileHandle?.DangerousGetHandle();
                    if (ptr == null)
                    {
                        throw new Exception($"Cannot open {_mapStream.Name}");
                    }
                    NativeReader.Read(ptr.Value, pTiles, 192);
                }

                return tiles;
            }
            catch
            {
                if (DateTime.UtcNow >= _nextLandWarning)
                {
                    Console.WriteLine("Warning: Land EOS for {0} ({1}, {2})", _owner, x, y);
                    _nextLandWarning = DateTime.UtcNow + TimeSpan.FromMinutes(1.0);
                }

                return _invalidLandBlock;
            }
        }

        public void Dispose()
        {
            _mapIndex?.Close();
            _mapStream?.Close();
            DataStream?.Close();
            _indexReader?.Close();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LandTile
    {
        internal short m_ID;
        internal sbyte m_Z;

        public int ID => m_ID;

        public int Z
        {
            get => m_Z;
            set => m_Z = (sbyte)value;
        }

        public int Height => 0;

        public bool Ignored => m_ID == 2 || m_ID == 0x1DB || m_ID >= 0x1AE && m_ID <= 0x1B5;

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

        public int X
        {
            get => m_X;
            set => m_X = (byte)value;
        }

        public int Y
        {
            get => m_Y;
            set => m_Y = (byte)value;
        }

        public int Z
        {
            get => m_Z;
            set => m_Z = (sbyte)value;
        }

        public int Hue
        {
            get => m_Hue;
            set => m_Hue = (short)value;
        }

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
        private readonly UOPEntry[] m_Entries;
        private readonly int m_Length;

        private readonly BinaryReader m_Reader;

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
            } while (nextTable != 0 && nextTable < m_Length);

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

        public int Version { get; }

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

        private class UOPEntry : IComparable<UOPEntry>
        {
            public readonly int m_Length;
            public int m_Offset;
            public int m_Order;

            public UOPEntry(int offset, int length)
            {
                m_Offset = offset;
                m_Length = length;
                m_Order = 0;
            }

            public int CompareTo(UOPEntry other) => m_Order.CompareTo(other.m_Order);
        }

        private class OffsetComparer : IComparer<UOPEntry>
        {
            public static readonly IComparer<UOPEntry> Instance = new OffsetComparer();

            public int Compare(UOPEntry x, UOPEntry y) =>
                x == null ? y == null ? 0 : 1 :
                y == null ? -1 : x.m_Offset.CompareTo(y.m_Offset);
        }
    }
}
