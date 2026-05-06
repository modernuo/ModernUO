using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class StepCacheLifecycleTests
{
    [Fact]
    public void Singleton_IsAvailable()
    {
        var cache = StepCache.Instance;
        Assert.NotNull(cache);
    }

    [Fact]
    public void Clear_OnEmptyCache_LeavesStatsZero()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var stats = cache.GetStats();
        Assert.Equal(0, stats.ResidentChunks);
        Assert.Equal(0L, stats.Hits);
        Assert.Equal(0L, stats.BuildsTotal);
    }

    [Fact]
    public void TryGetMask_FirstQuery_BuildsChunkAndReturnsBakerOutput()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];
        Assert.NotNull(map);

        // Pinned cell (1500, 1600, z=10): mask=0xC1
        var lookup = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);

        Assert.True(lookup.IsHit);
        Assert.Equal(CacheHitKind.Miss_NotBuilt, lookup.HitKind);
        Assert.Equal((byte)0xC1, lookup.WalkMask);
        Assert.Equal((sbyte)10, lookup.WalkZ_N);
        Assert.Equal((sbyte)10, lookup.WalkZ_W);
        Assert.Equal((sbyte)10, lookup.WalkZ_NW);

        var stats = cache.GetStats();
        Assert.Equal(1, stats.ResidentChunks);
        Assert.Equal(1L, stats.MissesNotBuilt);
        Assert.Equal(1L, stats.BuildsTotal);

        // Second query of same cell → Hit
        var lookup2 = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.True(lookup2.IsHit);
        Assert.Equal(CacheHitKind.Hit, lookup2.HitKind);
        Assert.Equal((byte)0xC1, lookup2.WalkMask);

        var stats2 = cache.GetStats();
        Assert.Equal(1, stats2.ResidentChunks);
        Assert.Equal(1L, stats2.Hits);
    }

    [Fact]
    public void TryGetMask_OffMap_ReturnsFalseFallthrough()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];

        var lookup = cache.TryGetMask(map, -1, -1, sourceZ: 0);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_OffMap, lookup.HitKind);
        Assert.Equal((byte)0, lookup.WalkMask);
    }

    [Fact]
    public void MultisVersion_Bump_TriggersDirtyRebuild()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];
        var sector = map.GetRealSector(1500 >> 4, 1600 >> 4);

        // First query: builds chunk, snapshots current MultisVersion.
        Assert.Equal(CacheHitKind.Miss_NotBuilt, cache.TryGetMask(map, 1500, 1600, 10).HitKind);

        // Bump _multisVersion via reflection.
        var versionField = typeof(Map.Sector).GetField(
            "_multisVersion",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        Assert.NotNull(versionField);
        var current = (int)versionField.GetValue(sector);
        versionField.SetValue(sector, current + 1);

        // Second query: detects version mismatch, rebuilds.
        Assert.Equal(CacheHitKind.Miss_DirtyRebuild, cache.TryGetMask(map, 1500, 1600, 10).HitKind);

        var stats = cache.GetStats();
        Assert.Equal(1L, stats.MissesDirtyRebuild);
        Assert.Equal(2L, stats.BuildsTotal);

        // Mutual-exclusivity invariant: every successful TryGetMask hits exactly one
        // outcome counter. Two queries above both returned true (the test cell is not
        // multi-Z and not off-map), so the three outcome counters must sum to 2 and the
        // fallthrough counters must be zero.
        Assert.Equal(2L, stats.MissesNotBuilt + stats.MissesDirtyRebuild + stats.Hits);
        Assert.Equal(0L, stats.FallthroughMultiZ);
        Assert.Equal(0L, stats.FallthroughOffMap);
    }

    [Fact]
    public void MultiZCell_RoutesToFallthrough()
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];

        // Build a chunk first so it exists.
        cache.TryGetMask(map, 1500, 1600, 10);

        // Snapshot current FallthroughMultiZ in case (1500, 1600) is naturally multi-Z
        // in real tile data; we only assert the synthetic injection produces a delta of 1.
        var preInjectionFallthroughMultiZ = cache.GetStats().FallthroughMultiZ;

        // Inject a multi-Z bit via reflection on the resident chunk.
        var chunksField = typeof(StepCache).GetField(
            "_chunks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        Assert.NotNull(chunksField);
        var chunks = (System.Collections.Generic.Dictionary<long, StepChunk>)chunksField.GetValue(cache);

        var key = StepCache.EncodeKey(map.MapID, 1500 >> 4, 1600 >> 4);
        Assert.True(chunks.ContainsKey(key));
        var chunk = chunks[key];

        // Mark cell at (1500, 1600) within the chunk as multi-Z.
        var cellIndex = ((1600 - ((1600 >> 4) << 4)) << 4) | (1500 - ((1500 >> 4) << 4));
        chunk.MarkCellMultiZ(cellIndex);

        var lookup = cache.TryGetMask(map, 1500, 1600, 10);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_MultiZ, lookup.HitKind);

        var stats = cache.GetStats();
        Assert.Equal(preInjectionFallthroughMultiZ + 1L, stats.FallthroughMultiZ);
    }

    [Fact]
    public void LruCap_OverflowEvictsToCap()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MaxResidentChunks = 4;

        try
        {
            var map = Map.Maps[1];

            // Build 5 distinct chunks by querying different sectors.
            for (var i = 0; i < 5; i++)
            {
                var x = 1500 + (i * 16);
                var y = 1600;
                cache.TryGetMask(map, x, y, 10);
                System.Threading.Thread.Sleep(2); // ensure LastTouchedTicks differs
            }

            cache.EnforceLruCap();

            Assert.Equal(4, cache.GetStats().ResidentChunks);
            Assert.True(cache.GetStats().EvictionsByLruCap >= 1L);

            // _keysList must stay in lockstep with _chunks. A desync would silently
            // break sampled eviction (KeyNotFoundException on stale keys, or a stuck
            // resident set on missing keys).
            var chunksField = typeof(StepCache).GetField(
                "_chunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var keysListField = typeof(StepCache).GetField(
                "_keysList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var chunks = (System.Collections.Generic.Dictionary<long, StepChunk>)chunksField.GetValue(cache);
            var keysList = (System.Collections.Generic.List<long>)keysListField.GetValue(cache);
            Assert.Equal(chunks.Count, keysList.Count);
            foreach (var k in keysList)
            {
                Assert.True(chunks.ContainsKey(k), $"keysList holds key {k} not in _chunks");
            }
        }
        finally
        {
            cache.MaxResidentChunks = 8192;
        }
    }
}
