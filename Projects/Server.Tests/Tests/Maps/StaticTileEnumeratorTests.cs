using System.Collections.Generic;
using Server.Items;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class StaticTileEnumeratorTests
{
    [Fact]
    public void StaticTileEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.StaticTileEnumerable(null, new Point2D(0, 0)).GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void StaticTileEnumerator_EmptyLocationYieldsEmpty()
    {
        var map = Map.Felucca;
        var location = new Point2D(100, 100);

        var tiles = new List<StaticTile>();
        foreach (var tile in new Map.StaticTileEnumerable(map, location, includeStatics: true, includeMultis: false))
        {
            tiles.Add(tile);
        }

        // Since we don't have actual map files loaded, this should be empty
        Assert.Empty(tiles);
    }

    [Fact]
    public void StaticTileEnumerator_IncludeStaticsOnlyWorks()
    {
        var map = Map.Felucca;
        var location = new Point2D(200, 200);

        TestMulti multi = null;
        try
        {
            // Create a multi at the location
            multi = CreateMultiWithComponents(map, new Point3D(200, 200, 0));

            // Get tiles with statics only (no multis)
            var tiles = new List<StaticTile>();
            foreach (var tile in new Map.StaticTileEnumerable(map, location, includeStatics: true, includeMultis: false))
            {
                tiles.Add(tile);
            }

            // Should not include multi tiles
            Assert.Empty(tiles);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void StaticTileEnumerator_IncludeMultisOnlyWorks()
    {
        var map = Map.Felucca;
        var location = new Point2D(300, 300);

        TestMulti multi = null;
        try
        {
            // Create a multi at the location with components
            multi = CreateMultiWithComponents(map, new Point3D(300, 300, 0));

            // Get tiles with multis only (no statics)
            var tiles = new List<StaticTile>();
            foreach (var tile in new Map.StaticTileEnumerable(map, location, includeStatics: false, includeMultis: true))
            {
                tiles.Add(tile);
            }

            // Should include multi tiles
            Assert.NotEmpty(tiles);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void StaticTileEnumerator_IncludeBothStaticsAndMultisWorks()
    {
        var map = Map.Felucca;
        var location = new Point2D(400, 400);

        TestMulti multi = null;
        try
        {
            // Create a multi at the location
            multi = CreateMultiWithComponents(map, new Point3D(400, 400, 0));

            // Get all tiles (statics and multis)
            var tiles = new List<StaticTile>();
            foreach (var tile in new Map.StaticTileEnumerable(map, location, includeStatics: true, includeMultis: true))
            {
                tiles.Add(tile);
            }

            // Should include multi tiles (statics would be empty without map files)
            Assert.NotEmpty(tiles);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void StaticTileEnumerator_MultiTileZOffsetApplied()
    {
        var map = Map.Felucca;
        var location = new Point2D(500, 500);
        var multiZ = 10;

        TestMulti multi = null;
        try
        {
            // Create a multi at Z=10
            multi = CreateMultiWithComponents(map, new Point3D(500, 500, multiZ));

            // Get multi tiles
            var tiles = new List<StaticTile>();
            foreach (var tile in new Map.StaticTileEnumerable(map, location, includeStatics: false, includeMultis: true))
            {
                tiles.Add(tile);
            }

            // All tiles should have Z offset by the multi's Z position
            Assert.NotEmpty(tiles);
            Assert.All(tiles, tile => Assert.True(tile.Z >= multiZ));
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void StaticTileEnumerator_TileMatrixGetStaticTilesWorks()
    {
        var map = Map.Felucca;
        var x = 600;
        var y = 600;

        // Test the TileMatrix.GetStaticTiles method
        var tiles = new List<StaticTile>();
        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            tiles.Add(tile);
        }

        // Without map files loaded, should be empty
        Assert.Empty(tiles);
    }

    [Fact]
    public void StaticTileEnumerator_TileMatrixGetStaticAndMultiTilesWorks()
    {
        var map = Map.Felucca;
        var x = 700;
        var y = 700;

        TestMulti multi = null;
        try
        {
            // Create a multi at the location
            multi = CreateMultiWithComponents(map, new Point3D(700, 700, 0));

            // Test the TileMatrix.GetStaticAndMultiTiles method
            var tiles = new List<StaticTile>();
            foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
            {
                tiles.Add(tile);
            }

            // Should include multi tiles
            Assert.NotEmpty(tiles);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void StaticTileEnumerator_TileMatrixGetMultiTilesWorks()
    {
        var map = Map.Felucca;
        var x = 800;
        var y = 800;

        TestMulti multi = null;
        try
        {
            // Create a multi at the location
            multi = CreateMultiWithComponents(map, new Point3D(800, 800, 0));

            // Test the TileMatrix.GetMultiTiles method
            var tiles = new List<StaticTile>();
            foreach (var tile in map.Tiles.GetMultiTiles(x, y))
            {
                tiles.Add(tile);
            }

            // Should include multi tiles
            Assert.NotEmpty(tiles);
        }
        finally
        {
            multi?.Delete();
        }
    }

    [Fact]
    public void StaticTileEnumerator_MultipleMultisAtSameLocation()
    {
        var map = Map.Felucca;
        var location = new Point2D(900, 900);

        var multis = new TestMulti[2];
        try
        {
            // Create two multis at the same location
            multis[0] = CreateMultiWithComponents(map, new Point3D(900, 900, 0));
            multis[1] = CreateMultiWithComponents(map, new Point3D(900, 900, 5));

            // Get all multi tiles
            var tiles = new List<StaticTile>();
            foreach (var tile in new Map.StaticTileEnumerable(map, location, includeStatics: false, includeMultis: true))
            {
                tiles.Add(tile);
            }

            // Should include tiles from both multis
            Assert.NotEmpty(tiles);
            // We expect at least tiles from both multis
            Assert.True(tiles.Count >= 2);
        }
        finally
        {
            multis[0]?.Delete();
            multis[1]?.Delete();
        }
    }

    [Fact]
    public void StaticTileEnumerator_EmptyReturnsCorrectly()
    {
        var enumerator = Map.StaticTileEnumerable.Empty.GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void StaticTileEnumerator_DeletedMultiSkipped()
    {
        var map = Map.Felucca;
        var location = new Point2D(1000, 1000);

        var multis = new TestMulti[2];
        try
        {
            // Create two multis
            multis[0] = CreateMultiWithComponents(map, new Point3D(1000, 1000, 0));
            multis[1] = CreateMultiWithComponents(map, new Point3D(1000, 1000, 5));

            // Delete the first multi
            multis[0].Delete();

            // Get all multi tiles
            var tiles = new List<StaticTile>();
            foreach (var tile in new Map.StaticTileEnumerable(map, location, includeStatics: false, includeMultis: true))
            {
                tiles.Add(tile);
            }

            // Should only include tiles from the second multi
            Assert.NotEmpty(tiles);
            Assert.All(tiles, tile => Assert.True(tile.Z >= 5));
        }
        finally
        {
            multis[0]?.Delete();
            multis[1]?.Delete();
        }
    }

    private static TestMulti CreateMultiWithComponents(Map map, Point3D location)
    {
        var multi = new TestMulti(World.NewItem);
        multi.MoveToWorld(location, map);
        return multi;
    }

    private class TestMulti : BaseMulti
    {
        public TestMulti(Serial serial) : base(serial)
        {
        }

        public override MultiComponentList Components => DefaultComponents;

        private static readonly MultiComponentList DefaultComponents = new(
            [
                new MultiTileEntry(0x1, 0, 0, 0, 0x0),
                new MultiTileEntry(0x2, 1, 0, 0, 0x0)
            ]
        );
    }
}

