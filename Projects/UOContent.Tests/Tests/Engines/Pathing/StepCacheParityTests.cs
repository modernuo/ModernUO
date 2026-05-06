using Server.Engines.Pathing.Cache;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class StepCacheParityTests
{
    private readonly ITestOutputHelper _output;

    public StepCacheParityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("britain_inn_dense", 1480, 1610, 32)]
    [InlineData("trammel_open_plain", 1500, 1600, 32)]
    [InlineData("britain_causeway", 1475, 1641, 32)]
    public void CacheMatchesBaker(string label, int xStart, int yStart, int size)
    {
        var cache = StepCache.Instance;
        cache.Clear();

        var map = Map.Maps[1];
        Assert.NotNull(map);

        var disagreements = 0;
        var samples = 0;
        var multiZ = 0;

        for (var x = xStart; x < xStart + size; x++)
        {
            for (var y = yStart; y < yStart + size; y++)
            {
                map.GetAverageZ(x, y, out _, out var avgZ, out _);

                // The cache bakes from the slow path's standing Z (the Z a creature actually
                // stands at on this cell). Query with the same Z so the source-Z guard
                // doesn't false-positive on every paver cell.
                var sourceZ = (sbyte)StepProbe.ComputeStandingZ(map, x, y, avgZ);

                var baker = StepProbe.ComputeMaskAt(map, x, y, sourceZ);

                var ok = cache.TryGetMask(
                    map, x, y, sourceZ,
                    out var mask,
                    out var dN, out var dNE, out var dE, out var dSE,
                    out var dS, out var dSW, out var dW, out var dNW,
                    out var hitKind
                );

                samples++;

                if (hitKind == CacheHitKind.Fallthrough_MultiZ)
                {
                    multiZ++;
                    continue;
                }

                Assert.True(ok, $"Cache returned !ok at ({x},{y}) hitKind={hitKind}");

                if (mask != baker.Mask)
                {
                    disagreements++;
                    _output.WriteLine($"MASK DIFF @ ({x},{y}) cache=0x{mask:X2} baker=0x{baker.Mask:X2}");
                    continue;
                }

                if (dN != baker.DestZ_N || dNE != baker.DestZ_NE || dE != baker.DestZ_E || dSE != baker.DestZ_SE
                    || dS != baker.DestZ_S || dSW != baker.DestZ_SW || dW != baker.DestZ_W || dNW != baker.DestZ_NW)
                {
                    disagreements++;
                    _output.WriteLine($"Z DIFF @ ({x},{y}) cache=({dN},{dNE},{dE},{dSE},{dS},{dSW},{dW},{dNW}) baker=({baker.DestZ_N},{baker.DestZ_NE},{baker.DestZ_E},{baker.DestZ_SE},{baker.DestZ_S},{baker.DestZ_SW},{baker.DestZ_W},{baker.DestZ_NW})");
                }
            }
        }

        _output.WriteLine($"[{label}] samples={samples} disagreements={disagreements} multiZ={multiZ}");

        // Non-vacuity: at least the inn region must have at least one cell that produced a real cache answer.
        if (label == "britain_inn_dense")
        {
            Assert.True(samples - multiZ > 0, "expected real cache answers in dense region");
        }

        Assert.Equal(0, disagreements);
    }
}
