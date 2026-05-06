using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Singleton store of per-chunk static walkability data. Chunks correspond to
/// Map.SectorSize = 16; key encoding packs (mapId, chunkX, chunkY) into a long.
/// Lazily built on first query; invalidated by version-check vs Sector.MultisVersion;
/// memory bounded by MaxResidentChunks via probabilistic LRU eviction.
///
/// Plan 2B scope: default ground walker only. Cells with multi-Z surfaces and queries
/// for non-default-walker mobiles route to the existing MovementImpl slow path.
/// </summary>
public sealed class StaticWalkabilityCache
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StaticWalkabilityCache));

    public static StaticWalkabilityCache Instance { get; } = new();

    private readonly Dictionary<long, WalkabilityChunk> _chunks = new();
    // Parallel list of keys for O(1) random sampling during eviction. Kept in lockstep
    // with _chunks: append on Miss_NotBuilt, swap-and-pop on eviction.
    private readonly List<long> _keysList = new();

    // Telemetry counters
    private long _hits;
    private long _missesNotBuilt;
    private long _missesDirtyRebuild;
    private long _fallthroughMultiZ;
    private long _fallthroughOffMap;
    private long _fallthroughSourceZMismatch;
    private long _evictionsByLruCap;
    private long _divergencesShadowMode;
    private long _buildsTotal;

    private StaticWalkabilityCache() { }

    /// <summary>Hard cap on resident chunk count. Default 8192. Override for tests / ops.</summary>
    public int MaxResidentChunks { get; set; } = 8192;

    /// <summary>
    /// Pack (mapId, chunkX, chunkY) into a single long key.
    /// Layout: [reserved 16][mapId 16][chunkX 16][chunkY 16].
    /// </summary>
    internal static long EncodeKey(int mapId, int chunkX, int chunkY) =>
        ((long)(mapId & 0xFFFF) << 32) | ((long)(chunkX & 0xFFFF) << 16) | (long)(chunkY & 0xFFFF);

    public CacheStats GetStats() => new CacheStats(
        residentChunks: _chunks.Count,
        hits: _hits,
        missesNotBuilt: _missesNotBuilt,
        missesDirtyRebuild: _missesDirtyRebuild,
        fallthroughMultiZ: _fallthroughMultiZ,
        fallthroughOffMap: _fallthroughOffMap,
        fallthroughSourceZMismatch: _fallthroughSourceZMismatch,
        evictionsByLruCap: _evictionsByLruCap,
        divergencesShadowMode: _divergencesShadowMode,
        buildsTotal: _buildsTotal
    );

    /// <summary>
    /// Drop all cached chunks AND zero every telemetry counter. Used by tests and
    /// benchmarks that need a known cold-start state. Counter reset is intentional —
    /// counters are since-last-clear, not since-startup.
    /// </summary>
    public void Clear()
    {
        _chunks.Clear();
        _keysList.Clear();
        _hits = 0;
        _missesNotBuilt = 0;
        _missesDirtyRebuild = 0;
        _fallthroughMultiZ = 0;
        _fallthroughOffMap = 0;
        _fallthroughSourceZMismatch = 0;
        _evictionsByLruCap = 0;
        _divergencesShadowMode = 0;
        _buildsTotal = 0;
    }

    /// <summary>
    /// Probabilistic LRU sample size — picks SampleSize random resident chunks per
    /// eviction and evicts the oldest of that sample. Approximates true LRU at a tiny
    /// fraction of the cost (no full sort). Redis uses the same approach (`maxmemory-samples`).
    /// 5 yields ~quality-of-true-LRU for cache eviction; higher values trade speed for accuracy.
    /// </summary>
    private const int LruSampleSize = 5;

    /// <summary>
    /// If resident chunk count exceeds MaxResidentChunks, evict via probabilistic LRU
    /// until the count is at or below the cap. Per-eviction cost is O(LruSampleSize),
    /// independent of resident count — sustained cap pressure has no perpetual perf hit.
    /// Called from CacheEvictionTimer; also callable directly from tests.
    /// </summary>
    public void EnforceLruCap()
    {
        var overflow = _chunks.Count - MaxResidentChunks;
        if (overflow <= 0)
        {
            return;
        }

        var rng = System.Random.Shared;
        while (overflow-- > 0 && _keysList.Count > 0)
        {
            var oldestIdx = -1;
            long oldestTouched = long.MaxValue;
            long oldestKey = 0;

            // Sample LruSampleSize random keys; track the oldest by LastTouchedTicks.
            // With replacement is fine — collisions are rare and don't break correctness.
            var samples = Math.Min(LruSampleSize, _keysList.Count);
            for (var s = 0; s < samples; s++)
            {
                var idx = rng.Next(_keysList.Count);
                var k = _keysList[idx];
                var touched = _chunks[k].LastTouchedTicks;
                if (touched < oldestTouched)
                {
                    oldestTouched = touched;
                    oldestKey = k;
                    oldestIdx = idx;
                }
            }

            _chunks.Remove(oldestKey);
            // Swap-and-pop _keysList[oldestIdx] with the tail; O(1) regardless of position.
            var last = _keysList.Count - 1;
            if (oldestIdx != last)
            {
                _keysList[oldestIdx] = _keysList[last];
            }
            _keysList.RemoveAt(last);
            _evictionsByLruCap++;
        }
    }

    internal static void DecodeKey(long key, out int mapId, out int chunkX, out int chunkY)
    {
        mapId  = (int)((key >> 32) & 0xFFFF);
        chunkX = (int)((key >> 16) & 0xFFFF);
        chunkY = (int)(key & 0xFFFF);
    }

    /// <summary>
    /// Increment the shadow-mode divergence counter. Called by CachedMovementCheck
    /// (Task 10+) when the cache's answer disagrees with MovementImpl during the
    /// canary observation window. Surfaced via GetStats().DivergencesShadowMode.
    /// </summary>
    internal void RecordDivergence() => _divergencesShadowMode++;

    private const int ChunkSize = 16;

    /// <summary>
    /// Hot-path query. Returns the cached mask + 8 destination Z values for (map, x, y, sourceZ).
    /// Returns false on off-map or multi-Z fallthrough; the caller should use the slow path.
    /// </summary>
    public bool TryGetMask(
        Map map, int x, int y, sbyte sourceZ,
        out byte mask,
        out sbyte destZN, out sbyte destZNE, out sbyte destZE, out sbyte destZSE,
        out sbyte destZS, out sbyte destZSW, out sbyte destZW, out sbyte destZNW,
        out CacheHitKind hitKind
    )
    {
        mask = 0;
        destZN = destZNE = destZE = destZSE = destZS = destZSW = destZW = destZNW = 0;

        if (map == null || map == Map.Internal || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            hitKind = CacheHitKind.Fallthrough_OffMap;
            _fallthroughOffMap++;
            return false;
        }

        var chunkX = x >> 4;
        var chunkY = y >> 4;
        var key = EncodeKey(map.MapID, chunkX, chunkY);

        var hitKindResult = CacheHitKind.Hit;
        if (!_chunks.TryGetValue(key, out var chunk))
        {
            chunk = ResolveMissingChunk(map, chunkX, chunkY);
            _chunks[key] = chunk;
            _keysList.Add(key);
            hitKindResult = CacheHitKind.Miss_NotBuilt;
        }
        else
        {
            var sector = map.GetRealSector(chunkX, chunkY);
            if (chunk.BuiltMultisVersion != sector.MultisVersion)
            {
                chunk = BuildChunk(map, chunkX, chunkY);
                _chunks[key] = chunk;
                hitKindResult = CacheHitKind.Miss_DirtyRebuild;
                // _missesDirtyRebuild++ moved into the success switch below to
                // preserve the mutual-exclusivity invariant (a multi-Z fallthrough
                // on a freshly dirty-rebuilt chunk must NOT count both counters).
            }
        }

        chunk.LastTouchedTicks = Core.TickCount;

        var cellIndex = ((y - (chunkY << 4)) << 4) | (x - (chunkX << 4));

        if (chunk.IsCellMultiZ(cellIndex))
        {
            hitKind = CacheHitKind.Fallthrough_MultiZ;
            _fallthroughMultiZ++;
            return false;
        }

        // Source-Z guard (Plan 2C Part 2.1): the cache stores one answer per cell, baked
        // for sourceZ == BakedSourceZ. Queries with a different source-Z would diverge from
        // the slow path. StepHeight tolerance accepts the engine's own per-step Z change
        // envelope (1-unit Z jitter from incremental movement is OK).
        // EXPERIMENT confirmed (Plan 2D): loosening tolerance beyond StepHeight breaks parity
        // — the cache mask was baked at BakedSourceZ exactly, and tile reachability shifts at
        // step-height boundaries. StepHeight is the right (correctness-preserving) value.
        if (Math.Abs(sourceZ - chunk.BakedSourceZ[cellIndex]) > StepHeight)
        {
            hitKind = CacheHitKind.Fallthrough_SourceZMismatch;
            _fallthroughSourceZMismatch++;
            return false;
        }

        mask = chunk.Mask[cellIndex];
        destZN  = chunk.DestZN[cellIndex];
        destZNE = chunk.DestZNE[cellIndex];
        destZE  = chunk.DestZE[cellIndex];
        destZSE = chunk.DestZSE[cellIndex];
        destZS  = chunk.DestZS[cellIndex];
        destZSW = chunk.DestZSW[cellIndex];
        destZW  = chunk.DestZW[cellIndex];
        destZNW = chunk.DestZNW[cellIndex];

        switch (hitKindResult)
        {
            case CacheHitKind.Miss_NotBuilt:
                {
                    _missesNotBuilt++;
                    break;
                }
            case CacheHitKind.Miss_DirtyRebuild:
                {
                    _missesDirtyRebuild++;
                    break;
                }
            case CacheHitKind.Hit:
                {
                    _hits++;
                    break;
                }
        }
        hitKind = hitKindResult;

        return true;
    }

    /// <summary>
    /// Chunk-miss resolution: build the chunk via the runtime baker.
    /// </summary>
    private WalkabilityChunk ResolveMissingChunk(Map map, int chunkX, int chunkY) =>
        BuildChunk(map, chunkX, chunkY);

    private WalkabilityChunk BuildChunk(Map map, int chunkX, int chunkY)
    {
        var chunk = new WalkabilityChunk();
        var sector = map.GetRealSector(chunkX, chunkY);
        chunk.BuiltMultisVersion = sector.MultisVersion;

        var baseX = chunkX << 4;
        var baseY = chunkY << 4;

        for (var dy = 0; dy < ChunkSize; dy++)
        {
            for (var dx = 0; dx < ChunkSize; dx++)
            {
                var x = baseX + dx;
                var y = baseY + dy;
                var cell = (dy << 4) | dx;

                map.GetAverageZ(x, y, out _, out var avgZ, out _);

                // Bake from the slow path's "standing Z" — the Z a creature actually stands at
                // on this cell. For paver-over-ground / bridge cells, this is the SURFACE Z, not
                // the ground avg. A* tracks `_nodes[idx].z` as the slow path's newZ (= standing Z),
                // so the cache's BakedSourceZ must match for the source-Z guard to not over-fire.
                var standingZ = (sbyte)StaticWalkabilityBaker.ComputeStandingZ(map, x, y, avgZ);

                var result = StaticWalkabilityBaker.ComputeMaskAt(map, x, y, standingZ);

                chunk.Mask[cell] = result.Mask;
                chunk.BakedSourceZ[cell] = standingZ;
                chunk.DestZN[cell]  = result.DestZ_N;
                chunk.DestZNE[cell] = result.DestZ_NE;
                chunk.DestZE[cell]  = result.DestZ_E;
                chunk.DestZSE[cell] = result.DestZ_SE;
                chunk.DestZS[cell]  = result.DestZ_S;
                chunk.DestZSW[cell] = result.DestZ_SW;
                chunk.DestZW[cell]  = result.DestZ_W;
                chunk.DestZNW[cell] = result.DestZ_NW;

                // Multi-Z detection (Plan 2C Part 1): Z-aware reachability check.
                // A cell is multi-Z only if >=2 surfaces are reachable from standingZ —
                // mirrors the baker's CheckStaticStep filter so we don't over-mark.
                if (CountReachableSurfaces(map, x, y, standingZ) > 1)
                {
                    chunk.MarkCellMultiZ(cell);
                }
            }
        }

        _buildsTotal++;
        return chunk;
    }

    /// <summary>
    /// Conservative heuristic for multi-Z detection: counts walkable static + multi tiles
    /// plus walkable land. Does NOT verify vertical separation (creature standability gap).
    /// Over-counting is safe — false positives route the cell to the MovementImpl slow
    /// path harmlessly. Under-counting would be dangerous (would let the cache answer
    /// for a multi-Z cell). True multi-Z encoding is deferred to Plan 2C.
    ///
    /// Plan 2C Part 1: kept around for diagnostic comparison only — production code
    /// uses CountReachableSurfaces below.
    /// </summary>
    internal static int CountWalkableSurfaces(Map map, int x, int y)
    {
        var count = 0;
        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            if (data.Surface && !data.Impassable)
            {
                count++;
            }
        }

        // Land itself counts as a surface unless impassable
        var landTile = map.Tiles.GetLandTile(x, y);
        var landFlags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        if (!landTile.Ignored && (landFlags & TileFlag.Impassable) == 0)
        {
            count++;
        }

        return count;
    }

    private const int PersonHeight = 16;
    private const int StepHeight = 2;

    /// <summary>
    /// Z-aware multi-Z detection: counts walkable surfaces that are actually reachable
    /// from a creature standing at sourceZ. Mirrors the filter logic in
    /// StaticWalkabilityBaker.CheckStaticStep so cells flagged multi-Z here are exactly
    /// those where the baker would have multiple distinct candidate destinations.
    ///
    /// A surface is reachable when:
    ///   1. itemData.Surface && !itemData.Impassable
    ///   2. stepTop >= itemTop (creature can step UP onto it)
    ///   3. sourceZ + PersonHeight > itemZ && itemZ + itemData.Height > sourceZ (vertical overlap)
    /// </summary>
    internal static int CountReachableSurfaces(Map map, int x, int y, sbyte sourceZ)
    {
        var startTop = sourceZ + PersonHeight;
        var stepTop = startTop + StepHeight;
        var count = 0;

        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            if (!data.Surface || data.Impassable)
            {
                continue;
            }

            var itemZ = tile.Z;
            var itemTop = data.Bridge ? itemZ : itemZ + data.Height;

            if (stepTop < itemTop)
            {
                continue;
            }

            if (sourceZ + PersonHeight > itemZ && itemZ + data.Height > sourceZ)
            {
                count++;
            }
        }

        // Land surface check — same shape, but use GetAverageZ for the land's effective top.
        var landTile = map.Tiles.GetLandTile(x, y);
        var landFlags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        if (!landTile.Ignored && (landFlags & TileFlag.Impassable) == 0)
        {
            map.GetAverageZ(x, y, out var landZ, out _, out var landTop);
            if (stepTop >= landZ && sourceZ + PersonHeight > landZ && landTop > sourceZ)
            {
                count++;
            }
        }

        return count;
    }
}
