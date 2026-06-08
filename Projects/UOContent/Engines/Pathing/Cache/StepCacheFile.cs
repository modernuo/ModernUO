using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Server.Compression;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Binary serializer + lazy reader for the step cache. Persists chunk records to disk
/// so a server warm-starts without paying chunk-build cost on the first pathfind through
/// a region. Lazy: opening a file reads only the header + chunk-offset index (~few KB
/// for tens of thousands of chunks), then individual chunks are seeked + deserialized
/// only when the cache asks for them. RAM stays bounded by MaxResidentChunks regardless
/// of file size.
///
/// File layout v8 (little-endian, BufferWriter / BufferReader convention):
///
///   Header (40 bytes):
///     u32   Magic           = 0x42575300 ('SWB\0')
///     u32   Version         = current FormatVersion (9)
///     u32   MapId
///     u64   Fingerprint     XxHash3 over (1) LandTable + ItemTable flags AND (2) the
///                           on-disk bytes of mapX.mul / .uop, staidxX.mul, staticsX.mul.
///                           Rejects a load when EITHER tile flags shifted (client patch)
///                           OR the map data was rewritten (CentredSharp / UOFiddler edit).
///                           The .mul format has no built-in CRC; this is the only way
///                           to detect those mutations.
///     u64   BakeTimestamp   DateTime.UtcNow.Ticks at write time (informational).
///     u32   ChunkCount
///     u64   IndexOffset     File position where the chunk index begins.
///
///   Per chunk (ChunkCount times, variable size):
///     u32    UncompressedLen   Size of the inflated record body below.
///     byte[] Payload           The record body (the v6 layout that follows), libdeflate-
///                              compressed. If the on-disk payload length (index recordLength −
///                              4) equals UncompressedLen, the body was stored raw because
///                              compression did not shrink it (tiny Uniform records).
///
///   Record body (after inflate — the v6 layout):
///     u16    ChunkX
///     u16    ChunkY
///     u32    BuiltMultisVersion  (reserved since v9 — always 0; chunks are static-only)
///     u8     Kind            0 = Full; 2 = Uniform
///     // Uniform (Kind == 2): ~28-byte record — all 256 cells share these single values:
///     byte   walkMask, wetMask;  sbyte sourceZ;  sbyte walkZ_N..NW (8);  sbyte swimZ_N..NW (8)
///     // Full (Kind == 0) body:
///     u8     HasStrata       0 = single-Z chunk (no strata trailer); 1 = strata trailer follows
///     u8     HasSwimLayer    0 = no shore cells (no swim trailer); 1 = swim trailer follows
///     u16    ZArrayMask      bit d set => base directional Z array d is present below as a
///                            residual[256] block; cleared => array equals its prediction and is
///                            omitted (synthesized at read). bits 0-7 = WalkZ N..NW (predicted via
///                            WalkMask), bits 8-15 = SwimZ N..NW (predicted via WetMask).
///     byte   WalkMask[256]
///     byte   WetMask[256]
///     sbyte  SourceZ[256]
///     // For each d in 0..15 with ZArrayMask bit d set, in N,NE,E,SE,S,SW,W,NW order
///     // (walk arrays first, then swim):
///     sbyte  residual_d[256]   reconstruct: Z_d[c] = (mask bit set ? SourceZ[c] : 0) + residual_d[c]
///     // Swim layer trailer — only when HasSwimLayer == 1 (chunks containing shore cells):
///     sbyte  SwimSourceZ[256]            (NoSwimLayerCell sentinel = sbyte.MinValue)
///     byte   SwimMask[256]               (per-cell swim mask baked at SwimSourceZ)
///     sbyte  SwimZN_Layer[256]..SwimZNW_Layer[256]  (8 arrays, dest-Z at swim perspective)
///     // Strata trailer — only when HasStrata == 1:
///     u16    StrataOffsetByCell[256]    (NoStrata sentinel = 0xFFFF)
///     u32    StrataDataLength
///     byte   StrataData[StrataDataLength]
///       For each multi-Z cell: u8 stratumCount, then stratumCount × Stratum (19 bytes):
///         sbyte zCenter
///         byte  walkMask, wetMask
///         sbyte walkZ_N..NW (8)
///         sbyte swimZ_N..NW (8)
///
///   Index trailer (8 × ChunkCount bytes), in record write order:
///     For each chunk: { u32 packedKey = (ChunkX << 16) | ChunkY, u32 recordLength }
///     The file offset is not stored — reconstructed as a cumulative sum of recordLength
///     starting at HeaderSize (the first record sits immediately after the header).
///
/// Per-chunk fixed portion (Kind + flags + ZArrayMask + WalkMask + WetMask + SourceZ):
/// ~783 bytes; each present base Z array adds 256 bytes (0..16 present, so up to ~4 KB).
/// A fully-flat Full chunk stores no residual blocks. Strata trailer: 516 + N × ~30 bytes
/// for a chunk with N multi-Z cells averaging ~2 strata each. LRU bookkeeping
/// (LastTouchedTicks) is intentionally not persisted.
///
/// Files with version &lt; <see cref="MinSupportedVersion"/> are silently rejected
/// at open time (treated as missing) and overwritten on the next save.
/// </summary>
internal static class StepCacheFile
{
    public const uint Magic = 0x42575300; // 'SWB\0'

