using System;
using Server.Items;
using Xunit;

namespace Server.Tests.Tests.Maps;

[Collection("Sequential Server Tests")]
public class CanSpawnMobileTests
{
    /// <summary>
    /// Checks if TileData was loaded (requires client files).
    /// When running from XUnit without client files, TileData.ItemTable will have default values.
    /// </summary>
    private static bool HasTileData => TileData.MaxItemValue > 0;

    #region Basic Tests (No Client Files Required)

    [Fact]
    public void CanSpawnMobile_InternalMapReturnsFalse()
    {
        var result = Map.Internal.CanSpawnMobile(100, 100, -128, 127, false, false, out var spawnZ);

        Assert.False(result);
        Assert.Equal(0, spawnZ);
    }

    [Fact]
    public void CanSpawnMobile_InvalidCoordinatesReturnsFalse()
    {
        var map = Map.Felucca;

        Assert.False(map.CanSpawnMobile(-1, 100, -128, 127, false, false, out _));
        Assert.False(map.CanSpawnMobile(100, -1, -128, 127, false, false, out _));
        Assert.False(map.CanSpawnMobile(map.Width + 1, 100, -128, 127, false, false, out _));
        Assert.False(map.CanSpawnMobile(100, map.Height + 1, -128, 127, false, false, out _));
    }

    [Fact]
    public void CanSpawnMobile_EmptyLocationWithLandReturnsTrue()
    {
        var map = Map.Felucca;
        // Empty location with no statics - should find land surface at avgZ
        var result = map.CanSpawnMobile(100, 100, -128, 127, false, false, out var spawnZ);

        // Without client files, land is at Z=0 by default
        Assert.True(result);
        Assert.Equal(0, spawnZ);
    }

    [Fact]
    public void CanSpawnMobile_ZRangeFiltersSurfaces()
    {
        var map = Map.Felucca;
        // Land is at Z=0, so a Z range that excludes 0 should fail
        var result = map.CanSpawnMobile(100, 100, 10, 50, false, false, out var spawnZ);

        Assert.False(result);
    }

    [Fact]
    public void CanSpawnMobile_CantWalkSkipsLandSurface()
    {
        var map = Map.Felucca;
        // cantWalk=true means the mob can only swim, so land shouldn't be valid
        var result = map.CanSpawnMobile(100, 100, -128, 127, false, true, out var spawnZ);

        Assert.False(result);
    }

    #endregion

    #region Mobile and Item Blocking Tests (No Client Files Required)

