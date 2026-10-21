using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class TileMatrixInjectionTests
{
    // Not registered in Map.Maps, and no data directory holds map96.mul, so this matrix has no file streams.
    private static Map NewFilelessMap() => new(0x60, 0x60, 0x60, 64, 64, 0, "inject-test", MapRules.TrammelRules);

    [Fact]
    public void InjectedLandBlock_IsReturned_WithNoMapFile()
    {
        var tiles = NewFilelessMap().Tiles;
        var block = new LandTile[64];
        block[9] = new LandTile(0x10, 7);

        tiles.SetLandBlock(1, 1, block);

        var tile = tiles.GetLandTile(9, 9);
        Assert.Equal(0x10, tile.ID);
        Assert.Equal(7, tile.Z);
    }

    [Fact]
    public void UninjectedLand_IsIdZeroAtZZero()
    {
        var tile = NewFilelessMap().Tiles.GetLandTile(30, 30);

        Assert.Equal(0, tile.ID);
        Assert.Equal(0, tile.Z);
    }

    [Fact]
    public void InjectedStaticBlock_IsReturned_WithNoStaticFiles()
    {
        var tiles = NewFilelessMap().Tiles;
        var block = new StaticTile[8][][];
        for (var x = 0; x < 8; x++)
        {
            block[x] = new StaticTile[8][];
            for (var y = 0; y < 8; y++)
            {
                block[x][y] = [];
            }
        }

        block[1][1] = [new StaticTile(0x20, 5)];

        tiles.SetStaticBlock(1, 1, block);

        var cell = tiles.GetStaticBlock(1, 1)[1][1];
        Assert.Single(cell);
        Assert.Equal(0x20, cell[0].ID);
        Assert.Equal(5, cell[0].Z);
    }

    [Fact]
    public void ClearingABlock_RestoresTheDefault()
    {
        var tiles = NewFilelessMap().Tiles;
        var block = new LandTile[64];
        block[9] = new LandTile(0x10, 7);
        tiles.SetLandBlock(1, 1, block);

        tiles.SetLandBlock(1, 1, null);

        Assert.Equal(0, tiles.GetLandTile(9, 9).ID);
    }

    [Fact]
    public void OutOfRange_StillReturnsDefault()
    {
        var tiles = NewFilelessMap().Tiles;

        Assert.Equal(0, tiles.GetLandBlock(-1, 0)[0].ID);
        Assert.Equal(0, tiles.GetLandBlock(8, 0)[0].ID);
    }
}
