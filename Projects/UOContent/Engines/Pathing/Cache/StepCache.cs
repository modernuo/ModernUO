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
        _hits = 0;
        _missesNotBuilt = 0;
        _missesDirtyRebuild = 0;
        _fallthroughMultiZ = 0;
        _fallthroughOffMap = 0;
        _fallthroughSourceZMismatch = 0;
        _evictionsByLruCap = 0;
        _buildsTotal = 0;
    }

    // Per-map open .swb readers, populated by TryOpenLazyReader at startup. Chunks are
    // fetched on demand from the file when ResolveMissingChunk fires; resident memory
    // stays bounded by MaxResidentChunks regardless of file size.
    private readonly Dictionary<int, StepCacheFile.LazyReader> _lazyReaders = new();

    /// <summary>
    /// Combined XxHash3 fingerprint of the running server's TileData flag tables AND
    /// the per-map .mul / .uop file contents (mapX.mul, staidxX.mul, staticsX.mul).
    /// Public surface for tooling (benchmark fixtures, bake utilities) that wants to
    /// detect a stale .swb file without round-tripping through the lazy-open path.
    /// </summary>
    public static ulong ComputeLiveFingerprint(int mapId) => StepCacheFile.ComputeFingerprint(mapId);

    /// <summary>
    /// Peek at a .swb file's stored fingerprint field without parsing the rest of the
    /// header. Returns false on missing file, bad magic, or wrong version.
    /// </summary>
    public static bool TryReadFingerprintFromFile(string path, out ulong fingerprint) =>
        StepCacheFile.TryReadFingerprint(path, out fingerprint);

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

        var chunkCols = (map.Width + ChunkSize - 1) / ChunkSize;
        var chunkRows = (map.Height + ChunkSize - 1) / ChunkSize;

        for (var cy = 0; cy < chunkRows; cy++)
        {
            for (var cx = 0; cx < chunkCols; cx++)
            {
                // Any sourceZ works — the chunk is built on first access regardless of
                // whether the query returns Hit or Fallthrough_SourceZMismatch.
                TryGetMask(map, cx * ChunkSize, cy * ChunkSize, sourceZ: 0);
            }
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
        return true;
    }

    /// <summary>
    /// Number of .swb readers currently open. Mostly for tests / telemetry.
    /// </summary>
    public int OpenLazyReaderCount => _lazyReaders.Count;

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
    /// Chunk-miss resolution: try the lazy file reader for this map first; if there's no
    /// file or no record at this (chunkX, chunkY), fall back to the runtime baker. The
    /// file path validates each loaded chunk's MultisVersion against the live sector — a
    /// stale snapshot triggers a rebuild rather than serving a wrong answer.
    /// </summary>
    private StepChunk ResolveMissingChunk(Map map, int chunkX, int chunkY)
    {
        if (_lazyReaders.TryGetValue(map.MapID, out var reader))
        {
            var loaded = reader.TryReadChunk(chunkX, chunkY);
            if (loaded != null)
            {
                var sector = map.GetRealSector(chunkX, chunkY);
                if (loaded.BuiltMultisVersion == sector.MultisVersion)
                {
                    return loaded;
                }
                // Snapshot is stale (multis added/removed since the bake). Fall through
                // to the runtime baker; a future SaveToFile will overwrite the entry.
            }
        }
        return BuildChunk(map, chunkX, chunkY);
    }

    private StepChunk BuildChunk(Map map, int chunkX, int chunkY)
    {
        var chunk = new StepChunk();
        var sector = map.GetRealSector(chunkX, chunkY);
        chunk.BuiltMultisVersion = sector.MultisVersion;

        var baseX = chunkX << 4;
        var baseY = chunkY << 4;

        // Tier 4 strata accumulator. Lazily allocated when the first multi-Z cell
        // appears; otherwise the chunk has zero strata overhead.
        ushort[] strataOffsetByCell = null;
        System.Collections.Generic.List<byte> strataData = null;

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

                // Multi-Z handling: if the cell has 2+ reachable surfaces, compute its
                // Tier 4 strata so future queries can be answered without falling through
                // to the slow path. ComputeStrataAt returns null for single-Z cells.
                if (CountReachableSurfaces(map, x, y, standingZ) > 1)
                {
                    var strata = StepProbe.ComputeStrataAt(map, x, y);
                    if (strata != null)
                    {
                        if (strataOffsetByCell == null)
                        {
                            strataOffsetByCell = new ushort[StepChunk.CellsPerChunk];
                            for (var i = 0; i < strataOffsetByCell.Length; i++)
                            {
                                strataOffsetByCell[i] = StepChunk.NoStrata;
                            }
                            strataData = new System.Collections.Generic.List<byte>(256);
                        }

                        // Cap at 65,535 byte offsets — well above realistic per-chunk
                        // strata volume. If we ever blow past this we'd silently truncate;
                        // assert as a defensive guard.
                        if (strataData.Count > ushort.MaxValue - StepChunk.StratumByteLength * 8)
                        {
                            // Should never happen for sane tile data; bail to fallthrough.
                        }
                        else
                        {
                            strataOffsetByCell[cell] = (ushort)strataData.Count;
                            strataData.Add((byte)strata.Length);
                            for (var s = 0; s < strata.Length; s++)
                            {
                                AppendStratumBytes(strataData, strata[s]);
                            }
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

    private static void AppendStratumBytes(
        System.Collections.Generic.List<byte> dst, in StepProbe.ComputedStratum s
    )
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
