using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Server.Compression;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Binary serializer and reader for the step cache, so a shard can warm-start instead of building
/// chunks on the first pathfind through each region. Opening a file reads only the header and chunk
/// index; a chunk record is seeked and inflated when the cache actually asks for it, which keeps
/// resident memory bounded by MaxResidentChunks no matter how large the file is.
///
/// File layout (little-endian, BufferWriter / BufferReader convention):
///
///   Header (40 bytes):
///     u32   Magic           = 0x42575300 ('SWB\0')
///     u32   Version         = FormatVersion
///     u32   MapId
///     u64   Fingerprint     XxHash3 over tiledata.mul and the map's own .mul / .uop files.
///                           Detects both a client patch that shifts tile flags and a map edit
///                           that rewrites the terrain; see ComputeFingerprint. The .mul format
///                           carries no CRC of its own, so hashing is the only way to catch either.
///     u64   BakeTimestamp   DateTime.UtcNow.Ticks at write time. Informational.
///     u32   ChunkCount
///     u64   IndexOffset     Where the index trailer begins.
///
///   Per chunk (ChunkCount times, variable size):
///     u32    UncompressedLen   Size of the inflated record body below.
///     byte[] Payload           The record body, libdeflate-compressed — or stored raw when
///                              compression didn't shrink it, as happens with tiny Uniform
///                              records. The reader tells the two apart by comparing the payload
///                              length against UncompressedLen.
///
///   Record body (after inflate):
///     u16    ChunkX
///     u16    ChunkY
///     u32    BuiltMultisVersion  Reserved, always 0 — chunks are static-only.
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
/// A chunk's fixed portion runs ~783 bytes, and each directional-Z array that survives prediction
/// adds 256 more, so a Full record lands between ~783 bytes and ~4 KB. The strata trailer adds
/// 516 bytes plus roughly 30 per multi-Z cell. LastTouchedTicks is deliberately not persisted —
/// LRU state means nothing across a restart.
///
/// Files below <see cref="MinSupportedVersion"/> are treated as missing and overwritten on the
/// next save. The cache regenerates from the map data, so a format bump only costs a one-time
/// re-bake.
/// </summary>
internal static class StepCacheFile
{
    public const uint Magic = 0x42575300; // 'SWB\0'

    public const uint FormatVersion = 9;

    /// <summary>
    /// Oldest format this binary will load. Anything older is treated as missing rather than
    /// migrated: the cache is fully regenerable from the map data, so a re-bake is always
    /// available and always correct.
    /// </summary>
    public const uint MinSupportedVersion = 9;

    // Record discriminator. 1 is reserved.
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

    // One index entry: u32 packedKey ((chunkX << 16) | chunkY) + u32 recordLength. The file offset
    // isn't stored — entries sit in record write order, so the reader rebuilds each offset as a
    // running sum of the lengths before it, starting at HeaderSize.
    private const int IndexEntryBytes = sizeof(uint) + sizeof(uint);

    /// <summary>A chunk record minus its optional strata and swim trailers.</summary>
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
    /// Where the header's IndexOffset u64 sits. It's written as a placeholder and patched once the
    /// chunks are down and the index position is known.
    /// </summary>
    private const int IndexOffsetFieldPosition = 32;

    public delegate bool ChunkEnumerator(out int chunkX, out int chunkY, out StepChunk chunk);

