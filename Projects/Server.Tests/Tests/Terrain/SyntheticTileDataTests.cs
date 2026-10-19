using System.IO;
using Xunit;

namespace Server.Tests.Terrain;

[Collection("Sequential Server Tests")]
public class SyntheticTileDataTests
{
    [Fact]
    public void GeneratedFile_IsPreHsSize()
    {
        Assert.Equal(1_036_288, SyntheticTileData.Build().Length);
    }

    // Inside the test host's output, rewritten every run, so runs leave nothing behind in the temp directory.
    [Fact]
    public void DataDirectory_IsTheOnlyDataDirectory_AndLivesInTheTestHost()
    {
        Assert.Equal(Path.Combine(Core.BaseDirectory, "SyntheticData"), SyntheticWorld.DataDirectory);
        Assert.Equal([SyntheticWorld.DataDirectory], ServerConfiguration.DataDirectories);
    }

    [Fact]
    public void KitIds_FitThePreHsTable()
    {
        for (var i = 0; i < TestTiles.Items.Count; i++)
        {
            Assert.True(TestTiles.Items[i].Id < 0x4000, $"{TestTiles.Items[i].Name} is outside the pre-HS item table");
        }

        for (var i = 0; i < TestTiles.Land.Count; i++)
        {
            Assert.True(TestTiles.Land[i].Id < 0x4000, $"{TestTiles.Land[i].Name} is outside the land table");
        }
    }

    [Fact]
    public void GeneratedFile_RoundTripsKitThroughLoad()
    {
        SyntheticTileData.LoadInto(SyntheticWorld.DataDirectory);

        for (var i = 0; i < TestTiles.Items.Count; i++)
        {
            var tile = TestTiles.Items[i];
            Assert.Equal(tile.Flags, TileData.ItemTable[tile.Id].Flags);
            Assert.Equal((int)tile.Height, TileData.ItemTable[tile.Id].Height);
            Assert.Equal(tile.Name, TileData.ItemTable[tile.Id].Name);
        }

        for (var i = 0; i < TestTiles.Land.Count; i++)
        {
            var tile = TestTiles.Land[i];
            Assert.Equal(tile.Flags, TileData.LandTable[tile.Id].Flags);
        }

        Assert.Equal(TileFlag.None, TileData.ItemTable[0x2A0].Flags);
        Assert.Equal(TileFlag.None, TileData.LandTable[3].Flags);
    }
}
