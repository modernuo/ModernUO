using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class MultiMaskCacheTests
{
    private const int MapId = 1;
    private const int GuildHouseId = 0x74;
    private const int PlaceX = 1480;
    private const int PlaceY = 1620;

    [Fact]
    public void TryResolveCoveringMulti_FindsPlacedMulti_AndLocalIndices()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);
        var loc = new Point3D(PlaceX, PlaceY, (sbyte)z);
        var multi = new TestMulti(GuildHouseId);
        try
        {
            multi.MoveToWorld(loc, map);

            Assert.True(MultiMaskCache.TryResolveCoveringMulti(map, PlaceX, PlaceY, out var found, out var lx, out var ly));
            Assert.Same(multi, found);
            var mcl = multi.Components;
            Assert.Equal(PlaceX - multi.X - mcl.Min.X, lx);
            Assert.Equal(PlaceY - multi.Y - mcl.Min.Y, ly);

            Assert.False(MultiMaskCache.TryResolveCoveringMulti(map, PlaceX + 200, PlaceY + 200, out _, out _, out _));
        }
        finally
        {
            multi.Delete();
        }
    }
}