    /// <summary>
    /// Reads just a .swb file's fingerprint — 20 bytes, no chunk data. False if the file is
    /// missing, isn't a .swb, or is a version this binary can't load.
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
    /// Hashes the inputs a bake depends on: <c>tiledata.mul</c> and the map's own .mul / .uop
    /// files. A file carrying a stale hash is refused at open time, which is what catches a client
    /// patch that shifts tile flags or a map editor that rewrites the terrain. Neither format has a
    /// CRC of its own, so hashing is the only signal available.
    ///
    /// This must hash the FILES, never the in-memory <see cref="TileData.LandTable"/> /
    /// <see cref="TileData.ItemTable"/>. The server patches those tables at runtime (ItemFixes,
    /// LOSBlocker, PotionKeg, CTF), so a hash of the live tables changes depending on when it is
    /// taken — useless as a fingerprint. Those server-side patches apply identically every boot and
    /// deliberately do NOT invalidate the cache; if you change one, run [PathCacheClear or bump
    /// <see cref="FormatVersion"/> yourself.
    /// </summary>
    public static ulong ComputeFingerprint(int mapId)
    {
        var hasher = HashUtility.CreateXxHash3();

        Span<byte> tileDataBytes = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(tileDataBytes, TileDataFileFingerprint());
        hasher.Append(tileDataBytes);

        // TileMatrix already streamed the map files through XxHash3 when it was built; reuse that
        // rather than re-reading them.
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
    /// XxHash3 of the raw <c>tiledata.mul</c> bytes, computed once — the file can't change while
    /// the server runs. Returns 0 when the file is absent, which only happens in stripped test
    /// hosts; a real server can't boot without it, and 0 is a fine deterministic stand-in.
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

        // A rough estimate: the base record plus a small strata budget per chunk. Coastline chunks
        // run ~2.5 KB over it for their swim layer, but they're a small share of any map, and the
        // writer grows on overflow — under-estimating costs a few reallocs during a bake, nothing more.
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

        // Each record is built into recordScratch, compressed into compScratch, then framed as
        // [u32 uncompressedLen][payload].
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
            var key = indexEntries[i].key;
            var packedKey = ((uint)(key >> 32) << 16) | (uint)(key & 0xFFFF);
            w.Write(packedKey);
            w.Write(indexEntries[i].length);
        }

        // Patch IndexOffset on the writer's CURRENT buffer: BufferWriter may have grown during the
        // chunk writes, which leaves the original `buffer` reference pointing at a stale array.
        var liveBuffer = w.Buffer;
        BinaryPrimitives.WriteUInt64LittleEndian(liveBuffer.AsSpan(IndexOffsetFieldPosition, 8), indexOffset);

