using Server.Engines.Pathing.Cache;
using Server.Items;
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

    [Fact]
    public void TerrainTopBelow_TrueWhenFloorWellAboveGround()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        map.GetAverageZ(PlaceX, PlaceY, out _, out var ground, out _);

        // Floor far above ground → terrain is below → guard passes.
        Assert.True(MultiMaskCache.TerrainTopBelow(map, PlaceX, PlaceY, (sbyte)(ground + 50)));
        // Floor at/below ground → terrain reaches the envelope → guard fails.
        Assert.False(MultiMaskCache.TerrainTopBelow(map, PlaceX, PlaceY, (sbyte)(ground - 50)));
    }

    [Fact]
    public void PathThroughHouseInterior_IncrementsMultiMaskCacheHits()
    {
        // (PlaceX,PlaceY)=(1480,1620) is cluttered (footprint overlaps tall map statics → dirty), so it
        // would never serve the interior cache under the footprint-clean gate. Use a known flat/clear
        // spot so a house there is footprint-clean and its interior cells serve from the cache.
        const int CleanX = 1560;
        const int CleanY = 1616;

        var map = Map.Maps[MapId];
        map.GetAverageZ(CleanX, CleanY, out _, out var z, out _);
        var houseLoc = new Point3D(CleanX, CleanY, (sbyte)z);
        var multi = new TestMulti(GuildHouseId);
        var cacheWas = Server.Systems.FeatureFlags.ContentFeatureFlags.BitmapPathfindingCache;
        Server.Systems.FeatureFlags.ContentFeatureFlags.BitmapPathfindingCache = true;
        var mover = MultiTestSupport.GetWalkerOracle(map, new Point3D(CleanX, CleanY + 10, (sbyte)z));
        try
        {
            multi.MoveToWorld(houseLoc, map);
            Assert.True(MultiMaskCache.ComputeFootprintClean(map, multi),
                "precondition: (1560,1616) must be footprint-clean for the cache to serve");
            StepCache.Instance.Clear();

            var start = new Point3D(CleanX, CleanY + 10, (sbyte)z);
            var goal = new Point3D(CleanX, CleanY - 10, (sbyte)z);
            // First pass builds the interior cache (live synth); second pass should hit it.
            Server.PathAlgorithms.BitmapAStarAlgorithm.Instance.Find(mover, map, start, goal);

            var before = StepCache.Instance.GetStats().MultiMaskCacheHits;
            Server.PathAlgorithms.BitmapAStarAlgorithm.Instance.Find(mover, map, start, goal);
            var after = StepCache.Instance.GetStats().MultiMaskCacheHits;

            Assert.True(after > before, $"expected MultiMaskCacheHits to increase, before={before} after={after}");
        }
        finally
        {
            mover.Delete();
            multi.Delete();
            Server.Systems.FeatureFlags.ContentFeatureFlags.BitmapPathfindingCache = cacheWas;
        }
    }

    [Fact]
    public void ClutteredHouse_DoesNotServeCache_ButStillPaths()
    {
        var map = Map.Maps[MapId];
        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);
        var multi = new TestMulti(GuildHouseId);
        var cacheWas = Server.Systems.FeatureFlags.ContentFeatureFlags.BitmapPathfindingCache;
        Server.Systems.FeatureFlags.ContentFeatureFlags.BitmapPathfindingCache = true;
        var mover = MultiTestSupport.GetWalkerOracle(map, new Point3D(PlaceX, PlaceY + 10, (sbyte)z));
        try
        {
            // (1480,1620) overlaps tall map statics → footprint dirty → cache must NOT serve.
            multi.MoveToWorld(new Point3D(PlaceX, PlaceY, (sbyte)z), map);
            Assert.False(MultiMaskCache.ComputeFootprintClean(map, multi),
                "precondition: (1480,1620) must be footprint-dirty for this test to be meaningful");
            StepCache.Instance.Clear();

            var start = new Point3D(PlaceX, PlaceY + 10, (sbyte)z);
            var goal = new Point3D(PlaceX, PlaceY - 10, (sbyte)z);
            Server.PathAlgorithms.BitmapAStarAlgorithm.Instance.Find(mover, map, start, goal); // warm
            var before = StepCache.Instance.GetStats().MultiMaskCacheHits;
            var path = Server.PathAlgorithms.BitmapAStarAlgorithm.Instance.Find(mover, map, start, goal);
            var after = StepCache.Instance.GetStats().MultiMaskCacheHits;

            Assert.NotNull(path);          // pathfinding still works (degraded to live-synth)
            Assert.Equal(before, after);   // dirty footprint → zero cache serves
        }
        finally
        {
            mover.Delete();
            multi.Delete();
            Server.Systems.FeatureFlags.ContentFeatureFlags.BitmapPathfindingCache = cacheWas;
        }
    }

    [Fact]
    public void PathInteriorCacheState_ResetsOnMove()
    {
        var map = Map.Maps[MapId];
        var multi = new TestMulti(GuildHouseId);
        try
        {
            multi.MoveToWorld(new Point3D(PlaceX, PlaceY, 0), map);
            multi.PathInteriorCacheState = MultiInteriorCacheState.Clean;

            // A move changes the footprint's world terrain, so the gate must reset to Unknown and
            // recompute on next use. (BaseMulti.OnLocationChange does this; subclasses like BaseHouse
            // and BaseBoat must call base for it to fire — this pins that contract.)
            multi.MoveToWorld(new Point3D(PlaceX + 8, PlaceY + 8, 0), map);
            Assert.Equal(MultiInteriorCacheState.Unknown, multi.PathInteriorCacheState);
        }
        finally
        {
            multi.Delete();
        }
    }

    [Fact]
    public void PathInteriorCacheState_ResetsOnItemIdChange()
    {
        var map = Map.Maps[MapId];
        var multi = new TestMulti(GuildHouseId);
        try
        {
            multi.MoveToWorld(new Point3D(PlaceX, PlaceY, 0), map);
            multi.PathInteriorCacheState = MultiInteriorCacheState.Clean;

            // Changing ItemID swaps the footprint (e.g. a boat changing heading), so the gate must
            // reset to recompute cleanliness for the new shape.
            multi.ItemID = 0x7A; // Tower
            Assert.Equal(MultiInteriorCacheState.Unknown, multi.PathInteriorCacheState);
        }
        finally
        {
            multi.Delete();
        }
    }

    [Fact]
    public void ComputeFootprintClean_TrueAtNormalPlacement_FalseWhenSunk()
    {
        // (PlaceX,PlaceY) is a cluttered spot whose footprint overlaps tall map statics, so a guild
        // house there is never footprint-clean. Use a known flat/clear spot for the clean assertion.
        const int CleanX = 1560;
        const int CleanY = 1616;

        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        map.GetAverageZ(CleanX, CleanY, out _, out var ground, out _);

        var normal = new TestMulti(GuildHouseId);
        var sunk = new TestMulti(GuildHouseId);
        try
        {
            normal.MoveToWorld(new Point3D(CleanX, CleanY, (sbyte)ground), map);
            Assert.True(MultiMaskCache.ComputeFootprintClean(map, normal));

            sunk.MoveToWorld(new Point3D(CleanX + 60, CleanY, (sbyte)(ground - 40)), map);
            Assert.False(MultiMaskCache.ComputeFootprintClean(map, sunk));
        }
        finally
        {
            normal.Delete();
            sunk.Delete();
        }
    }
}
