using System;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Per-chunk storage backing StepCache. Holds raw walk + swim masks and destination Z
/// values for each of 256 cells in a 16x16 chunk, plus build-time metadata (multis
/// version, multi-Z strata) and LRU bookkeeping.
/// </summary>
internal sealed class StepChunk
{
    public const int CellsPerChunk = 256; // 16 x 16

    /// <summary>Bit i of WalkMask[c] = "default walker can step from cell c to neighbor (Direction)i". Raw — no diagonal corner-cut applied here.</summary>
    public readonly byte[] WalkMask = new byte[CellsPerChunk];

    /// <summary>Bit i of WetMask[c] = "swim-only mob can step from cell c to neighbor (Direction)i". Layered with WalkMask via canSwim/cantWalk capability flags.</summary>
    public readonly byte[] WetMask = new byte[CellsPerChunk];

    public readonly sbyte[] SourceZ = new sbyte[CellsPerChunk];

    public readonly sbyte[] WalkZN  = new sbyte[CellsPerChunk];
    public readonly sbyte[] WalkZNE = new sbyte[CellsPerChunk];
    public readonly sbyte[] WalkZE  = new sbyte[CellsPerChunk];
    public readonly sbyte[] WalkZSE = new sbyte[CellsPerChunk];
    public readonly sbyte[] WalkZS  = new sbyte[CellsPerChunk];
    public readonly sbyte[] WalkZSW = new sbyte[CellsPerChunk];
    public readonly sbyte[] WalkZW  = new sbyte[CellsPerChunk];
    public readonly sbyte[] WalkZNW = new sbyte[CellsPerChunk];

    public readonly sbyte[] SwimZN  = new sbyte[CellsPerChunk];
    public readonly sbyte[] SwimZNE = new sbyte[CellsPerChunk];
    public readonly sbyte[] SwimZE  = new sbyte[CellsPerChunk];
    public readonly sbyte[] SwimZSE = new sbyte[CellsPerChunk];
    public readonly sbyte[] SwimZS  = new sbyte[CellsPerChunk];
    public readonly sbyte[] SwimZSW = new sbyte[CellsPerChunk];
    public readonly sbyte[] SwimZW  = new sbyte[CellsPerChunk];
    public readonly sbyte[] SwimZNW = new sbyte[CellsPerChunk];

    /// <summary>Sentinel: cell has no strata — single-Z, use the main Walk/Wet arrays.</summary>
    public const ushort NoStrata = ushort.MaxValue;

    /// <summary>
    /// Length-256 offset table: <c>StrataOffsetByCell[cell] = byte offset</c> into
    /// <see cref="StrataData"/> where this cell's strata begin, or <see cref="NoStrata"/>
    /// for cells without multi-Z. Null when the chunk has zero multi-Z cells.
    /// </summary>
    private ushort[] _strataOffsetByCell;

    /// <summary>
    /// Packed per-cell strata. For each cell with strata:
    ///   u8 stratumCount, then stratumCount × Stratum (19 bytes each):
    ///     sbyte zCenter, byte walkMask, byte wetMask,
    ///     sbyte walkZ_N..NW (8), sbyte swimZ_N..NW (8)
    /// </summary>
    private byte[] _strataData;

    /// <summary>Snapshot of Sector.MultisVersion at the time this chunk was built.</summary>
    public int BuiltMultisVersion;

    /// <summary>Updated on every cache hit/miss. Used by LRU fallback eviction.</summary>
    public long LastTouchedTicks;

    /// <summary>Size in bytes of one Stratum record in StrataData.</summary>
    public const int StratumByteLength = 1 + 1 + 1 + 8 + 8;

    public bool IsCellMultiZ(int cellIndex) => GetStrataOffset(cellIndex) != NoStrata;

    public ushort GetStrataOffset(int cellIndex) =>
        _strataOffsetByCell == null ? NoStrata : _strataOffsetByCell[cellIndex];

    public ReadOnlySpan<byte> StrataData =>
        _strataData == null ? ReadOnlySpan<byte>.Empty : _strataData.AsSpan();

    /// <summary>
    /// Single-shot setter for the chunk's strata. Pass null/null to clear (chunk becomes
    /// "no multi-Z"). Otherwise <paramref name="offsetByCell"/> must be length 256 with
    /// <see cref="NoStrata"/> for cells without strata, and <paramref name="data"/> the
    /// packed strata records.
    /// </summary>
    internal void SetStrata(ushort[] offsetByCell, byte[] data)
    {
        _strataOffsetByCell = offsetByCell;
        _strataData = data;
    }

    /// <summary>Serialization hook: returns the raw offset array (or null if no strata).</summary>
    internal ushort[] GetStrataOffsetByCellForSerialization() => _strataOffsetByCell;

    /// <summary>Serialization hook: returns the raw data array (or null if no strata).</summary>
    internal byte[] GetStrataDataForSerialization() => _strataData;
}
