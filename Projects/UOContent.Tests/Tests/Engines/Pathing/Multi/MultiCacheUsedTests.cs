using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;
using Server.Systems.FeatureFlags;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class MultiCacheUsedTests
{
    private const int MapId = 1;            // Trammel
    private const int GuildHouseId = 0x74;

    [Fact]
    public void PathNearMulti_IncrementsMultiLocalHits()
    {
        var map = Map.Maps[MapId];
        map.GetAverageZ(1480, 1620, out _, out var z, out _);
        var houseLoc = new Point3D(1480, 1620, (sbyte)z);
        var multi = new TestMulti(GuildHouseId);

        var cacheWas = ContentFeatureFlags.BitmapPathfindingCache;
        ContentFeatureFlags.BitmapPathfindingCache = true;

        var mover = MultiTestSupport.GetWalkerOracle(map, new Point3D(1480, 1630, (sbyte)z));
        try
        {
            multi.MoveToWorld(houseLoc, map);
            StepCache.Instance.Clear();

            var before = StepCache.Instance.GetStats().MultiLocalHits;

            // Start outside, goal on the far side, forcing expansion through the multi's halo.
            var start = new Point3D(1480, 1630, (sbyte)z);
            var goal = new Point3D(1480, 1610, (sbyte)z);
            var path = BitmapAStarAlgorithm.Instance.Find(mover, map, start, goal);

            var after = StepCache.Instance.GetStats().MultiLocalHits;

            Assert.NotNull(path);
            Assert.True(after > before, $"expected MultiLocalHits to increase, before={before} after={after}");
        }
        finally
        {
            mover.Delete();
            multi.Delete();
            ContentFeatureFlags.BitmapPathfindingCache = cacheWas;
        }
    }
}
