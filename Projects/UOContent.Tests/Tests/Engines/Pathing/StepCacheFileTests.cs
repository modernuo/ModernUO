using System.IO;
using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class StepCacheFileTests
{
    /// <summary>
    /// Round-trip a populated cache through a .swb file under lazy loading: build chunks,
    /// save, clear (closes lazy readers + drops residents), open as lazy backing store,
    /// then re-query and verify chunks come back from the file (not the runtime baker).
    /// </summary>
    [Fact]
    public void RoundTrip_LazyLoad_PreservesChunkMaskAndZ()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];
        Assert.NotNull(map);

        // Populate three distinct chunks by querying different sectors.
        var sourceQueries = new[] { (1500, 1600), (1516, 1600), (1500, 1616) };
        foreach (var (x, y) in sourceQueries)
        {
            cache.TryGetMask(map, x, y, sourceZ: 10);
        }

        Assert.Equal(3, cache.GetStats().ResidentChunks);
        Assert.Equal(3L, cache.GetStats().BuildsTotal);

        // Snapshot the answers we expect to recover after round-trip.
        var expected = new StepMask[sourceQueries.Length];
        for (var i = 0; i < sourceQueries.Length; i++)
        {
            var (x, y) = sourceQueries[i];
            expected[i] = cache.TryGetMask(map, x, y, sourceZ: 10);
        }

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-roundtrip-{System.Guid.NewGuid():N}.swb");
        try
        {
            var written = cache.SaveToFile(path, map.MapID);
            Assert.Equal(3, written);

            cache.Clear();
            Assert.Equal(0, cache.GetStats().ResidentChunks);

            Assert.True(cache.TryOpenLazyReader(path, map.MapID));
            Assert.Equal(1, cache.OpenLazyReaderCount);

            // Lazy: opening doesn't materialize chunks, so the resident set stays empty
            // until we query.
            Assert.Equal(0, cache.GetStats().ResidentChunks);
            Assert.Equal(0L, cache.GetStats().BuildsTotal);

            // Sanity: the lazy reader's index covers each chunk we're about to query.
            foreach (var (x, y) in sourceQueries)
            {
                Assert.True(cache.LazyReaderHasChunk(map.MapID, x >> 4, y >> 4),
                    $"lazy reader missing chunk ({x >> 4},{y >> 4})");
            }

            for (var i = 0; i < sourceQueries.Length; i++)
            {
                var (x, y) = sourceQueries[i];
                var lookup = cache.TryGetMask(map, x, y, sourceZ: 10);
                Assert.Equal(CacheHitKind.Miss_NotBuilt, lookup.HitKind);
                Assert.Equal(expected[i].WalkMask, lookup.WalkMask);
                Assert.Equal(expected[i].WetMask, lookup.WetMask);
                Assert.Equal(expected[i].WalkZ_N, lookup.WalkZ_N);
                Assert.Equal(expected[i].WalkZ_NW, lookup.WalkZ_NW);
            }

            Assert.Equal(0L, cache.GetStats().BuildsTotal);
            Assert.Equal(3, cache.GetStats().ResidentChunks);
        }
        finally
        {
            cache.Clear(); // Releases the FileStream so the delete below succeeds on Windows.
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void TryOpenLazyReader_MissingFile_ReturnsFalse()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-missing-{System.Guid.NewGuid():N}.swb");
        Assert.False(cache.TryOpenLazyReader(path, mapId: 1));
        Assert.Equal(0, cache.OpenLazyReaderCount);
    }

    [Fact]
    public void TryOpenLazyReader_BadMagic_ReturnsFalse()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-badmagic-{System.Guid.NewGuid():N}.swb");
        try
        {
            File.WriteAllBytes(path, new byte[]
            {
                0xDE, 0xAD, 0xBE, 0xEF,
                0x01, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            });
            Assert.False(cache.TryOpenLazyReader(path, mapId: 1));
            Assert.Equal(0, cache.OpenLazyReaderCount);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void TryOpenLazyReader_FingerprintMismatch_ReturnsFalse()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];
        cache.TryGetMask(map, 1500, 1600, sourceZ: 10);

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-stalehash-{System.Guid.NewGuid():N}.swb");
        try
        {
            cache.SaveToFile(path, map.MapID);

            // Corrupt the Fingerprint field at byte offset 12 (Magic[4] + Version[4] + MapId[4]).
            var bytes = File.ReadAllBytes(path);
            for (var i = 12; i < 20; i++)
            {
                bytes[i] ^= 0xFF;
            }
            File.WriteAllBytes(path, bytes);

            cache.Clear();
            Assert.False(cache.TryOpenLazyReader(path, map.MapID));
            Assert.Equal(0, cache.OpenLazyReaderCount);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// The lazy reader holds a FileStream open. Saving 100 chunks then opening them all
    /// must NOT materialize any of them in the resident set — that's the whole point of
    /// lazy loading on RAM-constrained shards. A query for one specific chunk pulls only
    /// that one chunk into memory.
    /// </summary>
    [Fact]
    public void LazyReader_DoesNotMaterializeUntilQueried()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];
        Assert.NotNull(map);

        // Populate a handful of chunks.
        var coords = new (int, int)[]
        {
            (1500, 1600), (1516, 1600), (1500, 1616), (1516, 1616), (1532, 1600)
        };
        foreach (var (x, y) in coords)
        {
            cache.TryGetMask(map, x, y, sourceZ: 10);
        }

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-lazy-{System.Guid.NewGuid():N}.swb");
        try
        {
            Assert.Equal(coords.Length, cache.SaveToFile(path, map.MapID));

            cache.Clear();
            Assert.True(cache.TryOpenLazyReader(path, map.MapID));

            // Open succeeded — but no chunks resident yet.
            Assert.Equal(0, cache.GetStats().ResidentChunks);

            // Query one specific chunk: only that chunk lands in the resident set.
            cache.TryGetMask(map, coords[0].Item1, coords[0].Item2, sourceZ: 10);
            Assert.Equal(1, cache.GetStats().ResidentChunks);
            Assert.Equal(0L, cache.GetStats().BuildsTotal); // resolved from file, not baker
        }
        finally
        {
            cache.Clear();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
