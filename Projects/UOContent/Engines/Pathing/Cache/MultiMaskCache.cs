using System;
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
    /// Returns the multi-aware StepMask for a covered cell (x,y,sourceZ). Serves a cached interior
    /// mask when available and the guards pass (counted as a MultiMaskCacheHit); otherwise falls
    /// back to the Phase-2 live synthesizer ComputeMultiMaskAt (counted as a MultiLocalHit), caching
    /// the result if the cell is interior and clean. Always returns a usable mask (HitKind == Hit).
    /// </summary>
    public StepMask GetMask(Map map, int x, int y, sbyte sourceZ)
    {
        if (!TryResolveCoveringMulti(map, x, y, out var multi, out var lx, out var ly)
            || multi is HouseFoundation)
        {
            // No covering multi (defensive) or a runtime-mutable foundation → live, no cache.
            return LiveSynth(map, x, y, sourceZ);
        }

        var mcl = multi.Components;
        var local = GetOrCreate(multi.ItemID & 0x3FFF, mcl.Width, mcl.Height);
        var state = local.GetState(lx, ly);

        if (state == MultiLocalMask.CellState.Cached)
        {
            // Reconstruct the floor Z in the world frame for THIS placement (int; avoids a silent
            // sbyte wrap when a different instance serves a cell cached at another instance's Z).
            var worldFloorZ = local.FloorZAt(lx, ly) + multi.Z;
            // KNOWN LIMITATION (cross-instance neighbour terrain): the cached mask is the multi's
            // terrain-free transition; serving it is exact only if terrain stays below the floor at
            // this cell AND its 8 neighbours (CheckStaticStep reads neighbour terrain). We guard only
            // the centre cell here for speed. For legitimately-placed fixed multis this is always
            // satisfied — house/boat placement requires flat, clear footprints, so the whole 3x3 sits
            // above terrain. It can only diverge for a contrived placement (e.g. a GM forcing a multi
            // onto a slope so a covered neighbour's land/static rises above the floor); such a cell
            // would serve this instance's clean mask instead of live-synthesizing. See the Phase-3
            // design risk list; the airtight fix (serve-time 3x3 guard, or a per-instance
            // footprint-clean flag) is deferred pending a perf/complexity decision.
            if (Math.Abs(sourceZ - worldFloorZ) <= StepHeight && TerrainTopBelow(map, x, y, (sbyte)worldFloorZ))
            {
                StepCache.Instance.RecordMultiMaskCacheHit();
                return ToWorldZ(local.MaskAt(lx, ly), multi.Z);
            }

            return LiveSynth(map, x, y, sourceZ); // guard failed this placement → live
        }

        if (state == MultiLocalMask.CellState.NonInterior)
        {
            return LiveSynth(map, x, y, sourceZ);
        }

        // Unknown → classify + (if interior + clean) build & cache.
        var mask = LiveSynth(map, x, y, sourceZ);
        if (IsInteriorLocalCell(mcl, lx, ly)
            && TerrainTopBelow(map, x, y, sourceZ)
            && TryToLocalZ(mask, multi.Z, out var localMask)
            && sourceZ - multi.Z is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            // Record this sourceZ as the cell's reference floor Z (an interior floor cell is reached
            // at the floor Z); later expansions are validated against it within StepHeight above.
            local.SetCached(lx, ly, localMask, (sbyte)(sourceZ - multi.Z));
        }
        else
        {
            local.SetNonInterior(lx, ly);
        }

        return mask;
    }

    private static StepMask LiveSynth(Map map, int x, int y, sbyte sourceZ)
    {
        StepCache.Instance.RecordMultiLocalHit();
        return StepProbe.ComputeMultiMaskAt(map, x, y, sourceZ);
    }

    private MultiLocalMask GetOrCreate(int key, int width, int height)
    {
        if (!_byMultiId.TryGetValue(key, out var m))
        {
            m = new MultiLocalMask(width, height);
            _byMultiId[key] = m;
        }

        return m;
    }

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

    /// <summary>
    /// Converts a world-frame mask's per-direction Zs to local Z (subtract multiZ). Returns false
    /// if any local Z doesn't fit sbyte (caller must then NOT cache the cell — rare; only when
    /// |multiZ| is large enough to push a world Z out of range). Mask (walk/wet) bits are copied.
    /// </summary>
    public static bool TryToLocalZ(StepMask world, int multiZ, out StepMask local)
    {
        local = default;
        Span<sbyte> w = stackalloc sbyte[8];
        Span<sbyte> s = stackalloc sbyte[8];
        for (var d = 0; d < 8; d++)
        {
            var lw = world.GetWalkZ((Direction)d) - multiZ;
            var ls = world.GetSwimZ((Direction)d) - multiZ;
            if (lw < sbyte.MinValue || lw > sbyte.MaxValue || ls < sbyte.MinValue || ls > sbyte.MaxValue)
            {
                return false;
            }
            w[d] = (sbyte)lw;
            s[d] = (sbyte)ls;
        }

        local = new StepMask(
            world.WalkMask, world.WetMask,
            w[0], w[1], w[2], w[3], w[4], w[5], w[6], w[7],
            s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7]
        );
        return true;
    }

    /// <summary>
    /// True iff all terrain (land + statics) at (x,y) sits strictly below <paramref name="floorZ"/>,
    /// so a creature standing on the multi floor never sees terrain in its envelope and the cached
    /// (terrain-free) mask is exact. Cheap: one land-top read + the cell's static-tile array scan.
    /// </summary>
    public static bool TerrainTopBelow(Map map, int x, int y, sbyte floorZ)
    {
        map.GetAverageZ(x, y, out _, out _, out var landTop);
        if (landTop >= floorZ)
        {
            return false;
        }

        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            var top = tile.Z + data.CalcHeight;
            if (top >= floorZ)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Inverse of <see cref="TryToLocalZ"/>: add multiZ back to recover world Zs.</summary>
    public static StepMask ToWorldZ(StepMask local, int multiZ)
    {
        Span<sbyte> w = stackalloc sbyte[8];
        Span<sbyte> s = stackalloc sbyte[8];
        for (var d = 0; d < 8; d++)
        {
            w[d] = (sbyte)(local.GetWalkZ((Direction)d) + multiZ);
            s[d] = (sbyte)(local.GetSwimZ((Direction)d) + multiZ);
        }

        return new StepMask(
            local.WalkMask, local.WetMask,
            w[0], w[1], w[2], w[3], w[4], w[5], w[6], w[7],
            s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7]
        );
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
