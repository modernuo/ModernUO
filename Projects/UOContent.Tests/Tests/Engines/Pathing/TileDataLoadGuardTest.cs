using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class TileDataLoadGuardTest
{
    [Fact]
    public void TileData_LandTable_IsLoaded()
    {
        // Regression guard for the reflection hack in PathfindingTestFixture.ForceLoadTileData.
        // TileData's static cctor short-circuits under xUnit (Server/TileData.cs:295), so without
        // the reflection-based force-load every tile's Flags would be None and pathfinding tests
        // would silently report "everything walkable" — including ocean.
        Assert.True(TileData.LandTable.Length > 0, "LandTable empty — tiledata.mul not loaded");
        Assert.True(TileData.ItemTable.Length > 0, "ItemTable empty — tiledata.mul not loaded");

        // If the loader ran, at least some entries will have non-None flags
        // (Impassable mountains, Wet water, doors, etc.). If every entry is None,
        // the static cctor short-circuited and the reflection load failed.
        var landWithFlags = 0;
        for (var i = 0; i < TileData.LandTable.Length; i++)
        {
            if (TileData.LandTable[i].Flags != TileFlag.None)
            {
                landWithFlags++;
            }
        }
        Assert.True(landWithFlags > 0, "No land tile has any flags set — TileData load failed");
    }
}
