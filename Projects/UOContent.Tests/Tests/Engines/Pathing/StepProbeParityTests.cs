using Server.Engines.Pathing.Cache;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class StaticWalkabilityParityTests
{
    private readonly ITestOutputHelper _output;

    public StaticWalkabilityParityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("britain_inn_dense", 1480, 1610, 32)]
    [InlineData("trammel_open_plain", 1500, 1600, 32)]
    public void BakerMatchesCheckMovement(string label, int xStart, int yStart, int size)
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var stub = new ParityStubMobile();
        stub.MoveToWorld(new Point3D(xStart, yStart, 0), map);

        var disagreements = 0;
        var samples = 0;
        var oldWalkable = 0;
        var newWalkable = 0;

        for (var x = xStart; x < xStart + size; x++)
        {
            for (var y = yStart; y < yStart + size; y++)
            {
                map.GetAverageZ(x, y, out _, out var avgZ, out _);
                var sourceZ = (sbyte)avgZ;

                var loc = new Point3D(x, y, sourceZ);

                var bakerResult = StepProbe.ComputeMaskAt(map, x, y, sourceZ);

                for (var d = 0; d < 8; d++)
                {
                    var dir = (Direction)d;
                    samples++;

                    var oldOk = Movement.Movement.CheckMovement(stub, map, loc, dir, out var oldZ);

                    // Apply creature diagonal corner-cut rule at query time:
                    // diagonal walkable iff raw-diagonal AND (left-partner OR right-partner).
                    // (Raw masks are correct per spec; baker omits diagonal logic per design.)
                    var newOk = bakerResult.IsWalkable(dir);
                    if (newOk && ((d & 1) == 1))
                    {
                        var leftPartner = (Direction)((d - 1) & 7);
                        var rightPartner = (Direction)((d + 1) & 7);
                        if (!bakerResult.IsWalkable(leftPartner) && !bakerResult.IsWalkable(rightPartner))
                        {
                            newOk = false;
                        }
                    }

                    var newZ = bakerResult.GetDestZ(dir);

                    if (oldOk)
                    {
                        oldWalkable++;
                    }
                    if (newOk)
                    {
                        newWalkable++;
                    }

                    if (oldOk != newOk)
                    {
                        disagreements++;
                        _output.WriteLine(
                            $"DISAGREE walkable @ ({x},{y},{sourceZ}) dir={dir}: " +
                            $"old={oldOk} new={newOk}"
                        );
                    }
                    else if (oldOk && oldZ != newZ)
                    {
                        disagreements++;
                        _output.WriteLine(
                            $"DISAGREE destZ @ ({x},{y},{sourceZ}) dir={dir}: " +
                            $"old={oldZ} new={newZ}"
                        );
                    }
                }
            }
        }

        stub.Delete();

        _output.WriteLine(
            $"[{label}] Samples: {samples}, Disagreements: {disagreements}, " +
            $"OldWalkable: {oldWalkable}, NewWalkable: {newWalkable}"
        );

        // Non-vacuity guard for the variety case: at least one region must show some
        // blocked directions. The open_plain region is allowed to be all-walkable.
        if (label == "britain_inn_dense")
        {
            Assert.NotEqual(0, oldWalkable);
            Assert.NotEqual(samples, oldWalkable);
        }

        Assert.Equal(0, disagreements);
    }

    /// <summary>
    /// Minimal Mobile stub for parity testing. Inherits directly from Mobile so that
    /// MovementImpl sees no BaseCreature-specific flags (CanSwim=false, CanFly=false,
    /// bc==null → BaseCreature branches skipped) giving us the default static walker baseline.
    /// </summary>
    private class ParityStubMobile : Mobile
    {
        public ParityStubMobile()
        {
            Body = 0xC9; // arbitrary horse body
        }
    }
}
