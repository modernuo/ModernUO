/***************************************************************************
 *                               TileMatrix.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server
{
    public class TileMatrix
    {
        private static readonly List<TileMatrix> m_Instances = new List<TileMatrix>();

        private readonly int m_FileIndex;
        private readonly List<TileMatrix> m_FileShare = new List<TileMatrix>();

        private readonly BinaryReader m_IndexReader;

        private readonly LandTile[] m_InvalidLandBlock;
        private readonly int[][] m_LandPatches;
        private readonly LandTile[][][] m_LandTiles;

        private readonly UOPIndex m_MapIndex;

        private readonly FileStream m_MapStream;

        private readonly Map m_Owner;

        private readonly int[][] m_StaticPatches;
        private readonly StaticTile[][][][][] m_StaticTiles;

        private readonly TileList m_TilesList = new TileList();

        private TileList[][] m_Lists;
        private DateTime m_NextLandWarning;

        private DateTime m_NextStaticWarning;

        private StaticTile[] m_TileBuffer = new StaticTile[128];

        public TileMatrix(Map owner, int fileIndex, int mapID, int width, int height)
        {
            lock (m_Instances)
            {
                for (var i = 0; i < m_Instances.Count; ++i)
                {
                    var tm = m_Instances[i];

                    if (tm.m_FileIndex == fileIndex)
                        lock (m_FileShare)
                        {
                            lock (tm.m_FileShare)
                            {
                                tm.m_FileShare.Add(this);
                                m_FileShare.Add(tm);
                            }
                        }
                }

                m_Instances.Add(this);
            }

            m_FileIndex = fileIndex;
            BlockWidth = width >> 3;
            BlockHeight = height >> 3;

            m_Owner = owner;

            if (fileIndex != 0x7F)
            {
                var mapPath = Core.FindDataFile($"map{fileIndex}LegacyMUL.uop", false, true);

                if (mapPath != null)
                {
                    m_MapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    m_MapIndex = new UOPIndex(m_MapStream);
                }
                else
                {
                    mapPath = Core.FindDataFile($"map{fileIndex}.mul", false, true);

                    if (mapPath != null)
                        m_MapStream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }

                var indexPath = Core.FindDataFile($"staidx{fileIndex}.mul", false, true);

                if (indexPath != null)
                {
                    IndexStream = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    m_IndexReader = new BinaryReader(IndexStream);
                }

                var staticsPath = Core.FindDataFile($"statics{fileIndex}.mul", false, true);

                if (staticsPath != null)
                    DataStream = new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }

            EmptyStaticBlock = new StaticTile[8][][];

            for (var i = 0; i < 8; ++i)
            {
                EmptyStaticBlock[i] = new StaticTile[8][];

                for (var j = 0; j < 8; ++j)
                    EmptyStaticBlock[i][j] = Array.Empty<StaticTile>();
            }

            m_InvalidLandBlock = new LandTile[196];

            m_LandTiles = new LandTile[BlockWidth][][];
            m_StaticTiles = new StaticTile[BlockWidth][][][][];
            m_StaticPatches = new int[BlockWidth][];
            m_LandPatches = new int[BlockWidth][];

            Patch = new TileMatrixPatch(this, mapID);
        }

        public FileStream IndexStream { get; }

        public FileStream DataStream { get; }

        public TileMatrixPatch Patch { get; }

        public int BlockWidth { get; }

        public int BlockHeight { get; }

        public StaticTile[][][] EmptyStaticBlock { get; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetStaticBlock(int x, int y, StaticTile[][][] value)
        {
            if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
                return;

            m_StaticTiles[x] ??= new StaticTile[BlockHeight][][][];
            m_StaticTiles[x][y] = value;

            m_StaticPatches[x] ??= new int[(BlockHeight + 31) >> 5];
            m_StaticPatches[x][y >> 5] |= 1 << (y & 0x1F);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public StaticTile[][][] GetStaticBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight || DataStream == null || IndexStream == null)
                return EmptyStaticBlock;

            m_StaticTiles[x] ??= new StaticTile[BlockHeight][][][];

            var tiles = m_StaticTiles[x][y];

            if (tiles != null)
                return tiles;

            lock (m_FileShare)
            {
                for (var i = 0; tiles == null && i < m_FileShare.Count; ++i)
                {
                    var shared = m_FileShare[i];

                    lock (shared)
                    {
                        if (x < shared.BlockWidth && y < shared.BlockHeight)
                        {
                            var theirTiles = shared.m_StaticTiles[x];

                            if (theirTiles != null)
                                tiles = theirTiles[y];

                            if (tiles != null)
                            {
                                var theirBits = shared.m_StaticPatches[x];

                                if (theirBits != null && (theirBits[y >> 5] & (1 << (y & 0x1F))) != 0)
                                    tiles = null;
                            }
                        }
                    }
                }
            }

            return m_StaticTiles[x][y] = tiles ?? ReadStaticBlock(x, y);
        }

        public StaticTile[] GetStaticTiles(int x, int y) => GetStaticBlock(x >> 3, y >> 3)[x & 0x7][y & 0x7];

        [MethodImpl(MethodImplOptions.Synchronized)]
        public StaticTile[] GetStaticTiles(int x, int y, bool multis)
        {
            var tiles = GetStaticBlock(x >> 3, y >> 3);

            if (!multis)
                return tiles[x & 0x7][y & 0x7];

            var eable = m_Owner.GetMultiTilesAt(x, y);

            if (eable == Map.NullEnumerable<StaticTile[]>.Instance)
                return tiles[x & 0x7][y & 0x7];

            var any = false;

            m_TilesList.AddRange(
                eable.SelectMany(
                        t =>
                        {
                            any = true;
                            return t;
                        }
                    )
                    .ToArray()
            );

            eable.Free();

            if (!any)
                return tiles[x & 0x7][y & 0x7];

            m_TilesList.AddRange(tiles[x & 0x7][y & 0x7]);

            return m_TilesList.ToArray();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetLandBlock(int x, int y, LandTile[] value)
        {
            if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
                return;

            m_LandTiles[x] ??= new LandTile[BlockHeight][];
            m_LandTiles[x][y] = value;

            m_LandPatches[x] ??= new int[(BlockHeight + 31) >> 5];
            m_LandPatches[x][y >> 5] |= 1 << (y & 0x1F);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public LandTile[] GetLandBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight || m_MapStream == null)
                return m_InvalidLandBlock;

            m_LandTiles[x] ??= new LandTile[BlockHeight][];

            var tiles = m_LandTiles[x][y];

            if (tiles != null)
                return tiles;

            lock (m_FileShare)
            {
                for (var i = 0; tiles == null && i < m_FileShare.Count; ++i)
                {
                    var shared = m_FileShare[i];

                    lock (shared)
                    {
                        if (x < shared.BlockWidth && y < shared.BlockHeight)
                        {
                            var theirTiles = shared.m_LandTiles[x];

                            if (theirTiles != null)
                                tiles = theirTiles[y];

                            if (tiles != null)
                            {
                                var theirBits = shared.m_LandPatches[x];

                                if (theirBits != null && (theirBits[y >> 5] & (1 << (y & 0x1F))) != 0)
                                    tiles = null;
                            }
                        }
                    }
                }
            }

            return m_LandTiles[x][y] = tiles ?? ReadLandBlock(x, y);
        }

        public LandTile GetLandTile(int x, int y) => GetLandBlock(x >> 3, y >> 3)[((y & 0x7) << 3) + (x & 0x7)];

        [MethodImpl(MethodImplOptions.Synchronized)]
        private unsafe StaticTile[][][] ReadStaticBlock(int x, int y)
        {
            try
            {
                m_IndexReader.BaseStream.Seek((x * BlockHeight + y) * 12, SeekOrigin.Begin);

                var lookup = m_IndexReader.ReadInt32();
                var length = m_IndexReader.ReadInt32();

                if (lookup < 0 || length <= 0)
                    return EmptyStaticBlock;

                var count = length / 7;

                DataStream.Seek(lookup, SeekOrigin.Begin);

                if (m_TileBuffer.Length < count)
                    m_TileBuffer = new StaticTile[count];

                var staTiles = m_TileBuffer; // new StaticTile[tileCount];

                fixed (StaticTile* pTiles = staTiles)
                {
                    NativeReader.Read(DataStream.SafeFileHandle.DangerousGetHandle(), pTiles, length);
                    if (m_Lists == null)
                    {
                        m_Lists = new TileList[8][];

                        for (var i = 0; i < 8; ++i)
                        {
                            m_Lists[i] = new TileList[8];

                            for (var j = 0; j < 8; ++j)
                                m_Lists[i][j] = new TileList();
                        }
                    }

                    var lists = m_Lists;

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
                            tiles[i][j] = lists[i][j].ToArray();
                    }

                    return tiles;
                }
            }
            catch (EndOfStreamException)
            {
                if (DateTime.UtcNow >= m_NextStaticWarning)
                {
                    Console.WriteLine("Warning: Static EOS for {0} ({1}, {2})", m_Owner, x, y);
                    m_NextStaticWarning = DateTime.UtcNow + TimeSpan.FromMinutes(1.0);
                }

                return EmptyStaticBlock;
            }
        }

        public void Force()
        {
            if ((AssemblyHandler.Assemblies?.Length ?? 0) == 0)
                throw new Exception();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private unsafe LandTile[] ReadLandBlock(int x, int y)
        {
            try
            {
                var offset = (x * BlockHeight + y) * 196 + 4;

                if (m_MapIndex != null)
                    offset = m_MapIndex.Lookup(offset);

                m_MapStream.Seek(offset, SeekOrigin.Begin);

                var tiles = new LandTile[64];

                fixed (LandTile* pTiles = tiles)
                {
                    NativeReader.Read(m_MapStream.SafeFileHandle.DangerousGetHandle(), pTiles, 192);
                }

                return tiles;
            }
            catch
            {
                if (DateTime.UtcNow >= m_NextLandWarning)
                {
                    Console.WriteLine("Warning: Land EOS for {0} ({1}, {2})", m_Owner, x, y);
                    m_NextLandWarning = DateTime.UtcNow + TimeSpan.FromMinutes(1.0);
                }

                return m_InvalidLandBlock;
            }
        }

        public void Dispose()
        {
            m_MapIndex?.Close();
            m_MapStream?.Close();
            DataStream?.Close();
            m_IndexReader?.Close();
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
                throw new ArgumentException("Invalid UOP file.");

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
                    return m_Entries[i].m_Offset + (offset - total);

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
