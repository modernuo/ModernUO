using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Server.Engines.Pathing.Cache;
using Server.Items;
using Xunit;
using static Server.Tests.Pathfinding.PathingTestSupport;

namespace Server.Tests.Pathfinding;

/// <summary>
/// How the cache decides what to build, what to serve, and what to throw away: the promotion gate,
/// the four fallthrough routes out of <see cref="StepCache.TryGetMask"/>, the strata and swim
/// layers, and LRU eviction.
/// </summary>
[Collection("Sequential Pathfinding Tests")]
public class StepCacheLifecycleTests
{
    /// <summary>Resets to a known state and returns the singleton.</summary>
    private static StepCache FreshCache(int promotionThreshold)
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = promotionThreshold;
        return cache;
    }

    /// <summary>Builds the plain chunk and hands it back for a test to inject state into.</summary>
    private static StepChunk BuiltPlainChunk(StepCache cache, Map map)
    {
        cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);

        var chunk = cache.GetResidentChunk(map.MapID, PlainX >> 4, PlainY >> 4);
        Assert.NotNull(chunk);
        return chunk;
    }

    [Fact]
    public void Clear_OnEmptyCache_LeavesStatsZero()
    {
        var stats = FreshCache(2).GetStats();

        Assert.Equal(0, stats.ResidentChunks);
        Assert.Equal(0L, stats.Hits);
        Assert.Equal(0L, stats.BuildsTotal);
    }

    // ---- promotion gate ----

    /// <summary>
    /// A chunk nothing has shown sustained interest in must not be built. The caller reads
    /// IsHit=false as "use the slow path", which is the cheaper trade for a pet crossing a chunk
    /// once: BuildChunk costs far more than the handful of slow-path steps it would save.
    /// </summary>
    [Fact]
    public void FirstTouch_DefersBuild_AndFallsThrough()
    {
        var cache = FreshCache(promotionThreshold: 2);
        var map = TestMap;

        var lookup = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, lookup.HitKind);

        var stats = cache.GetStats();
        Assert.Equal(0, stats.ResidentChunks);
        Assert.Equal(0L, stats.BuildsTotal);
        Assert.Equal(0L, stats.MissesNotBuilt);
        Assert.Equal(1L, stats.FallthroughNotBuilt);
    }

    [SkippableFact]
    public void SecondTouchInsideWindow_PromotesAndServes()
    {
        TileDataRequirement.SkipIfMissing();

        var cache = FreshCache(promotionThreshold: 2);
        var map = TestMap;

        Assert.False(cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10).IsHit);

        var promoted = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);
        Assert.True(promoted.IsHit);
        Assert.Equal(CacheHitKind.Miss_NotBuilt, promoted.HitKind);
        Assert.Equal((byte)0xC1, promoted.WalkMask); // pinned: open plain, walkable N/NE/... per the bake
        Assert.Equal((sbyte)10, promoted.WalkZ_N);

        var stats = cache.GetStats();
        Assert.Equal(1, stats.ResidentChunks);
        Assert.Equal(1L, stats.MissesNotBuilt);
        Assert.Equal(1L, stats.BuildsTotal);
        Assert.Equal(1L, stats.FallthroughNotBuilt);

        // Now resident: a third query is a clean hit, not another miss.
        var hit = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);
        Assert.Equal(CacheHitKind.Hit, hit.HitKind);
        Assert.Equal((byte)0xC1, hit.WalkMask);
    }

    /// <summary>
    /// Two touches spread wider than the window are not interest, they're coincidence — a chunk
    /// someone glanced through, then an unrelated creature wandering past minutes later. The count
    /// restarts rather than accumulating toward a build.
    /// </summary>
    [Fact]
    public void SecondTouchAfterWindow_RestartsTheCount()
    {
        var cache = FreshCache(promotionThreshold: 2);
        cache.MissPromotionWindowMs = 1;

        var map = TestMap;

        Assert.False(cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10).IsHit);
        Thread.Sleep(20); // outrun the window

        var second = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);
        Assert.False(second.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, second.HitKind);
        Assert.Equal(0, cache.GetStats().ResidentChunks);
        Assert.Equal(2L, cache.GetStats().FallthroughNotBuilt);
    }

    /// <summary>
    /// The gate counts Finds, not probes. A single pathfind hits a chunk once per cell it expands
    /// there, so counting probes would cross any threshold on the second cell and gate nothing at
    /// all — the deferral would be dead code.
    /// </summary>
    [SkippableFact]
    public void ManyProbesInOneFind_CountAsOneTouch()
    {
        TileDataRequirement.SkipIfMissing();

        var cache = FreshCache(promotionThreshold: 2);
        var map = TestMap;

        cache.BeginFindGeneration();
        for (var i = 0; i < 8; i++)
        {
            // Eight different cells, all inside the same chunk.
            var lookup = cache.TryGetMask(map, PlainX + i, PlainY, sourceZ: 10);
            Assert.False(lookup.IsHit);
            Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, lookup.HitKind);
        }

        Assert.Equal(0, cache.GetStats().ResidentChunks);
        Assert.Equal(0L, cache.GetStats().BuildsTotal);
        Assert.Equal(8L, cache.GetStats().FallthroughNotBuilt);

        // A second Find is the second distinct touch, and crosses the threshold.
        cache.BeginFindGeneration();
        var promoted = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);

        Assert.Equal(CacheHitKind.Miss_NotBuilt, promoted.HitKind);
        Assert.Equal(1, cache.GetStats().ResidentChunks);
        Assert.Equal(1L, cache.GetStats().BuildsTotal);
    }

    /// <summary>Distinct Finds still don't promote if they straddle the window.</summary>
    [Fact]
    public void TwoFindsAcrossTheWindow_DoNotPromote()
    {
        var cache = FreshCache(promotionThreshold: 2);
        cache.MissPromotionWindowMs = 1;

        var map = TestMap;

        cache.BeginFindGeneration();
        Assert.False(cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10).IsHit);

        Thread.Sleep(20);

        cache.BeginFindGeneration();
        var second = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);

        Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, second.HitKind);
        Assert.Equal(0, cache.GetStats().ResidentChunks);
    }

    [Fact]
    public void EachChunkIsTrackedSeparately()
    {
        var cache = FreshCache(promotionThreshold: 2);
        var map = TestMap;

        // One touch each, in two different chunks: neither reaches the threshold on its own.
        Assert.False(cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10).IsHit);
        Assert.False(cache.TryGetMask(map, 1600, 1700, sourceZ: 10).IsHit);

        Assert.Equal(0, cache.GetStats().ResidentChunks);
        Assert.Equal(2L, cache.GetStats().FallthroughNotBuilt);
    }

    // ---- fallthrough routes ----

    [Fact]
    public void OffMapCell_FallsThrough()
    {
        var lookup = FreshCache(2).TryGetMask(TestMap, -1, -1, sourceZ: 0);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_OffMap, lookup.HitKind);
        Assert.Equal((byte)0, lookup.WalkMask);
    }

    /// <summary>
    /// A multi's cells fall through, and so does the 1-cell halo around it: a cell's mask encodes
    /// the edges TO its neighbours, so a wall one cell over has to block them.
    /// </summary>
    [SkippableFact]
    public void MultiCoveredCell_AndItsHalo_FallThrough()
    {
        TileDataRequirement.SkipIfMissing();

        var cache = FreshCache(promotionThreshold: 1);
        var map = TestMap;

        // A cell nowhere near a multi still serves from the static cache.
        Assert.True(cache.TryGetMask(map, PlainX, PlainY, 10).IsHit);

        // Mark an isolated sector as multi-bearing. Sector.HasMultis only tests Count > 0 and the
        // fallthrough never dereferences the multi, so a single null entry is enough — no real
        // BaseMulti needed.
        const int mx = 2000;
        const int my = 2000;
        var sx = mx >> 4;
        var sy = my >> 4;

        var sector = map.GetRealSector(sx, sy);
        var multisField = typeof(Map.Sector).GetField("_multis", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(multisField);

        var original = multisField.GetValue(sector);
        try
        {
            multisField.SetValue(sector, new List<BaseMulti> { null });

            // Inside the multi's sector.
            Assert.Equal(CacheHitKind.Fallthrough_Multi, cache.TryGetMask(map, mx, my, 0).HitKind);

            // Last cell of the neighbouring sector: its halo reaches across the boundary.
            Assert.Equal(CacheHitKind.Fallthrough_Multi, cache.TryGetMask(map, sx * 16 - 1, my, 0).HitKind);

            // One cell further out: halo no longer reaches, so the static cache handles it.
            Assert.NotEqual(CacheHitKind.Fallthrough_Multi, cache.TryGetMask(map, sx * 16 - 2, my, 0).HitKind);

            Assert.True(cache.GetStats().FallthroughMulti >= 2);
        }
        finally
        {
            multisField.SetValue(sector, original);
        }
    }

    /// <summary>A query too far from the cell's baked Z gets no answer, rather than a wrong one.</summary>
    [Fact]
    public void SourceZFarFromBake_FallsThrough()
    {
        var cache = FreshCache(promotionThreshold: 1);
        var map = TestMap;

        cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);
        var before = cache.GetStats().FallthroughSourceZMismatch;

        var lookup = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 100);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_SourceZMismatch, lookup.HitKind);
        Assert.Equal(before + 1L, cache.GetStats().FallthroughSourceZMismatch);
    }

    // ---- strata ----

    [Fact]
    public void Stratum_MatchingQueryZ_IsServed()
    {
        var cache = FreshCache(promotionThreshold: 1);
        var map = TestMap;
        var chunk = BuiltPlainChunk(cache, map);

        var offsets = NoStrataOffsets();
        offsets[CellIndex(PlainX, PlainY)] = 0;
        chunk.SetStrata(offsets, OneStratum(zCenter: 42, walkMask: 0b0000_0011, walkZs: [42, 42]));

        var lookup = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 42);

        Assert.True(lookup.IsHit);
        Assert.Equal((byte)0b0000_0011, lookup.WalkMask);
        Assert.Equal((sbyte)42, lookup.WalkZ_N);
        Assert.Equal((sbyte)42, lookup.WalkZ_NE);
    }

    [Fact]
    public void Stratum_QueryZOutOfReach_FallsThrough()
    {
        var cache = FreshCache(promotionThreshold: 1);
        var map = TestMap;
        var chunk = BuiltPlainChunk(cache, map);

        var offsets = NoStrataOffsets();
        offsets[CellIndex(PlainX, PlainY)] = 0;
        chunk.SetStrata(offsets, OneStratum(zCenter: 42));

        // 10 is more than StepHeight from the only stratum, so nothing can answer.
        var lookup = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_MultiZ, lookup.HitKind);
    }

    /// <summary>
    /// A cell flagged multi-Z is served only from its strata. If it has none that match — here, a
    /// zero-count record — it must fall through rather than quietly fall back to the main mask,
    /// which was baked for a different surface.
    /// </summary>
    [Fact]
    public void MultiZCell_WithNoUsableStratum_FallsThrough()
    {
        var cache = FreshCache(promotionThreshold: 1);
        var map = TestMap;
        var chunk = BuiltPlainChunk(cache, map);

        var before = cache.GetStats().FallthroughMultiZ;

        var offsets = NoStrataOffsets();
        offsets[CellIndex(PlainX, PlainY)] = 0;
        chunk.SetStrata(offsets, [0]); // a record declaring zero strata

        var lookup = cache.TryGetMask(map, PlainX, PlainY, sourceZ: 10);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_MultiZ, lookup.HitKind);
        Assert.Equal(before + 1L, cache.GetStats().FallthroughMultiZ);
    }

    // ---- swim layer ----

    [Fact]
    public void SwimLayer_QueryAtWaterZ_IsServedFromTheLayer()
    {
        var cache = FreshCache(promotionThreshold: 1);
        var map = TestMap;
        var chunk = BuiltPlainChunk(cache, map);

        var cell = CellIndex(PlainX, PlainY);
        var bakedZ = chunk.SourceZ[cell];

        // Place the water surface well clear of the walk surface, so the primary source-Z guard is
        // guaranteed to reject the swim query and hand it to the layer.
        var swimZ = (sbyte)(bakedZ - 20);

        chunk.AllocateSwimLayer();
        chunk.SwimSourceZ[cell] = swimZ;
        chunk.SwimMask[cell] = 0b0000_0011;
        chunk.SwimZN_Layer[cell] = swimZ;
        chunk.SwimZNE_Layer[cell] = swimZ;

        // At the walk surface, the layer is not consulted at all.
        Assert.Equal(CacheHitKind.Hit, cache.TryGetMask(map, PlainX, PlainY, bakedZ).HitKind);

        var swim = cache.TryGetMask(map, PlainX, PlainY, swimZ);
        Assert.True(swim.IsHit);
        Assert.Equal((byte)0, swim.WalkMask); // a swimmer can't walk
        Assert.Equal((byte)0b0000_0011, swim.WetMask);
        Assert.Equal(swimZ, swim.SwimZ_N);
        Assert.Equal(swimZ, swim.SwimZ_NE);
    }

    /// <summary>
    /// An inland cell in a chunk that has a swim layer carries the NoSwimLayerCell sentinel. That
    /// sentinel is sbyte.MinValue, so a query at sbyte.MinValue would match it exactly on a naive
    /// distance check — the guard has to reject the sentinel before measuring anything.
    /// </summary>
    [Fact]
    public void SwimLayer_SentinelCell_IsNeverMatched()
    {
        var cache = FreshCache(promotionThreshold: 1);
        var map = TestMap;
        var chunk = BuiltPlainChunk(cache, map);

        chunk.AllocateSwimLayer(); // allocated for some other cell; this one stays at the sentinel

        var cell = CellIndex(PlainX, PlainY);
        Assert.Equal(StepChunk.NoSwimLayerCell, chunk.SwimSourceZ[cell]);

        var before = cache.GetStats().FallthroughSourceZMismatch;
        var lookup = cache.TryGetMask(map, PlainX, PlainY, sourceZ: sbyte.MinValue);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_SourceZMismatch, lookup.HitKind);
        Assert.Equal(before + 1L, cache.GetStats().FallthroughSourceZMismatch);
    }

    // ---- eviction ----

    [Fact]
    public void LruCap_EvictsDownToTheCap()
    {
        var cache = FreshCache(promotionThreshold: 1);
        cache.MaxResidentChunks = 4;

        try
        {
            var map = TestMap;

            // Five chunks into a cache that holds four.
            for (var i = 0; i < 5; i++)
            {
                cache.TryGetMask(map, PlainX + i * 16, PlainY, sourceZ: 10);
                Thread.Sleep(2); // separate their LastTouchedTicks so LRU has something to order by
            }

            cache.EnforceLruCap();

            Assert.Equal(4, cache.GetStats().ResidentChunks);
            Assert.True(cache.GetStats().EvictionsByLruCap >= 1L);
            Assert.True(cache.ResidentIndexInSync(), "eviction desynced the key list from the resident set");
        }
        finally
        {
            cache.MaxResidentChunks = 8192;
        }
    }
}
