using Server.Engines.Pathing.Cache;
using Server.Items;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class MultiSplitRoutingTests
{
    private readonly ITestOutputHelper _output;

    public MultiSplitRoutingTests(ITestOutputHelper output) => _output = output;

    // Open plain on Trammel used by existing pathfinding tests; room for a 7x7 house.
    private const int MapId = 1;
    private const int PlaceX = 1500;
    private const int PlaceY = 1600;

    [Fact]
    public void PlacedMulti_RoutesFootprintAndHalo_ToFallthroughMulti()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        Assert.NotNull(map);

        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);
        var multi = new TestMulti(0x74); // GuildHouse footprint
        multi.MoveToWorld(new Point3D(PlaceX, PlaceY, (sbyte)z), map);

        try
        {
            var sector = map.GetRealSector(PlaceX >> 4, PlaceY >> 4);
            Assert.True(sector.HasMultis, "placing a multi must set Sector.HasMultis");

            var cells = MultiArt.FootprintWithHalo(multi);
            var checkedCovered = 0;
            foreach (var c in cells)
            {
                if (c.X < 0 || c.Y < 0 || c.X >= map.Width || c.Y >= map.Height)
                {
                    continue;
                }
                var mask = StepCache.Instance.TryGetMask(map, c.X, c.Y, (sbyte)z);
                Assert.Equal(CacheHitKind.Fallthrough_Multi, mask.HitKind);
                checkedCovered++;
            }

            Assert.True(checkedCovered > 0, "non-vacuity: expected at least one covered cell");
            _output.WriteLine($"covered/halo cells routed to fallthrough: {checkedCovered}");
        }
        finally
        {
            multi.Delete();
        }
    }

    [Fact]
    public void CleanMap_AfterMultiRemoved_ServesStaticHitAgain()
    {
        StepCache.Instance.Clear();
        var prevThreshold = StepCache.Instance.MissPromotionThreshold;
        StepCache.Instance.MissPromotionThreshold = 1; // build on first touch
        var map = Map.Maps[MapId];
        Assert.NotNull(map);

        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);

        var multi = new TestMulti(0x74);
        multi.MoveToWorld(new Point3D(PlaceX, PlaceY, (sbyte)z), map);
        var sector = map.GetRealSector(PlaceX >> 4, PlaceY >> 4);
        Assert.True(sector.HasMultis, "placing a multi must set Sector.HasMultis");

        try
        {
            multi.Delete();
            Assert.False(sector.HasMultis, "removing the only multi must clear HasMultis");

            // A cell that is NOT a static-fallthrough kind (e.g. off-map / multi) should now be
            // eligible for a real static answer. Use the placement center, which is open plain.
            var mask = StepCache.Instance.TryGetMask(map, PlaceX, PlaceY, (sbyte)z);
            Assert.True(mask.IsHit, $"expected a real static answer after removal, got {mask.HitKind}");
            _output.WriteLine($"post-removal hitKind at center: {mask.HitKind}");
        }
        finally
        {
            StepCache.Instance.MissPromotionThreshold = prevThreshold;
        }
    }
}
