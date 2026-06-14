using System.Collections.Generic;
using System.Reflection;
using Server.Engines.Pathing.Cache;
using Server.Items;
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
    public void TryGetMask_FirstTouchOnUnbuiltChunk_DefersBuildAndReturnsFallthrough()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 2;

        var map = Map.Maps[1];
        Assert.NotNull(map);

        // First touch on a chunk that has no resident copy and no lazy reader behind it
        // must NOT eagerly build. Caller (BitmapAStarAlgorithm) interprets IsHit=false as
        // "use slow path" — pets/hireables passing briefly through a chunk avoid the
        // ~700µs BuildChunk cost they'd never amortize.
        var lookup = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, lookup.HitKind);

        var stats = cache.GetStats();
        Assert.Equal(0, stats.ResidentChunks);
        Assert.Equal(0L, stats.BuildsTotal);
        Assert.Equal(0L, stats.MissesNotBuilt);
        Assert.Equal(1L, stats.FallthroughNotBuilt);
    }

    [Fact]
    public void TryGetMask_SecondTouchWithinWindow_PromotesAndBuilds()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 2;

        var map = Map.Maps[1];

        // First touch defers; second touch inside the promotion window builds + serves.
        // Pinned cell (1500, 1600, z=10): mask=0xC1
        var first = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.False(first.IsHit);

        var second = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.True(second.IsHit);
        Assert.Equal(CacheHitKind.Miss_NotBuilt, second.HitKind);
        Assert.Equal((byte)0xC1, second.WalkMask);
        Assert.Equal((sbyte)10, second.WalkZ_N);

        var stats = cache.GetStats();
        Assert.Equal(1, stats.ResidentChunks);
        Assert.Equal(1L, stats.MissesNotBuilt);
        Assert.Equal(1L, stats.BuildsTotal);
        Assert.Equal(1L, stats.FallthroughNotBuilt);

        // Third query of same cell → Hit (chunk now resident).
        var third = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.True(third.IsHit);
        Assert.Equal(CacheHitKind.Hit, third.HitKind);
        Assert.Equal((byte)0xC1, third.WalkMask);
    }

    [Fact]
    public void TryGetMask_SecondTouchAfterWindow_RestartsCounterAndDefers()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 2;
        cache.MissPromotionWindowMs = 1; // 1ms window for testability

        var map = Map.Maps[1];

        var first = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.False(first.IsHit);

        System.Threading.Thread.Sleep(20); // exceed the window

        // Second touch lands outside the window: tracker resets the count to 1, returns
        // Fallthrough_NotBuilt again — chunks the player just glanced through don't get
        // promoted just because they get re-touched minutes later by an unrelated NPC.
        var second = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.False(second.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, second.HitKind);
        Assert.Equal(0, cache.GetStats().ResidentChunks);
        Assert.Equal(2L, cache.GetStats().FallthroughNotBuilt);
    }

    [Fact]
    public void TryGetMask_MultipleCallsInSameFindGeneration_StayInFallthrough()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 2;

        var map = Map.Maps[1];

        // Open a pathfind. Multiple TryGetMask calls inside this Find target the same chunk
        // (different cells). The promotion gate counts distinct Finds, not raw probes — these
        // calls must NOT increment the per-chunk counter, even though there are many of them.
        // Without this, A* expansion would trip the gate on the second cell expansion in any
        // visited chunk, defeating the whole point of deferred promotion.
        cache.BeginFindGeneration();
        for (var i = 0; i < 8; i++)
        {
            // All cells are inside chunk (1500>>4, 1600>>4) = (93, 100).
            var lookup = cache.TryGetMask(map, 1500 + i, 1600, sourceZ: 10);
            Assert.False(lookup.IsHit);
            Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, lookup.HitKind);
        }

        Assert.Equal(0, cache.GetStats().ResidentChunks);
        Assert.Equal(0L, cache.GetStats().BuildsTotal);
        Assert.Equal(8L, cache.GetStats().FallthroughNotBuilt);

        // Begin a NEW Find — this is the second distinct touch under the per-Find gate.
        // The chunk now crosses the threshold and promotes.
        cache.BeginFindGeneration();
        var promoted = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.True(promoted.IsHit);
        Assert.Equal(CacheHitKind.Miss_NotBuilt, promoted.HitKind);
        Assert.Equal(1, cache.GetStats().ResidentChunks);
        Assert.Equal(1L, cache.GetStats().BuildsTotal);
    }

    [Fact]
    public void TryGetMask_TwoFindGenerationsAcrossWindow_RestartsCounter()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 2;
        cache.MissPromotionWindowMs = 1; // 1ms window for testability

        var map = Map.Maps[1];

        cache.BeginFindGeneration();
        Assert.False(cache.TryGetMask(map, 1500, 1600, sourceZ: 10).IsHit);

        System.Threading.Thread.Sleep(20); // exceed window

        // Second Find lands outside the window. Even though it's a distinct generation,
        // the elapsed-time check resets the counter to 1, so no promotion.
        cache.BeginFindGeneration();
        var second = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        Assert.False(second.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_NotBuilt, second.HitKind);
        Assert.Equal(0, cache.GetStats().ResidentChunks);
    }

    [Fact]
    public void TryGetMask_DistinctChunks_TrackedIndependently()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 2;

        var map = Map.Maps[1];

        // Two different chunks, one touch each — both must defer (each has its own counter).
        var chunkA = cache.TryGetMask(map, 1500, 1600, sourceZ: 10);
        var chunkB = cache.TryGetMask(map, 1600, 1700, sourceZ: 10); // different chunk

        Assert.False(chunkA.IsHit);
        Assert.False(chunkB.IsHit);
        Assert.Equal(0, cache.GetStats().ResidentChunks);
        Assert.Equal(2L, cache.GetStats().FallthroughNotBuilt);
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
    public void MultiCoveredCell_AndHalo_RouteToFallthrough()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1; // eager build so a multi-free cell serves immediately

        var map = Map.Maps[1];

        // A cell far from any multi serves from the static cache.
        Assert.True(cache.TryGetMask(map, 1500, 1600, 10).IsHit);

        // Inject a multi into an isolated sector. Sector.HasMultis only checks Count > 0, so a
        // single-entry list is enough to mark the sector as multi-bearing — the fallthrough
        // decision never dereferences the multi, so no real BaseMulti instance is needed.
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

            // Cell inside the multi sector → routed to the live path.
            Assert.Equal(CacheHitKind.Fallthrough_Multi, cache.TryGetMask(map, mx, my, 0).HitKind);

            // Cell in the adjacent sector but on the shared boundary → caught by the 1-cell halo
            // (its mask would otherwise propose an edge into the multi sector).
            var boundaryX = sx * 16 - 1; // last tile of sector sx-1; halo (x+1) reaches into sx
            Assert.Equal(CacheHitKind.Fallthrough_Multi, cache.TryGetMask(map, boundaryX, my, 0).HitKind);

            // Two tiles out → interior of the multi-free sector, unaffected.
            Assert.NotEqual(CacheHitKind.Fallthrough_Multi, cache.TryGetMask(map, sx * 16 - 2, my, 0).HitKind);

            Assert.True(cache.GetStats().FallthroughMulti >= 2);
        }
        finally
        {
            multisField.SetValue(sector, original);
        }
    }

    [Fact]
    public void MultiZCell_RoutesToFallthrough()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1; // eager build for prime-then-inspect tests

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

        // Inject "this cell has strata but none match the query Z" — proves the cache
        // still falls through to slow path when no stratum can answer.
        var cellIndex = ((1600 - ((1600 >> 4) << 4)) << 4) | (1500 - ((1500 >> 4) << 4));
        var offsets = new ushort[StepChunk.CellsPerChunk];
        for (var i = 0; i < offsets.Length; i++)
        {
            offsets[i] = StepChunk.NoStrata;
        }
        offsets[cellIndex] = 0; // points to a 0-stratum-count entry → no match
        var data = new byte[] { 0 };
        chunk.SetStrata(offsets, data);

        var lookup = cache.TryGetMask(map, 1500, 1600, 10);

        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_MultiZ, lookup.HitKind);

        var stats = cache.GetStats();
        Assert.Equal(preInjectionFallthroughMultiZ + 1L, stats.FallthroughMultiZ);
    }

    [Fact]
    public void Tier4Strata_MatchingZ_ReturnsHitFromStratum()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1;

        var map = Map.Maps[1];
        cache.TryGetMask(map, 1500, 1600, 10);

        var chunksField = typeof(StepCache).GetField(
            "_chunks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var chunks = (System.Collections.Generic.Dictionary<long, StepChunk>)chunksField.GetValue(cache);
        var key = StepCache.EncodeKey(map.MapID, 1500 >> 4, 1600 >> 4);
        var chunk = chunks[key];

        // Inject one stratum at zCenter=42, walkMask=0b00000011 (N + NE).
        // Query at sourceZ=42 must hit and return that stratum's data.
        var cellIndex = ((1600 - ((1600 >> 4) << 4)) << 4) | (1500 - ((1500 >> 4) << 4));
        var offsets = new ushort[StepChunk.CellsPerChunk];
        for (var i = 0; i < offsets.Length; i++)
        {
            offsets[i] = StepChunk.NoStrata;
        }
        offsets[cellIndex] = 0;

        var data = new byte[1 + StepChunk.StratumByteLength];
        data[0] = 1;          // count
        data[1] = 42;         // zCenter
        data[2] = 0b0000_0011; // walkMask (N | NE)
        data[3] = 0;          // wetMask
        data[4] = 42; data[5] = 42; data[6] = 0; data[7] = 0;
        data[8] = 0; data[9] = 0; data[10] = 0; data[11] = 0;
        data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 0;
        data[16] = 0; data[17] = 0; data[18] = 0; data[19] = 0;
        chunk.SetStrata(offsets, data);

        var lookup = cache.TryGetMask(map, 1500, 1600, 42);
        Assert.True(lookup.IsHit);
        Assert.Equal((byte)0b0000_0011, lookup.WalkMask);
        Assert.Equal((sbyte)42, lookup.WalkZ_N);
        Assert.Equal((sbyte)42, lookup.WalkZ_NE);
    }

    [Fact]
    public void SwimLayer_NotInjected_StaysFallthroughOnSourceZMismatch()
    {
        // Sanity check: a chunk WITHOUT a swim layer falls through on source-Z mismatch
        // exactly like before. Validates we didn't accidentally serve garbage when the
        // chunk has no shore cells.
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1;

        var map = Map.Maps[1];

        cache.TryGetMask(map, 1500, 1600, sourceZ: 10); // build chunk
        var beforeMismatch = cache.GetStats().FallthroughSourceZMismatch;

        // Same cell but query Z far from baked Z → source-Z guard fires.
        var lookup = cache.TryGetMask(map, 1500, 1600, sourceZ: 100);
        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_SourceZMismatch, lookup.HitKind);
        Assert.Equal(beforeMismatch + 1L, cache.GetStats().FallthroughSourceZMismatch);
    }

    [Fact]
    public void SwimLayer_InjectedMatchingZ_ReturnsHitFromSwimLayer()
    {
        // Inject a synthetic swim layer onto a resident chunk and verify a query at the
        // swim source Z routes through the swim-layer fallback, returning the swim mask.
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1;

        var map = Map.Maps[1];
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
        chunk.SwimSourceZ[cellIndex]   = -5;
        chunk.SwimMask[cellIndex]      = 0b0000_0011;
        chunk.SwimZN_Layer[cellIndex]  = -5;
        chunk.SwimZNE_Layer[cellIndex] = -5;
        // Other directions stay 0 — Mask bits 0 and 1 cover N and NE.

        // Query at the chunk's primary SourceZ — primary path serves walk-layer data,
        // swim layer not consulted.
        var bakedSourceZ = chunk.SourceZ[cellIndex];
        var walkLookup = cache.TryGetMask(map, 1500, 1600, bakedSourceZ);
        Assert.True(walkLookup.IsHit);
        Assert.Equal(CacheHitKind.Hit, walkLookup.HitKind);
        // Walk-layer query produces walk-layer walkMask (whatever the bake found), NOT
        // the synthetic swim mask we injected.

        // Query at the swim source Z — primary source-Z guard fails (|−5 − bakedZ| > 2
        // assuming baked Z is land surface), swim-layer fallback serves with our mask.
        if (System.Math.Abs(-5 - bakedSourceZ) <= 2)
        {
            // Bake landed near water Z — adjust the test to a clearer swim Z.
            chunk.SwimSourceZ[cellIndex] = (sbyte)(bakedSourceZ - 20);
        }
        var swimLookup = cache.TryGetMask(map, 1500, 1600, chunk.SwimSourceZ[cellIndex]);
        Assert.True(swimLookup.IsHit);
        Assert.Equal(CacheHitKind.Hit, swimLookup.HitKind);
        Assert.Equal((byte)0, swimLookup.WalkMask); // walk = 0 at swim Z
        Assert.Equal(chunk.SwimMask[cellIndex], swimLookup.WetMask);
        Assert.Equal(chunk.SwimZN_Layer[cellIndex], swimLookup.SwimZ_N);
        Assert.Equal(chunk.SwimZNE_Layer[cellIndex], swimLookup.SwimZ_NE);
    }

    [Fact]
    public void SwimLayer_InjectedButCellHasNoSentinel_FallsThrough()
    {
        // Chunk has the swim layer (some other cell is shore), but THIS cell is inland
        // (SwimSourceZ = NoSwimLayerCell). Query at non-matching walk Z must fall through,
        // not erroneously match -128 against the query.
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1;

        var map = Map.Maps[1];
        cache.TryGetMask(map, 1500, 1600, sourceZ: 10);

        var chunksField = typeof(StepCache).GetField(
            "_chunks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var chunks = (System.Collections.Generic.Dictionary<long, StepChunk>)chunksField!.GetValue(cache)!;
        var key = StepCache.EncodeKey(map.MapID, 1500 >> 4, 1600 >> 4);
        var chunk = chunks[key];

        // Allocate layer but leave THIS cell at the sentinel.
        chunk.AllocateSwimLayer();
        var cellIndex = ((1600 - ((1600 >> 4) << 4)) << 4) | (1500 - ((1500 >> 4) << 4));
        Assert.Equal(StepChunk.NoSwimLayerCell, chunk.SwimSourceZ[cellIndex]);

        var beforeMismatch = cache.GetStats().FallthroughSourceZMismatch;
        // Query at -128 (the sentinel value) — must NOT match. The guard short-circuits
        // on the sentinel before computing |sourceZ - SwimSourceZ|.
        var lookup = cache.TryGetMask(map, 1500, 1600, sbyte.MinValue);
        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_SourceZMismatch, lookup.HitKind);
        Assert.Equal(beforeMismatch + 1L, cache.GetStats().FallthroughSourceZMismatch);
    }

    [Fact]
    public void Tier4Strata_NonMatchingZ_FallsThrough()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MissPromotionThreshold = 1;

        var map = Map.Maps[1];
        cache.TryGetMask(map, 1500, 1600, 10);

        var chunksField = typeof(StepCache).GetField(
            "_chunks",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var chunks = (System.Collections.Generic.Dictionary<long, StepChunk>)chunksField.GetValue(cache);
        var key = StepCache.EncodeKey(map.MapID, 1500 >> 4, 1600 >> 4);
        var chunk = chunks[key];

        // Stratum at zCenter=42; query at sourceZ=10 (delta > StepHeight=2). Must fallthrough.
        var cellIndex = ((1600 - ((1600 >> 4) << 4)) << 4) | (1500 - ((1500 >> 4) << 4));
        var offsets = new ushort[StepChunk.CellsPerChunk];
        for (var i = 0; i < offsets.Length; i++)
        {
            offsets[i] = StepChunk.NoStrata;
        }
        offsets[cellIndex] = 0;

        var data = new byte[1 + StepChunk.StratumByteLength];
        data[0] = 1; data[1] = 42; // zCenter=42, all other bytes 0
        chunk.SetStrata(offsets, data);

        var lookup = cache.TryGetMask(map, 1500, 1600, 10);
        Assert.False(lookup.IsHit);
        Assert.Equal(CacheHitKind.Fallthrough_MultiZ, lookup.HitKind);
    }

    [Fact]
    public void LruCap_OverflowEvictsToCap()
    {
        var cache = StepCache.Instance;
        cache.Clear();
        cache.MaxResidentChunks = 4;
        cache.MissPromotionThreshold = 1;

        try
        {
            var map = Map.Maps[1];

            // Build 5 distinct chunks by querying different sectors.
            for (var i = 0; i < 5; i++)
            {
                var x = 1500 + i * 16;
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