    // v9: chunks are STATIC-ONLY (land + statics.mul, no multis). v8 and earlier baked multis
    // (houses/boats) into chunks, which is unsafe to persist — multis are dynamic, and the
    // BuiltMultisVersion they were tagged with is a non-persisted session counter. Bumping the
    // version rejects those old files so they re-bake static-only. The BuiltMultisVersion record
    // field is retained as a reserved (always-0) u32 to avoid a layout change.
    public const uint FormatVersion = 9;

    /// <summary>
    /// Lowest format version this binary can load. Files below it are treated as missing
    /// (silently rejected) and overwritten by the next SaveToFile / BakeMap. The cache is
    /// fully regenerable, so a format bump just forces a one-time re-bake of stale files.
    /// </summary>
    public const uint MinSupportedVersion = 9;

    // Per-chunk record discriminator (first byte after BuiltMultisVersion). 1 is reserved.
    private const byte KindFull = 0;
    private const byte KindUniform = 2;

    private const int HeaderSize =
        sizeof(uint)    // Magic
        + sizeof(uint)  // Version
        + sizeof(uint)  // MapId
        + sizeof(ulong) // Fingerprint
        + sizeof(ulong) // BakeTimestamp
        + sizeof(uint)  // ChunkCount
        + sizeof(ulong); // IndexOffset

    // Index entry (v8 compact): u32 packedKey ((chunkX << 16) | chunkY) + u32 recordLength.
    // The file offset is NOT stored — entries are in record write order, so the reader
    // reconstructs each offset by cumulative sum of record lengths starting at HeaderSize.
    private const int IndexEntryBytes = sizeof(uint) + sizeof(uint);

    /// <summary>Fixed-size portion of a chunk record (everything except the optional strata + swim trailers).</summary>
    private const int BytesPerChunkBase =
        sizeof(ushort) + sizeof(ushort) + sizeof(uint)
        + sizeof(byte) + sizeof(byte) + sizeof(byte) // Kind + HasStrata + HasSwimLayer
        + sizeof(ushort)                             // ZArrayMask
        + StepChunk.CellsPerChunk           // WalkMask
        + StepChunk.CellsPerChunk           // WetMask
        + StepChunk.CellsPerChunk           // SourceZ
        + 8 * StepChunk.CellsPerChunk       // WalkZ[8]
        + 8 * StepChunk.CellsPerChunk;      // SwimZ[8]

    /// <summary>
    /// Byte offset of the IndexOffset u64 within the header
    /// (Magic+Version+MapId+Fingerprint+BakeTimestamp+ChunkCount = 32). Patched after chunks land.
    /// </summary>
    private const int IndexOffsetFieldPosition = 32;

    public delegate bool ChunkEnumerator(out int chunkX, out int chunkY, out StepChunk chunk);

