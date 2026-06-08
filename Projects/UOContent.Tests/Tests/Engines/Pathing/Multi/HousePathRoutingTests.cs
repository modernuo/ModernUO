using Server.Engines.Pathing.Cache;
using Server.Items;
using Server.Mobiles;
using Server.PathAlgorithms;
using Xunit;
using Xunit.Abstractions;
using CalcMoves = Server.Movement.Movement;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class HousePathRoutingTests
{
    private readonly ITestOutputHelper _output;

    public HousePathRoutingTests(ITestOutputHelper output) => _output = output;

    private const int MapId = 1;

    private sealed class WalkerStub : Mobile
    {
        public WalkerStub() => Body = 0xC9;
    }

    [Fact]
    public void PathAround_NeverTraversesAWallCell()
    {
        StepCache.Instance.Clear();
        var prevThreshold = StepCache.Instance.MissPromotionThreshold;
        StepCache.Instance.MissPromotionThreshold = 1;
        var map = Map.Maps[MapId];
        Assert.NotNull(map);

        // Open area with lateral room to flank a 7x7 house (verified reachable all 8 dirs).
        const int hx = 1480, hy = 1620;
        const int sx = 1480, sy = 1630;
        const int gx = 1480, gy = 1610;

        map.GetAverageZ(hx, hy, out _, out var hz, out _);
        var multi = new TestMulti(0x74);
        multi.MoveToWorld(new Point3D(hx, hy, (sbyte)hz), map);

        var wall = MultiArt.FindWallCell(multi);
        Assert.True(wall.HasValue, "non-vacuity: house must have wall cells");

        var walker = new WalkerStub();
        map.GetAverageZ(sx, sy, out _, out var sz, out _);
        var start = new Point3D(sx, sy, (sbyte)sz);
        var goal = new Point3D(gx, gy, (sbyte)sz);
        walker.MoveToWorld(start, map);

        try
        {
            var path = BitmapAStarAlgorithm.Instance.Find(walker, map, start, goal);
            Assert.NotNull(path); // non-vacuity: a route around must exist
            Assert.NotEmpty(path);

            // Walk the path; assert it never lands on the known wall cell.
            var x = sx;
            var y = sy;
            foreach (var dir in path)
            {
                CalcMoves.Offset(dir, ref x, ref y);
                Assert.False(x == wall.Value.X && y == wall.Value.Y,
                    $"path traversed wall cell ({wall.Value.X},{wall.Value.Y})");
            }
            _output.WriteLine($"around house: {path.Length} steps, avoided wall");
        }
        finally
        {
            StepCache.Instance.MissPromotionThreshold = prevThreshold;
            walker.Delete();
            multi.Delete();
        }
    }

    [Fact]
    public void Demolish_ReopensCoveredCells()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        Assert.NotNull(map);

        const int hx = 1480, hy = 1620;
        map.GetAverageZ(hx, hy, out _, out var hz, out _);
        var multi = new TestMulti(0x74);
        multi.MoveToWorld(new Point3D(hx, hy, (sbyte)hz), map);

        // Find a wall cell that is blocked purely by the multi — not by underlying static/land
        // terrain. FindWallCell returns the first MCL-impassable cell, which may land over
        // terrain that is itself impassable. Instead scan until we find one where the base
        // terrain is passable so that after demolish the cell provably reopens.
        var w = FindPureMultiWallCell(multi, map);
        Assert.True(w.HasValue, "non-vacuity: house must have a wall cell over passable terrain");
        var walker = new WalkerStub();

        var cell = w.Value;

        try
        {
            // Before demolish: every neighbour fails to step onto the wall cell (it is blocked).
            var blockedBefore = NeighbourBlockedInto(map, walker, cell);
            Assert.True(blockedBefore, "wall cell should block entry while the house stands");

            multi.Delete();

            // After demolish: the cell reverts to open ground; entry from a neighbour should now
            // succeed in at least one direction.
            StepCache.Instance.Clear();
            var openAfter = !NeighbourBlockedInto(map, walker, cell);
            Assert.True(openAfter, $"cell ({cell.X},{cell.Y}) should reopen after demolish");
            _output.WriteLine($"demolish reopened ({cell.X},{cell.Y})");
        }
        finally
        {
            if (!multi.Deleted)
            {
                multi.Delete();
            }
            walker.Delete();
        }
    }

    /// <summary>
    /// Find the first MCL wall cell (Impassable &amp;&amp; !Surface) over terrain that is not
    /// independently blocked by land or statics. Using this instead of
    /// <see cref="MultiArt.FindWallCell"/> ensures the cell reopens after demolish.
    /// </summary>
    private static MultiArt.Cell? FindPureMultiWallCell(BaseMulti multi, Map map)
    {
        var mcl = multi.Components;
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                var hasWall = false;
                var hasSurface = false;
                foreach (var t in mcl.Tiles[lx][ly])
                {
                    var data = TileData.ItemTable[t.ID & TileData.MaxItemValue];
                    if (data.Impassable && !data.Surface)
                    {
                        hasWall = true;
                    }
                    if (data.Surface)
                    {
                        hasSurface = true;
                    }
                }

                if (!hasWall || hasSurface)
                {
                    continue;
                }

                var wx = multi.X + mcl.Min.X + lx;
                var wy = multi.Y + mcl.Min.Y + ly;

                // Check the underlying terrain is not independently impassable.
                if (IsTerrainBlocked(map, wx, wy))
                {
                    continue;
                }

                return new MultiArt.Cell(wx, wy);
            }
        }
        return null;
    }

    /// <summary>
    /// Returns true if land tile or any static tile (non-multi) at (x,y) is impassable.
    /// </summary>
    private static bool IsTerrainBlocked(Map map, int x, int y)
    {
        var lt = map.Tiles.GetLandTile(x, y);
        if (!lt.Ignored)
        {
            var landData = TileData.LandTable[lt.ID & TileData.MaxLandValue];
            if ((landData.Flags & TileFlag.Impassable) != 0)
            {
                return true;
            }
        }

        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            if (data.Impassable)
            {
                return true;
            }
        }

        return false;
    }

    // True if EVERY in-range neighbour fails to step into target (target is blocked).
    private static bool NeighbourBlockedInto(Map map, Mobile walker, MultiArt.Cell target)
    {
        for (var d = 0; d < 8; d++)
        {
            var nx = target.X;
            var ny = target.Y;
            CalcMoves.Offset((Direction)((d + 4) & 7), ref nx, ref ny);
            if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
            {
                continue;
            }
            map.GetAverageZ(nx, ny, out _, out var nz, out _);
            walker.MoveToWorld(new Point3D(nx, ny, (sbyte)nz), map);
            if (CalcMoves.CheckMovement(walker, map, walker.Location, (Direction)d, out _))
            {
                var tx = nx;
                var ty = ny;
                CalcMoves.Offset((Direction)d, ref tx, ref ty);
                if (tx == target.X && ty == target.Y)
                {
                    return false; // an entry succeeded → not blocked
                }
            }
        }
        return true;
    }
}
