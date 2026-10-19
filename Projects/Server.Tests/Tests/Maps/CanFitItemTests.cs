using Server.Items;
using Xunit;

namespace Server.Tests.Maps;

/// <summary>
/// Tests for CanFitItem which allows Surface+Impassable tiles (tables, furniture) as valid surfaces.
/// </summary>
[Collection("Sequential Server Tests")]
public class CanFitItemTests
{
    [Fact]
    public void CanFitItem_InternalMapReturnsFalse()
    {
        var result = Map.Internal.CanFitItem(100, 100, 0, 1);

        Assert.False(result);
    }

    [Fact]
    public void CanFitItem_InvalidCoordinatesReturnsFalse()
    {
        var map = Map.Felucca;

        Assert.False(map.CanFitItem(-1, 100, 0, 1));
        Assert.False(map.CanFitItem(100, -1, 0, 1));
        Assert.False(map.CanFitItem(map.Width + 1, 100, 0, 1));
        Assert.False(map.CanFitItem(100, map.Height + 1, 0, 1));
    }

    // The synthetic map's default ground: land ID 0 at Z 0 with no flags, a surface with nothing above it.
    private const int TestLandX = 1500;
    private const int TestLandY = 1600;

    [Fact]
    public void CanFitItem_LandSurfaceReturnsTrue()
    {
        var map = Map.Felucca;
        var avgZ = map.GetAverageZ(TestLandX, TestLandY);

        var result = map.CanFitItem(TestLandX, TestLandY, avgZ, 1);

        Assert.True(result, $"Expected land at Z={avgZ} to be valid surface for items");
    }

    [Fact]
    public void CanFitItem_AboveLandWithNoSurfaceReturnsFalse()
    {
        var map = Map.Felucca;
        var avgZ = map.GetAverageZ(TestLandX, TestLandY);

        // Try to place item 50 units above land with no surface there
        var result = map.CanFitItem(TestLandX, TestLandY, avgZ + 50, 1);

        Assert.False(result, "No surface 50 units above land");
    }

    [Fact]
    public void CanFitItem_SurfaceImpassableMultiIsValidSurface()
    {

        var map = Map.Felucca;
        const int x = 1700;
        const int y = 1700;

        TestMulti multi = null;
        try
        {
            // Get the actual tile height from TileData
            var tileData = TileData.ItemTable[ServerFixture.SurfaceImpassableTileId];
            var tileHeight = tileData.CalcHeight;
            const int tileZ = 10;

            // Create a multi with a Surface+Impassable tile (like a table) at Z=10
            multi = CreateSurfaceImpassableMulti(map, new Point3D(x, y, 0), surfaceZ: tileZ);

            // CanFitItem should treat Surface+Impassable as a valid surface
            var surfaceTop = tileZ + tileHeight;
            var result = map.CanFitItem(x, y, surfaceTop, 1);

            Assert.True(result, $"Surface+Impassable tile should be valid surface for items at Z={surfaceTop}");
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanFitItem_CanFitDoesNotAllowSurfaceImpassable()
    {

        var map = Map.Felucca;
        const int x = 1750;
        const int y = 1750;

        TestMulti multi = null;
        try
        {
            // Get the actual tile height from TileData
            var tileData = TileData.ItemTable[ServerFixture.SurfaceImpassableTileId];
            var tileHeight = tileData.CalcHeight;
            const int tileZ = 10;

            // Create a multi with a Surface+Impassable tile at Z=10
            multi = CreateSurfaceImpassableMulti(map, new Point3D(x, y, 0), surfaceZ: tileZ);

            var surfaceTop = tileZ + tileHeight;

            // Regular CanFit should NOT treat Surface+Impassable as valid (for comparison)
            var canFitResult = map.CanFit(x, y, surfaceTop, 1, requireSurface: true);

            // This test documents the difference between CanFit and CanFitItem
            // CanFit requires surface && !impassable
            Assert.False(canFitResult, "CanFit should NOT allow Surface+Impassable as valid surface");
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void CanFitItem_NonMovableWorldItemSurfaceWorks()
    {
        var map = Map.Felucca;
        const int x = 1800;
        const int y = 1800;

        Item surfaceItem = null;
        try
        {
            // Create a non-movable surface item (like a placed table)
            surfaceItem = new Item(ServerFixture.SurfaceTileId)
            {
                Movable = false
            };
            surfaceItem.MoveToWorld(new Point3D(x, y, 10), map);

            var itemTop = 10 + surfaceItem.ItemData.CalcHeight;
            var result = map.CanFitItem(x, y, itemTop, 1);

            Assert.True(result, $"Non-movable surface item should be valid surface at Z={itemTop}");
        }
        finally
        {
            surfaceItem?.Delete();
        }
    }

    [Fact]
    public void CanFitItem_MovableWorldItemNotValidSurface()
    {
        var map = Map.Felucca;
        const int x = 1850;
        const int y = 1850;

        Item movableItem = null;
        try
        {
            // Raised well clear of the land so the movable item's top is the only candidate surface there
            var itemZ = map.GetAverageZ(x, y) + 20;

            movableItem = new Item(ServerFixture.SurfaceTileId)
            {
                Movable = true
            };
            movableItem.MoveToWorld(new Point3D(x, y, itemZ), map);

            var itemTop = itemZ + movableItem.ItemData.CalcHeight;
            var result = map.CanFitItem(x, y, itemTop, 1);

            Assert.False(result, "Movable item should not count as valid surface");
        }
        finally
        {
            movableItem?.Delete();
        }
    }

    [Fact]
    public void CanFitItem_ImpassableTileBlocksPlacement()
    {
        var map = Map.Felucca;
        const int x = 1900;
        const int y = 1900;

        TestMulti multi = null;
        try
        {
            // Create a multi with an impassable blocker at Z=10
            multi = CreateImpassableMulti(map, new Point3D(x, y, 0), blockerZ: 10);

            // Try to place item at Z=10 (inside the blocker)
            var result = map.CanFitItem(x, y, 10, 5);

            Assert.False(result, "Should not be able to place item inside impassable tile");
        }
        finally
        {
            multi?.Delete();
        }
    }

    private TestMulti CreateSurfaceImpassableMulti(Map map, Point3D location, int surfaceZ)
    {
        // Use SurfaceImpassableTileId - a tile that has both Surface and Impassable flags
        var multi = new TestMulti(new MultiComponentList(
        [
            new MultiTileEntry(
                ServerFixture.SurfaceImpassableTileId,
                0, 0, (short)surfaceZ,
                TileFlag.Surface | TileFlag.Impassable
            )
        ]));
        multi.MoveToWorld(location, map);
        return multi;
    }

    private TestMulti CreateImpassableMulti(Map map, Point3D location, int blockerZ)
    {
        var multi = new TestMulti(new MultiComponentList(
        [
            new MultiTileEntry(
                ServerFixture.ImpassableTileId,
                0, 0, (short)blockerZ,
                TileFlag.Impassable
            )
        ]));
        multi.MoveToWorld(location, map);
        return multi;
    }
}