        var totalBytes = (int)w.Position;
        File.WriteAllBytes(path, liveBuffer.AsSpan(0, totalBytes).ToArray());
    }

    /// <summary>
    /// Opens a .swb file, reading only its header and chunk index. Null if the file is missing,
    /// isn't a loadable .swb, or is a stale bake whose fingerprint no longer matches the live tile
    /// and map data. The caller owns the returned reader.
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

            // Pull the whole index in one read.
            var indexBytes = (int)chunkCount * IndexEntryBytes;
            var indexBuf = new byte[indexBytes];
            stream.Position = (long)indexOffset;
            if (stream.Read(indexBuf, 0, indexBytes) != indexBytes)
            {
                stream.Dispose();
                return null;
            }

            // Entries are in record write order and carry no offset, so rebuild each one as a
            // running sum of the record lengths, starting just past the header.
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
    /// Guesses a cell's destination Z for one direction: on flat ground a step lands at the Z you
    /// left from, so predict SourceZ where the direction is passable and 0 where it isn't. The
    /// zero matches the baker, which only writes a slot on a successful step and leaves the rest
    /// cleared. Most terrain is flat, so most predictions are exact and most residuals are 0 —
    /// which is what makes the residual arrays compress away to nothing.
    /// </summary>
    internal static sbyte Predict(byte dirMaskByte, int bit, sbyte sourceZ) =>
        (dirMaskByte >> bit & 1) != 0 ? sourceZ : (sbyte)0;

    /// <summary>
    /// A destination Z's difference from its prediction. Wraps deliberately: two's-complement
    /// round-trips exactly for every sbyte input, so no value range is off-limits.
    /// </summary>
    internal static sbyte EncodeResidual(sbyte z, sbyte predict) => unchecked((sbyte)(z - predict));

    /// <summary>Inverse of <see cref="EncodeResidual"/>.</summary>
    internal static sbyte DecodeZ(sbyte predict, sbyte residual) => unchecked((sbyte)(predict + residual));

    /// <summary>
    /// The destination-Z array for direction index d, in the canonical order the format stores them:
    /// walk N..NW as 0-7, then swim N..NW as 8-15.
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
    /// Builds one chunk's record, compresses it, and frames it as [u32 uncompressedLen][payload].
    /// When compression fails to shrink the record — as it does on the tiny Uniform ones — the raw
    /// record is stored instead, and the reader tells the two apart by payload length.
    /// </summary>
    private static void WriteChunk(
        BufferWriter w, int chunkX, int chunkY, StepChunk chunk,
        LibDeflateBinding packer, ref byte[] recordScratch, ref byte[] compScratch
    )
    {
        var rw = new BufferWriter(recordScratch, prefixStr: false);
        BuildRecord(rw, chunkX, chunkY, chunk);
        recordScratch = rw.Buffer; // may have grown; hold onto the larger buffer for the next chunk
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
            // Compression didn't help, so store the record raw. Payload length == uncompressedLen
            // is how the reader recognizes that.
            w.Write(recordScratch.AsSpan(0, recordLen));
        }
    }

    private static void BuildRecord(BufferWriter w, int chunkX, int chunkY, StepChunk chunk)
    {
        w.Write((ushort)chunkX);
        w.Write((ushort)chunkY);
        w.Write((uint)chunk.BuiltMultisVersion);

        // A uniform chunk — every cell identical — collapses to one cell's worth of data, ~28 bytes.
        // Open water and solid rock make up a lot of a map, so this is worth the branch.
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

        w.Write(KindFull);

        var strataOffsetByCell = chunk.GetStrataOffsetByCellForSerialization();
        var strataData = chunk.GetStrataDataForSerialization();
        var hasStrata = strataOffsetByCell != null;
        var hasSwimLayer = chunk.HasSwimLayer;
        w.Write((byte)(hasStrata ? 1 : 0));
        w.Write((byte)(hasSwimLayer ? 1 : 0));

        // Each destination-Z array is stored as residuals against its prediction (see Predict). An
        // array that matches its prediction everywhere — the common case on flat terrain — is
        // omitted entirely, and its ZArrayMask bit stays clear so the reader synthesizes it.
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
        // ChunkX + ChunkY — already known from the index lookup that got us here.
        r.ReadUShort();
        r.ReadUShort();
        var multisVersion = (int)r.ReadUInt();
        var kind = r.ReadByte();

        var chunk = new StepChunk { BuiltMultisVersion = multisVersion };

        if (kind == KindUniform) // one cell's values, broadcast to all 256
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

        // Inverse of the write path: a stored array carries residuals to add back to the
        // prediction, an omitted one IS the prediction.
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
    /// An open .swb file: the stream plus the chunk index. Only the records actually asked for are
    /// ever read or inflated. Dispose releases the stream.
    /// </summary>
    internal sealed class LazyReader : IDisposable
    {
        private FileStream _stream;
        private readonly Dictionary<ulong, (ulong offset, uint length)> _offsets;
        private byte[] _buffer;      // the raw framed record as it sits on disk
        private byte[] _bodyBuffer;  // that record, inflated, ready for ReadChunk

        public uint MapId { get; }
        public ulong Fingerprint { get; }
        public ulong BakeTimestamp { get; }
        public uint ChunkCount { get; }
        public int IndexedChunkCount => _offsets.Count;

        public bool Has(int chunkX, int chunkY) => _offsets.ContainsKey(PackChunkKey(chunkX, chunkY));

        /// <summary>Every (chunkX, chunkY) the file holds. Used to preload the whole file.</summary>
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
        /// Reads one chunk from the file, or null if the file has no record for it. One seek and
        /// one read, sized to the record's indexed length.
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

            // [u32 uncompressedLen][payload], where the payload is compressed unless its length
            // already equals uncompressedLen — then it was stored raw.
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
                // Deflate.Standard, not .Maximum: the level only affects packing, and inflate has
                // to accept whatever the writer produced regardless.
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
