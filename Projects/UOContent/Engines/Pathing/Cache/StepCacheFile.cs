using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Binary serializer + lazy reader for the step cache. Persists chunk records to disk
/// so a server warm-starts without paying chunk-build cost on the first pathfind through
/// a region. Lazy: opening a file reads only the header + chunk-offset index (~few KB
/// for tens of thousands of chunks), then individual chunks are seeked + deserialized
/// only when the cache asks for them. RAM stays bounded by MaxResidentChunks regardless
/// of file size.
///
/// File layout v2 (little-endian, BufferWriter / BufferReader convention):
///
///   Header (48 bytes):
///     u32   Magic           = 0x42575300 ('SWB\0')
///     u32   Version         = current FormatVersion (2)
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
///     u16    ChunkX
///     u16    ChunkY
///     u32    BuiltMultisVersion
///     u8     HasStrata       0 = single-Z chunk (no strata trailer); 1 = strata trailer follows
///     byte   WalkMask[256]
///     byte   WetMask[256]
///     sbyte  SourceZ[256]
///     sbyte  WalkZN[256]..WalkZNW[256]   (8 arrays in N,NE,E,SE,S,SW,W,NW order)
///     sbyte  SwimZN[256]..SwimZNW[256]   (8 arrays in same order)
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
///   Index trailer (20 × ChunkCount bytes):
///     For each chunk: { u64 chunkKey, u64 fileOffset, u32 recordLength }
///
/// Per-chunk fixed portion: ~5,393 bytes. Strata trailer: 516 + N × ~30 bytes for a
/// chunk with N multi-Z cells averaging ~2 strata each. LRU bookkeeping
/// (LastTouchedTicks) is intentionally not persisted.
///
/// Files with version &lt; <see cref="MinSupportedVersion"/> are silently rejected
/// at open time (treated as missing) and overwritten on the next save.
/// </summary>
internal static class StepCacheFile
{
    public const uint Magic = 0x42575300; // 'SWB\0'
    public const uint FormatVersion = 2;

    /// <summary>
    /// Lowest format version this binary can load. Files below this version are treated as
    /// missing (silently rejected) — a subsequent SaveToFile / BakeMap overwrites them with
    /// the current FormatVersion. Bumped to 2 when Tier 4 multi-Z strata landed; v1 had no
    /// strata data and is incompatible with the strata-aware lookup path.
    /// </summary>
    public const uint MinSupportedVersion = 2;

    private const int HeaderSize =
        sizeof(uint)    // Magic
        + sizeof(uint)  // Version
        + sizeof(uint)  // MapId
        + sizeof(ulong) // Fingerprint
        + sizeof(ulong) // BakeTimestamp
        + sizeof(uint)  // ChunkCount
        + sizeof(ulong); // IndexOffset

    // Index entry: chunkKey + fileOffset + recordLength. Bumped to include length when
    // strata made chunk records variable-size; the lazy reader uses length to do a
    // single bulk read per chunk without consulting the next offset.
    private const int IndexEntryBytes = sizeof(ulong) + sizeof(ulong) + sizeof(uint);

    /// <summary>Fixed-size portion of a chunk record (everything except the optional strata trailer).</summary>
    private const int BytesPerChunkBase =
        sizeof(ushort) + sizeof(ushort) + sizeof(uint) + sizeof(byte)
        + StepChunk.CellsPerChunk           // WalkMask
        + StepChunk.CellsPerChunk           // WetMask
        + StepChunk.CellsPerChunk           // SourceZ
        + 8 * StepChunk.CellsPerChunk       // WalkZ[8]
        + 8 * StepChunk.CellsPerChunk;      // SwimZ[8]

