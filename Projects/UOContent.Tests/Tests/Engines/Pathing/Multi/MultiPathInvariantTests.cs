using Server.Engines.Pathing.Cache;
using Server.Items;
using Server.Mobiles;
using Server.PathAlgorithms;
using Server.Systems.FeatureFlags;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class MultiPathInvariantTests
{
    private readonly ITestOutputHelper _output;

    public MultiPathInvariantTests(ITestOutputHelper output) => _output = output;

    private const int MapId = 1;

    private sealed class WalkerStub : Mobile
    {
        public WalkerStub() => Body = 0xC9;
    }

    private static Direction[] FindWithFlag(Mobile m, Map map, Point3D start, Point3D goal, bool cacheOn)
    {
        var prev = ContentFeatureFlags.BitmapPathfindingCache;
        try
        {
            ContentFeatureFlags.BitmapPathfindingCache = cacheOn;
            StepCache.Instance.Clear();
            StepCache.Instance.MissPromotionThreshold = 1;
            return BitmapAStarAlgorithm.Instance.Find(m, map, start, goal);
        }
        finally
        {
            ContentFeatureFlags.BitmapPathfindingCache = prev;
        }
    }

    [Theory]
    // start, goal: straddle a house placed between them (house at ~ midpoint).
    [InlineData(1500, 1600, 1500, 1612)] // N-S across the footprint
    [InlineData(1494, 1606, 1512, 1606)] // E-W across the footprint
    public void Find_CacheOn_EqualsCacheOff_WithHousePresent(int sx, int sy, int gx, int gy)
    {
        var map = Map.Maps[MapId];
        Assert.NotNull(map);

        // Place the house at the midpoint so it sits between start and goal.
        var hx = (sx + gx) / 2;
        var hy = (sy + gy) / 2;
        map.GetAverageZ(hx, hy, out _, out var hz, out _);
        var multi = new TestMulti(0x74);
        multi.MoveToWorld(new Point3D(hx, hy, (sbyte)hz), map);

        var walker = new WalkerStub();
        map.GetAverageZ(sx, sy, out _, out var sz, out _);
        var start = new Point3D(sx, sy, (sbyte)sz);
        var goal = new Point3D(gx, gy, (sbyte)sz);
        walker.MoveToWorld(start, map);

        try
        {
            var on = FindWithFlag(walker, map, start, goal, cacheOn: true);
            var off = FindWithFlag(walker, map, start, goal, cacheOn: false);

            // Both-null or both-equal arrays. Equality of the direction sequence is the invariant.
            Assert.Equal(off == null, on == null);
            if (on != null)
            {
                Assert.Equal(off, on);
                _output.WriteLine($"({sx},{sy})->({gx},{gy}) house: {on.Length} steps, cache==slow");
            }
            else
            {
                _output.WriteLine($"({sx},{sy})->({gx},{gy}) house: no path (both)");
            }
        }
        finally
        {
            walker.Delete();
            multi.Delete();
        }
    }

    [Theory]
    [InlineData(1500, 1600, 1498, 1598)]
    [InlineData(1500, 1600, 1497, 1599)]
    public void Find_CacheOn_EqualsCacheOff_NoMultiControl(int sx, int sy, int gx, int gy)
    {
        var map = Map.Maps[MapId];
        var walker = new WalkerStub();
        map.GetAverageZ(sx, sy, out _, out var sz, out _);
        var start = new Point3D(sx, sy, (sbyte)sz);
        var goal = new Point3D(gx, gy, (sbyte)sz);
        walker.MoveToWorld(start, map);

        try
        {
            var on = FindWithFlag(walker, map, start, goal, cacheOn: true);
            var off = FindWithFlag(walker, map, start, goal, cacheOn: false);
            Assert.Equal(off == null, on == null);
            if (on != null)
            {
                Assert.Equal(off, on);
            }
        }
        finally
        {
            walker.Delete();
        }
    }

    [Fact]
    public void Find_CacheOn_EqualsCacheOff_RoutesAroundHouse()
    {
        var map = Map.Maps[MapId];
        Assert.NotNull(map);

        // Open area at (1480,1620,z=20): paths exist in all 8 directions (probe confirmed).
        // N-S route: start=(1480,1630) goal=(1480,1610). House at (1480,1620) on direct line.
        // Detour exists E (~1488+) and W (~1472-). Both cache-on and cache-off must agree.
        const int hx = 1480, hy = 1620;
        const int sx = 1480, sy = 1630;
        const int gx = 1480, gy = 1610;

        map.GetAverageZ(hx, hy, out _, out var hz, out _);
        var multi = new TestMulti(0x74);
        multi.MoveToWorld(new Point3D(hx, hy, (sbyte)hz), map);

        var walker = new WalkerStub();
        map.GetAverageZ(sx, sy, out _, out var sz, out _);
        map.GetAverageZ(gx, gy, out _, out var gz, out _);
        var start = new Point3D(sx, sy, (sbyte)sz);
        var goal = new Point3D(gx, gy, (sbyte)gz);
        walker.MoveToWorld(start, map);

        try
        {
            var on = FindWithFlag(walker, map, start, goal, cacheOn: true);
            var off = FindWithFlag(walker, map, start, goal, cacheOn: false);

            // Non-vacuity: a real around-the-house route must exist on BOTH sides.
            Assert.NotNull(off);
            Assert.NotNull(on);
            // The invariant: cache and slow path agree on that route.
            Assert.Equal(off, on);

            _output.WriteLine($"around-house: {on.Length} steps, cache==slow");
        }
        finally
        {
            walker.Delete();
            multi.Delete();
        }
    }
}
