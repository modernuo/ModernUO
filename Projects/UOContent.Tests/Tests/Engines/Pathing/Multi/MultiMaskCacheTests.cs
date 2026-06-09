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

    [Fact]
    public void IsInteriorLocalCell_TrueDeepInside_FalseAtEdge()
    {
        var multi = new TestMulti(GuildHouseId);
        try
        {
            var mcl = multi.Components;

            var foundInterior = false;
            var foundEdge = false;
            for (var ly = 0; ly < mcl.Height && (!foundInterior || !foundEdge); ly++)
            {
                for (var lx = 0; lx < mcl.Width; lx++)
                {
                    if (mcl.Tiles[lx][ly].Length == 0)
                    {
                        continue;
                    }
                    if (MultiMaskCache.IsInteriorLocalCell(mcl, lx, ly))
                    {
                        foundInterior = true;
                    }
                    else
                    {
                        foundEdge = true;
                    }
                }
            }

            Assert.True(foundInterior, "a guild house must have at least one interior cell");
            Assert.True(foundEdge, "a guild house must have at least one edge/perimeter cell");
        }
        finally
        {
            multi.Delete();
        }
    }

    [Fact]
    public void LocalWorldZ_RoundTrips_AndPreservesMasks()
    {
        var world = new StepMask(
            walkMask: 0b1010_1010, wetMask: 0b0101_0101,
            walkZN: 10, walkZNE: 11, walkZE: 12, walkZSE: 13, walkZS: 14, walkZSW: 15, walkZW: 16, walkZNW: 17,
            swimZN: -1, swimZNE: -2, swimZE: -3, swimZSE: -4, swimZS: -5, swimZSW: -6, swimZW: -7, swimZNW: -8
        );
        const int multiZ = 7;

        Assert.True(MultiMaskCache.TryToLocalZ(world, multiZ, out var local));
        var back = MultiMaskCache.ToWorldZ(local, multiZ);

        Assert.Equal(world.WalkMask, back.WalkMask);
        Assert.Equal(world.WetMask, back.WetMask);
        for (var d = 0; d < 8; d++)
        {
            Assert.Equal(world.GetWalkZ((Direction)d), back.GetWalkZ((Direction)d));
            Assert.Equal(world.GetSwimZ((Direction)d), back.GetSwimZ((Direction)d));
        }
    }

    [Fact]
    public void TryToLocalZ_RejectsOverflow()
    {
        var world = new StepMask(
            walkMask: 0xFF, wetMask: 0,
            walkZN: 100, walkZNE: 0, walkZE: 0, walkZSE: 0, walkZS: 0, walkZSW: 0, walkZW: 0, walkZNW: 0,
            swimZN: 0, swimZNE: 0, swimZE: 0, swimZSE: 0, swimZS: 0, swimZSW: 0, swimZW: 0, swimZNW: 0
        );
        Assert.False(MultiMaskCache.TryToLocalZ(world, -100, out _));
    }
}
