using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Buffers;
using Server.Collections;
using Server.Logging;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Singleton store of static walkability, keyed by 16x16 chunk (one per map sector). Chunks build
/// on demand and memory stays bounded by MaxResidentChunks through probabilistic LRU eviction, so
/// the cache is usable with no on-disk bake at all; a baked .swb file only removes the first-touch
/// build cost.
///
/// The cache answers for a default walker on static terrain. Anything outside that — a multi
/// covering the cell, a query Z that doesn't match what the cell was baked at, stacked surfaces
/// with no matching stratum — returns a Fallthrough_* kind, and the caller resolves that cell
/// through MovementImpl instead. Callers must check <see cref="StepMask.IsHit"/>.
/// </summary>
public sealed class StepCache
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StepCache));

    public static StepCache Instance { get; } = new();

    private readonly Dictionary<long, StepChunk> _chunks = [];
    // Keys of _chunks, kept in lockstep with it, so eviction can sample a random resident chunk
    // in O(1). Appended on insert, swap-and-popped on eviction.
    private readonly List<long> _keysList = [];

    // Promotion gate. A chunk's first miss returns Fallthrough_NotBuilt and the caller takes the
    // slow path; only once misses reach MissPromotionThreshold within MissPromotionWindowMs does
    // the chunk get built and served. This keeps one-off traffic — a pet trailing a player across
    // the map — from building chunks nothing will query again, while a creature working a fixed
    // territory still warms the chunks it revisits.
    //
    // The gate counts distinct Finds, not TryGetMask calls: A* probes each chunk it visits dozens
    // of times within a single pathfind, so per-call counting would cross any threshold instantly
    // and gate nothing.
    private readonly Dictionary<long, ChunkMissState> _chunkMissTracker = [];
    private const int MaxMissTrackerEntries = 4096;

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
    private long _multiLocalHits;
    private long _multiMaskCacheHits;
    private long _evictionsByLruCap;
    private long _buildsTotal;

    private StepCache() { }

    public void RecordMultiLocalHit() => _multiLocalHits++;

    public void RecordMultiMaskCacheHit() => _multiMaskCacheHits++;

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
    /// Misses on the same chunk, within <see cref="MissPromotionWindowMs"/>, needed to build it.
    /// 1 builds eagerly on first touch; the default 2 waits for a second Find to show interest.
    /// </summary>
    public int MissPromotionThreshold { get; set; } = 2;

    /// <summary>
    /// How long misses on a chunk accumulate toward promotion. A gap wider than this restarts
    /// the count.
    /// </summary>
    public uint MissPromotionWindowMs { get; set; } = 30_000;

    /// <summary>
    /// Opens a new pathfind for the promotion gate. Call once per pathfind: the gate counts
    /// distinct Finds, so without this every cell expansion would count separately and the
    /// threshold would be met immediately. Wraps back to 1, since 0 means "no Find open".
    /// </summary>
    public void BeginFindGeneration()
    {
        unchecked { CurrentFindGeneration++; }

        if (CurrentFindGeneration == 0)
        {
            CurrentFindGeneration = 1;
        }
    }

    /// <summary>The open pathfind's generation, or 0 if none. See <see cref="BeginFindGeneration"/>.</summary>
    internal uint CurrentFindGeneration { get; private set; }

    /// <summary>
    /// Packs (mapId, chunkX, chunkY) into one key: [reserved 16][mapId 16][chunkX 16][chunkY 16].
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
        multiLocalHits: _multiLocalHits,
        multiMaskCacheHits: _multiMaskCacheHits,
        evictionsByLruCap: _evictionsByLruCap,
        buildsTotal: _buildsTotal
    );

    /// <summary>
    /// Returns the cache to a cold-start state: drops every chunk, closes the .swb readers, and
    /// zeroes the counters.
    /// </summary>
    public void Clear()
    {
        ClearResidentChunks();
        CloseLazyReaders();
        MultiMaskCache.Instance.Clear();
    }

    /// <summary>
    /// <see cref="Clear"/> without the file-handle teardown: drops the resident chunks and zeroes
    /// the counters, but leaves the .swb readers open so the next query can refill from them.
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
        _multiLocalHits = 0;
        _multiMaskCacheHits = 0;
        _evictionsByLruCap = 0;
        _buildsTotal = 0;
    }

    // Open .swb readers, one per map. Chunks are pulled from them on demand, so resident memory
    // stays bounded by MaxResidentChunks no matter how large the file is.
    private readonly Dictionary<int, StepCacheFile.LazyReader> _lazyReaders = [];

    /// <summary>
    /// Builds every chunk in the map and writes them to <paramref name="path"/>, returning the
    /// number written. Blocks the caller for many seconds on a full-size map — run it offline or
    /// during maintenance, not on a live shard at peak.
    /// </summary>
    public int BakeMap(int mapId, string path)
    {
        var map = Map.Maps[mapId];
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        // A bake touches each chunk exactly once, so the promotion gate would defer every one of
        // them and write an empty file. Baking is an explicit decision to populate everything, so
        // build eagerly for the duration.
        var prevThreshold = MissPromotionThreshold;
        MissPromotionThreshold = 1;
        try
        {
            var chunkCols = (map.Width + ChunkSize - 1) / ChunkSize;
            var chunkRows = (map.Height + ChunkSize - 1) / ChunkSize;
            var logEvery = Math.Max(1, chunkRows / 32);

            logger.Information(
                "PathBake map {MapId}: walking {Cols}x{Rows} = {Total} chunks (synchronous; no eviction during the walk)...",
                mapId, chunkCols, chunkRows, chunkCols * chunkRows
            );

            var stopWatch = Stopwatch.StartNew();
            for (var cy = 0; cy < chunkRows; cy++)
            {
                for (var cx = 0; cx < chunkCols; cx++)
                {
                    // The sourceZ is irrelevant here: the chunk gets built on first access whether
                    // the query ends up a Hit or a Fallthrough_SourceZMismatch.
                    TryGetMask(map, cx * ChunkSize, cy * ChunkSize, sourceZ: 0);
                }

                if ((cy + 1) % logEvery == 0 || cy == chunkRows - 1)
                {
                    logger.Information(
                        "PathBake map {MapId}: row {Row}/{Rows} ({Pct}%), {Resident} chunks resident, {Elapsed:F1}s, {HeapMB} MB heap",
                        mapId, cy + 1, chunkRows, (cy + 1) * 100 / chunkRows,
                        _chunks.Count, stopWatch.ElapsedMilliseconds / 1000.0, GC.GetTotalMemory(false) >> 20
                    );
                }
            }

            logger.Information(
                "PathBake map {MapId}: walk complete in {Elapsed:F1}s, writing {Resident} chunks to disk...",
                mapId, stopWatch.ElapsedMilliseconds / 1000.0, _chunks.Count
            );
        }
        finally
        {
            MissPromotionThreshold = prevThreshold;
        }

        return SaveToFile(path, mapId);
    }

    /// <summary>
    /// Writes the map's resident chunks to a .swb file and returns the count. The file carries a
    /// fingerprint of the tile and map data, so a bake made before a client patch is detected and
    /// rejected when it is next opened.
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
    /// Opens a .swb file as a backing store for the map, reading only the header and chunk index
    /// up front; records are pulled as queries ask for them. Returns false if the file is missing,
    /// unreadable, or a stale bake whose fingerprint no longer matches the live tile data.
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
    /// Loads every chunk in the file into the resident set, for <see cref="PreloadOnLazyOpen"/>.
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

    /// <summary>Number of .swb readers currently open.</summary>
    public int OpenLazyReaderCount => _lazyReaders.Count;

    /// <summary>
    /// True when a .swb reader is open for the map. A reader only opens after its fingerprint
    /// validates against the live tile data, so this already answers "is there an up-to-date bake
    /// for this map?" — the boot prebake leans on that to skip maps rather than fingerprint them
    /// a second time.
    /// </summary>
    public bool HasLazyReader(int mapId) => _lazyReaders.ContainsKey(mapId);

    /// <summary>Diagnostic: does the map's .swb hold a record for (chunkX, chunkY)?</summary>
    internal bool LazyReaderHasChunk(int mapId, int chunkX, int chunkY) =>
        _lazyReaders.TryGetValue(mapId, out var r) && r.Has(chunkX, chunkY);

    /// <summary>Closes every open .swb reader, releasing the underlying file streams.</summary>
    public void CloseLazyReaders()
    {
        foreach (var reader in _lazyReaders.Values)
        {
            reader.Dispose();
        }
        _lazyReaders.Clear();
    }

    /// <summary>
    /// How many random resident chunks each eviction samples before dropping the oldest of them.
    /// Sampling approximates true LRU closely enough at a fraction of the cost, since it needs no
    /// sort and no access-ordered structure. Raising it trades speed for accuracy.
    /// </summary>
    private const int LruSampleSize = 5;

    /// <summary>
    /// Evicts chunks until the resident count is back within MaxResidentChunks. Each eviction costs
    /// O(<see cref="LruSampleSize"/>) regardless of how many chunks are resident, so sustained cap
    /// pressure doesn't degrade. Driven by <see cref="CacheEvictionTimer"/>.
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

            // Sampling with replacement: a repeated key just wastes one sample, it can't pick a
            // wrong victim.
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
    /// All-zero mask carrying a Fallthrough_* kind. <see cref="StepMask.IsHit"/> is false for
    /// these, so the caller ignores the payload and takes the slow path.
    /// </summary>
    private static StepMask Fallthrough(CacheHitKind kind) =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, kind);

    /// <summary>Bumps the telemetry counter matching a served (non-fallthrough) hit kind.</summary>
    private void RecordServed(CacheHitKind kind)
    {
        switch (kind)
        {
            case CacheHitKind.Miss_NotBuilt:     { _missesNotBuilt++;     break; }
            case CacheHitKind.Miss_DirtyRebuild: { _missesDirtyRebuild++; break; }
            case CacheHitKind.Hit:               { _hits++;               break; }
        }
    }

    /// <summary>
    /// True when a multi covers (x, y) or any of its 8 neighbours. The halo matters because a
    /// cell's mask encodes the edges TO its neighbours, so a wall one cell over has to block those
    /// edges. Since a chunk is a sector, an interior cell only inspects its own sector's HasMultis
    /// flag; edge and corner cells additionally check whichever adjacent sectors the halo reaches.
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
            return false; // interior cell: its whole halo lies in this sector, which has no multis
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
    /// The hot-path query: one lookup yields the cell's 8-direction mask, its 8 destination Zs,
    /// and the hit kind. Check <see cref="StepMask.IsHit"/> before trusting the payload — on any
    /// fallthrough it is all zeroes and the caller must resolve the cell through MovementImpl.
    /// </summary>
    public StepMask TryGetMask(Map map, int x, int y, sbyte sourceZ)
    {
        if (map == null || map == Map.Internal || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            _fallthroughOffMap++;
            return Fallthrough(CacheHitKind.Fallthrough_OffMap);
        }

        // Multis are dynamic, so they are never baked into a chunk. Cells they touch go to the
        // multi-aware path instead. The check is gated on Sector.HasMultis, so the multi-free
        // majority of the map pays one sector lookup for it.
        if (MultiInfluence(map, x, y))
        {
            _fallthroughMulti++;
            return Fallthrough(CacheHitKind.Fallthrough_Multi);
        }

        var chunkX = x >> 4;
        var chunkY = y >> 4;
        var key = EncodeKey(map.MapID, chunkX, chunkY);

        var hitKindResult = CacheHitKind.Hit;
        if (!_chunks.TryGetValue(key, out var chunk))
        {
            // The .swb is consulted before the promotion gate: a baked chunk is already an explicit
            // decision to keep this area warm, and loading it is far cheaper than building it.
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
                return Fallthrough(CacheHitKind.Fallthrough_NotBuilt);
            }
        }

        // No staleness check: a resident chunk holds only static terrain, and every cell a multi
        // could have changed already fell through above.
        chunk.LastTouchedTicks = Core.TickCount;

        var cellIndex = ((y - (chunkY << 4)) << 4) | (x - (chunkX << 4));

        if (chunk.IsCellMultiZ(cellIndex))
        {
            // Stacked surfaces: pick the stratum baked nearest the query Z. Multi-Z cells are
            // served only from strata, never from the main mask.
            if (TryStratumHit(chunk, cellIndex, sourceZ, hitKindResult, out var stratumResult))
            {
                RecordServed(hitKindResult);
                return stratumResult;
            }

            _fallthroughMultiZ++;
            return Fallthrough(CacheHitKind.Fallthrough_MultiZ);
        }

        // Source-Z guard. A cell holds one answer, baked at one standing Z, so a query from too far
        // above or below it would get an answer that doesn't apply. The StepHeight tolerance
        // absorbs ordinary Z jitter and cannot be widened: reachability flips at exactly that
        // boundary, so a looser guard would serve answers that disagree with MovementImpl.
        if (Math.Abs(sourceZ - chunk.SourceZ[cellIndex]) > StepHeight)
        {
            // Unless this is a shore cell and the query is coming from the water, in which case the
            // swim layer holds the answer baked from the water surface.
            if (chunk.HasSwimLayer)
            {
                var swimSrc = chunk.SwimSourceZ[cellIndex];
                if (swimSrc != StepChunk.NoSwimLayerCell && Math.Abs(sourceZ - swimSrc) <= StepHeight)
                {
                    RecordServed(hitKindResult);
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
            return Fallthrough(CacheHitKind.Fallthrough_SourceZMismatch);
        }

        RecordServed(hitKindResult);

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
    /// Loads a chunk from the map's .swb, or null if no reader is open or the file has no record at
    /// (chunkX, chunkY). No staleness check is needed here — the fingerprint was validated when the
    /// file was opened, and the chunks are static-only.
    /// </summary>
    private StepChunk TryLoadFromLazyReader(Map map, int chunkX, int chunkY) =>
        _lazyReaders.TryGetValue(map.MapID, out var reader) ? reader.TryReadChunk(chunkX, chunkY) : null;

    /// <summary>
    /// Records a miss and answers whether the chunk has now earned a build. True means build and
    /// serve; false means return Fallthrough_NotBuilt and let the caller take the slow path.
    ///
    /// A miss only counts once per Find (see <see cref="BeginFindGeneration"/>). With no Find open
    /// — a direct caller, or a bake — every call counts separately.
    /// </summary>
    private bool ShouldPromoteAfterMiss(long chunkKey)
    {
        // Environment.TickCount rather than Core.TickCount: the window is wall-clock, and test and
        // benchmark fixtures don't necessarily advance the game loop's tick.
        var now = (uint)Environment.TickCount;
        var gen = CurrentFindGeneration;

        // One hash lookup for the whole update — the entry is mutated through the ref instead
        // of being re-hashed and re-probed by an indexer assignment. Safe to hold across the
        // Remove below only because nothing reads it afterwards.
        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_chunkMissTracker, chunkKey);
        if (!Unsafe.IsNullRef(ref state))
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
                // Outside the window — restart the count. Never promotes on this call, even at
                // threshold 1, matching the pre-existing gate semantics.
                state.MissCount = 1;
                state.LastMissTickStamp = now;
                state.LastFindGeneration = gen;
                return false;
            }

            var newCount = (byte)Math.Min(state.MissCount + 1, byte.MaxValue);
            if (newCount >= MissPromotionThreshold)
            {
                _chunkMissTracker.Remove(chunkKey);
                return true;
            }

            state.MissCount = newCount;
            state.LastMissTickStamp = now;
            state.LastFindGeneration = gen;
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
    /// Drops tracker entries that have aged out of the promotion window, once the tracker hits its
    /// capacity ceiling. When nothing has aged out, the whole tracker is cleared to enforce the cap
    /// — that costs a few extra Fallthrough_NotBuilt returns while traffic re-establishes the hot
    /// chunks, which is cheaper than letting the tracker grow without bound.
    /// </summary>
    private void PruneMissTracker(uint now)
    {
        var window = MissPromotionWindowMs;
        var beforeCount = _chunkMissTracker.Count;

        using var toRemove = PooledRefQueue<long>.Create();
        foreach (var kvp in _chunkMissTracker)
        {
            if (now - kvp.Value.LastMissTickStamp > window)
            {
                toRemove.Enqueue(kvp.Key);
            }
        }

        while (toRemove.Count > 0)
        {
            _chunkMissTracker.Remove(toRemove.Dequeue());
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

        // Strata accumulator, created on the first multi-Z cell so single-Z chunks pay nothing.
        // strataData is rented scratch; the chunk receives an exact-size copy, so the pooled array
        // never escapes this method.
        ushort[] strataOffsetByCell = null;
        byte[] strataData = null;
        var strataLen = 0;

        // Standable surface Zs for the current cell. 16 slots is generous: clearance forces
        // surfaces at least PersonHeight apart, so an sbyte Z range can't hold more than ~16.
        Span<sbyte> surfaceZs = stackalloc sbyte[16];

        for (var dy = 0; dy < ChunkSize; dy++)
        {
            for (var dx = 0; dx < ChunkSize; dx++)
            {
                var x = baseX + dx;
                var y = baseY + dy;
                var cell = (dy << 4) | dx;

                map.GetAverageZ(x, y, out _, out var avgZ, out _);

                // Anchor the cell at the surface a creature stands on, not the land average. On
                // open terrain those coincide, but on static-over-land geometry — walkways,
                // bridges, stair treads, upper floors — the walkable surface is the static, and
                // anchoring at the land below it would make every query fall through the source-Z
                // guard. Surfaces come back ascending and the lowest is the anchor; A* tracks its
                // per-cell Z to match. A cell with no standable surface at all (deep water, solid
                // rock) falls back to the land average so its swim data still bakes.
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

                // Shore cell: a walkable surface and a water surface more than StepHeight apart.
                // The main mask is baked at the walk surface, so a swimmer querying from the water
                // would fail the source-Z guard; bake it a second answer from the water surface.
                // An empty swim mask means the water is unreachable anyway — a dock or pier with
                // too little clearance for a swimmer's body — so leave those cells at the
                // NoSwimLayerCell sentinel rather than store an answer that always says "blocked".
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

                // Stacked walkable surfaces — a bridge over a path, the floors of a building —
                // need one stratum each so a query at any of their Zs finds an answer. Every
                // surface goes in, including the lowest, because a multi-Z cell is served only
                // from its strata and never from the main mask baked above.
                if (surfaceCount >= 2)
                {
                    if (strataOffsetByCell == null)
                    {
                        strataOffsetByCell = new ushort[StepChunk.CellsPerChunk];
                        strataOffsetByCell.AsSpan().Fill(StepChunk.NoStrata);
                        // NoStrata bounds the packed data to NoStrata bytes (see StepChunk), so
                        // renting that much up front leaves the record guard below as the only
                        // bound the writes need.
                        strataData = STArrayPool<byte>.Shared.Rent(StepChunk.NoStrata);
                    }

                    // One count byte plus a record per surface. A cell whose record won't fit stays
                    // single-Z: it keeps the main mask and falls through off its anchor surface.
                    var recordLength = 1 + surfaceCount * StepChunk.StratumByteLength;
                    if (strataLen + recordLength <= StepChunk.NoStrata)
                    {
                        strataOffsetByCell[cell] = (ushort)strataLen;
                        strataData[strataLen++] = (byte)surfaceCount;
                        for (var i = 0; i < surfaceCount; i++)
                        {
                            var sz = surfaceZs[i];
                            WriteStratum(strataData, ref strataLen, sz, StepProbe.ComputeMaskAt(map, x, y, sz));
                        }
                    }
                }
            }
        }

        if (strataOffsetByCell != null)
        {
            chunk.SetStrata(strataOffsetByCell, strataData.AsSpan(0, strataLen).ToArray());
            STArrayPool<byte>.Shared.Return(strataData);
        }

        _buildsTotal++;
        return chunk;
    }

    /// <summary>
    /// Finds the cell's stratum matching <paramref name="sourceZ"/> — the first whose zCenter is
    /// within StepHeight — and builds its mask. False when the cell has no strata or none of them
    /// sit near enough, in which case the caller falls through. Reads the layout
    /// <see cref="WriteStratum"/> writes.
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

    /// <summary>
    /// Packs one stratum into <paramref name="dst"/> at <paramref name="pos"/>, advancing it by
    /// <see cref="StepChunk.StratumByteLength"/>. Layout must stay in lockstep with
    /// <see cref="TryStratumHit"/> and <see cref="StepCacheFile"/>.
    /// </summary>
    private static void WriteStratum(Span<byte> dst, ref int pos, sbyte zCenter, in StepMask mask)
    {
        dst[pos++] = (byte)zCenter;
        dst[pos++] = mask.WalkMask;
        dst[pos++] = mask.WetMask;
        dst[pos++] = (byte)mask.WalkZ_N;
        dst[pos++] = (byte)mask.WalkZ_NE;
        dst[pos++] = (byte)mask.WalkZ_E;
        dst[pos++] = (byte)mask.WalkZ_SE;
        dst[pos++] = (byte)mask.WalkZ_S;
        dst[pos++] = (byte)mask.WalkZ_SW;
        dst[pos++] = (byte)mask.WalkZ_W;
        dst[pos++] = (byte)mask.WalkZ_NW;
        dst[pos++] = (byte)mask.SwimZ_N;
        dst[pos++] = (byte)mask.SwimZ_NE;
        dst[pos++] = (byte)mask.SwimZ_E;
        dst[pos++] = (byte)mask.SwimZ_SE;
        dst[pos++] = (byte)mask.SwimZ_S;
        dst[pos++] = (byte)mask.SwimZ_SW;
        dst[pos++] = (byte)mask.SwimZ_W;
        dst[pos++] = (byte)mask.SwimZ_NW;
    }

    private const int StepHeight = 2;
}
