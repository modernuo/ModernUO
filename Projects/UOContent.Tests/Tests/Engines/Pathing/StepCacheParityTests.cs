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
        var wetCells = 0;

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

                var lookup = cache.TryGetMask(map, x, y, sourceZ);

                samples++;

                if (lookup.HitKind == CacheHitKind.Fallthrough_MultiZ)
                {
                    multiZ++;
                    continue;
                }

                Assert.True(lookup.IsHit, $"Cache returned !ok at ({x},{y}) hitKind={lookup.HitKind}");

                if (lookup.WalkMask != baker.WalkMask)
                {
                    disagreements++;
                    _output.WriteLine($"WALK MASK DIFF @ ({x},{y}) cache=0x{lookup.WalkMask:X2} baker=0x{baker.WalkMask:X2}");
                    continue;
                }

                if (lookup.WetMask != baker.WetMask)
                {
                    disagreements++;
                    _output.WriteLine($"WET MASK DIFF @ ({x},{y}) cache=0x{lookup.WetMask:X2} baker=0x{baker.WetMask:X2}");
                    continue;
                }

                if (lookup.WetMask != 0)
                {
                    wetCells++;
                }

                if (lookup.WalkZ_N != baker.WalkZ_N || lookup.WalkZ_NE != baker.WalkZ_NE
                    || lookup.WalkZ_E != baker.WalkZ_E || lookup.WalkZ_SE != baker.WalkZ_SE
                    || lookup.WalkZ_S != baker.WalkZ_S || lookup.WalkZ_SW != baker.WalkZ_SW
                    || lookup.WalkZ_W != baker.WalkZ_W || lookup.WalkZ_NW != baker.WalkZ_NW)
                {
                    disagreements++;
                    _output.WriteLine($"Z DIFF @ ({x},{y}) cache=({lookup.WalkZ_N},{lookup.WalkZ_NE},{lookup.WalkZ_E},{lookup.WalkZ_SE},{lookup.WalkZ_S},{lookup.WalkZ_SW},{lookup.WalkZ_W},{lookup.WalkZ_NW}) baker=({baker.WalkZ_N},{baker.WalkZ_NE},{baker.WalkZ_E},{baker.WalkZ_SE},{baker.WalkZ_S},{baker.WalkZ_SW},{baker.WalkZ_W},{baker.WalkZ_NW})");
                }
            }
        }

        _output.WriteLine($"[{label}] samples={samples} disagreements={disagreements} multiZ={multiZ} wetCells={wetCells}");

        // Non-vacuity: at least the inn region must have at least one cell that produced a real cache answer.
        if (label == "britain_inn_dense")
        {
            Assert.True(samples - multiZ > 0, "expected real cache answers in dense region");
        }

        Assert.Equal(0, disagreements);
    }

    /// <summary>
    /// Non-vacuity guard for the swim bake: scans a wide swath of the south-Britain bay
    /// (Atlantic coast) and asserts at least one cell has a non-zero WetMask. Catches the
    /// failure mode where StepProbe silently bakes zero swim output everywhere.
    /// </summary>
    [Fact]
    public void SwimBake_ProducesWetCells_OnKnownWaterRegion()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);

        // South Britain → Britain bay, includes Atlantic shoreline. 64×64 = 4096 cells;
        // even a partial coastline straddle should yield dozens of wet cells.
        const int xStart = 1430;
        const int yStart = 1740;
        const int size = 64;

        var wetCells = 0;
        for (var x = xStart; x < xStart + size; x++)
        {
            for (var y = yStart; y < yStart + size; y++)
            {
                map.GetAverageZ(x, y, out _, out var avgZ, out _);
                var sourceZ = (sbyte)StepProbe.ComputeStandingZ(map, x, y, avgZ);
                var baker = StepProbe.ComputeMaskAt(map, x, y, sourceZ);
                if (baker.WetMask != 0)
                {
                    wetCells++;
                }
            }
        }

        _output.WriteLine($"south-britain swim probe: wetCells={wetCells} of 4096");
        Assert.True(wetCells > 0, "swim bake produced zero wet cells across a 64×64 coastal region");
    }
}
