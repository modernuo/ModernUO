namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Per-chunk storage backing StepCache. Holds raw walk + swim masks and destination Z
/// values for each of 256 cells in a 16x16 chunk, plus build-time metadata (multis
/// version, multi-Z bitmap) and LRU bookkeeping.
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

    /// <summary>
    /// Serialization hook for <see cref="StepCacheFile"/>: returns the multi-Z bitmap,
    /// or null if no cells in this chunk are multi-Z. Read-only — callers must not mutate.
    /// </summary>
    internal byte[] GetMultiZCellsForSerialization() => _multiZCells;

    /// <summary>
    /// Deserialization hook: assigns the multi-Z bitmap from a file load. Caller is
    /// responsible for passing a 32-byte array (or null for "no cells multi-Z").
    /// </summary>
    internal void RestoreMultiZCellsFromSerialization(byte[] multiZ) => _multiZCells = multiZ;
}
