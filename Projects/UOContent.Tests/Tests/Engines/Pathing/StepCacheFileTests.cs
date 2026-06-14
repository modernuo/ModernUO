using System;
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
        cache.MissPromotionThreshold = 1; // eager build to populate chunks for save

        var map = Map.Maps[1];
        Assert.NotNull(map);

        // Populate three distinct chunks by querying different sectors. Query at each cell's
        // real standable surface Z (where the cache anchors) so first-touch yields a clean
        // hit rather than an off-surface fallthrough.
        var sourceQueries = new[] { (1500, 1600), (1516, 1600), (1500, 1616) };
        var standZ = new sbyte[sourceQueries.Length];
        {
            Span<sbyte> surfZ = stackalloc sbyte[16];
            for (var i = 0; i < sourceQueries.Length; i++)
            {
                var (qx, qy) = sourceQueries[i];
                var n = StepProbe.ComputeStandableSurfaceZs(map, qx, qy, surfZ);
                Assert.True(n > 0, $"({qx},{qy}) has no standable surface — bad test cell");
                standZ[i] = surfZ[0];
            }
        }
        for (var i = 0; i < sourceQueries.Length; i++)
        {
            var (x, y) = sourceQueries[i];
            cache.TryGetMask(map, x, y, standZ[i]);
        }

        Assert.Equal(3, cache.GetStats().ResidentChunks);
        Assert.Equal(3L, cache.GetStats().BuildsTotal);

        // Snapshot the answers we expect to recover after round-trip.
        var expected = new StepMask[sourceQueries.Length];
        for (var i = 0; i < sourceQueries.Length; i++)
        {
            var (x, y) = sourceQueries[i];
            expected[i] = cache.TryGetMask(map, x, y, standZ[i]);
        }

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-roundtrip-{Guid.NewGuid():N}.swb");
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
                var lookup = cache.TryGetMask(map, x, y, standZ[i]);
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

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-missing-{Guid.NewGuid():N}.swb");
        Assert.False(cache.TryOpenLazyReader(path, mapId: 1));
        Assert.Equal(0, cache.OpenLazyReaderCount);
    }

    /// <summary>
    /// HasLazyReader is the boot prebake's skip predicate (PathCacheCommands.Initialize): a map
    /// with an open, fingerprint-valid reader needs no bake. Lock the open/clear contract.
    /// </summary>
    [Fact]
    public void HasLazyReader_TracksOpenAndClear()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];
        Assert.NotNull(map);
        Assert.False(cache.HasLazyReader(map.MapID));

        // Build + save a chunk so there's a valid .swb to open.
        cache.MissPromotionThreshold = 1;
        Span<sbyte> surfZ = stackalloc sbyte[16];
        Assert.True(StepProbe.ComputeStandableSurfaceZs(map, 1500, 1600, surfZ) > 0);
        cache.TryGetMask(map, 1500, 1600, surfZ[0]);

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-haslazy-{Guid.NewGuid():N}.swb");
        try
        {
            Assert.True(cache.SaveToFile(path, map.MapID) > 0);
            cache.Clear();
            Assert.False(cache.HasLazyReader(map.MapID));

            Assert.True(cache.TryOpenLazyReader(path, map.MapID));
            Assert.True(cache.HasLazyReader(map.MapID)); // open → true

            cache.Clear();
            Assert.False(cache.HasLazyReader(map.MapID)); // clear closes the reader → false
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

    [Fact]
    public void TryOpenLazyReader_BadMagic_ReturnsFalse()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-badmagic-{Guid.NewGuid():N}.swb");
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

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-stalehash-{Guid.NewGuid():N}.swb");
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
        cache.MissPromotionThreshold = 1; // eager build to populate chunks for save

        var map = Map.Maps[1];
        Assert.NotNull(map);

        // Populate a handful of chunks.
        var coords = new[]
        {
            (1500, 1600), (1516, 1600), (1500, 1616), (1516, 1616), (1532, 1600)
        };
        foreach (var (x, y) in coords)
        {
            cache.TryGetMask(map, x, y, sourceZ: 10);
        }

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-lazy-{Guid.NewGuid():N}.swb");
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

    /// <summary>
    /// First-touch on a chunk that the lazy reader can satisfy must NOT route through the
    /// miss tracker — file-loaded chunks represent an explicit prior decision to keep
    /// them warm. This guards the deployment shape where an admin ships .swb files and
    /// expects the very first NPC pathfind in any region to use cache (not slow path).
    /// </summary>
    /// <summary>
    /// A chunk with an injected swim layer must serialize and deserialize via the lazy
    /// reader without losing the layer. Validates v3 file format end-to-end: swim layer
    /// fields survive Save → Clear → LazyOpen → first-touch query.
    /// </summary>
    [Fact]
    public void SwimLayer_RoundTrips_ThroughLazyReader()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1;

        var map = Map.Maps[1];
        Assert.NotNull(map);

        // Build a chunk and inject a synthetic swim layer onto cell (1500, 1600).
        cache.TryGetMask(map, 1500, 1600, sourceZ: 10);

        var chunksField = typeof(StepCache).GetField(
            "_chunks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var chunks = (System.Collections.Generic.Dictionary<long, StepChunk>)chunksField!.GetValue(cache)!;
        var key = StepCache.EncodeKey(map.MapID, 1500 >> 4, 1600 >> 4);
        var chunk = chunks[key];

        chunk.AllocateSwimLayer();
        var cellIndex = ((1600 - ((1600 >> 4) << 4)) << 4) | (1500 - ((1500 >> 4) << 4));
        chunk.SwimSourceZ[cellIndex]   = -7;
        chunk.SwimMask[cellIndex]      = 0b0000_1111;
        chunk.SwimZN_Layer[cellIndex]  = -7;
        chunk.SwimZNE_Layer[cellIndex] = -7;
        chunk.SwimZE_Layer[cellIndex]  = -7;
        chunk.SwimZSE_Layer[cellIndex] = -7;

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-swim-{Guid.NewGuid():N}.swb");
        try
        {
            Assert.Equal(1, cache.SaveToFile(path, map.MapID));

            cache.Clear();
            cache.MissPromotionThreshold = 1;
            Assert.True(cache.TryOpenLazyReader(path, map.MapID));

            // Pull the chunk back via a query at swim Z; the layer must hit and serve our
            // injected mask. Walk-Z query of the same cell should still hit the walk
            // layer with whatever the bake produced.
            var swim = cache.TryGetMask(map, 1500, 1600, sourceZ: -7);
            Assert.True(swim.IsHit);
            Assert.Equal((byte)0, swim.WalkMask);
            Assert.Equal((byte)0b0000_1111, swim.WetMask);
            Assert.Equal((sbyte)-7, swim.SwimZ_N);
            Assert.Equal((sbyte)-7, swim.SwimZ_E);
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

    /// <summary>
    /// PreloadOnLazyOpen=true must materialize every chunk in the .swb file into the
    /// resident set immediately, eliminating first-touch file-read latency. Counterpart
    /// to <see cref="LazyReader_DoesNotMaterializeUntilQueried"/> which proves the
    /// default lazy behavior.
    /// </summary>
    [Fact]
    public void TryOpenLazyReader_WithPreloadFlag_MaterializesAllChunksImmediately()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1;

        var map = Map.Maps[1];
        Assert.NotNull(map);

        var coords = new[]
        {
            (1500, 1600), (1516, 1600), (1500, 1616), (1516, 1616), (1532, 1600)
        };
        foreach (var (x, y) in coords)
        {
            cache.TryGetMask(map, x, y, sourceZ: 10);
        }

        var path = Path.Combine(Path.GetTempPath(), $"step-cache-preload-{Guid.NewGuid():N}.swb");
        try
        {
            Assert.Equal(coords.Length, cache.SaveToFile(path, map.MapID));

            cache.Clear();
            cache.PreloadOnLazyOpen = true;
            try
            {
                Assert.True(cache.TryOpenLazyReader(path, map.MapID));

                // Every chunk should be resident — no further queries needed.
                Assert.Equal(coords.Length, cache.GetStats().ResidentChunks);
                Assert.Equal(0L, cache.GetStats().BuildsTotal); // came from file, not baker

                // Subsequent query is a clean Hit, not a Miss_NotBuilt.
                var lookup = cache.TryGetMask(map, coords[0].Item1, coords[0].Item2, sourceZ: 10);
                Assert.Equal(CacheHitKind.Hit, lookup.HitKind);
                Assert.Equal(coords.Length, cache.GetStats().ResidentChunks);
            }
            finally
            {
                cache.PreloadOnLazyOpen = false;
            }
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

    [Fact]
    public void LazyReaderHit_BypassesMissTrackerOnFirstTouch()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1; // eager build for save phase

        var map = Map.Maps[1];

        // Build + save one chunk.
        cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        var path = Path.Combine(Path.GetTempPath(), $"step-cache-bypass-{Guid.NewGuid():N}.swb");
        try
        {
            Assert.Equal(1, cache.SaveToFile(path, map.MapID));

            // Reset to a fresh state with the file open as a lazy reader and the deferred
            // promotion threshold restored to 2.
            cache.Clear();
            cache.MissPromotionThreshold = 2;
            Assert.True(cache.TryOpenLazyReader(path, map.MapID));

            // First touch must NOT return Fallthrough_NotBuilt — the lazy reader has the
            // chunk and serves it before the miss tracker is consulted.
            var lookup = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
            Assert.True(lookup.IsHit);
            Assert.Equal(CacheHitKind.Miss_NotBuilt, lookup.HitKind);
            Assert.Equal(1, cache.GetStats().ResidentChunks);
            Assert.Equal(0L, cache.GetStats().FallthroughNotBuilt);
            Assert.Equal(0L, cache.GetStats().BuildsTotal); // came from file, not baker
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
