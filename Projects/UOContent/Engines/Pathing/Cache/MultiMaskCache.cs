using System;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Caches walkability masks for the interior cells of a multi, shared across every instance of the
/// same multiID.
///
/// An interior cell — one whose 8 neighbours are all covered by the multi — has no terrain
/// neighbour, so its mask depends only on the multi's own component tiles and is identical at every
/// position the design is placed. That makes it cacheable in the multi's local frame and reusable
/// across instances; perimeter cells are not, and fall back to <see cref="StepProbe.ComputeMultiMaskAt"/>.
///
/// The catch is terrain intruding into the multi's floor envelope, which would make a cell's mask
/// position-dependent after all. <see cref="ComputeFootprintClean"/> rules that out per instance,
/// once: if the whole footprint's terrain sits below the lowest floor, no interior cell can see it.
/// A dirty instance, or a <see cref="HouseFoundation"/> (whose design mutates at runtime), always
/// synthesizes live rather than risk serving a wrong mask.
/// </summary>
public sealed class MultiMaskCache
{
    public static MultiMaskCache Instance { get; } = new();

    private const int StepHeight = 2;

    private readonly Dictionary<int, MultiLocalMask> _byMultiId = [];

    public void Clear() => _byMultiId.Clear();

    /// <summary>
    /// The multi-aware mask for a covered cell, always usable (HitKind is always Hit). Served from
    /// the shared cache when the cell is a clean interior one, synthesized live otherwise.
    /// </summary>
    public StepMask GetMask(Map map, int x, int y, sbyte sourceZ)
    {
        if (!TryResolveCoveringMulti(map, x, y, out var multi, out var lx, out var ly)
            || multi is HouseFoundation)      // its DesignState MCL changes at runtime
        {
            return LiveSynth(map, x, y, sourceZ);
        }

        // Boats are cached despite moving: a deck mask is built in the local frame and is invariant
        // under translation, and the clean gate plus the ItemID/location/map resets cover turning.

        if (multi.PathInteriorCacheState == MultiInteriorCacheState.Unknown)
        {
            multi.PathInteriorCacheState =
                ComputeFootprintClean(map, multi) ? MultiInteriorCacheState.Clean : MultiInteriorCacheState.Dirty;
        }

        if (multi.PathInteriorCacheState != MultiInteriorCacheState.Clean)
        {
            return LiveSynth(map, x, y, sourceZ);
        }

        var mcl = multi.Components;
        var local = GetOrCreate(multi.ItemID & 0x3FFF, mcl.Width, mcl.Height);
        var state = local.GetState(lx, ly);

        if (state == MultiLocalMask.CellState.Cached)
        {
            // A clean footprint rules terrain out, so the source-Z match is the only guard left.
            var worldFloorZ = local.FloorZAt(lx, ly) + multi.Z;
            if (Math.Abs(sourceZ - worldFloorZ) <= StepHeight)
            {
                StepCache.Instance.RecordMultiMaskCacheHit();
                return ToWorldZ(local.MaskAt(lx, ly), multi.Z);
            }

            return LiveSynth(map, x, y, sourceZ);
        }

        if (state == MultiLocalMask.CellState.NonInterior)
        {
            return LiveSynth(map, x, y, sourceZ);
        }

        // First touch of this cell: synthesize, then classify and cache if it's interior.
        var mask = LiveSynth(map, x, y, sourceZ);
        if (IsInteriorLocalCell(mcl, lx, ly)
            && TryToLocalZ(mask, multi.Z, out var localMask)
            && sourceZ - multi.Z is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
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
    /// Finds the multi covering (x,y) and the cell's indices into its MCL, or false if none does.
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
    /// True when (lx,ly) and all 8 of its neighbours carry MCL tiles. Such a cell has no terrain
    /// neighbour, so the multi alone determines its transitions and its mask is position-invariant.
    /// A pure function of the MCL.
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
    /// Rebases a world-frame mask's per-direction Zs into the multi's local frame. Returns false
    /// when a local Z overflows sbyte — only reachable at extreme |multiZ| — and the caller must
    /// then leave the cell uncached.
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
    /// True when all terrain (land + statics) at (x,y) sits strictly below <paramref name="floorZ"/>,
    /// so a creature standing on the multi's floor never sees terrain in its envelope.
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

    /// <summary>Highest terrain (land + statics) top at (x,y).</summary>
    public static int TerrainTop(Map map, int x, int y)
    {
        map.GetAverageZ(x, y, out _, out _, out var top);
        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            var t = tile.Z + data.CalcHeight;
            if (t > top)
            {
                top = t;
            }
        }

        return top;
    }

    /// <summary>
    /// True when the multi's entire footprint terrain sits below its lowest standable floor. No
    /// covered cell's terrain — nor any neighbour's — can then intrude into a creature's floor
    /// envelope, which is what makes this instance's interior cells safe to serve from the shared
    /// per-multiID cache. Evaluated once per instance and cached on the multi.
    /// </summary>
    public static bool ComputeFootprintClean(Map map, BaseMulti multi)
    {
        var mcl = multi.Components;
        var minFloorLocal = int.MaxValue;
        var maxTerrain = int.MinValue;

        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                var col = mcl.Tiles[lx][ly];
                if (col.Length == 0)
                {
                    continue;
                }

                foreach (var tile in col)
                {
                    var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
                    if (data.Surface && !data.Impassable)
                    {
                        var top = tile.Z + data.CalcHeight;
                        if (top < minFloorLocal)
                        {
                            minFloorLocal = top;
                        }
                    }
                }

                var terrain = TerrainTop(map, multi.X + mcl.Min.X + lx, multi.Y + mcl.Min.Y + ly);
                if (terrain > maxTerrain)
                {
                    maxTerrain = terrain;
                }
            }
        }

        if (minFloorLocal == int.MaxValue)
        {
            return false; // no standable floor anywhere; refuse to cache rather than guess
        }

        return maxTerrain < minFloorLocal + multi.Z;
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
/// One multiID's grid of interior-cell masks, filled in as cells are first touched. A cell is
/// Unknown until classified, then either Cached (interior — the mask is valid) or NonInterior
/// (perimeter — synthesize live).
/// </summary>
internal sealed class MultiLocalMask
{
    public enum CellState : byte { Unknown = 0, Cached = 1, NonInterior = 2 }

    private readonly int _width;
    private readonly int _height;
    private readonly CellState[] _state;
    private readonly StepMask[] _mask;   // local-frame mask, valid only when state == Cached
    private readonly sbyte[] _floorZ;    // local floor Z, valid only when state == Cached

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
