using Server.Engines.Pathing.Cache;
using Server.Items;
using Server.Mobiles;
using Xunit;
using Xunit.Abstractions;
using CalcMoves = Server.Movement.Movement;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class MultiWalkabilityTests
{
    private readonly ITestOutputHelper _output;

    public MultiWalkabilityTests(ITestOutputHelper output) => _output = output;

    private const int MapId = 1;
    private const int PlaceX = 1500;
    private const int PlaceY = 1600;

    private sealed class WalkerStub : Mobile
    {
        public WalkerStub() => Body = 0xC9;
    }

    [Fact]
    public void WallCell_CannotBeEnteredFromAnyAdjacentCell()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);

        var multi = new TestMulti(0x74);
        multi.MoveToWorld(new Point3D(PlaceX, PlaceY, (sbyte)z), map);

        var walker = new WalkerStub();
        walker.MoveToWorld(new Point3D(PlaceX, PlaceY, (sbyte)z), map);

        try
        {
            var wall = MultiArt.FindWallCell(multi);
            Assert.True(wall.HasValue, "non-vacuity: house MCL must contain a wall tile");
            var w = wall.Value;

            // From each of the 8 cells surrounding the wall, try stepping in all 8 directions.
            // No step may land ON the wall cell. We also require that SOME step succeeds, so a
            // "zero wall entries" result reflects the wall blocking — not the walker being
            // unable to move here at all (e.g. a Z mismatch blocking everything: a vacuous pass).
            var entriesIntoWall = 0;
            var successfulSteps = 0;
            for (var around = 0; around < 8; around++)
            {
                var cx = w.X;
                var cy = w.Y;
                CalcMoves.Offset((Direction)around, ref cx, ref cy);
                map.GetAverageZ(cx, cy, out _, out var cz, out _);
                var from = new Point3D(cx, cy, (sbyte)cz);

                for (var d = 0; d < 8; d++)
                {
                    if (!CalcMoves.CheckMovement(walker, map, from, (Direction)d, out _))
                    {
                        continue;
                    }

                    successfulSteps++;

                    var tx = cx;
                    var ty = cy;
                    CalcMoves.Offset((Direction)d, ref tx, ref ty);
                    if (tx == w.X && ty == w.Y)
                    {
                        entriesIntoWall++;
                        _output.WriteLine($"UNEXPECTED entry into wall ({w.X},{w.Y}) from ({cx},{cy}) dir {d}");
                    }
                }
            }

            Assert.True(successfulSteps > 0, "non-vacuity: walker must be able to move near the wall");
            Assert.Equal(0, entriesIntoWall);
        }
        finally
        {
            walker.Delete();
            multi.Delete();
        }
    }

    [Fact]
    public void FloorCell_IsStandable()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);

        var multi = new TestMulti(0x74);
        multi.MoveToWorld(new Point3D(PlaceX, PlaceY, (sbyte)z), map);

        var walker = new WalkerStub();

        try
        {
            var floor = MultiArt.FindFloorCell(multi);
            Assert.True(floor.HasValue, "non-vacuity: house MCL must contain a floor tile");
            var f = floor.Value;

            // Stand the walker on a cardinal neighbour of the floor cell and require at least
            // one direction that successfully steps onto the floor cell.
            var enteredFloor = false;
            for (var d = 0; d < 8 && !enteredFloor; d++)
            {
                var nx = f.X;
                var ny = f.Y;
                CalcMoves.Offset((Direction)((d + 4) & 7), ref nx, ref ny);
                map.GetAverageZ(nx, ny, out _, out var nz, out _);
                walker.MoveToWorld(new Point3D(nx, ny, (sbyte)nz), map);
                if (CalcMoves.CheckMovement(walker, map, walker.Location, (Direction)d, out _))
                {
                    var tx = nx;
                    var ty = ny;
                    CalcMoves.Offset((Direction)d, ref tx, ref ty);
                    if (tx == f.X && ty == f.Y)
                    {
                        enteredFloor = true;
                    }
                }
            }

            Assert.True(enteredFloor, $"expected the floor cell ({f.X},{f.Y}) to be reachable from a neighbour");
        }
        finally
        {
            walker.Delete();
            multi.Delete();
        }
    }
}
