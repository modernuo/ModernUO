using System;
using System.Collections.Generic;
using Server.Items;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class ItemEnumeratorTests
{
    [Fact]
    public void ItemEnumerator_FiltersByBoundsAndOrder()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(100, 100, 32, 32);

        var items = new Item[3];
        try
        {
            items[0] = CreateItem(map, new Point3D(105, 105, 0));
            items[1] = CreateItem(map, new Point3D(130, 130, 0));
            items[2] = CreateItem(map, new Point3D(90, 90, 0));

            var found = new List<Item>();
            foreach (var item in map.GetItemsInBounds<Item>(rect))
            {
                found.Add(item);
            }

            Assert.Equal(2, found.Count);
            Assert.All(found, item => Assert.True(rect.Contains(item.Location)));
            Assert.Equal(new[] { items[0], items[1] }, found);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemEnumerator_DeletedItemsAreSkipped()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(200, 200, 16, 16);

        var items = new Item[3];
        try
        {
            items[0] = CreateItem(map, new Point3D(205, 205, 0));
            items[1] = CreateItem(map, new Point3D(206, 205, 0));
            items[2] = CreateItem(map, new Point3D(207, 205, 0));

            items[1].Delete();

            var found = new List<Item>();
            foreach (var item in map.GetItemsInBounds<Item>(rect))
            {
                found.Add(item);
            }

            Assert.Equal(new[] { items[0], items[2] }, found);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemEnumerator_ItemsWithParentAreSkipped()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(250, 250, 16, 16);

        var items = new Item[2];
        var container = new Container(0xE75);
        try
        {
            items[0] = CreateItem(map, new Point3D(255, 255, 0));
            items[1] = CreateItem(map, new Point3D(256, 255, 0));
            container.MoveToWorld(new Point3D(255, 255, 0), map);

            // Move items[1] into the container - it should be skipped
            items[1].Parent = container;

            var found = new List<Item>();
            foreach (var item in map.GetItemsInBounds<Item>(rect))
            {
                found.Add(item);
            }

            // Should only find items[0] and container, not items[1] (which has a parent)
            Assert.Equal(2, found.Count);
            Assert.Contains(items[0], found);
            Assert.Contains(container, found);
            Assert.DoesNotContain(items[1], found);
        }
        finally
        {
            DeleteAll(items);
            container?.Delete();
        }
    }

    [Fact]
    public void ItemEnumerator_RespectsMakeBoundsInclusiveFlag()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(300, 300, 1, 1);

        var items = new Item[1];
        try
        {
            items[0] = CreateItem(map, new Point3D(301, 301, 0));

            var enumerator = map.GetItemsInBounds<Item>(rect, makeBoundsInclusive: true).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(items[0], enumerator.Current);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.ItemEnumerator<Item>(null, Rectangle2D.Empty, false);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void ItemEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(400, 400, 16, 16);

        var items = new[]
        {
            CreateItem(map, new Point3D(405, 405, 0)),
            CreateItem(map, new Point3D(406, 405, 0))
        };

        try
        {
            var enumerator = map.GetItemsInBounds<Item>(rect).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            items[1].Delete();

            // Ref structs cannot be captured in lambdas, so we test the exception directly
            var exceptionThrown = false;
            try
            {
                enumerator.MoveNext();
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "Expected InvalidOperationException when collection version changes");
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemEnumerator_StepsAcrossSectors()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(500, 500, Map.SectorSize * 2, Map.SectorSize * 2);

        var items = new[]
        {
            CreateItem(map, new Point3D(rect.X + 1, rect.Y + 1, 0)),
            CreateItem(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + 1, 0)),
            CreateItem(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + Map.SectorSize + 1, 0))
        };

        try
        {
            var result = new List<Item>();
            foreach (var item in map.GetItemsInBounds<Item>(rect))
            {
                result.Add(item);
            }

            Assert.Equal(items, result);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemEnumerator_MapBoundsAreClamped()
    {
        var map = Map.Felucca;
        var width = map.Width;
        var height = map.Height;

        var rect = new Rectangle2D(width - Map.SectorSize - 2, height - Map.SectorSize - 2, Map.SectorSize * 2, Map.SectorSize * 2);

        var items = new[]
        {
            CreateItem(map, new Point3D(width - 2, height - 2, 0))
        };

        try
        {
            var enumerator = map.GetItemsInBounds<Item>(rect).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(items[0], enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemAtEnumerator_FiltersExactLocation()
    {
        var map = Map.Felucca;
        var location = new Point3D(600, 600, 0);

        var items = new Item[3];
        try
        {
            items[0] = CreateItem(map, location);
            items[1] = CreateItem(map, location);
            items[2] = CreateItem(map, new Point3D(601, 600, 0)); // Different location

            var found = new List<Item>();
            foreach (var item in map.GetItemsAt<Item>(location))
            {
                found.Add(item);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(items[0], found);
            Assert.Contains(items[1], found);
            Assert.DoesNotContain(items[2], found);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemAtEnumerator_DeletedItemsAreSkipped()
    {
        var map = Map.Felucca;
        var location = new Point3D(650, 650, 0);

        var items = new Item[3];
        try
        {
            items[0] = CreateItem(map, location);
            items[1] = CreateItem(map, location);
            items[2] = CreateItem(map, location);

            items[1].Delete();

            var found = new List<Item>();
            foreach (var item in map.GetItemsAt<Item>(location))
            {
                found.Add(item);
            }

            Assert.Equal(new[] { items[0], items[2] }, found);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemAtEnumerator_ItemsWithParentAreSkipped()
    {
        var map = Map.Felucca;
        var location = new Point3D(700, 700, 0);

        var items = new Item[2];
        var container = new Container(0xE75);
        try
        {
            items[0] = CreateItem(map, location);
            items[1] = CreateItem(map, location);
            container.MoveToWorld(location, map);

            // Move items[1] into the container - it should be skipped
            items[1].Parent = container;

            var found = new List<Item>();
            foreach (var item in map.GetItemsAt<Item>(location))
            {
                found.Add(item);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(items[0], found);
            Assert.Contains(container, found);
            Assert.DoesNotContain(items[1], found);
        }
        finally
        {
            DeleteAll(items);
            container?.Delete();
        }
    }

    [Fact]
    public void ItemAtEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.ItemAtEnumerator<Item>(null, new Point2D(0, 0));
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void ItemAtEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var location = new Point3D(750, 750, 0);

        var items = new[]
        {
            CreateItem(map, location),
            CreateItem(map, location)
        };

        try
        {
            var enumerator = map.GetItemsAt<Item>(location).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            items[1].Delete();

            // Ref structs cannot be captured in lambdas, so we test the exception directly
            var exceptionThrown = false;
            try
            {
                enumerator.MoveNext();
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "Expected InvalidOperationException when collection version changes");
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemAtEnumerator_UsesDifferentPoint3DOverloads()
    {
        var map = Map.Felucca;
        var location = new Point3D(800, 800, 5);

        var items = new Item[1];
        try
        {
            items[0] = CreateItem(map, location);

            // Test Point3D overload
            var found1 = new List<Item>();
            foreach (var item in map.GetItemsAt(location))
            {
                found1.Add(item);
            }

            // Test (int, int) overload - should find the same item (Z is ignored)
            var found2 = new List<Item>();
            foreach (var item in map.GetItemsAt(location.X, location.Y))
            {
                found2.Add(item);
            }

            // Test Point2D overload
            var found3 = new List<Item>();
            foreach (var item in map.GetItemsAt(new Point2D(location.X, location.Y)))
            {
                found3.Add(item);
            }

            Assert.Single(found1);
            Assert.Equal(items[0], found1[0]);
            Assert.Equal(found1, found2);
            Assert.Equal(found1, found3);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemEnumerator_ZeroRangeReturnsOnlyCenter()
    {
        var map = Map.Felucca;
        var center = new Point3D(850, 850, 0);
        const int range = 0;

        var items = new Item[2];
        try
        {
            items[0] = CreateItem(map, center);              // Exact center
            items[1] = CreateItem(map, new Point3D(851, 850, 0)); // 1 tile away

            var found = new List<Item>();
            foreach (var item in map.GetItemsInRange<Item>(center, range))
            {
                found.Add(item);
            }

            Assert.Single(found);
            Assert.Equal(items[0], found[0]);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    [Fact]
    public void ItemEnumerator_NegativeRangeCreates1x1Bounds()
    {
        var map = Map.Felucca;
        var center = new Point3D(900, 900, 0);
        const int range = -5;

        var items = new Item[2];
        try
        {
            items[0] = CreateItem(map, center);
            items[1] = CreateItem(map, new Point3D(901, 900, 0)); // 1 tile away

            var found = new List<Item>();
            foreach (var item in map.GetItemsInRange<Item>(center, range))
            {
                found.Add(item);
            }

            // With negative range creating a 1x1 bounds, only exact center matches
            Assert.Single(found);
            Assert.Equal(items[0], found[0]);
        }
        finally
        {
            DeleteAll(items);
        }
    }

    private static Item CreateItem(Map map, Point3D location)
    {
        var item = new Item(0x1);
        item.Movable = false;
        item.MoveToWorld(location, map);
        return item;
    }

    private static void DeleteAll(Item[] items)
    {
        for (var i = 0; i < items.Length; i++)
        {
            items[i]?.Delete();
        }
    }
}