    /// <summary>
    /// Peek at a .swb file's Fingerprint field (header byte offset 12) without
    /// reading any chunk data. Returns false on missing file, bad magic, or wrong
    /// version. Cheap — reads 20 bytes total.
    /// </summary>
    public static bool TryReadFingerprint(string path, out ulong fingerprint)
    {
        fingerprint = 0;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            Span<byte> buf = stackalloc byte[20];
            if (stream.Read(buf) < 20)
            {
                return false;
            }
            if (BinaryPrimitives.ReadUInt32LittleEndian(buf) != Magic)
            {
                return false;
            }
            var version = BinaryPrimitives.ReadUInt32LittleEndian(buf[4..]);
            if (version < MinSupportedVersion || version > FormatVersion)
            {
                return false;
            }
            // mapId is at buf[8..12], we skip; hash is at buf[12..20].
            fingerprint = BinaryPrimitives.ReadUInt64LittleEndian(buf[12..]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Combined XxHash3 fingerprint over (1) the on-disk <c>tiledata.mul</c> file and (2) the
    /// per-map .mul / .uop file contents (via <see cref="TileMatrix.MapFilesFingerprint"/>).
    /// Bake files carry this hash so a load can refuse to populate the cache when EITHER the
    /// tile data shifted (client patch) OR the map data was rewritten (CentredSharp / UOFiddler
    /// edit). The .mul format has no built-in CRC; this is the only way to detect those mutations.
    ///
    /// IMPORTANT: hash the FILES, never the in-memory <see cref="TileData.LandTable"/> /
    /// <see cref="TileData.ItemTable"/>. The server patches those tables at runtime (ItemFixes,
    /// LOSBlocker, PotionKeg, CTF, ...) at nondeterministic lifecycle points, so a fingerprint over
    /// the live tables varies with WHEN it is taken; the file hash is the only lifecycle-stable
    /// "did the client's tile data change?" signal. Server-side tile patches are applied identically
    /// every boot and intentionally do NOT invalidate the cache — change one and you must
    /// [PathCacheClear or bump the format.
    /// </summary>
    public static ulong ComputeFingerprint(int mapId)
    {
        var hasher = HashUtility.CreateXxHash3();

        // (1) tiledata.mul — hashed once, cached. The authoritative source for tile flags/heights.
        Span<byte> tileDataBytes = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(tileDataBytes, TileDataFileFingerprint());
        hasher.Append(tileDataBytes);

        // (2) Map files (mapX.mul / .uop, staidxX.mul, staticsX.mul). TileMatrix already
        // streamed them through XxHash3 once at construction; mix the result in.
        var map = Map.Maps[mapId];
        if (map != null && map != Map.Internal && map.Tiles != null)
        {
            Span<byte> mapHashBytes = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(mapHashBytes, map.Tiles.MapFilesFingerprint);
            hasher.Append(mapHashBytes);
        }

        return hasher.GetCurrentHashAsUInt64();
    }

    private static ulong _tileDataFileFingerprint;
    private static bool _tileDataFileFingerprintComputed;

    /// <summary>
    /// XxHash3 over the raw <c>tiledata.mul</c> bytes, computed once and cached — the file never
    /// changes during a run. Mirrors <see cref="TileMatrix.MapFilesFingerprint"/> for the map
    /// files. Returns 0 if the file can't be found (the server can't run without it anyway, so
    /// this only matters in stripped test hosts, where 0 is a fine deterministic constant).
    /// </summary>
    private static ulong TileDataFileFingerprint()
    {
        if (_tileDataFileFingerprintComputed)
        {
            return _tileDataFileFingerprint;
        }

        var path = Core.FindDataFile("tiledata.mul", false);
        if (path != null)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var hasher = HashUtility.CreateXxHash3();
            hasher.Append(fs);
            _tileDataFileFingerprint = hasher.GetCurrentHashAsUInt64();
        }

        _tileDataFileFingerprintComputed = true;
        return _tileDataFileFingerprint;
    }

    /// <summary>
    /// Writes the file: header (with placeholder IndexOffset) → chunks (offsets recorded)
    /// → index trailer → patches the header IndexOffset. <paramref name="chunkCount"/> must
    /// equal the actual number of chunks <paramref name="next"/> will yield.
    /// </summary>
    public static void Write(string path, uint mapId, uint chunkCount, ChunkEnumerator next)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        // Initial estimate: base record + a modest strata budget per chunk. Coastline
        // chunks add another ~2.5 KB (swim layer) but they're a small fraction of any
        // map; the writer grows on overflow so under-estimating just causes a few
        // realloc/copy cycles during the bake — not a correctness issue.
        var capacity = HeaderSize + (BytesPerChunkBase + 256) * (int)chunkCount + IndexEntryBytes * (int)chunkCount;
        var buffer = new byte[capacity];
        var w = new BufferWriter(buffer, prefixStr: false);

        w.Write(Magic);
        w.Write(FormatVersion);
        w.Write(mapId);
        w.Write(ComputeFingerprint((int)mapId));
        w.Write((ulong)DateTime.UtcNow.Ticks);
        w.Write(chunkCount);
        w.Write(0UL); // IndexOffset placeholder, patched after chunks

        // Each record is built uncompressed into recordScratch, then libdeflate-compressed into
        // compScratch and framed as [u32 uncompressedLen][payload].
        var packer = Deflate.Maximum;
        var recordScratch = new byte[BytesPerChunkBase + 1024];
        var compScratch = new byte[packer.MaxPackSize(recordScratch.Length)];

        var indexEntries = new (ulong key, ulong offset, uint length)[chunkCount];
        var written = 0u;
        while (next(out var chunkX, out var chunkY, out var chunk))
        {
            if (written >= chunkCount)
            {
                throw new InvalidOperationException(
                    $"StepCacheFile.Write: enumerator yielded more than the declared {chunkCount} chunks"
                );
            }
            var chunkOffset = (ulong)w.Position;
            WriteChunk(w, chunkX, chunkY, chunk, packer, ref recordScratch, ref compScratch);
            var chunkLength = (uint)((ulong)w.Position - chunkOffset);
            indexEntries[written] = (PackChunkKey(chunkX, chunkY), chunkOffset, chunkLength);
            written++;
        }

        if (written != chunkCount)
        {
            throw new InvalidOperationException(
                $"StepCacheFile.Write: declared {chunkCount} chunks but enumerator yielded {written}"
            );
        }

        var indexOffset = (ulong)w.Position;
        for (var i = 0u; i < chunkCount; i++)
        {
            // v8 compact entry: u32 packedKey ((chunkX << 16) | chunkY) + u32 recordLength.
            // Offset is omitted; entries are in record write order so the reader derives it.
            var key = indexEntries[i].key;
            var packedKey = ((uint)(key >> 32) << 16) | (uint)(key & 0xFFFF);
            w.Write(packedKey);
            w.Write(indexEntries[i].length);
        }

        // Patch IndexOffset on the writer's current backing buffer (BufferWriter may
        // have grown during chunk writes; the original `buffer` ref is stale after grow).
        var liveBuffer = w.Buffer;
        BinaryPrimitives.WriteUInt64LittleEndian(liveBuffer.AsSpan(IndexOffsetFieldPosition, 8), indexOffset);

        var totalBytes = (int)w.Position;
        File.WriteAllBytes(path, liveBuffer.AsSpan(0, totalBytes).ToArray());
    }