    /// <summary>Strata trailer overhead when present: 256×u16 offset table + u32 data length.</summary>
    private const int StrataTrailerOverhead = StepChunk.CellsPerChunk * sizeof(ushort) + sizeof(uint);

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
    /// Combined XxHash3 fingerprint over (1) the loaded TileData flag tables and (2) the
    /// per-map .mul / .uop file contents (via <see cref="TileMatrix.MapFilesFingerprint"/>).
    /// Bake files carry this hash so a load can refuse to populate the cache when EITHER
    /// tile flags shifted (client patch) OR the map data was rewritten (CentredSharp /
    /// UOFiddler edit). The .mul format has no built-in CRC; this is the only way to
    /// detect those mutations.
    /// </summary>
    public static ulong ComputeFingerprint(int mapId)
    {
        var hasher = HashUtility.CreateXxHash3();

        // TileData flag tables — same projection trick as before: just the Flags ulong
        // from each entry, written little-endian into a contiguous byte buffer. The
        // struct itself has a string Name (reference) whose object identity isn't
        // stable across runs, so MemoryMarshal.Cast over the whole struct would drift.
        var landTable = TileData.LandTable;
        var itemTable = TileData.ItemTable;
        var bytes = new byte[(landTable.Length + itemTable.Length) * sizeof(ulong)];
        var span = bytes.AsSpan();

        for (var i = 0; i < landTable.Length; i++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(span[(i * 8)..], (ulong)landTable[i].Flags);
        }
        var itemOffset = landTable.Length * 8;
        for (var i = 0; i < itemTable.Length; i++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(span[(itemOffset + i * 8)..], (ulong)itemTable[i].Flags);
        }
        hasher.Append(bytes);

        // Map files (mapX.mul / .uop, staidxX.mul, staticsX.mul). TileMatrix already
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

    /// <summary>
    /// Writes the file: header (with placeholder IndexOffset) → chunks (offsets recorded)
    /// → index trailer → patches the header IndexOffset. <paramref name="chunkCount"/> must
    /// equal the actual number of chunks <paramref name="next"/> will yield.
    /// </summary>
    public static void Write(string path, uint mapId, uint chunkCount, ChunkEnumerator next)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        // Initial estimate: base record + a modest strata budget per chunk. BufferWriter
        // grows on overflow, so under-estimating just causes a few realloc/copy cycles
        // during the bake — not a correctness issue.
        var capacity = HeaderSize
                       + (BytesPerChunkBase + 256) * (int)chunkCount
                       + IndexEntryBytes * (int)chunkCount;
        var buffer = new byte[capacity];
        var w = new BufferWriter(buffer, prefixStr: false);

        w.Write(Magic);
        w.Write(FormatVersion);
        w.Write(mapId);
        w.Write(ComputeFingerprint((int)mapId));
        w.Write((ulong)DateTime.UtcNow.Ticks);
        w.Write(chunkCount);
        w.Write(0UL); // IndexOffset placeholder, patched after chunks

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
            WriteChunk(w, chunkX, chunkY, chunk);
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
            w.Write(indexEntries[i].key);
            w.Write(indexEntries[i].offset);
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

            var offsets = new Dictionary<ulong, (ulong offset, uint length)>((int)chunkCount);
            for (var i = 0; i < chunkCount; i++)
            {
                var entry = indexBuf.AsSpan(i * IndexEntryBytes);
                var key = BinaryPrimitives.ReadUInt64LittleEndian(entry);
                var off = BinaryPrimitives.ReadUInt64LittleEndian(entry[8..]);
                var len = BinaryPrimitives.ReadUInt32LittleEndian(entry[16..]);
                offsets[key] = (off, len);
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

    private static void WriteChunk(BufferWriter w, int chunkX, int chunkY, StepChunk chunk)
    {
        w.Write((ushort)chunkX);
        w.Write((ushort)chunkY);
        w.Write((uint)chunk.BuiltMultisVersion);

        var strataOffsetByCell = chunk.GetStrataOffsetByCellForSerialization();
        var strataData = chunk.GetStrataDataForSerialization();
        var hasStrata = strataOffsetByCell != null;
        w.Write((byte)(hasStrata ? 1 : 0));

        w.Write(chunk.WalkMask);
        w.Write(chunk.WetMask);
        WriteSBytes(w, chunk.SourceZ);

        WriteSBytes(w, chunk.WalkZN);
        WriteSBytes(w, chunk.WalkZNE);
        WriteSBytes(w, chunk.WalkZE);
        WriteSBytes(w, chunk.WalkZSE);
        WriteSBytes(w, chunk.WalkZS);
        WriteSBytes(w, chunk.WalkZSW);
        WriteSBytes(w, chunk.WalkZW);
        WriteSBytes(w, chunk.WalkZNW);

        WriteSBytes(w, chunk.SwimZN);
        WriteSBytes(w, chunk.SwimZNE);
        WriteSBytes(w, chunk.SwimZE);
        WriteSBytes(w, chunk.SwimZSE);
        WriteSBytes(w, chunk.SwimZS);
        WriteSBytes(w, chunk.SwimZSW);
        WriteSBytes(w, chunk.SwimZW);
        WriteSBytes(w, chunk.SwimZNW);

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
        var hasStrata = r.ReadByte() != 0;

        var chunk = new StepChunk { BuiltMultisVersion = multisVersion };

        r.Read(chunk.WalkMask);
        r.Read(chunk.WetMask);
        ReadSBytes(r, chunk.SourceZ);

        ReadSBytes(r, chunk.WalkZN);
        ReadSBytes(r, chunk.WalkZNE);
        ReadSBytes(r, chunk.WalkZE);
        ReadSBytes(r, chunk.WalkZSE);
        ReadSBytes(r, chunk.WalkZS);
        ReadSBytes(r, chunk.WalkZSW);
        ReadSBytes(r, chunk.WalkZW);
        ReadSBytes(r, chunk.WalkZNW);

        ReadSBytes(r, chunk.SwimZN);
        ReadSBytes(r, chunk.SwimZNE);
        ReadSBytes(r, chunk.SwimZE);
        ReadSBytes(r, chunk.SwimZSE);
        ReadSBytes(r, chunk.SwimZS);
        ReadSBytes(r, chunk.SwimZSW);
        ReadSBytes(r, chunk.SwimZW);
        ReadSBytes(r, chunk.SwimZNW);

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
        private byte[] _buffer;

        public uint MapId { get; }
        public ulong Fingerprint { get; }
        public ulong BakeTimestamp { get; }
        public uint ChunkCount { get; }
        public int IndexedChunkCount => _offsets.Count;

        public bool Has(int chunkX, int chunkY) => _offsets.ContainsKey(PackChunkKey(chunkX, chunkY));

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

            // Grow the scratch buffer if this chunk's record is larger than what we have.
            // Common case: chunks fit in BytesPerChunkBase; only multi-Z-heavy chunks grow.
            if (entry.length > _buffer.Length)
            {
                _buffer = new byte[entry.length];
            }

            _stream.Position = (long)entry.offset;
            var read = _stream.Read(_buffer, 0, (int)entry.length);
            return read < (int)entry.length ? null : ReadChunk(_buffer);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
            _buffer = null;
        }
    }
}
