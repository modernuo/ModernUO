using System;
using Server.Engines.Pathing.Cache;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// The ground the navigation graph is baked from: where a body can stand in a cell, and where it can step from there.
/// An interface so the graph and its planners can be tested on small synthetic grids; the live one is
/// <see cref="MapNavTerrain"/>.
/// </summary>
public interface INavTerrain
{
    int Width { get; }

    int Height { get; }

    /// <summary>
    /// The standable surface heights at (x, y), ascending, into <paramref name="zs"/>; returns how many. Two surfaces of
    /// one cell are always at least a body's height apart.
    /// </summary>
    int Surfaces(int x, int y, Span<sbyte> zs);

    /// <summary>
    /// The raw 8-direction walk mask from (x, y) standing at <paramref name="z"/>, and the height each step lands at. No
    /// corner rule applied: the planners apply the strict one (both flanks of a diagonal).
    /// </summary>
    StepMask Step(int x, int y, sbyte z);
}

/// <summary>
/// The live ground of one map: the engine's static step probe, and its multi-aware form in any 16-tile sector a house
/// or a boat touches (with the ring round it, since a step from outside can land on a multi's edge).
/// </summary>
public sealed class MapNavTerrain : INavTerrain
{
    private readonly Map _map;

    public MapNavTerrain(Map map) => _map = map;

    public Map Map => _map;

    public int Width => _map.Width;

    public int Height => _map.Height;

    public int Surfaces(int x, int y, Span<sbyte> zs) =>
        Multis(x, y)
            ? StepProbe.ComputeStandableSurfaceZsWithMultis(_map, x, y, zs)
            : StepProbe.ComputeStandableSurfaceZs(_map, x, y, zs);

    public StepMask Step(int x, int y, sbyte z) =>
        Multis(x, y) ? StepProbe.ComputeMultiMaskAt(_map, x, y, z) : StepProbe.ComputeMaskAt(_map, x, y, z);

    /// <summary>
    /// Whether a multi's sector touches (x, y): its own sector, or the one beside it when the cell lies on that edge — the
    /// halo <c>StepCache</c> uses, since a step from the edge of a sector can land on a multi in the next.
    /// </summary>
    public bool Multis(int x, int y)
    {
        var sx = x >> Map.SectorShift;
        var sy = y >> Map.SectorShift;

        if (_map.GetRealSector(sx, sy).HasMultis)
        {
            return true;
        }

        var west = (x & 15) == 0;
        var east = (x & 15) == 15;
        var north = (y & 15) == 0;
        var south = (y & 15) == 15;

        if (!(west || east || north || south))
        {
            return false;
        }

        return west && _map.GetRealSector(sx - 1, sy).HasMultis
            || east && _map.GetRealSector(sx + 1, sy).HasMultis
            || north && _map.GetRealSector(sx, sy - 1).HasMultis
            || south && _map.GetRealSector(sx, sy + 1).HasMultis
            || west && north && _map.GetRealSector(sx - 1, sy - 1).HasMultis
            || east && north && _map.GetRealSector(sx + 1, sy - 1).HasMultis
            || west && south && _map.GetRealSector(sx - 1, sy + 1).HasMultis
            || east && south && _map.GetRealSector(sx + 1, sy + 1).HasMultis;
    }
}
