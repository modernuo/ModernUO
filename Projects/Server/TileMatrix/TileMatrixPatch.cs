using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server
{
    public class TileMatrixPatch
    {
        private StaticTile[] _tileBuffer = new StaticTile[128];

        public static bool PatchLandEnabled { get; private set; }
        public static bool PatchStaticsEnabled { get; private set; }

        public int LandBlocks { get; }
        public int StaticBlocks { get; }

        public TileMatrixPatch(TileMatrix matrix, int index)
        {
            if (PatchLandEnabled)
            {
                var mapDataPath = Core.FindDataFile($"mapdif{index}.mul", false);
                var mapIndexPath = Core.FindDataFile($"mapdifl{index}.mul", false);

                if (mapDataPath != null && mapIndexPath != null)
                {
                    LandBlocks = PatchLand(matrix, mapDataPath, mapIndexPath);
                }
            }

            if (PatchStaticsEnabled)
            {
                var staDataPath = Core.FindDataFile($"stadif{index}.mul", false);
                var staIndexPath = Core.FindDataFile($"stadifl{index}.mul", false);
                var staLookupPath = Core.FindDataFile($"stadifi{index}.mul", false);

                if (staDataPath != null && staIndexPath != null && staLookupPath != null)
                {
                    StaticBlocks = PatchStatics(matrix, staDataPath, staIndexPath, staLookupPath);
                }
            }
        }

        public static void Configure()
        {
            PatchLandEnabled = ServerConfiguration.GetOrUpdateSetting("maps.enableMapDiffPatches", !Core.HS);
            PatchStaticsEnabled = ServerConfiguration.GetOrUpdateSetting("maps.enableStaticsDiffPatches", true);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private unsafe int PatchLand(TileMatrix matrix, string dataPath, string indexPath)
        {
            using var fsData = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var fsIndex = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var indexReader = new BinaryReader(fsIndex);

            var count = (int)(indexReader.BaseStream.Length / 4);

            for (var i = 0; i < count; i++)
            {
                var blockID = indexReader.ReadInt32();
                var x = Math.DivRem(blockID, matrix.BlockHeight, out var y);

                fsData.Seek(4, SeekOrigin.Current);

                var tiles = new LandTile[64];
                fixed (LandTile* pTiles = tiles)
                {
                    fsData.Read(new Span<byte>(pTiles, 192));
                }

                matrix.SetLandBlock(x, y, tiles);
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private unsafe int PatchStatics(TileMatrix matrix, string dataPath, string indexPath, string lookupPath)
        {
            using var fsData = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var fsIndex = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var fsLookup = new FileStream(lookupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var indexReader = new BinaryReader(fsIndex);
            using var lookupReader = new BinaryReader(fsLookup);

            var count = (int)(indexReader.BaseStream.Length / 4);

            var lists = new TileList[8][];

            for (var x = 0; x < 8; ++x)
            {
                lists[x] = new TileList[8];

                for (var y = 0; y < 8; ++y)
                {
                    lists[x][y] = new TileList();
                }
            }

            for (var i = 0; i < count; ++i)
            {
                var blockID = indexReader.ReadInt32();
                var blockX = blockID / matrix.BlockHeight;
                var blockY = blockID % matrix.BlockHeight;

                var offset = lookupReader.ReadInt32();
                var length = lookupReader.ReadInt32();
                lookupReader.ReadInt32(); // Extra

                if (offset < 0 || length <= 0)
                {
                    matrix.SetStaticBlock(blockX, blockY, matrix.EmptyStaticBlock);
                    continue;
                }

                fsData.Seek(offset, SeekOrigin.Begin);

                var tileCount = length / 7;

                if (_tileBuffer.Length < tileCount)
                {
                    _tileBuffer = new StaticTile[tileCount];
                }

                var staTiles = _tileBuffer;

                fixed (StaticTile* pTiles = staTiles)
                {
                    fsData.Read(new Span<byte>(pTiles, length));

                    StaticTile* pCur = pTiles, pEnd = pTiles + tileCount;

                    while (pCur < pEnd)
                    {
                        lists[pCur->m_X & 0x7][pCur->m_Y & 0x7].Add(pCur->m_ID, pCur->m_Z);
                        pCur += 1;
                    }

                    var tiles = new StaticTile[8][][];

                    for (var x = 0; x < 8; ++x)
                    {
                        tiles[x] = new StaticTile[8][];

                        for (var y = 0; y < 8; ++y)
                        {
                            tiles[x][y] = lists[x][y].ToArray();
                        }
                    }

                    matrix.SetStaticBlock(blockX, blockY, tiles);
                }
            }

            return count;
        }
    }
}
