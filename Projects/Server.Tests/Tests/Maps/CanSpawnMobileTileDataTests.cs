using Server.Items;
using Xunit;

namespace Server.Tests.Maps;

/// <summary>
/// Tests for CanSpawnMobile that require TileData (client files) to be loaded.
/// These tests will be skipped if client files are not available.
/// Configure client path via MODERNUO_CLIENT_PATH environment variable or place files at C:\Ultima Online Classic.
/// </summary>
[Collection("Sequential Server Tests")]
public class CanSpawnMobileTileDataTests
{
    private readonly ServerFixture _fixture;

    public CanSpawnMobileTileDataTests(ServerFixture fixture)
    {
        _fixture = fixture;
    }

    private void SkipIfNoTileData()
    {
        Skip.If(!ServerFixture.TileDataLoaded, "TileData not loaded - client files required");
    }

    [SkippableFact]
    public void CanSpawnMobile_FindsSurfaceOnMulti()
    {
        SkipIfNoTileData();

        var map = Map.Felucca;
        const int x = 1100;
        const int y = 1100;

        TestMulti multi = null;
        try
        {
            // Create a multi with a floor at Z=20 using a real surface tile
            multi = CreateFloorMulti(map, new Point3D(x, y, 0), floorZ: 20);

            var result = map.CanSpawnMobile(x, y, 15, 30, false, false, out var spawnZ);

            Assert.True(result);
            Assert.True(spawnZ >= 15 && spawnZ <= 30);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [SkippableFact]
    public void CanSpawnMobile_FindsLowestValidSurface()
    {
        SkipIfNoTileData();

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

    [SkippableFact]
    public void CanSpawnMobile_ZRangeSelectsCorrectFloor()
    {
        SkipIfNoTileData();

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

    [SkippableFact]
    public void CanSpawnMobile_LowCeilingBlocksSpawn()
    {
        SkipIfNoTileData();

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
            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out var spawnZ);

            Assert.False(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [SkippableFact]
    public void CanSpawnMobile_HighCeilingAllowsSpawn()
    {
        SkipIfNoTileData();

        var map = Map.Felucca;
        const int x = 1450;
        const int y = 1450;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and ceiling at Z=50 (30 units clearance)
            multi = CreateFloorWithCeilingMulti(map, new Point3D(x, y, 0), floorZ: 20, ceilingZ: 50);

            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out var spawnZ);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [SkippableFact]
    public void CanSpawnMobile_BlockerOutsideZRangeIgnored()
    {
        SkipIfNoTileData();

        var map = Map.Felucca;
        const int x = 1475;
        const int y = 1475;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and a blocker at Z=100 (outside our search range)
            multi = CreateFloorWithCeilingMulti(map, new Point3D(x, y, 0), floorZ: 20, ceilingZ: 100);

            // The blocker at Z=100 shouldn't affect spawning in range 15-35
            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out var spawnZ);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [SkippableFact]
    public void CanSpawnMobile_WaterSurfaceForSwimmingMob()
    {
        SkipIfNoTileData();

        var map = Map.Felucca;
        const int x = 1550;
        const int y = 1550;

        TestMulti multi = null;
        try
        {
            // Create a multi with a water tile at Z=0
            multi = CreateWaterMulti(map, new Point3D(x, y, 0), waterZ: 0);

            // canSwim=true should find the water surface
            var result = map.CanSpawnMobile(x, y, -10, 10, true, false, out var spawnZ);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [SkippableFact]
    public void CanSpawnMobile_WorldItemSurfaceWorks()
    {
        SkipIfNoTileData();

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

            var result = map.CanSpawnMobile(x, y, 5, 30, false, false, out var spawnZ);

            // Should find either land at Z=0 or the item surface, whichever is lowest
            Assert.True(result);
        }
        finally
        {
            surfaceItem?.Delete();
        }
    }

    [SkippableFact]
    public void CanSpawnMobile_MalasBuilding_FindsGroundFloor()
    {
        SkipIfNoTileData();

        var map = Map.Malas;
        const int x = 991;
        const int y = 519;

        var result = map.CanSpawnMobile(x, y, -60, -40, false, false, out var spawnZ);

        Assert.True(result);
        Assert.True(spawnZ >= -60 && spawnZ <= -40, $"Expected ground floor around -50, got {spawnZ}");
    }

    [SkippableFact]
    public void CanSpawnMobile_MalasBuilding_FindsSecondFloor()
    {
        SkipIfNoTileData();

        var map = Map.Malas;
        const int x = 991;
        const int y = 519;

        var result = map.CanSpawnMobile(x, y, -30, 0, false, false, out var spawnZ);

        Assert.True(result);
        Assert.True(spawnZ >= -30 && spawnZ <= 0, $"Expected second floor, got {spawnZ}");
    }

    [SkippableFact]
    public void CanSpawnMobile_MalasBuilding_LowestFloorPreferred()
    {
        SkipIfNoTileData();

        var map = Map.Malas;
        const int x = 991;
        const int y = 519;

        var result = map.CanSpawnMobile(x, y, -60, 0, false, false, out var spawnZ);

        Assert.True(result);
        Assert.True(spawnZ <= -40, $"Expected lowest floor around -50, got {spawnZ}");
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

    private class TestMulti : BaseMulti
    {
        private readonly MultiComponentList _components;

        public TestMulti(MultiComponentList components) : base(0x1)
        {
            _components = components ?? MultiComponentList.Empty;
        }

        public override MultiComponentList Components => _components;
    }
}
