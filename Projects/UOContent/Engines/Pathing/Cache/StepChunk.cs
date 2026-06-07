using System;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Per-chunk storage backing StepCache. Holds raw walk + swim masks and destination Z
/// values for each of 256 cells in a 16x16 chunk, plus build-time metadata (multis version, multi-Z strata)
/// and LRU bookkeeping.
/// </summary>
internal sealed class StepChunk
{
    public const int CellsPerChunk = 256; // 16 x 16

    /// <summary>Bit i of WalkMask[c] = "default walker can step from cell c to neighbor (Direction)i".
    /// Raw — no diagonal corner-cut applied here.</summary>
    public readonly byte[] WalkMask = new byte[CellsPerChunk];

    /// <summary>Bit i of WetMask[c] = "swim-only mob can step from cell c to neighbor (Direction)i".
    /// Layered with WalkMask via canSwim/cantWalk capability flags.</summary>
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

    /// <summary>
    /// Swim layer — populated only for chunks containing at least one shore cell (a cell
    /// with both a walkable land surface and a water surface separated by > StepHeight).
    /// On shore cells, queries from the swim source Z miss the primary source-Z guard;
    /// the swim layer carries the correct wetMask + per-direction destination Zs computed
    /// from the water surface's perspective. For non-shore cells in a chunk that has the
    /// layer, <see cref="SwimSourceZ"/>[cell] = <see cref="NoSwimLayerCell"/> sentinel.
    /// All swim-layer arrays are null on chunks with no shore cells (~90% of map chunks
    /// on Trammel) — zero memory cost on the common case.
    /// </summary>
    public const sbyte NoSwimLayerCell = sbyte.MinValue;

    private sbyte[] _swimSourceZ;
    private byte[]  _swimMask;
    private sbyte[] _swimZN_extra;
    private sbyte[] _swimZNE_extra;
    private sbyte[] _swimZE_extra;
    private sbyte[] _swimZSE_extra;
    private sbyte[] _swimZS_extra;
    private sbyte[] _swimZSW_extra;
    private sbyte[] _swimZW_extra;
    private sbyte[] _swimZNW_extra;

    /// <summary>True when this chunk has at least one shore cell with a populated swim layer.</summary>
    public bool HasSwimLayer => _swimSourceZ != null;

    /// <summary>Per-cell water-surface standing Z (or <see cref="NoSwimLayerCell"/>). Null when chunk has no swim layer.</summary>
    public sbyte[] SwimSourceZ => _swimSourceZ;
    /// <summary>Per-cell swim mask computed at <see cref="SwimSourceZ"/>. Null when chunk has no swim layer.</summary>
    public byte[]  SwimMask    => _swimMask;
    public sbyte[] SwimZN_Layer  => _swimZN_extra;
    public sbyte[] SwimZNE_Layer => _swimZNE_extra;
    public sbyte[] SwimZE_Layer  => _swimZE_extra;
    public sbyte[] SwimZSE_Layer => _swimZSE_extra;
    public sbyte[] SwimZS_Layer  => _swimZS_extra;
    public sbyte[] SwimZSW_Layer => _swimZSW_extra;
    public sbyte[] SwimZW_Layer  => _swimZW_extra;
    public sbyte[] SwimZNW_Layer => _swimZNW_extra;

    /// <summary>
    /// Lazily allocates the swim-layer arrays and seeds <see cref="SwimSourceZ"/> with
    /// the <see cref="NoSwimLayerCell"/> sentinel. Called at bake time the first time a
    /// shore cell is detected in this chunk.
    /// </summary>
    internal void AllocateSwimLayer()
    {
        if (_swimSourceZ != null)
        {
            return;
        }
        _swimSourceZ = new sbyte[CellsPerChunk];
        _swimMask    = new byte[CellsPerChunk];
        _swimZN_extra  = new sbyte[CellsPerChunk];
        _swimZNE_extra = new sbyte[CellsPerChunk];
        _swimZE_extra  = new sbyte[CellsPerChunk];
        _swimZSE_extra = new sbyte[CellsPerChunk];
        _swimZS_extra  = new sbyte[CellsPerChunk];
        _swimZSW_extra = new sbyte[CellsPerChunk];
        _swimZW_extra  = new sbyte[CellsPerChunk];
        _swimZNW_extra = new sbyte[CellsPerChunk];
        for (var i = 0; i < CellsPerChunk; i++)
        {
            _swimSourceZ[i] = NoSwimLayerCell;
        }
    }

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

    public ushort GetStrataOffset(int cellIndex) => _strataOffsetByCell == null ? NoStrata : _strataOffsetByCell[cellIndex];

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

    /// <summary>
    /// True when every cell shares one value across WalkMask, WetMask, SourceZ, and all 16
    /// directional-Z arrays, and the chunk has neither multi-Z strata nor a swim layer. Such a
    /// chunk serializes to a ~28-byte uniform record (StepCacheFile v5) instead of the full
    /// record. Chunks with a swim layer (shore cells) are never uniform — their per-cell swim
    /// data must be preserved via the Full record.
    /// </summary>
    internal bool IsUniform() => _strataOffsetByCell == null
                                 && !HasSwimLayer
                                 && AllSame(WalkMask) && AllSame(WetMask) && AllSame(SourceZ)
                                 && AllSame(WalkZN) && AllSame(WalkZNE) && AllSame(WalkZE) && AllSame(WalkZSE)
                                 && AllSame(WalkZS) && AllSame(WalkZSW) && AllSame(WalkZW) && AllSame(WalkZNW)
                                 && AllSame(SwimZN) && AllSame(SwimZNE) && AllSame(SwimZE) && AllSame(SwimZSE)
                                 && AllSame(SwimZS) && AllSame(SwimZSW) && AllSame(SwimZW) && AllSame(SwimZNW);

    // "All 256 cells equal" via SIMD-accelerated ContainsAnyExcept (skip cell 0, the reference).
    private static bool AllSame(byte[] a) => a.Length < 2 || !a.AsSpan(1).ContainsAnyExcept(a[0]);

    private static bool AllSame(sbyte[] a) => a.Length < 2 || !a.AsSpan(1).ContainsAnyExcept(a[0]);
}
