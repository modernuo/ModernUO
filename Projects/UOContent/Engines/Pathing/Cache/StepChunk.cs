using System;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Per-chunk storage backing <see cref="StepCache"/>: walk + swim masks and destination Zs for
/// each of the 256 cells in a 16x16 chunk, plus the optional multi-Z strata and swim layers and
/// the LRU timestamp.
/// </summary>
internal sealed class StepChunk
{
    public const int CellsPerChunk = 256; // 16 x 16

    /// <summary>Bit i of WalkMask[c]: a default walker can step from cell c to neighbour (Direction)i.
    /// Raw — no diagonal corner-cut applied, so callers must AND the partner bits themselves.</summary>
    public readonly byte[] WalkMask = new byte[CellsPerChunk];

    /// <summary>Bit i of WetMask[c]: a swim-only mob can step from cell c to neighbour (Direction)i.</summary>
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
    /// Marks a cell with no swim-layer entry, in a chunk that has the layer.
    ///
    /// The swim layer exists only on chunks holding at least one shore cell — a cell with both a
    /// walkable surface and a water surface more than StepHeight apart. A swim query there sits
    /// too far from the primary SourceZ to pass the source-Z guard, so the layer carries a second
    /// mask and destination-Z set computed from the water surface instead. Chunks with no shore
    /// cells leave every swim-layer array null.
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
    /// Allocates the swim-layer arrays and seeds <see cref="SwimSourceZ"/> with
    /// <see cref="NoSwimLayerCell"/>. Called on the first shore cell found in this chunk.
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

    /// <summary>
    /// Marks a single-Z cell: no strata, read the main Walk/Wet arrays instead. Because this
    /// takes ushort.MaxValue, a real strata offset is at most NoStrata - 1, which bounds
    /// <see cref="StrataData"/> to NoStrata bytes.
    /// </summary>
    public const ushort NoStrata = ushort.MaxValue;

    /// <summary>
    /// Length-256 table mapping a cell to the byte offset in <see cref="StrataData"/> where its
    /// strata begin, or <see cref="NoStrata"/>. Null when no cell in the chunk is multi-Z.
    /// </summary>
    private ushort[] _strataOffsetByCell;

    /// <summary>
    /// Packed strata for the multi-Z cells. Per cell: u8 stratumCount, then stratumCount records
    /// of <see cref="StratumByteLength"/> bytes — sbyte zCenter, byte walkMask, byte wetMask,
    /// sbyte walkZ_N..NW (8), sbyte swimZ_N..NW (8).
    /// </summary>
    private byte[] _strataData;

    /// <summary>Reserved. Chunks are static-only, so this is always 0.</summary>
    public int BuiltMultisVersion;

    /// <summary>Refreshed on every query that reaches this chunk. Drives LRU eviction.</summary>
    public long LastTouchedTicks;

    /// <summary>Size in bytes of one Stratum record in StrataData.</summary>
    public const int StratumByteLength = 1 + 1 + 1 + 8 + 8;

    public bool IsCellMultiZ(int cellIndex) => GetStrataOffset(cellIndex) != NoStrata;

    public ushort GetStrataOffset(int cellIndex) => _strataOffsetByCell == null ? NoStrata : _strataOffsetByCell[cellIndex];

    public ReadOnlySpan<byte> StrataData =>
        _strataData == null ? ReadOnlySpan<byte>.Empty : _strataData.AsSpan();

    /// <summary>
    /// Sets the chunk's strata in one shot. <paramref name="offsetByCell"/> must be length 256,
    /// carrying <see cref="NoStrata"/> for single-Z cells. Pass null/null to clear.
    /// </summary>
    internal void SetStrata(ushort[] offsetByCell, byte[] data)
    {
        _strataOffsetByCell = offsetByCell;
        _strataData = data;
    }

    /// <summary>Serialization hook: the raw offset array, or null if the chunk has no strata.</summary>
    internal ushort[] GetStrataOffsetByCellForSerialization() => _strataOffsetByCell;

    /// <summary>Serialization hook: the raw data array, or null if the chunk has no strata.</summary>
    internal byte[] GetStrataDataForSerialization() => _strataData;

    /// <summary>
    /// True when all 256 cells share one value across WalkMask, WetMask, SourceZ and every
    /// directional-Z array, with no strata and no swim layer — open water or solid rock, mostly.
    /// <see cref="StepCacheFile"/> collapses such a chunk to a ~28-byte record. A swim layer
    /// disqualifies a chunk outright: its per-cell shore data would not survive the collapse.
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
