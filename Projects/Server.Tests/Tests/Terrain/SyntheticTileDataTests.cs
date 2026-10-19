using System;
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
        using var restore = new TileDataRestore();

        var directory = Directory.CreateTempSubdirectory("modernuo-synth-").FullName;
        SyntheticTileData.WriteTo(directory);
        SyntheticTileData.LoadInto(directory);

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

    // The fixtures may have loaded other tile data; put it back so later tests in this host see what they expect.
    private sealed class TileDataRestore : IDisposable
    {
        private readonly LandData[] _land = (LandData[])TileData.LandTable.Clone();
        private readonly ItemData[] _items = (ItemData[])TileData.ItemTable.Clone();
        private readonly string[] _directories = [..ServerConfiguration.DataDirectories];

        public void Dispose()
        {
            Array.Copy(_land, TileData.LandTable, _land.Length);
            Array.Copy(_items, TileData.ItemTable, _items.Length);
            ServerConfiguration.DataDirectories.Clear();
            for (var i = 0; i < _directories.Length; i++)
            {
                ServerConfiguration.DataDirectories.Add(_directories[i]);
            }
        }
    }
}
