namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Per-chunk storage backing StepCache. Holds raw walkability masks and
/// destination Z values for each of 256 cells in a 16x16 chunk, plus build-time
/// metadata (multis version, multi-Z bitmap) and LRU bookkeeping.
/// </summary>
internal sealed class StepChunk
{
    public const int CellsPerChunk = 256; // 16 x 16

    /// <summary>Bit i of Mask[c] = "can step from cell c to neighbor (Direction)i". Raw — no diagonal corner-cut applied here.</summary>
    public readonly byte[] Mask = new byte[CellsPerChunk];

    public readonly sbyte[] SourceZ = new sbyte[CellsPerChunk];

    public readonly sbyte[] DestZN  = new sbyte[CellsPerChunk];
    public readonly sbyte[] DestZNE = new sbyte[CellsPerChunk];
    public readonly sbyte[] DestZE  = new sbyte[CellsPerChunk];
    public readonly sbyte[] DestZSE = new sbyte[CellsPerChunk];
    public readonly sbyte[] DestZS  = new sbyte[CellsPerChunk];
    public readonly sbyte[] DestZSW = new sbyte[CellsPerChunk];
    public readonly sbyte[] DestZW  = new sbyte[CellsPerChunk];
    public readonly sbyte[] DestZNW = new sbyte[CellsPerChunk];

    /// <summary>
    /// 32 bytes = 256 bits when allocated. Lazy: most chunks are entirely single-Z,
    /// so we only pay the 32 bytes on chunks that actually need it.
    /// </summary>
    private byte[] _multiZCells;

    /// <summary>Snapshot of Sector.MultisVersion at the time this chunk was built.</summary>
    public int BuiltMultisVersion;

    /// <summary>Updated on every cache hit/miss. Used by LRU fallback eviction.</summary>
    public long LastTouchedTicks;

    public bool IsCellMultiZ(int cellIndex) =>
        _multiZCells != null && (_multiZCells[cellIndex >> 3] & (1 << (cellIndex & 7))) != 0;

    public void MarkCellMultiZ(int cellIndex)
    {
        _multiZCells ??= new byte[32];
        _multiZCells[cellIndex >> 3] |= (byte)(1 << (cellIndex & 7));
    }
}
