using Server.Items;
using Server.Tests.Terrain;
using Xunit;

namespace Server.Tests.Maps;

/// <summary>
/// Tests for CanSpawnMobile over tile flags, using tiles from the generated test kit.
/// </summary>
[Collection("Sequential Server Tests")]
public class CanSpawnMobileTileDataTests
{
    [Fact]
    public void CanSpawnMobile_FindsLowestValidSurface()
    {
        var map = Map.Felucca;
        const int x = 1200;
        const int y = 1200;

        TestMulti multi = null;
        try
        {
            // Create a multi with floors at Z=10 and Z=50
            // Need 16+ units between floors for mobile clearance
            multi = CreateTwoFloorMulti(map, new Point3D(x, y, 0), floor1Z: 10, floor2Z: 50);

            var result = map.CanSpawnMobile(x, y, 5, 60, false, false, out var spawnZ);

            Assert.True(result);
            // Should find the lowest floor (around Z=10 + tile height)
            Assert.True(spawnZ <= 20, $"Expected lowest floor around Z=10-15, got {spawnZ}");
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_ZRangeSelectsCorrectFloor()
    {
        var map = Map.Felucca;
        const int x = 1300;
        const int y = 1300;

        TestMulti multi = null;
        try
        {
            // Create a multi with floors at Z=10 and Z=50
            // Need 16+ units between floors for mobile clearance
            multi = CreateTwoFloorMulti(map, new Point3D(x, y, 0), floor1Z: 10, floor2Z: 50);

            // Request Z range that only includes the second floor
            var result = map.CanSpawnMobile(x, y, 45, 60, false, false, out var spawnZ);

            Assert.True(result);
            Assert.True(spawnZ >= 45 && spawnZ <= 60, $"Expected second floor around Z=50, got {spawnZ}");
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_LowCeilingBlocksSpawn()
    {
        var map = Map.Felucca;
        const int x = 1400;
        const int y = 1400;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and ceiling at Z=28 (only 8 units clearance)
            // Mobile height is 16, so this should block
            multi = CreateFloorWithCeilingMulti(map, new Point3D(x, y, 0), floorZ: 20, ceilingZ: 28);

            // The low ceiling should block spawning (need 16 units clearance for mobiles)
            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out _);

            Assert.False(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_HighCeilingAllowsSpawn()
    {
        var map = Map.Felucca;
        const int x = 1450;
        const int y = 1450;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and ceiling at Z=50 (30 units clearance)
            multi = CreateFloorWithCeilingMulti(map, new Point3D(x, y, 0), floorZ: 20, ceilingZ: 50);

            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out _);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_BlockerOutsideZRangeIgnored()
    {
        var map = Map.Felucca;
        const int x = 1475;
        const int y = 1475;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and a blocker at Z=100 (outside our search range)
            multi = CreateFloorWithCeilingMulti(map, new Point3D(x, y, 0), floorZ: 20, ceilingZ: 100);

            // The blocker at Z=100 shouldn't affect spawning in range 15-35
            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out _);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_WaterSurfaceForSwimmingMob()
    {
        var map = Map.Felucca;
        const int x = 1550;
        const int y = 1550;

        TestMulti multi = null;
        try
        {
            // Create a multi with a water tile at Z=0
            multi = CreateWaterMulti(map, new Point3D(x, y, 0), waterZ: 0);

            // canSwim=true should find the water surface
            var result = map.CanSpawnMobile(x, y, -10, 10, true, false, out _);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_WorldItemSurfaceWorks()
    {
        var map = Map.Felucca;
        const int x = 1650;
        const int y = 1650;

        Item surfaceItem = null;
        try
        {
            // Create a non-movable item that acts as a surface using a real surface tile ID
            surfaceItem = new Item(ServerFixture.SurfaceTileId)
            {
                Movable = false
            };
            surfaceItem.MoveToWorld(new Point3D(x, y, 10), map);

            var result = map.CanSpawnMobile(x, y, 5, 30, false, false, out _);

            // Should find either land at Z=0 or the item surface, whichever is lowest
            Assert.True(result);
        }
        finally
        {
            surfaceItem?.Delete();
        }
    }

    // CantWalk alone makes a stationary land mobile; only a swimmer that can't walk is water-only.
    // The sea cell is far enough out that every corner is sea, so its average Z is the sea's.
    private static Point2D SeaCell(Point3D grass) => new(grass.X - 8, grass.Y);

    [Fact]
    public void CanSpawnMobile_CantWalkNonSwimmer_FindsLandNotWater()
    {
        Prefabs.Coastline.AssertPresent();
        var map = Map.Felucca;
        var grass = Prefabs.Coastline.Anchor;
        var sea = SeaCell(grass);

        Assert.True(map.CanSpawnMobile(grass.X, grass.Y, -128, 127, false, true, out var landZ));
        Assert.Equal(0, landZ);
        Assert.False(map.CanSpawnMobile(sea.X, sea.Y, -128, 127, false, true, out _));
    }

    [Fact]
    public void CanSpawnMobile_CantWalkSwimmer_IsWaterOnly()
    {
        Prefabs.Coastline.AssertPresent();
        var map = Map.Felucca;
        var grass = Prefabs.Coastline.Anchor;
        var sea = SeaCell(grass);

        Assert.False(map.CanSpawnMobile(grass.X, grass.Y, -128, 127, true, true, out _));
        Assert.True(map.CanSpawnMobile(sea.X, sea.Y, -128, 127, true, true, out var waterZ));
        Assert.Equal(-5, waterZ);
    }

    [Fact]
    public void CanSpawnMobile_SwimmingWalker_FindsLandAndWater()
    {
        Prefabs.Coastline.AssertPresent();
        var map = Map.Felucca;
        var grass = Prefabs.Coastline.Anchor;
        var sea = SeaCell(grass);

        Assert.True(map.CanSpawnMobile(grass.X, grass.Y, -128, 127, true, false, out var landZ));
        Assert.Equal(0, landZ);
        Assert.True(map.CanSpawnMobile(sea.X, sea.Y, -128, 127, true, false, out var waterZ));
        Assert.Equal(-5, waterZ);
    }

    [Fact]
    public void CanSpawnMobile_Walker_FindsLandNotWater()
    {
        Prefabs.Coastline.AssertPresent();
        var map = Map.Felucca;
        var grass = Prefabs.Coastline.Anchor;
        var sea = SeaCell(grass);

        Assert.True(map.CanSpawnMobile(grass.X, grass.Y, -128, 127, false, false, out var landZ));
        Assert.Equal(0, landZ);
        Assert.False(map.CanSpawnMobile(sea.X, sea.Y, -128, 127, false, false, out _));
    }

    [Fact]
    public void CanSpawnMobile_CantWalkNonSwimmer_StandsOnStaticFloor()
    {
        var map = Map.Felucca;
        const int x = 1700;
        const int y = 1700;

        using var terrain = TestTerrain.Scope();
        TestTerrain.AddStatic(x, y, TestTiles.Floor, 10);

        // The Z range leaves out the land at Z 0, so only the floor can be found.
        Assert.True(map.CanSpawnMobile(x, y, 5, 30, false, true, out var spawnZ));
        Assert.Equal(10, spawnZ);
    }

    [Fact]
    public void CanSpawnMobile_CantWalkNonSwimmer_StandsOnMultiFloor()
    {
        var map = Map.Felucca;
        const int x = 1725;
        const int y = 1725;

        TestMulti multi = null;
        try
        {
            multi = CreateFloorMulti(map, new Point3D(x, y, 0), floorZ: 10);

            Assert.True(map.CanSpawnMobile(x, y, 5, 30, false, true, out var spawnZ));
            Assert.Equal(10, spawnZ);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_CantWalkNonSwimmer_StandsOnWorldItem()
    {
        var map = Map.Felucca;
        const int x = 1750;
        const int y = 1750;

        Item surfaceItem = null;
        try
        {
            surfaceItem = new Item(ServerFixture.SurfaceTileId)
            {
                Movable = false
            };
            surfaceItem.MoveToWorld(new Point3D(x, y, 10), map);

            Assert.True(map.CanSpawnMobile(x, y, 5, 30, false, true, out var spawnZ));
            Assert.Equal(10, spawnZ);
        }
        finally
        {
            surfaceItem?.Delete();
        }
    }

    private TestMulti CreateFloorMulti(Map map, Point3D location, int floorZ)
    {
        var multi = new TestMulti(CreateFloorComponents(floorZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private TestMulti CreateTwoFloorMulti(Map map, Point3D location, int floor1Z, int floor2Z)
    {
        var multi = new TestMulti(CreateTwoFloorComponents(floor1Z, floor2Z));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private TestMulti CreateFloorWithCeilingMulti(Map map, Point3D location, int floorZ, int ceilingZ)
    {
        var multi = new TestMulti(CreateFloorWithCeilingComponents(floorZ, ceilingZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private TestMulti CreateWaterMulti(Map map, Point3D location, int waterZ)
    {
        var multi = new TestMulti(CreateWaterComponents(waterZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private MultiComponentList CreateFloorComponents(int floorZ)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(ServerFixture.SurfaceTileId, 0, 0, (short)floorZ, TileFlag.Surface)
        ]);
    }

    private MultiComponentList CreateTwoFloorComponents(int floor1Z, int floor2Z)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(ServerFixture.SurfaceTileId, 0, 0, (short)floor1Z, TileFlag.Surface),
            new MultiTileEntry(ServerFixture.SurfaceTileId, 0, 0, (short)floor2Z, TileFlag.Surface)
        ]);
    }

    private MultiComponentList CreateFloorWithCeilingComponents(int floorZ, int ceilingZ)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(ServerFixture.SurfaceTileId, 0, 0, (short)floorZ, TileFlag.Surface),
            new MultiTileEntry(ServerFixture.ImpassableTileId, 0, 0, (short)ceilingZ, TileFlag.Impassable)
        ]);
    }

    private MultiComponentList CreateWaterComponents(int waterZ)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(ServerFixture.WetTileId, 0, 0, (short)waterZ, TileFlag.Wet)
        ]);
    }
}
