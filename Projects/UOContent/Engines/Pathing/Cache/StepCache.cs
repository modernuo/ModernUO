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
/// Default-walker scope only. Cells with multi-Z surfaces and queries for non-default
/// walkers route to the MovementImpl slow path via the Fallthrough_* hit kinds.
/// </summary>
public sealed class StepCache
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StepCache));

    public static StepCache Instance { get; } = new();

    private readonly Dictionary<long, StepChunk> _chunks = new();
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
    private long _buildsTotal;

    private StepCache() { }

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
                var idx = Utility.Random(_keysList.Count);
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

    private const int ChunkSize = 16;

    /// <summary>
    /// Hot-path query. Returns the cached mask + 8 destination Z values + hit kind.
    /// Inspect <see cref="StepMask.IsHit"/> to decide whether to use the result or fall
    /// back to the slow path.
    /// </summary>
    public StepMask TryGetMask(Map map, int x, int y, sbyte sourceZ)
    {
        if (map == null || map == Map.Internal || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            _fallthroughOffMap++;
            return new StepMask(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, CacheHitKind.Fallthrough_OffMap);
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
                // _missesDirtyRebuild++ deferred to the outcome switch below so a
                // multi-Z fallthrough on a freshly dirty-rebuilt chunk doesn't double-count.
            }
        }

        chunk.LastTouchedTicks = Core.TickCount;

        var cellIndex = ((y - (chunkY << 4)) << 4) | (x - (chunkX << 4));

        if (chunk.IsCellMultiZ(cellIndex))
        {
            _fallthroughMultiZ++;
            return new StepMask(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, CacheHitKind.Fallthrough_MultiZ);
        }

        // Source-Z guard: the cache stores one answer per cell baked at SourceZ.
        // StepHeight tolerance accepts incremental Z jitter; loosening it breaks parity
        // because tile reachability shifts at step-height boundaries.
        if (Math.Abs(sourceZ - chunk.SourceZ[cellIndex]) > StepHeight)
        {
            _fallthroughSourceZMismatch++;
            return new StepMask(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, CacheHitKind.Fallthrough_SourceZMismatch);
        }

        switch (hitKindResult)
        {
            case CacheHitKind.Miss_NotBuilt:    { _missesNotBuilt++;     break; }
            case CacheHitKind.Miss_DirtyRebuild: { _missesDirtyRebuild++; break; }
            case CacheHitKind.Hit:              { _hits++;               break; }
        }

        return new StepMask(
            chunk.WalkMask[cellIndex],
            chunk.WetMask[cellIndex],
            chunk.WalkZN[cellIndex],
            chunk.WalkZNE[cellIndex],
            chunk.WalkZE[cellIndex],
            chunk.WalkZSE[cellIndex],
            chunk.WalkZS[cellIndex],
            chunk.WalkZSW[cellIndex],
            chunk.WalkZW[cellIndex],
            chunk.WalkZNW[cellIndex],
            chunk.SwimZN[cellIndex],
            chunk.SwimZNE[cellIndex],
            chunk.SwimZE[cellIndex],
            chunk.SwimZSE[cellIndex],
            chunk.SwimZS[cellIndex],
            chunk.SwimZSW[cellIndex],
            chunk.SwimZW[cellIndex],
            chunk.SwimZNW[cellIndex],
            hitKindResult
        );
    }

    /// <summary>
    /// Chunk-miss resolution: build the chunk via the runtime baker.
    /// </summary>
    private StepChunk ResolveMissingChunk(Map map, int chunkX, int chunkY) =>
        BuildChunk(map, chunkX, chunkY);

    private StepChunk BuildChunk(Map map, int chunkX, int chunkY)
    {
        var chunk = new StepChunk();
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

                // Bake from the slow path's "standing Z" (the surface Z a creature actually
                // stands at, not the ground avg). A* tracks newZ as standing Z, so SourceZ
                // must match for the source-Z guard not to over-fire.
                var standingZ = (sbyte)StepProbe.ComputeStandingZ(map, x, y, avgZ);

                var result = StepProbe.ComputeMaskAt(map, x, y, standingZ);

                chunk.WalkMask[cell] = result.WalkMask;
                chunk.WetMask[cell]  = result.WetMask;
                chunk.SourceZ[cell]  = standingZ;
                chunk.WalkZN[cell]   = result.WalkZ_N;
                chunk.WalkZNE[cell]  = result.WalkZ_NE;
                chunk.WalkZE[cell]   = result.WalkZ_E;
                chunk.WalkZSE[cell]  = result.WalkZ_SE;
                chunk.WalkZS[cell]   = result.WalkZ_S;
                chunk.WalkZSW[cell]  = result.WalkZ_SW;
                chunk.WalkZW[cell]   = result.WalkZ_W;
                chunk.WalkZNW[cell]  = result.WalkZ_NW;
                chunk.SwimZN[cell]   = result.SwimZ_N;
                chunk.SwimZNE[cell]  = result.SwimZ_NE;
                chunk.SwimZE[cell]   = result.SwimZ_E;
                chunk.SwimZSE[cell]  = result.SwimZ_SE;
                chunk.SwimZS[cell]   = result.SwimZ_S;
                chunk.SwimZSW[cell]  = result.SwimZ_SW;
                chunk.SwimZW[cell]   = result.SwimZ_W;
                chunk.SwimZNW[cell]  = result.SwimZ_NW;

                // Multi-Z = ≥2 surfaces reachable from standingZ. Mirrors the baker's
                // CheckStaticStep filter so we don't over-mark.
                if (CountReachableSurfaces(map, x, y, standingZ) > 1)
                {
                    chunk.MarkCellMultiZ(cell);
                }
            }
        }

        _buildsTotal++;
        return chunk;
    }

    private const int PersonHeight = 16;
    private const int StepHeight = 2;

    /// <summary>
    /// Counts walkable surfaces actually reachable from a creature standing at sourceZ.
    /// Mirrors <see cref="StepProbe"/>.CheckStaticStep so cells flagged multi-Z
    /// here are exactly those where the baker would have multiple candidate destinations.
    /// Reachable when: surface and !impassable; stepTop ≥ itemTop; vertical overlap with
    /// the creature's PersonHeight envelope.
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
