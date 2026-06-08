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
    private readonly List<long> _keysList = [];

    // Second-touch promotion tracker. A chunk's first miss within the window returns
    // Fallthrough_NotBuilt; the caller takes the slow path. The Nth DISTINCT-FIND miss
    // within the same window (where N = MissPromotionThreshold) promotes to BuildChunk +
    // serve. We count distinct Find generations, not raw TryGetMask calls — A* expansion
    // hits each visited chunk many times in one Find, so per-call counting hits threshold
    // immediately and defeats the gate. Per-Find counting filters single-Find pass-throughs
    // (pet following a moving player) while still promoting chunks revisited by multiple
    // Finds (NPC patrolling fixed territory).
    private readonly Dictionary<long, ChunkMissState> _chunkMissTracker = new();
    private const int MaxMissTrackerEntries = 4096;

    // Generation counter incremented by BeginFindGeneration(). Sentinel 0 = "no Find started
    // yet"; treated as a distinct generation per call so callers that bypass BeginFindGeneration
    // (single-call tests, BakeMap with threshold=1) get sensible behavior.

    private struct ChunkMissState
    {
        public byte MissCount;
        public uint LastMissTickStamp;
        public uint LastFindGeneration;
    }

    // Telemetry counters
    private long _hits;
    private long _missesNotBuilt;
    private long _missesDirtyRebuild;
    private long _fallthroughMultiZ;
    private long _fallthroughOffMap;
    private long _fallthroughSourceZMismatch;
    private long _fallthroughNotBuilt;
    private long _fallthroughMulti;
    private long _evictionsByLruCap;
    private long _buildsTotal;

    private StepCache() { }

    /// <summary>Hard cap on resident chunk count. Default 8192. Override for tests / ops.</summary>
    public int MaxResidentChunks { get; set; } = 8192;

    /// <summary>
    /// When true, <see cref="TryOpenLazyReader"/> immediately materializes every chunk in
    /// the .swb file into the resident set, paying the file-load cost upfront at boot
    /// instead of on first query. Trades ~25–50ms boot time per fully-baked map for zero
    /// first-touch latency in production. Default off — preserves the lazy memory profile.
    /// </summary>
    public bool PreloadOnLazyOpen { get; set; }

    /// <summary>
    /// Number of misses on the same chunk within <see cref="MissPromotionWindowMs"/>
    /// required to trigger a build. 1 = eager (legacy behavior). 2 = second-touch (default,
    /// filters single-touch pass-throughs).
    /// </summary>
    public int MissPromotionThreshold { get; set; } = 2;

    /// <summary>
    /// Window over which misses against the same chunk accumulate toward promotion.
    /// Misses spaced wider than this restart the count. Default 30s.
    /// </summary>
    public uint MissPromotionWindowMs { get; set; } = 30_000;

    /// <summary>
    /// Marks the start of a new pathfind. The promotion gate counts distinct Find
    /// generations per chunk, not raw TryGetMask calls — call this once at the top of
    /// each pathfind invocation so multiple cell expansions within one Find don't trip
    /// the threshold. Wraps at uint.MaxValue back to 1 (0 is reserved as the
    /// "no Find started yet" sentinel).
    /// </summary>
    public void BeginFindGeneration()
    {
        unchecked { CurrentFindGeneration++; }

        if (CurrentFindGeneration == 0)
        {
            CurrentFindGeneration = 1;
        }
    }

    /// <summary>Test-only: read the current Find generation.</summary>
    internal uint CurrentFindGeneration { get; private set; }

    /// <summary>
    /// Pack (mapId, chunkX, chunkY) into a single long key.
    /// Layout: [reserved 16][mapId 16][chunkX 16][chunkY 16].
    /// </summary>
    internal static long EncodeKey(int mapId, int chunkX, int chunkY) =>
        ((long)(mapId & 0xFFFF) << 32) | ((long)(chunkX & 0xFFFF) << 16) | (long)(chunkY & 0xFFFF);

    public CacheStats GetStats() => new(
        residentChunks: _chunks.Count,
        hits: _hits,
        missesNotBuilt: _missesNotBuilt,
        missesDirtyRebuild: _missesDirtyRebuild,
        fallthroughMultiZ: _fallthroughMultiZ,
        fallthroughOffMap: _fallthroughOffMap,
        fallthroughSourceZMismatch: _fallthroughSourceZMismatch,
        fallthroughNotBuilt: _fallthroughNotBuilt,
        fallthroughMulti: _fallthroughMulti,
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
        ClearResidentChunks();
        CloseLazyReaders();
    }

    /// <summary>
    /// Drop all resident chunks AND zero counters, but keep lazy readers open.
    /// Useful in benchmark loops that want to measure "first query after boot" cost
    /// without paying the lazy-reader reopen overhead each iteration. Same intent as
    /// <see cref="Clear"/> minus the file-handle teardown.
    /// </summary>
    public void ClearResidentChunks()
    {
        _chunks.Clear();
        _keysList.Clear();
        _chunkMissTracker.Clear();
        CurrentFindGeneration = 0;
        _hits = 0;
        _missesNotBuilt = 0;
        _missesDirtyRebuild = 0;
        _fallthroughMultiZ = 0;
        _fallthroughOffMap = 0;
        _fallthroughSourceZMismatch = 0;
        _fallthroughNotBuilt = 0;
        _fallthroughMulti = 0;
        _evictionsByLruCap = 0;
        _buildsTotal = 0;
    }

    // Per-map open .swb readers, populated by TryOpenLazyReader at startup. Chunks are
    // fetched on demand from the file when ResolveMissingChunk fires; resident memory
    // stays bounded by MaxResidentChunks regardless of file size.
    private readonly Dictionary<int, StepCacheFile.LazyReader> _lazyReaders = new();

    /// <summary>
    /// Walk every chunk in <paramref name="mapId"/>, populate the resident set, then
    /// save to <paramref name="path"/>. Returns the number of chunks written.
    /// Designed for offline / fixture use; blocks the calling thread for many seconds
    /// on a full Trammel walk.
    /// </summary>
    public int BakeMap(int mapId, string path)
    {
        var map = Map.Maps[mapId];
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        // BakeMap is an explicit decision to populate every chunk; the promotion gate
        // would otherwise return Fallthrough_NotBuilt for every chunk (each touched once)
        // and the bake would write an empty file. Force eager build for the duration.
        var prevThreshold = MissPromotionThreshold;
        MissPromotionThreshold = 1;
        var startTick = Core.TickCount;
        try
        {
            var chunkCols = (map.Width + ChunkSize - 1) / ChunkSize;
            var chunkRows = (map.Height + ChunkSize - 1) / ChunkSize;
            var logEvery = Math.Max(1, chunkRows / 32);

            logger.Information(
                "PathBake map {MapId}: walking {Cols}x{Rows} = {Total} chunks (synchronous; no eviction during the walk)...",
                mapId, chunkCols, chunkRows, chunkCols * chunkRows
            );

            for (var cy = 0; cy < chunkRows; cy++)
            {
                for (var cx = 0; cx < chunkCols; cx++)
                {
                    // Any sourceZ works — the chunk is built on first access regardless of
                    // whether the query returns Hit or Fallthrough_SourceZMismatch.
                    TryGetMask(map, cx * ChunkSize, cy * ChunkSize, sourceZ: 0);
                }

                if ((cy + 1) % logEvery == 0 || cy == chunkRows - 1)
                {
                    logger.Information(
                        "PathBake map {MapId}: row {Row}/{Rows} ({Pct}%), {Resident} chunks resident, {Elapsed:F1}s, {HeapMB} MB heap",
                        mapId, cy + 1, chunkRows, (cy + 1) * 100 / chunkRows,
                        _chunks.Count, (Core.TickCount - startTick) / 1000.0, GC.GetTotalMemory(false) >> 20
                    );
                }
            }

            logger.Information(
                "PathBake map {MapId}: walk complete in {Elapsed:F1}s, writing {Resident} chunks to disk...",
                mapId, (Core.TickCount - startTick) / 1000.0, _chunks.Count
            );
        }
        finally
        {
            MissPromotionThreshold = prevThreshold;
        }

        return SaveToFile(path, mapId);
    }

    /// <summary>
    /// Persist all resident chunks for <paramref name="mapId"/> to a .swb file. Returns
    /// the number of chunks written. The file embeds a TileData fingerprint so a stale
    /// file (built before a client patch) can be detected and rejected at open time.
    /// </summary>
    public int SaveToFile(string path, int mapId)
    {
        var matching = 0;
        foreach (var key in _keysList)
        {
            DecodeKey(key, out var keyMapId, out _, out _);
            if (keyMapId == mapId)
            {
                matching++;
            }
        }

        var enumerator = _keysList.GetEnumerator();
        StepCacheFile.Write(path, (uint)mapId, (uint)matching, EmitChunk);
        enumerator.Dispose();
        return matching;

        bool EmitChunk(out int chunkX, out int chunkY, out StepChunk chunk)
        {
            while (enumerator.MoveNext())
            {
                var key = enumerator.Current;
                DecodeKey(key, out var emittedMapId, out chunkX, out chunkY);
                if (emittedMapId == mapId)
                {
                    chunk = _chunks[key];
                    return true;
                }
            }
            chunkX = chunkY = 0;
            chunk = null!;
            return false;
        }
    }

    /// <summary>
    /// Open a .swb file as a lazy backing store for <paramref name="mapId"/>. Reads only
    /// header + chunk-offset index (~16 bytes per chunk); individual records are fetched
    /// on demand by <see cref="ResolveMissingChunk"/>. Returns false on missing file,
    /// magic / version mismatch, or TileData hash mismatch (stale bake).
    /// </summary>
    public bool TryOpenLazyReader(string path, int mapId)
    {
        var reader = StepCacheFile.OpenForLazy(path);
        if (reader == null)
        {
            return false;
        }

        if (reader.MapId != (uint)mapId)
        {
            logger.Warning(
                "StepCache: {Path} declares mapId {FileMapId} but caller requested {RequestedMapId}; ignoring",
                path, reader.MapId, mapId
            );
            reader.Dispose();
            return false;
        }

        if (_lazyReaders.TryGetValue(mapId, out var existing))
        {
            existing.Dispose();
        }
        _lazyReaders[mapId] = reader;

        logger.Information(
            "StepCache: opened {Path} ({ChunkCount} chunks indexed) for map {MapId}",
            path, reader.IndexedChunkCount, mapId
        );

        if (PreloadOnLazyOpen)
        {
            PreloadFromLazyReader(mapId, reader);
        }

        return true;
    }

    /// <summary>
    /// Materializes every chunk in <paramref name="reader"/> into the resident set.
    /// Called from <see cref="TryOpenLazyReader"/> when <see cref="PreloadOnLazyOpen"/>
    /// is set. Skips chunks whose live <see cref="Map.Sector.MultisVersion"/> doesn't
    /// match the file's snapshot — those will rebake on first query.
    /// </summary>
    private void PreloadFromLazyReader(int mapId, StepCacheFile.LazyReader reader)
    {
        var map = Map.Maps[mapId];
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var loaded = 0;
        foreach (var (chunkX, chunkY) in reader.EnumerateChunkCoords())
        {
            var key = EncodeKey(mapId, chunkX, chunkY);
            if (_chunks.ContainsKey(key))
            {
                continue;
            }

            var chunk = TryLoadFromLazyReader(map, chunkX, chunkY);
            if (chunk == null)
            {
                continue;
            }

            _chunks[key] = chunk;
            _keysList.Add(key);
            loaded++;
        }

        logger.Information(
            "StepCache: preloaded {Loaded} chunks from .swb for map {MapId}", loaded, mapId
        );
    }

    /// <summary>
    /// Number of .swb readers currently open. Mostly for tests / telemetry.
    /// </summary>
    public int OpenLazyReaderCount => _lazyReaders.Count;

    /// <summary>
    /// True if a valid .swb reader is open for <paramref name="mapId"/>. A reader only opens via
    /// <see cref="TryOpenLazyReader"/> after <see cref="StepCacheFile.OpenForLazy"/> validates the
    /// file's fingerprint against the live tile data, so "has reader" already means "present and
    /// up-to-date" — the boot prebake uses this to skip baking maps that don't need it, instead of
    /// recomputing the fingerprint a second time.
    /// </summary>
    public bool HasLazyReader(int mapId) => _lazyReaders.ContainsKey(mapId);

    /// <summary>Test-only diagnostic: does the lazy reader for <paramref name="mapId"/> hold an offset for (chunkX, chunkY)?</summary>
    internal bool LazyReaderHasChunk(int mapId, int chunkX, int chunkY) =>
        _lazyReaders.TryGetValue(mapId, out var r) && r.Has(chunkX, chunkY);

    /// <summary>
    /// Closes all open lazy readers, releasing their underlying file streams. Called from
    /// <see cref="Clear"/> so test cleanup can delete .swb files (they're held with
    /// FileShare.Read | FileShare.Delete, so this is mostly belt-and-suspenders).
    /// </summary>
    public void CloseLazyReaders()
    {
        foreach (var reader in _lazyReaders.Values)
        {
            reader.Dispose();
        }
        _lazyReaders.Clear();
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
    /// True if a multi (house / boat) covers (x, y) or any of its 8 neighbours. Multi-covered
    /// cells — plus the 1-cell halo, because a cell's mask encodes the edges TO its neighbours, so
    /// a neighbouring wall must block those edges — are served by the live movement path, not the
    /// static chunk cache. Cheap: an interior cell checks only its own sector (chunk == sector);
    /// only edge/corner cells additionally check the adjacent sector(s) the halo reaches.
    /// </summary>
    private static bool MultiInfluence(Map map, int x, int y)
    {
        var sx = x >> 4;
        var sy = y >> 4;
        if (map.GetRealSector(sx, sy).HasMultis)
        {
            return true;
        }

        var west = (x & 15) == 0;
        var east = (x & 15) == 15;
        var north = (y & 15) == 0;
        var south = (y & 15) == 15;
        if (!(west || east || north || south))
        {
            return false; // interior cell — its whole halo is inside the (multi-free) own sector
        }

        return west && map.GetRealSector(sx - 1, sy).HasMultis
            || east && map.GetRealSector(sx + 1, sy).HasMultis
            || north && map.GetRealSector(sx, sy - 1).HasMultis
            || south && map.GetRealSector(sx, sy + 1).HasMultis
            || west && north && map.GetRealSector(sx - 1, sy - 1).HasMultis
            || east && north && map.GetRealSector(sx + 1, sy - 1).HasMultis
            || west && south && map.GetRealSector(sx - 1, sy + 1).HasMultis
            || east && south && map.GetRealSector(sx + 1, sy + 1).HasMultis;
    }

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
            return new StepMask(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                CacheHitKind.Fallthrough_OffMap
            );
        }

        // Multis (houses, boats) are not baked into the static chunk cache (they're dynamic
        // content). If a multi covers this cell or its 1-cell halo, route to the live movement
        // path, which is fully multi-aware. Gated on Sector.HasMultis, so the multi-free majority
        // of the map pays a single (interior) sector lookup.
        if (MultiInfluence(map, x, y))
        {
            _fallthroughMulti++;
            return new StepMask(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                CacheHitKind.Fallthrough_Multi
            );
        }

        var chunkX = x >> 4;
        var chunkY = y >> 4;
        var key = EncodeKey(map.MapID, chunkX, chunkY);

        var hitKindResult = CacheHitKind.Hit;
        if (!_chunks.TryGetValue(key, out var chunk))
        {
            // Try lazy file first — file-loaded chunks bypass the miss tracker because
            // the .swb represents an explicit prior decision to keep this chunk warm.
            chunk = TryLoadFromLazyReader(map, chunkX, chunkY);
            if (chunk != null)
            {
                _chunks[key] = chunk;
                _keysList.Add(key);
                hitKindResult = CacheHitKind.Miss_NotBuilt;
            }
            else if (ShouldPromoteAfterMiss(key))
            {
                chunk = BuildChunk(map, chunkX, chunkY);
                _chunks[key] = chunk;
                _keysList.Add(key);
                hitKindResult = CacheHitKind.Miss_NotBuilt;
            }
            else
            {
                _fallthroughNotBuilt++;
                return new StepMask(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    CacheHitKind.Fallthrough_NotBuilt
                );
            }
        }

        // A resident chunk is static-only — it never goes stale from multis (multi-covered cells
        // fall through to the live path above).
        chunk.LastTouchedTicks = Core.TickCount;

        var cellIndex = ((y - (chunkY << 4)) << 4) | (x - (chunkX << 4));

        if (chunk.IsCellMultiZ(cellIndex))
        {
            // Tier 4: try the per-cell strata. Each stratum is keyed by its bake-time
            // standing-Z; a query matches when |sourceZ - stratum.zCenter| <= StepHeight.
            if (TryStratumHit(chunk, cellIndex, sourceZ, hitKindResult, out var stratumResult))
            {
                switch (hitKindResult)
                {
                    case CacheHitKind.Miss_NotBuilt:    { _missesNotBuilt++;     break; }
                    case CacheHitKind.Miss_DirtyRebuild: { _missesDirtyRebuild++; break; }
                    case CacheHitKind.Hit:              { _hits++;               break; }
                }
                return stratumResult;
            }
            _fallthroughMultiZ++;
            return new StepMask(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                CacheHitKind.Fallthrough_MultiZ
            );
        }

        // Source-Z guard: the cache stores one answer per cell baked at SourceZ.
        // StepHeight tolerance accepts incremental Z jitter; loosening it breaks parity
        // because tile reachability shifts at step-height boundaries.
        if (Math.Abs(sourceZ - chunk.SourceZ[cellIndex]) > StepHeight)
        {
            // Swim-layer fallback for shore cells: if the chunk has the layer and this
            // cell's water-surface Z is within StepHeight of the query, serve from the
            // swim layer (computed at swim-perspective Z). Walker queries on shore cells
            // fall through this branch via their Z mismatch with SwimSourceZ.
            if (chunk.HasSwimLayer)
            {
                var swimSrc = chunk.SwimSourceZ[cellIndex];
                if (swimSrc != StepChunk.NoSwimLayerCell && Math.Abs(sourceZ - swimSrc) <= StepHeight)
                {
                    switch (hitKindResult)
                    {
                        case CacheHitKind.Miss_NotBuilt:    { _missesNotBuilt++;     break; }
                        case CacheHitKind.Miss_DirtyRebuild: { _missesDirtyRebuild++; break; }
                        case CacheHitKind.Hit:              { _hits++;               break; }
                    }
                    return new StepMask(
                        0, chunk.SwimMask[cellIndex],
                        0, 0, 0, 0, 0, 0, 0, 0,
                        chunk.SwimZN_Layer[cellIndex],
                        chunk.SwimZNE_Layer[cellIndex],
                        chunk.SwimZE_Layer[cellIndex],
                        chunk.SwimZSE_Layer[cellIndex],
                        chunk.SwimZS_Layer[cellIndex],
                        chunk.SwimZSW_Layer[cellIndex],
                        chunk.SwimZW_Layer[cellIndex],
                        chunk.SwimZNW_Layer[cellIndex],
                        hitKindResult
                    );
                }
            }

            _fallthroughSourceZMismatch++;
            return new StepMask(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                CacheHitKind.Fallthrough_SourceZMismatch
            );
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
    /// Returns a fresh StepChunk loaded from the lazy file reader, or null if there's no
    /// open reader for the map / no record at (chunkX, chunkY) / the loaded snapshot is
    /// stale relative to the live sector's <see cref="Map.Sector.MultisVersion"/>. A null
    /// return means the caller should consult the miss tracker; a stale return means
    /// "rebuild, the .swb is out of date and a future SaveToFile will overwrite it."
    /// </summary>
    private StepChunk TryLoadFromLazyReader(Map map, int chunkX, int chunkY)
    {
        if (!_lazyReaders.TryGetValue(map.MapID, out var reader))
        {
            return null;
        }
        // Static-only chunks are valid once the file fingerprint matched at open time; multi-covered
        // cells fall through before reaching here. Returns null when the file lacks this chunk.
        return reader.TryReadChunk(chunkX, chunkY);
    }

    /// <summary>
    /// Records a miss for <paramref name="chunkKey"/> and decides whether to build now
    /// or defer to slow path. Counts distinct Find generations, not raw calls — multiple
    /// TryGetMask calls within one Find (BeginFindGeneration scope) count as one touch.
    /// Returns true when DISTINCT-FIND misses within the window cross
    /// <see cref="MissPromotionThreshold"/>; caller should run BuildChunk and serve.
    /// Returns false otherwise; caller should return Fallthrough_NotBuilt so the algorithm
    /// uses the slow path. Generation 0 ("no Find active") treats every call as distinct,
    /// preserving legacy semantics for callers that don't call BeginFindGeneration.
    /// </summary>
    private bool ShouldPromoteAfterMiss(long chunkKey)
    {
        // Environment.TickCount, not Core.TickCount: tests/bench fixtures may not advance
        // the game-loop tick. The promotion window is wall-clock anyway.
        var now = (uint)Environment.TickCount;
        var gen = CurrentFindGeneration;

        if (_chunkMissTracker.TryGetValue(chunkKey, out var state))
        {
            // Same Find generation as the last touch — A* expansion is probing this chunk
            // multiple times in one pathfind. Don't increment; the gate counts distinct
            // Finds. Skip when gen==0 (no Find started) so legacy single-call tests still
            // see incrementing behavior.
            if (gen != 0 && state.LastFindGeneration == gen)
            {
                return false;
            }

            var elapsed = now - state.LastMissTickStamp;
            if (elapsed > MissPromotionWindowMs)
            {
                _chunkMissTracker[chunkKey] = new ChunkMissState
                {
                    MissCount = 1,
                    LastMissTickStamp = now,
                    LastFindGeneration = gen
                };
                return false;
            }

            var newCount = (byte)Math.Min(state.MissCount + 1, byte.MaxValue);
            if (newCount >= MissPromotionThreshold)
            {
                _chunkMissTracker.Remove(chunkKey);
                return true;
            }

            _chunkMissTracker[chunkKey] = new ChunkMissState
            {
                MissCount = newCount,
                LastMissTickStamp = now,
                LastFindGeneration = gen
            };
            return false;
        }

        if (MissPromotionThreshold <= 1)
        {
            return true;
        }

        if (_chunkMissTracker.Count >= MaxMissTrackerEntries)
        {
            PruneMissTracker(now);
        }

        _chunkMissTracker[chunkKey] = new ChunkMissState
        {
            MissCount = 1,
            LastMissTickStamp = now,
            LastFindGeneration = gen
        };
        return false;
    }

    /// <summary>
    /// Drop tracker entries older than the promotion window. Called when the tracker hits
    /// its capacity ceiling. If the prune doesn't reclaim anything (every entry is in
    /// window), the cap is enforced by clearing — the worst case is a few extra
    /// Fallthrough_NotBuilt returns until traffic re-establishes hot chunks.
    /// </summary>
    private void PruneMissTracker(uint now)
    {
        var window = MissPromotionWindowMs;
        var beforeCount = _chunkMissTracker.Count;
        var toRemove = new List<long>();
        foreach (var kvp in _chunkMissTracker)
        {
            if (now - kvp.Value.LastMissTickStamp > window)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var k in toRemove)
        {
            _chunkMissTracker.Remove(k);
        }
        if (_chunkMissTracker.Count == beforeCount)
        {
            _chunkMissTracker.Clear();
        }
    }

    private StepChunk BuildChunk(Map map, int chunkX, int chunkY)
    {
        var chunk = new StepChunk();

        var baseX = chunkX << 4;
        var baseY = chunkY << 4;

        // Tier 4 strata accumulator. Lazily allocated when the first multi-Z cell
        // appears; otherwise the chunk has zero strata overhead.
        ushort[] strataOffsetByCell = null;
        List<byte> strataData = null;

        // Reused per cell: the standable surface Zs (walkway / bridge / floor levels). 16 is
        // generous — clearance forces standable surfaces >= PersonHeight apart, so a 256-tall
        // Z range admits at most ~16 anyway.
        Span<sbyte> surfaceZs = stackalloc sbyte[16];

        for (var dy = 0; dy < ChunkSize; dy++)
        {
            for (var dx = 0; dx < ChunkSize; dx++)
            {
                var x = baseX + dx;
                var y = baseY + dy;
                var cell = (dy << 4) | dx;

                map.GetAverageZ(x, y, out _, out var avgZ, out _);

                // Anchor the cell at the surface a creature actually STANDS on, not the land
                // average. For plain overworld that's the land; for static-over-land terrain
                // (sewer/dungeon walkways, bridges, stair treads, raised foundations, upper
                // building floors) it's the walkable static surface — which the old
                // ComputeStandingZ(avgZ) anchor missed, producing source-Z fallthroughs (or,
                // within the StepHeight tolerance band on stairs, a wrong vertical-neighbor
                // answer baked at the adjacent tread). ComputeStandableSurfaceZs returns the
                // standable surfaces ascending; the lowest is the primary anchor and A* tracks
                // newZ to match it. Cells with no standable walk surface (deep water, solid
                // rock) fall back to the land avg so the swim layer / wetMask still bake.
                var surfaceCount = StepProbe.ComputeStandableSurfaceZs(map, x, y, surfaceZs);
                var standingZ = surfaceCount > 0 ? surfaceZs[0] : (sbyte)Math.Clamp(avgZ, sbyte.MinValue, sbyte.MaxValue);

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

                // Shore-cell handling: if the cell has BOTH a walk surface (standing Z)
                // AND a water surface (Wet land tile or wet static) at a Z separated by
                // > StepHeight, populate the swim layer at swim-perspective Z. Only when
                // ComputeMaskAt produces a non-zero swim mask — bridges/docks/piers with
                // insufficient vertical clearance for a swim creature's body envelope
                // produce wetMask=0 (StaticsBlockAt rejects them), and we skip those cells
                // rather than baking a stratum that always answers "no movement." The
                // sentinel NoSwimLayerCell stays in SwimSourceZ for skipped cells; the
                // chunk only sets HasSwimLayer when at least one cell got a usable entry.
                var swimZRaw = StepProbe.ComputeSwimStandingZ(map, x, y);
                if (swimZRaw != int.MinValue && Math.Abs(swimZRaw - standingZ) > StepHeight)
                {
                    var swimSrc = (sbyte)Math.Clamp(swimZRaw, sbyte.MinValue + 1, sbyte.MaxValue);
                    var swimResult = StepProbe.ComputeMaskAt(map, x, y, swimSrc);
                    if (swimResult.WetMask != 0)
                    {
                        if (chunk.SwimSourceZ == null)
                        {
                            chunk.AllocateSwimLayer();
                        }
                        chunk.SwimSourceZ[cell]    = swimSrc;
                        chunk.SwimMask[cell]       = swimResult.WetMask;
                        chunk.SwimZN_Layer[cell]   = swimResult.SwimZ_N;
                        chunk.SwimZNE_Layer[cell]  = swimResult.SwimZ_NE;
                        chunk.SwimZE_Layer[cell]   = swimResult.SwimZ_E;
                        chunk.SwimZSE_Layer[cell]  = swimResult.SwimZ_SE;
                        chunk.SwimZS_Layer[cell]   = swimResult.SwimZ_S;
                        chunk.SwimZSW_Layer[cell]  = swimResult.SwimZ_SW;
                        chunk.SwimZW_Layer[cell]   = swimResult.SwimZ_W;
                        chunk.SwimZNW_Layer[cell]  = swimResult.SwimZ_NW;
                    }
                }

                // Stacked walkable surfaces at one cell (ground + 1st + 2nd building floors,
                // a bridge over a walkable path, etc.): bake a stratum per standable surface
                // so a query at any floor's Z hits. The primary (lowest) surface is also in
                // the main mask above, but multi-Z cells are served exclusively from strata,
                // so every standable surface — including the primary — must appear here.
                // Single-surface cells (the common case, incl. stair treads and sewer
                // walkways) skip this entirely and stay on the fast single-mask path.
                if (surfaceCount >= 2)
                {
                    if (strataOffsetByCell == null)
                    {
                        strataOffsetByCell = new ushort[StepChunk.CellsPerChunk];
                        for (var i = 0; i < strataOffsetByCell.Length; i++)
                        {
                            strataOffsetByCell[i] = StepChunk.NoStrata;
                        }
                        strataData = new List<byte>(256);
                    }

                    // Cap at 65,535 byte offsets — well above realistic per-chunk strata
                    // volume. If we ever blow past this we silently leave the cell single-Z
                    // (it keeps the land-anchored main mask and falls through off-surface).
                    if (strataData.Count <= ushort.MaxValue - StepChunk.StratumByteLength * 8)
                    {
                        strataOffsetByCell[cell] = (ushort)strataData.Count;
                        strataData.Add((byte)surfaceCount);
                        for (var i = 0; i < surfaceCount; i++)
                        {
                            var sz = surfaceZs[i];
                            AppendStratumBytes(strataData, new StepProbe.ComputedStratum(sz, StepProbe.ComputeMaskAt(map, x, y, sz)));
                        }
                    }
                }
            }
        }

        if (strataOffsetByCell != null)
        {
            chunk.SetStrata(strataOffsetByCell, strataData.ToArray());
        }

        _buildsTotal++;
        return chunk;
    }

    /// <summary>
    /// Tier 4 strata lookup. Walks the cell's stratum list, returns true with the first
    /// stratum whose <c>zCenter</c> is within StepHeight of <paramref name="sourceZ"/>.
    /// Layout matches <see cref="StepChunk.StrataData"/>: u8 count, then count × 19-byte
    /// stratum (sbyte zCenter, byte walkMask, byte wetMask, 8 sbyte walkZ, 8 sbyte swimZ).
    /// </summary>
    private static bool TryStratumHit(
        StepChunk chunk, int cellIndex, sbyte sourceZ, CacheHitKind hitKind, out StepMask result
    )
    {
        var off = chunk.GetStrataOffset(cellIndex);
        if (off == StepChunk.NoStrata)
        {
            result = default;
            return false;
        }

        var data = chunk.StrataData;
        if (off >= data.Length)
        {
            result = default;
            return false;
        }

        var count = data[off];
        var entryStart = off + 1;
        for (var i = 0; i < count; i++)
        {
            var entryOff = entryStart + i * StepChunk.StratumByteLength;
            if (entryOff + StepChunk.StratumByteLength > data.Length)
            {
                break;
            }
            var zCenter = (sbyte)data[entryOff];
            if (Math.Abs(sourceZ - zCenter) > StepHeight)
            {
                continue;
            }
            result = new StepMask(
                /* walkMask */ data[entryOff + 1],
                /* wetMask  */ data[entryOff + 2],
                (sbyte)data[entryOff + 3],
                (sbyte)data[entryOff + 4],
                (sbyte)data[entryOff + 5],
                (sbyte)data[entryOff + 6],
                (sbyte)data[entryOff + 7],
                (sbyte)data[entryOff + 8],
                (sbyte)data[entryOff + 9],
                (sbyte)data[entryOff + 10],
                (sbyte)data[entryOff + 11],
                (sbyte)data[entryOff + 12],
                (sbyte)data[entryOff + 13],
                (sbyte)data[entryOff + 14],
                (sbyte)data[entryOff + 15],
                (sbyte)data[entryOff + 16],
                (sbyte)data[entryOff + 17],
                (sbyte)data[entryOff + 18],
                hitKind
            );
            return true;
        }

        result = default;
        return false;
    }

    private static void AppendStratumBytes(List<byte> dst, in StepProbe.ComputedStratum s)
    {
        dst.Add((byte)s.ZCenter);
        dst.Add(s.Mask.WalkMask);
        dst.Add(s.Mask.WetMask);
        dst.Add((byte)s.Mask.WalkZ_N);
        dst.Add((byte)s.Mask.WalkZ_NE);
        dst.Add((byte)s.Mask.WalkZ_E);
        dst.Add((byte)s.Mask.WalkZ_SE);
        dst.Add((byte)s.Mask.WalkZ_S);
        dst.Add((byte)s.Mask.WalkZ_SW);
        dst.Add((byte)s.Mask.WalkZ_W);
        dst.Add((byte)s.Mask.WalkZ_NW);
        dst.Add((byte)s.Mask.SwimZ_N);
        dst.Add((byte)s.Mask.SwimZ_NE);
        dst.Add((byte)s.Mask.SwimZ_E);
        dst.Add((byte)s.Mask.SwimZ_SE);
        dst.Add((byte)s.Mask.SwimZ_S);
        dst.Add((byte)s.Mask.SwimZ_SW);
        dst.Add((byte)s.Mask.SwimZ_W);
        dst.Add((byte)s.Mask.SwimZ_NW);
    }

    private const int PersonHeight = 16;
    private const int StepHeight = 2;
}
