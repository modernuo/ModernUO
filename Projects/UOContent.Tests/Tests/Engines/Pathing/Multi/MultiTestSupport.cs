using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Tests.Pathfinding;

/// <summary>
/// Minimal concrete <see cref="BaseMulti"/> for tests. Walkability depends only on
/// Components (the shared MCL for the multiID) + Location, so this is a faithful stand-in
/// for any fixed-design multi (classic house, camp, boat heading) without the owning
/// house/boat machinery. Never serialized in tests.
/// </summary>
public sealed class TestMulti : BaseMulti
{
    public TestMulti(int itemID) : base(itemID)
    {
    }
}

/// <summary>
/// Helpers that derive expected geometry from a multi's MCL art at runtime, so tests
/// encode no hardcoded cell coordinates and survive art-data changes.
/// </summary>
public static class MultiArt
{
    public readonly record struct Cell(int X, int Y);

    /// <summary>Every world cell the multi's footprint covers (Tiles stack non-empty).</summary>
    public static List<Cell> FootprintCells(BaseMulti multi)
    {
        var mcl = multi.Components;
        var result = new List<Cell>();
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                if (mcl.Tiles[lx][ly].Length == 0)
                {
                    continue;
                }
                result.Add(new Cell(multi.X + mcl.Min.X + lx, multi.Y + mcl.Min.Y + ly));
            }
        }
        return result;
    }

    /// <summary>Footprint cells plus a 1-cell halo ring (the cells the split also routes to slow path).</summary>
    public static HashSet<Cell> FootprintWithHalo(BaseMulti multi)
    {
        var foot = FootprintCells(multi);
        var set = new HashSet<Cell>();
        foreach (var c in foot)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    set.Add(new Cell(c.X + dx, c.Y + dy));
                }
            }
        }
        return set;
    }

    /// <summary>First world cell whose MCL stack contains an impassable, non-surface (wall) tile, or null.</summary>
    public static Cell? FindWallCell(BaseMulti multi)
    {
        var mcl = multi.Components;
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                foreach (var t in mcl.Tiles[lx][ly])
                {
                    var data = TileData.ItemTable[t.ID & TileData.MaxItemValue];
                    if (data.Impassable && !data.Surface)
                    {
                        return new Cell(multi.X + mcl.Min.X + lx, multi.Y + mcl.Min.Y + ly);
                    }
                }
            }
        }
        return null;
    }

    /// <summary>First world cell whose MCL stack contains a walkable surface (floor) tile, or null.</summary>
    public static Cell? FindFloorCell(BaseMulti multi)
    {
        var mcl = multi.Components;
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                foreach (var t in mcl.Tiles[lx][ly])
                {
                    var data = TileData.ItemTable[t.ID & TileData.MaxItemValue];
                    if (data.Surface && !data.Impassable)
                    {
                        return new Cell(multi.X + mcl.Min.X + lx, multi.Y + mcl.Min.Y + ly);
                    }
                }
            }
        }
        return null;
    }
}
