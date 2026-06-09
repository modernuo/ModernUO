using System.Collections.Generic;
using Server.Items;
using Server.Multis;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Warm, in-memory cache of per-multiID local-frame walkability masks for INTERIOR multi cells
/// (cell + all 8 neighbours covered by the multi → terrain-neighbour-free → position-invariant).
/// Wraps the Phase-2 synthesizer: GetMask serves a cached mask when a cell is interior and the
/// guards pass, else falls back to StepProbe.ComputeMultiMaskAt. Fixed multis only (keyed by
/// multiID &amp; 0x3FFF); HouseFoundation uses the live path.
/// </summary>
public sealed class MultiMaskCache
{
    public static MultiMaskCache Instance { get; } = new();

    private const int StepHeight = 2;

    private readonly Dictionary<int, MultiLocalMask> _byMultiId = new();

    public void Clear() => _byMultiId.Clear();

    /// <summary>
    /// Finds the multi covering (x,y) and the local cell indices into its MCL. Mirrors
    /// Map.StaticTileEnumerator / BaseMulti.Contains. Returns false if no multi covers the cell.
    /// </summary>
    public static bool TryResolveCoveringMulti(Map map, int x, int y, out BaseMulti multi, out int lx, out int ly)
    {
        foreach (var candidate in map.GetMultisInSector(x, y))
        {
            var mcl = candidate.Components;
            var cx = x - candidate.X - mcl.Min.X;
            var cy = y - candidate.Y - mcl.Min.Y;
            if (cx >= 0 && cy >= 0 && cx < mcl.Width && cy < mcl.Height && mcl.Tiles[cx][cy].Length > 0)
            {
                multi = candidate;
                lx = cx;
                ly = cy;
                return true;
            }
        }

        multi = null;
        lx = ly = 0;
        return false;
    }

    /// <summary>
    /// True iff local cell (lx,ly) and all 8 neighbours are covered by the multi (have MCL tiles).
    /// Such a cell's 8-direction transition is fully determined by the multi (no terrain neighbour),
    /// so its mask is position-invariant. A pure function of the MCL.
    /// </summary>
    public static bool IsInteriorLocalCell(MultiComponentList mcl, int lx, int ly)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var nx = lx + dx;
                var ny = ly + dy;
                if (nx < 0 || ny < 0 || nx >= mcl.Width || ny >= mcl.Height || mcl.Tiles[nx][ny].Length == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

/// <summary>
/// Per-multiID lazily-filled grid of interior-cell masks. Cell state: Unknown (not yet classified),
/// Cached (interior + clean → mask valid), NonInterior (perimeter/edge/terrain-dirty → live-synth).
/// </summary>
internal sealed class MultiLocalMask
{
    public enum CellState : byte { Unknown = 0, Cached = 1, NonInterior = 2 }

    private readonly int _width;
    private readonly int _height;
    private readonly CellState[] _state;
    private readonly StepMask[] _mask;   // local-Z mask, valid when state == Cached
    private readonly sbyte[] _floorZ;    // local floor Z, valid when state == Cached

    public MultiLocalMask(int width, int height)
    {
        _width = width;
        _height = height;
        _state = new CellState[width * height];
        _mask = new StepMask[width * height];
        _floorZ = new sbyte[width * height];
    }

    public int Width => _width;
    public int Height => _height;

    public CellState GetState(int lx, int ly) => _state[ly * _width + lx];

    public void SetCached(int lx, int ly, StepMask localMask, sbyte localFloorZ)
    {
        var i = ly * _width + lx;
        _mask[i] = localMask;
        _floorZ[i] = localFloorZ;
        _state[i] = CellState.Cached;
    }

    public void SetNonInterior(int lx, int ly) => _state[ly * _width + lx] = CellState.NonInterior;

    public StepMask MaskAt(int lx, int ly) => _mask[ly * _width + lx];
    public sbyte FloorZAt(int lx, int ly) => _floorZ[ly * _width + lx];
}