    [Fact]
    public void CanSpawnMobile_MobileBlocksSpawn()
    {
        var map = Map.Felucca;
        var x = 1500;
        var y = 1500;

        Mobile blockingMobile = null;
        try
        {
            // Create a mobile at this location
            blockingMobile = new TestMobile();
            blockingMobile.MoveToWorld(new Point3D(x, y, 0), map);

            // The mobile should block spawning at its location and Z
            var result = map.CanSpawnMobile(x, y, -16, 16, false, false, out var spawnZ);

            // Land at Z=0 is blocked by mobile at Z=0
            Assert.False(result);
        }
        finally
        {
            blockingMobile?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_HiddenGMDoesNotBlock()
    {
        var map = Map.Felucca;
        var x = 1600;
        var y = 1600;

        Mobile gm = null;
        try
        {
            // Create a hidden GM mobile
            gm = new TestMobile
            {
                AccessLevel = AccessLevel.GameMaster,
                Hidden = true
            };
            gm.MoveToWorld(new Point3D(x, y, 0), map);

            // Hidden GM should not block spawning
            var result = map.CanSpawnMobile(x, y, -16, 16, false, false, out var spawnZ);

            Assert.True(result);
            Assert.Equal(0, spawnZ);
        }
        finally
        {
            gm?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_MobileAtDifferentZDoesNotBlock()
    {
        var map = Map.Felucca;
        var x = 1700;
        var y = 1700;

        Mobile blockingMobile = null;
        try
        {
            // Create a mobile at Z=50 (far above the land at Z=0)
            blockingMobile = new TestMobile();
            blockingMobile.MoveToWorld(new Point3D(x, y, 50), map);

            // Land at Z=0 should still be valid (mobile at Z=50 doesn't block Z=0)
            var result = map.CanSpawnMobile(x, y, -16, 16, false, false, out var spawnZ);

            Assert.True(result);
            Assert.Equal(0, spawnZ);
        }
        finally
        {
            blockingMobile?.Delete();
        }
    }

    #endregion

    #region Tests Requiring TileData (Client Files)

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_FindsSurfaceOnMulti()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1100;
        var y = 1100;

        TestMulti multi = null;
        try
        {
            // Create a multi with a floor at Z=20
            multi = CreateFloorMulti(map, new Point3D(x, y, 0), floorZ: 20);

            var result = map.CanSpawnMobile(x, y, 15, 30, false, false, out var spawnZ);

            Assert.True(result);
            // Should find the floor surface (lowest valid surface in range)
            Assert.True(spawnZ >= 15 && spawnZ <= 30);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_FindsLowestValidSurface()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1200;
        var y = 1200;

        TestMulti multi = null;
        try
        {
            // Create a multi with floors at Z=10 and Z=30
            multi = CreateTwoFloorMulti(map, new Point3D(x, y, 0), floor1Z: 10, floor2Z: 30);

            var result = map.CanSpawnMobile(x, y, 5, 50, false, false, out var spawnZ);

            Assert.True(result);
            // Should find the lowest floor
            Assert.True(spawnZ <= 15, $"Expected lowest floor around Z=10, got {spawnZ}");
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_ZRangeSelectsCorrectFloor()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1300;
        var y = 1300;

        TestMulti multi = null;
        try
        {
            // Create a multi with floors at Z=10 and Z=30
            multi = CreateTwoFloorMulti(map, new Point3D(x, y, 0), floor1Z: 10, floor2Z: 30);

            // Request Z range that only includes the second floor
            var result = map.CanSpawnMobile(x, y, 25, 50, false, false, out var spawnZ);

            Assert.True(result);
            // Should find the second floor
            Assert.True(spawnZ >= 25 && spawnZ <= 35, $"Expected second floor around Z=30, got {spawnZ}");
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_LowCeilingBlocksSpawn()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1400;
        var y = 1400;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and ceiling at Z=28 (only 8 units clearance)
            multi = CreateFloorWithLowCeilingMulti(map, new Point3D(x, y, 0), floorZ: 20, ceilingZ: 28);

            // The low ceiling should block spawning (need 16 units clearance for mobiles)
            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out var spawnZ);

            Assert.False(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_HighCeilingAllowsSpawn()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1450;
        var y = 1450;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and ceiling at Z=50 (30 units clearance)
            multi = CreateFloorWithHighCeilingMulti(map, new Point3D(x, y, 0), floorZ: 20, ceilingZ: 50);

            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out var spawnZ);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_BlockerOutsideZRangeIgnored()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1475;
        var y = 1475;

        TestMulti multi = null;
        try
        {
            // Create a multi with floor at Z=20 and a blocker at Z=100 (outside our search range)
            multi = CreateFloorWithDistantBlockerMulti(map, new Point3D(x, y, 0), floorZ: 20, blockerZ: 100);

            // The blocker at Z=100 shouldn't affect spawning in range 15-35
            var result = map.CanSpawnMobile(x, y, 15, 35, false, false, out var spawnZ);

            Assert.True(result);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_WaterSurfaceForSwimmingMob()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1550;
        var y = 1550;

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

    [Fact(Skip = "Requires client files - TileData must be loaded")]
    public void CanSpawnMobile_WorldItemSurfaceWorks()
    {
        if (!HasTileData)
        {
            return;
        }

        var map = Map.Felucca;
        var x = 1650;
        var y = 1650;

        Item surfaceItem = null;
        try
        {
            // Create a non-movable item that acts as a surface (e.g., a table)
            // ItemID 0x0B60 is a table in standard UO
            surfaceItem = new Item(0x0B60)
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

    #endregion

    #region Client-File-Dependent Tests (Real Map Data)

    [Fact(Skip = "Requires client files - run manually with MODERNUO_HAS_CLIENT_FILES=true")]
    public void CanSpawnMobile_MalasBuilding_FindsGroundFloor()
    {
        if (!HasClientFiles)
        {
            return;
        }

        var map = Map.Malas;
        // Building near [991, 519, -50] in Malas
        var x = 991;
        var y = 519;

        var result = map.CanSpawnMobile(x, y, -60, -40, false, false, out var spawnZ);

        Assert.True(result);
        // Should find the ground floor around Z=-50
        Assert.True(spawnZ >= -60 && spawnZ <= -40, $"Expected ground floor around -50, got {spawnZ}");
    }

    [Fact(Skip = "Requires client files - run manually with MODERNUO_HAS_CLIENT_FILES=true")]
    public void CanSpawnMobile_MalasBuilding_FindsSecondFloor()
    {
        if (!HasClientFiles)
        {
            return;
        }

        var map = Map.Malas;
        // Building near [991, 519] in Malas - check for second floor
        var x = 991;
        var y = 519;

        // Search for surfaces above ground floor
        var result = map.CanSpawnMobile(x, y, -30, 0, false, false, out var spawnZ);

        Assert.True(result);
        // Should find the second floor
        Assert.True(spawnZ >= -30 && spawnZ <= 0, $"Expected second floor, got {spawnZ}");
    }

    [Fact(Skip = "Requires client files - run manually with MODERNUO_HAS_CLIENT_FILES=true")]
    public void CanSpawnMobile_MalasBuilding_LowestFloorPreferred()
    {
        if (!HasClientFiles)
        {
            return;
        }

        var map = Map.Malas;
        // Building with multiple floors
        var x = 991;
        var y = 519;

        // Search for any valid surface
        var result = map.CanSpawnMobile(x, y, -60, 0, false, false, out var spawnZ);

        Assert.True(result);
        // Should find the lowest floor (ground floor around -50)
        Assert.True(spawnZ <= -40, $"Expected lowest floor around -50, got {spawnZ}");
    }

    #endregion

    #region Helper Methods and Classes

    /// <summary>
    /// Tests that require actual client files. These should be skipped on CI/CD.
    /// Set environment variable MODERNUO_HAS_CLIENT_FILES=true to run these.
    /// </summary>
    private static bool HasClientFiles =>
        Environment.GetEnvironmentVariable("MODERNUO_HAS_CLIENT_FILES") == "true";

    private static TestMulti CreateFloorMulti(Map map, Point3D location, int floorZ)
    {
        var multi = new TestMulti(CreateFloorComponents(floorZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private static TestMulti CreateTwoFloorMulti(Map map, Point3D location, int floor1Z, int floor2Z)
    {
        var multi = new TestMulti(CreateTwoFloorComponents(floor1Z, floor2Z));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private static TestMulti CreateFloorWithLowCeilingMulti(Map map, Point3D location, int floorZ, int ceilingZ)
    {
        var multi = new TestMulti(CreateFloorWithCeilingComponents(floorZ, ceilingZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private static TestMulti CreateFloorWithHighCeilingMulti(Map map, Point3D location, int floorZ, int ceilingZ)
    {
        var multi = new TestMulti(CreateFloorWithCeilingComponents(floorZ, ceilingZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private static TestMulti CreateFloorWithDistantBlockerMulti(Map map, Point3D location, int floorZ, int blockerZ)
    {
        var multi = new TestMulti(CreateFloorWithCeilingComponents(floorZ, blockerZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private static TestMulti CreateWaterMulti(Map map, Point3D location, int waterZ)
    {
        var multi = new TestMulti(CreateWaterComponents(waterZ));
        multi.MoveToWorld(location, map);
        return multi;
    }

    // Floor tile ID that has Surface flag (stone floor)
    private const ushort FloorTileId = 0x0519; // Stone floor

    // Impassable tile ID for ceiling/blocking
    private const ushort CeilingTileId = 0x0001; // Typically impassable

    // Water tile ID
    private const ushort WaterTileId = 0x346E; // Water tile

    private static MultiComponentList CreateFloorComponents(int floorZ)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(FloorTileId, 0, 0, (short)floorZ, TileFlag.Surface)
        ]);
    }

    private static MultiComponentList CreateTwoFloorComponents(int floor1Z, int floor2Z)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(FloorTileId, 0, 0, (short)floor1Z, TileFlag.Surface),
            new MultiTileEntry(FloorTileId, 0, 0, (short)floor2Z, TileFlag.Surface)
        ]);
    }

    private static MultiComponentList CreateFloorWithCeilingComponents(int floorZ, int ceilingZ)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(FloorTileId, 0, 0, (short)floorZ, TileFlag.Surface),
            new MultiTileEntry(CeilingTileId, 0, 0, (short)ceilingZ, TileFlag.Impassable)
        ]);
    }

    private static MultiComponentList CreateWaterComponents(int waterZ)
    {
        return new MultiComponentList(
        [
            new MultiTileEntry(WaterTileId, 0, 0, (short)waterZ, TileFlag.Wet)
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

    private class TestMobile : Mobile
    {
        public TestMobile()
        {
            // Initialize as a visible player-level mobile
            AccessLevel = AccessLevel.Player;
            Hidden = false;
        }
    }

    #endregion
}