    /// <summary>
    /// Opens a .swb file and reads only its header + chunk-offset index. Returns null on
    /// missing file, magic / version mismatch, or Fingerprint mismatch (a stale bake
    /// against a freshly patched client). Callers own disposal of the returned reader.
    /// </summary>
    public static LazyReader OpenForLazy(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        FileStream stream = null;
        try
        {
            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete
            );

            Span<byte> headerBuf = stackalloc byte[HeaderSize];
            if (stream.Read(headerBuf) != HeaderSize)
            {
                stream.Dispose();
                return null;
            }

            var magic = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf);
            if (magic != Magic)
            {
                stream.Dispose();
                return null;
            }
            var version = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[4..]);
            if (version < MinSupportedVersion || version > FormatVersion)
            {
                // Below the minimum supported version: treat as missing. Older files
                // get silently overwritten on the next SaveToFile / BakeMap.
                stream.Dispose();
                return null;
            }

            var mapId = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[8..]);
            var fingerprint = BinaryPrimitives.ReadUInt64LittleEndian(headerBuf[12..]);
            var bakeTimestamp = BinaryPrimitives.ReadUInt64LittleEndian(headerBuf[20..]);
            var chunkCount = BinaryPrimitives.ReadUInt32LittleEndian(headerBuf[28..]);
            var indexOffset = BinaryPrimitives.ReadUInt64LittleEndian(headerBuf[32..]);

            if (fingerprint != ComputeFingerprint((int)mapId))
            {
                stream.Dispose();
                return null;
            }

            // Read the chunk-offset index in one shot.
            var indexBytes = (int)chunkCount * IndexEntryBytes;
            var indexBuf = new byte[indexBytes];
            stream.Position = (long)indexOffset;
            if (stream.Read(indexBuf, 0, indexBytes) != indexBytes)
            {
                stream.Dispose();
                return null;
            }

            // v8 compact index: { u32 packedKey, u32 length } per chunk, in record write order.
            // The file offset is not stored — reconstruct it by cumulative record length starting
            // at the first record (immediately after the header).
            var offsets = new Dictionary<ulong, (ulong offset, uint length)>((int)chunkCount);
            var runningOffset = (ulong)HeaderSize;
            for (var i = 0; i < chunkCount; i++)
            {
                var entry = indexBuf.AsSpan(i * IndexEntryBytes);
                var packedKey = BinaryPrimitives.ReadUInt32LittleEndian(entry);
                var len = BinaryPrimitives.ReadUInt32LittleEndian(entry[4..]);
                var key = PackChunkKey((int)(packedKey >> 16), (int)(packedKey & 0xFFFF));
                offsets[key] = (runningOffset, len);
                runningOffset += len;
            }

            return new LazyReader(stream, mapId, fingerprint, bakeTimestamp, chunkCount, offsets);
        }
        catch
        {
            stream?.Dispose();
            return null;
        }
    }

    private static ulong PackChunkKey(int chunkX, int chunkY) => ((ulong)(uint)chunkX << 32) | (uint)chunkY;

    /// <summary>
    /// Predicted directional-Z for one cell/direction: the cell's own SourceZ when the
    /// direction is walkable/wet (mask bit set), else 0 — matching the baker, which leaves
    /// non-walkable directional slots at their zero-initialized default
    /// (StepProbe.ComputeMaskAt clears walkZs/swimZs and writes only on a successful step).
    /// </summary>
    internal static sbyte Predict(byte dirMaskByte, int bit, sbyte sourceZ) =>
        (dirMaskByte >> bit & 1) != 0 ? sourceZ : (sbyte)0;

    /// <summary>
    /// Residual of an absolute directional-Z against its prediction. Unchecked two's-complement
    /// so the transform is byte-exact for ALL sbyte inputs (no value-range constraint).
    /// </summary>
    internal static sbyte EncodeResidual(sbyte z, sbyte predict) => unchecked((sbyte)(z - predict));

    /// <summary>Inverse of <see cref="EncodeResidual"/>: absolute directional-Z = predict + residual.</summary>
    internal static sbyte DecodeZ(sbyte predict, sbyte residual) => unchecked((sbyte)(predict + residual));

    /// <summary>
    /// The base directional-Z array for direction index d in canonical order: walk N..NW (0-7),
    /// then swim N..NW (8-15). Index d uses WalkMask (d &lt; 8) or WetMask (d &gt;= 8) with
    /// direction bit (d &amp; 7).
    /// </summary>
    private static sbyte[] GetBaseZArray(StepChunk c, int d) => d switch
    {
        0  => c.WalkZN,  1  => c.WalkZNE, 2  => c.WalkZE,  3  => c.WalkZSE,
        4  => c.WalkZS,  5  => c.WalkZSW, 6  => c.WalkZW,  7  => c.WalkZNW,
        8  => c.SwimZN,  9  => c.SwimZNE, 10 => c.SwimZE,  11 => c.SwimZSE,
        12 => c.SwimZS,  13 => c.SwimZSW, 14 => c.SwimZW,  15 => c.SwimZNW,
        _  => throw new ArgumentOutOfRangeException(nameof(d))
    };

    /// <summary>
    /// Builds the uncompressed v6 record for one chunk into <paramref name="w"/>, libdeflate-
    /// compresses it, and writes it framed as [u32 uncompressedLen][payload]. The payload is the
    /// compressed bytes, or — when compression does not shrink the record (tiny Uniform records) —
    /// the raw record itself; the reader distinguishes the two by payload length vs uncompressedLen.
    /// </summary>
    private static void WriteChunk(
        BufferWriter w, int chunkX, int chunkY, StepChunk chunk,
        LibDeflateBinding packer, ref byte[] recordScratch, ref byte[] compScratch
    )
    {
        var rw = new BufferWriter(recordScratch, prefixStr: false);
        BuildRecord(rw, chunkX, chunkY, chunk);
        recordScratch = rw.Buffer; // may have grown; keep the larger buffer for reuse
        var recordLen = (int)rw.Position;

        var bound = packer.MaxPackSize(recordLen);
        if (compScratch.Length < bound)
        {
            compScratch = new byte[bound];
        }

        var compLen = packer.Pack(compScratch, recordScratch.AsSpan(0, recordLen));

        w.Write((uint)recordLen);
        if (compLen > 0 && compLen < recordLen)
        {
            w.Write(compScratch.AsSpan(0, compLen));
        }
        else
        {
            // Incompressible (or expanded): store the record raw. The reader detects this when
            // the on-disk payload length equals the uncompressed length.
            w.Write(recordScratch.AsSpan(0, recordLen));
        }
    }

    private static void BuildRecord(BufferWriter w, int chunkX, int chunkY, StepChunk chunk)
    {
        w.Write((ushort)chunkX);
        w.Write((ushort)chunkY);
        w.Write((uint)chunk.BuiltMultisVersion);

        // Kind: 0 = Full, 2 = Uniform. A uniform chunk (no strata, no swim layer, all 19 base
        // arrays constant) stores one cell's worth of data (~28-byte record total).
        if (chunk.IsUniform())
        {
            w.Write(KindUniform);
            w.Write(chunk.WalkMask[0]);
            w.Write(chunk.WetMask[0]);
            w.Write((byte)chunk.SourceZ[0]);
            w.Write((byte)chunk.WalkZN[0]);
            w.Write((byte)chunk.WalkZNE[0]);
            w.Write((byte)chunk.WalkZE[0]);
            w.Write((byte)chunk.WalkZSE[0]);
            w.Write((byte)chunk.WalkZS[0]);
            w.Write((byte)chunk.WalkZSW[0]);
            w.Write((byte)chunk.WalkZW[0]);
            w.Write((byte)chunk.WalkZNW[0]);
            w.Write((byte)chunk.SwimZN[0]);
            w.Write((byte)chunk.SwimZNE[0]);
            w.Write((byte)chunk.SwimZE[0]);
            w.Write((byte)chunk.SwimZSE[0]);
            w.Write((byte)chunk.SwimZS[0]);
            w.Write((byte)chunk.SwimZSW[0]);
            w.Write((byte)chunk.SwimZW[0]);
            w.Write((byte)chunk.SwimZNW[0]);
            return;
        }

        w.Write(KindFull); // Full

        var strataOffsetByCell = chunk.GetStrataOffsetByCellForSerialization();
        var strataData = chunk.GetStrataDataForSerialization();
        var hasStrata = strataOffsetByCell != null;
        var hasSwimLayer = chunk.HasSwimLayer;
        w.Write((byte)(hasStrata ? 1 : 0));
        w.Write((byte)(hasSwimLayer ? 1 : 0));

        // Predictive-Z: each base directional Z array is stored as a masked residual against
        // SourceZ. Bit d of ZArrayMask is set only when array d differs from its prediction
        // somewhere; cleared arrays are omitted and rebuilt from mask+SourceZ at read.
        ushort zArrayMask = 0;
        for (var d = 0; d < 16; d++)
        {
            var z = GetBaseZArray(chunk, d);
            var dirMask = d < 8 ? chunk.WalkMask : chunk.WetMask;
            var bit = d & 7;
            for (var cell = 0; cell < StepChunk.CellsPerChunk; cell++)
            {
                if (z[cell] != Predict(dirMask[cell], bit, chunk.SourceZ[cell]))
                {
                    zArrayMask |= (ushort)(1 << d);
                    break;
                }
            }
        }
        w.Write(zArrayMask);

        w.Write(chunk.WalkMask);
        w.Write(chunk.WetMask);
        WriteSBytes(w, chunk.SourceZ);

        Span<sbyte> residual = stackalloc sbyte[StepChunk.CellsPerChunk];
        for (var d = 0; d < 16; d++)
        {
            if ((zArrayMask >> d & 1) == 0)
            {
                continue;
            }
            var z = GetBaseZArray(chunk, d);
            var dirMask = d < 8 ? chunk.WalkMask : chunk.WetMask;
            var bit = d & 7;
            for (var cell = 0; cell < StepChunk.CellsPerChunk; cell++)
            {
                residual[cell] = EncodeResidual(z[cell], Predict(dirMask[cell], bit, chunk.SourceZ[cell]));
            }
            w.Write(MemoryMarshal.Cast<sbyte, byte>(residual));
        }

        if (hasSwimLayer)
        {
            WriteSBytes(w, chunk.SwimSourceZ);
            w.Write(chunk.SwimMask);
            WriteSBytes(w, chunk.SwimZN_Layer);
            WriteSBytes(w, chunk.SwimZNE_Layer);
            WriteSBytes(w, chunk.SwimZE_Layer);
            WriteSBytes(w, chunk.SwimZSE_Layer);
            WriteSBytes(w, chunk.SwimZS_Layer);
            WriteSBytes(w, chunk.SwimZSW_Layer);
            WriteSBytes(w, chunk.SwimZW_Layer);
            WriteSBytes(w, chunk.SwimZNW_Layer);
        }

        if (hasStrata)
        {
            // 256 × u16 offsets, then u32 length-prefixed strata byte array.
            for (var i = 0; i < StepChunk.CellsPerChunk; i++)
            {
                w.Write(strataOffsetByCell[i]);
            }
            var dataLen = (uint)(strataData?.Length ?? 0);
            w.Write(dataLen);
            if (dataLen > 0)
            {
                w.Write(strataData);
            }
        }
    }

    private static StepChunk ReadChunk(byte[] buffer)
    {
        var r = new BufferReader(buffer);
        // Skip ChunkX + ChunkY (already known via the index lookup).
        r.ReadUShort();
        r.ReadUShort();
        var multisVersion = (int)r.ReadUInt();
        var kind = r.ReadByte();

        var chunk = new StepChunk { BuiltMultisVersion = multisVersion };

        if (kind == KindUniform) // Uniform — one cell's worth of the 19 base arrays, fill all 256 cells.
        {
            Array.Fill(chunk.WalkMask, r.ReadByte());
            Array.Fill(chunk.WetMask, r.ReadByte());
            Array.Fill(chunk.SourceZ, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZN, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZNE, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZE, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZSE, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZS, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZSW, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZW, (sbyte)r.ReadByte());
            Array.Fill(chunk.WalkZNW, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZN, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZNE, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZE, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZSE, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZS, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZSW, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZW, (sbyte)r.ReadByte());
            Array.Fill(chunk.SwimZNW, (sbyte)r.ReadByte());
            return chunk;
        }

        var hasStrata = r.ReadByte() != 0;
        var hasSwimLayer = r.ReadByte() != 0;
        var zArrayMask = r.ReadUShort();

        r.Read(chunk.WalkMask);
        r.Read(chunk.WetMask);
        ReadSBytes(r, chunk.SourceZ);

        // Predictive-Z reconstruction: present arrays carry residuals (z = predict + residual);
        // absent arrays are synthesized from mask+SourceZ (z = predict, residual implicitly 0).
        Span<sbyte> residual = stackalloc sbyte[StepChunk.CellsPerChunk];
        for (var d = 0; d < 16; d++)
        {
            var z = GetBaseZArray(chunk, d);
            var dirMask = d < 8 ? chunk.WalkMask : chunk.WetMask;
            var bit = d & 7;
            if ((zArrayMask >> d & 1) != 0)
            {
                r.Read(MemoryMarshal.Cast<sbyte, byte>(residual));
                for (var cell = 0; cell < StepChunk.CellsPerChunk; cell++)
                {
                    z[cell] = DecodeZ(Predict(dirMask[cell], bit, chunk.SourceZ[cell]), residual[cell]);
                }
            }
            else
            {
                for (var cell = 0; cell < StepChunk.CellsPerChunk; cell++)
                {
                    z[cell] = Predict(dirMask[cell], bit, chunk.SourceZ[cell]);
                }
            }
        }

        if (hasSwimLayer)
        {
            chunk.AllocateSwimLayer();
            ReadSBytes(r, chunk.SwimSourceZ);
            r.Read(chunk.SwimMask);
            ReadSBytes(r, chunk.SwimZN_Layer);
            ReadSBytes(r, chunk.SwimZNE_Layer);
            ReadSBytes(r, chunk.SwimZE_Layer);
            ReadSBytes(r, chunk.SwimZSE_Layer);
            ReadSBytes(r, chunk.SwimZS_Layer);
            ReadSBytes(r, chunk.SwimZSW_Layer);
            ReadSBytes(r, chunk.SwimZW_Layer);
            ReadSBytes(r, chunk.SwimZNW_Layer);
        }

        if (hasStrata)
        {
            var offsets = new ushort[StepChunk.CellsPerChunk];
            for (var i = 0; i < offsets.Length; i++)
            {
                offsets[i] = r.ReadUShort();
            }
            var dataLen = (int)r.ReadUInt();
            var data = new byte[dataLen];
            if (dataLen > 0)
            {
                r.Read(data);
            }
            chunk.SetStrata(offsets, data);
        }

        return chunk;
    }

    private static void WriteSBytes(BufferWriter w, sbyte[] arr) =>
        w.Write(MemoryMarshal.Cast<sbyte, byte>(arr.AsSpan()));

    private static void ReadSBytes(BufferReader r, sbyte[] arr) =>
        r.Read(MemoryMarshal.Cast<sbyte, byte>(arr.AsSpan()));

    /// <summary>
    /// Open handle on a .swb file. Holds the FileStream + chunk-offset index. Chunks are
    /// fetched on demand via <see cref="TryReadChunk"/>; only the records actually queried
    /// are ever materialized. Dispose releases the underlying stream.
    /// </summary>
    internal sealed class LazyReader : IDisposable
    {
        private FileStream _stream;
        private readonly Dictionary<ulong, (ulong offset, uint length)> _offsets;
        private byte[] _buffer;      // raw on-disk record: [u32 uncompressedLen][payload]
        private byte[] _bodyBuffer;  // decompressed v6 record, parsed by ReadChunk

        public uint MapId { get; }
        public ulong Fingerprint { get; }
        public ulong BakeTimestamp { get; }
        public uint ChunkCount { get; }
        public int IndexedChunkCount => _offsets.Count;

        public bool Has(int chunkX, int chunkY) => _offsets.ContainsKey(PackChunkKey(chunkX, chunkY));

        /// <summary>
        /// Enumerates every (chunkX, chunkY) coordinate the file holds. Used by
        /// <see cref="StepCache"/> when preload is enabled to materialize all chunks
        /// upfront instead of on first query.
        /// </summary>
        public IEnumerable<(int chunkX, int chunkY)> EnumerateChunkCoords()
        {
            foreach (var key in _offsets.Keys)
            {
                yield return ((int)(key >> 32), (int)(key & 0xFFFFFFFF));
            }
        }

        internal LazyReader(
            FileStream stream, uint mapId, ulong fingerprint, ulong bakeTimestamp,
            uint chunkCount, Dictionary<ulong, (ulong offset, uint length)> offsets
        )
        {
            _stream = stream;
            MapId = mapId;
            Fingerprint = fingerprint;
            BakeTimestamp = bakeTimestamp;
            ChunkCount = chunkCount;
            _offsets = offsets;
            _buffer = new byte[BytesPerChunkBase];
            _bodyBuffer = new byte[BytesPerChunkBase];
        }

        /// <summary>
        /// Returns the chunk record at (<paramref name="chunkX"/>, <paramref name="chunkY"/>)
        /// from the file, or null if the file doesn't contain it. Single seek + bulk read,
        /// sized exactly to the chunk's recorded length (which varies with strata size).
        /// </summary>
        public StepChunk TryReadChunk(int chunkX, int chunkY)
        {
            if (_stream == null)
            {
                return null;
            }

            var key = PackChunkKey(chunkX, chunkY);
            if (!_offsets.TryGetValue(key, out var entry))
            {
                return null;
            }

            // Grow the on-disk scratch buffer if this chunk's record is larger than what we have.
            if (entry.length > _buffer.Length)
            {
                _buffer = new byte[entry.length];
            }

            _stream.Position = (long)entry.offset;
            var read = _stream.Read(_buffer, 0, (int)entry.length);
            if (read < (int)entry.length || entry.length < sizeof(uint))
            {
                return null;
            }

            // Frame: [u32 uncompressedLen][payload]. payload is libdeflate-compressed, unless its
            // length equals uncompressedLen, in which case it was stored raw (incompressible).
            var uncompressedLen = (int)BinaryPrimitives.ReadUInt32LittleEndian(_buffer);
            var payloadLen = (int)entry.length - sizeof(uint);
            if (_bodyBuffer.Length < uncompressedLen)
            {
                _bodyBuffer = new byte[uncompressedLen];
            }

            if (payloadLen == uncompressedLen)
            {
                Array.Copy(_buffer, sizeof(uint), _bodyBuffer, 0, uncompressedLen);
            }
            else
            {
                // Decompression is level-independent, so reuse the shared per-thread binding.
                var result = Deflate.Standard.Unpack(
                    _bodyBuffer.AsSpan(0, uncompressedLen),
                    _buffer.AsSpan(sizeof(uint), payloadLen),
                    out var produced
                );
                if (result != LibDeflateResult.Success || produced != uncompressedLen)
                {
                    return null;
                }
            }

            return ReadChunk(_bodyBuffer);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
            _buffer = null;
            _bodyBuffer = null;
        }
    }
}
