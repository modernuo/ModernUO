using System;
using Xunit;

namespace Server.Tests.Terrain;

[Collection("Sequential Server Tests")]
public class TestTerrainTests
{
    // Inside the prefab region but clear of every prefab Prefabs.All defines.
    private const int X = 1200;
    private const int Y = 1200;

    [Fact]
    public void StampingOneTile_KeepsTheRestOfTheBlock()
    {
        using var scope = TestTerrain.Scope();

        TestTerrain.SetLand(X, Y, TestTiles.Grass, 3);
        TestTerrain.SetLand(X + 1, Y, TestTiles.Dirt, 4);

        Assert.Equal(TestTiles.Grass, Map.Felucca.Tiles.GetLandTile(X, Y).ID);
        Assert.Equal(TestTiles.Dirt, Map.Felucca.Tiles.GetLandTile(X + 1, Y).ID);
    }

    [Fact]
    public void StampingAnUninjectedBlock_DoesNotTouchOtherBlocks()
    {
        using var scope = TestTerrain.Scope();

        TestTerrain.SetLand(X, Y, TestTiles.Grass, 3);

        Assert.Equal(0, Map.Felucca.Tiles.GetLandTile(X + 64, Y + 64).ID);
        Assert.Equal(0, Map.Felucca.Tiles.GetLandTile(X + 1, Y).ID);
    }

    [Fact]
    public void Stamps_ReachBothFacets()
    {
        using var scope = TestTerrain.Scope();

        TestTerrain.SetLand(X, Y, TestTiles.Grass, 3);
        TestTerrain.AddStatic(X, Y, TestTiles.Wall, 3);

        foreach (var map in (ReadOnlySpan<Map>)[Map.Felucca, Map.Trammel])
        {
            Assert.Equal(TestTiles.Grass, map.Tiles.GetLandTile(X, Y).ID);
            var statics = map.Tiles.GetStaticBlock(X >> 3, Y >> 3)[X & 7][Y & 7];
            Assert.Single(statics);
            Assert.Equal(TestTiles.Wall, statics[0].ID);
        }
    }

    [Fact]
    public void ScopeDispose_RestoresBothFacets()
    {
        using (TestTerrain.Scope())
        {
            TestTerrain.SetLand(X, Y, TestTiles.Grass, 3);
            TestTerrain.AddStatic(X, Y, TestTiles.Wall, 3);
        }

        foreach (var map in (ReadOnlySpan<Map>)[Map.Felucca, Map.Trammel])
        {
            Assert.Equal(0, map.Tiles.GetLandTile(X, Y).ID);
            Assert.Empty(map.Tiles.GetStaticBlock(X >> 3, Y >> 3)[X & 7][Y & 7]);
        }
    }

    [Fact]
    public void ScopeDispose_RestoresAPrefabItOverwrote()
    {
        Prefabs.StampAll();
        var coast = Prefabs.Coastline.Anchor;
        var before = Map.Felucca.Tiles.GetLandTile(coast.X, coast.Y);

        using (TestTerrain.Scope())
        {
            TestTerrain.SetLand(coast.X, coast.Y, TestTiles.RockLand, 40);
        }

        Assert.Equal(before.ID, Map.Felucca.Tiles.GetLandTile(coast.X, coast.Y).ID);
        Assert.Equal(before.Z, Map.Felucca.Tiles.GetLandTile(coast.X, coast.Y).Z);
        Prefabs.Coastline.AssertPresent();
    }

    [Fact]
    public void NestedScopes_Throw()
    {
        using var scope = TestTerrain.Scope();

        Assert.Throws<InvalidOperationException>(() => TestTerrain.Scope());
    }

    [Fact]
    public void StampingOutsideAScope_Throws_OncePrefabsAreStamped()
    {
        Prefabs.StampAll();

        Assert.Throws<InvalidOperationException>(() => TestTerrain.SetLand(X, Y, TestTiles.Grass, 0));
    }

    [Fact]
    public void StampAll_PrefabsDoNotOverlap_AndAreAllPresent()
    {
        Prefabs.StampAll();

        for (var i = 0; i < Prefabs.All.Count; i++)
        {
            Prefabs.All[i].AssertPresent();
        }
    }

    [Fact]
    public void AssertPresent_Throws_WhenThePrefabIsGone()
    {
        Prefabs.StampAll();
        var coast = Prefabs.Coastline.Anchor;

        using var scope = TestTerrain.Scope();
        TestTerrain.SetLand(coast.X, coast.Y, 0, 0);

        Assert.Throws<InvalidOperationException>(() => Prefabs.Coastline.AssertPresent());
    }
}
