using System;
using Server.Engines.Pathing.Cache;

namespace Server.Tests.Pathfinding;

/// <summary>
/// Shared fixtures for the step-cache tests: the walker the parity tests measure against, the
/// cell-index arithmetic, and builders for the chunk state several tests inject by hand.
/// </summary>
internal static class PathingTestSupport
{
    /// <summary>
    /// Trammel. Every seed coordinate below is a real location on it, so these tests need the
    /// client's map files; they skip when those are absent.
    /// </summary>
    public static Map TestMap => Map.Maps[1];

    /// <summary>
    /// A cell in open Britain countryside — flat, walkable in all directions, no statics. The
    /// default subject when a test needs a chunk to exist and doesn't care what's in it.
    /// </summary>
    public const int PlainX = 1500;
    public const int PlainY = 1600;

    /// <summary>Index of world cell (x, y) within its own chunk.</summary>
    public static int CellIndex(int x, int y) => ((y & 15) << 4) | (x & 15);

    /// <summary>A strata offset table with every cell marked single-Z.</summary>
    public static ushort[] NoStrataOffsets()
    {
        var offsets = new ushort[StepChunk.CellsPerChunk];
        Array.Fill(offsets, StepChunk.NoStrata);
        return offsets;
    }

    /// <summary>
    /// Packs a one-stratum record: a count byte, then the stratum itself. Directions not named in
    /// <paramref name="walkZs"/> stay at 0. Mirrors the layout StepCache.WriteStratum produces.
    /// </summary>
    public static byte[] OneStratum(sbyte zCenter, byte walkMask = 0, byte wetMask = 0, params sbyte[] walkZs)
    {
        var data = new byte[1 + StepChunk.StratumByteLength];
        data[0] = 1; // stratum count
        data[1] = (byte)zCenter;
        data[2] = walkMask;
        data[3] = wetMask;

        // walkZ_N..NW occupy bytes 4..11; swimZ_N..NW follow at 12..19.
        for (var i = 0; i < walkZs.Length && i < 8; i++)
        {
            data[4 + i] = (byte)walkZs[i];
        }

        return data;
    }

    /// <summary>
    /// The default static walker. Deriving straight from <see cref="Mobile"/> rather than
    /// BaseCreature is the point: MovementImpl then sees no creature capabilities (no swim, no fly,
    /// no door-opening), which is exactly the walker the cache bakes for.
    /// </summary>
    public sealed class StaticWalker : Mobile
    {
        public StaticWalker()
        {
            Body = 0xC9;
        }
    }
}
