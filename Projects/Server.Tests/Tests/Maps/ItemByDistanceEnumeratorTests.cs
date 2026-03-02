using System;
using System.Collections.Generic;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class ItemByDistanceEnumeratorTests
{
    [Fact]
    public void ItemByDistanceEnumerator_ReturnsNearbyItems()
    {
        var map = Map.Felucca;
        var center = new Point3D(100, 100, 0);
        const int range = 5;

        var Items = new TestItem[3];
        try
        {
            Items[0] = CreateItem(map, new Point3D(102, 102, 0)); // Within range
            Items[1] = CreateItem(map, new Point3D(98, 98, 0));   // Within range
            Items[2] = CreateItem(map, new Point3D(110, 110, 0)); // Outside range

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(Items[0], found);
            Assert.Contains(Items[1], found);
            Assert.DoesNotContain(Items[2], found);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_DeletedItemsAreSkipped()
    {
        var map = Map.Felucca;
        var center = new Point3D(200, 200, 0);
        const int range = 5;

        var Items = new TestItem[3];
        try
        {
            Items[0] = CreateItem(map, new Point3D(202, 202, 0));
            Items[1] = CreateItem(map, new Point3D(203, 202, 0));
            Items[2] = CreateItem(map, new Point3D(204, 202, 0));

            Items[1].Delete();

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(Items[0], found);
            Assert.Contains(Items[2], found);
            Assert.DoesNotContain(Items[1], found);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_ReturnsMinDistance()
    {
        var map = Map.Felucca;
        var center = new Point3D(300, 300, 0);
        const int range = 10;

        var Items = new TestItem[2];
        try
        {
            Items[0] = CreateItem(map, new Point3D(305, 305, 0));
            Items[1] = CreateItem(map, new Point3D(302, 302, 0));

            var foundWithDistance = new List<(Item, int)>();
            foreach (var result in map.GetItemsInRangeByDistance(center, range))
            {
                foundWithDistance.Add(result);
            }

            Assert.Equal(2, foundWithDistance.Count);
            // Each Item should have a non-negative min distance
            Assert.All(foundWithDistance, item => Assert.True(item.Item2 >= 0));
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_OrderedBySector()
    {
        var map = Map.Felucca;
        var center = new Point3D(400, 400, 0);
        const int range = Map.SectorSize * 2;

        var Items = new TestItem[3];
        try
        {
            // Place Items in different sectors
            Items[0] = CreateItem(map, new Point3D(center.X + 2, center.Y + 2, 0));
            Items[1] = CreateItem(map, new Point3D(center.X + Map.SectorSize + 2, center.Y + 2, 0));
            Items[2] = CreateItem(map, new Point3D(center.X + 2, center.Y + Map.SectorSize + 2, 0));

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
            }

            Assert.Equal(3, found.Count);
            Assert.Contains(Items[0], found);
            Assert.Contains(Items[1], found);
            Assert.Contains(Items[2], found);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_MapNullYieldsEmpty()
    {
        var center = new Point2D(0, 0);
        var bounds = new Rectangle2D(center.m_X - 10, center.m_Y - 10, 21, 21);
        var enumerator = new Map.ItemDistanceEnumerable<Item>(null, bounds, center, false).GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void ItemByDistanceEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var center = new Point3D(500, 500, 0);
        const int range = 5;

        var Items = new[]
        {
            CreateItem(map, new Point3D(502, 502, 0)),
            CreateItem(map, new Point3D(503, 502, 0))
        };

        try
        {
            var enumerator = map.GetItemsInRangeByDistance(center, range).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            Items[1].Delete();

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
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_ZeroRangeReturnsOnlyCenter()
    {
        var map = Map.Felucca;
        var center = new Point3D(600, 600, 0);
        const int range = 0;

        var Items = new TestItem[2];
        try
        {
            Items[0] = CreateItem(map, center);              // Exact center
            Items[1] = CreateItem(map, new Point3D(601, 600, 0)); // 1 tile away

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
            }

            Assert.Single(found);
            Assert.Equal(Items[0], found[0]);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_FiltersByType()
    {
        var map = Map.Felucca;
        var center = new Point3D(700, 700, 0);
        const int range = 5;

        var testItem2 = new TestItem2(World.NewItem);
        var testItem = new TestItem(World.NewItem);

        try
        {
            testItem2.MoveToWorld(new Point3D(702, 702, 0), map);
            testItem.MoveToWorld(new Point3D(703, 702, 0), map);

            var foundPlayers = new List<TestItem2>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance<TestItem2>(center, range))
            {
                foundPlayers.Add(Item);
            }

            Assert.Single(foundPlayers);
            Assert.Equal(testItem2, foundPlayers[0]);
        }
        finally
        {
            testItem2.Delete();
            testItem.Delete();
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_UsesDifferentPointOverloads()
    {
        var map = Map.Felucca;
        var center = new Point3D(800, 800, 5);
        const int range = 5;

        var Items = new TestItem[1];
        try
        {
            Items[0] = CreateItem(map, new Point3D(802, 802, 0));

            // Test Point3D overload
            var found1 = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found1.Add(Item);
            }

            // Test (int, int) overload
            var found2 = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance<Item>(center.X, center.Y, range))
            {
                found2.Add(Item);
            }

            // Test Point2D overload
            var found3 = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(new Point2D(center.X, center.Y), range))
            {
                found3.Add(Item);
            }

            Assert.Single(found1);
            Assert.Equal(Items[0], found1[0]);
            Assert.Equal(found1, found2);
            Assert.Equal(found1, found3);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_RingTraversal()
    {
        var map = Map.Felucca;
        var center = new Point3D(900, 900, 0);
        const int range = Map.SectorSize * 2;

        var Items = new TestItem[4];
        try
        {
            // Place Items in different rings around the center
            Items[0] = CreateItem(map, center); // Ring 0 (center sector)
            Items[1] = CreateItem(map, new Point3D(center.X + Map.SectorSize, center.Y, 0)); // Ring 1
            Items[2] = CreateItem(map, new Point3D(center.X, center.Y + Map.SectorSize, 0)); // Ring 1
            Items[3] = CreateItem(map, new Point3D(center.X + Map.SectorSize * 2 - 1, center.Y, 0)); // Ring 2

            var found = new List<Item>();
            var distances = new List<int>();
            foreach (var (Item, minDistance) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
                distances.Add(minDistance);
            }

            // All Items should be found
            Assert.Equal(4, found.Count);
            Assert.Contains(Items[0], found);
            Assert.Contains(Items[1], found);
            Assert.Contains(Items[2], found);
            Assert.Contains(Items[3], found);

            // Items should be processed by sector distance (ring-based)
            // The center Item should have distance 0
            var centerIndex = found.IndexOf(Items[0]);
            Assert.Equal(0, distances[centerIndex]);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_MapBoundsAreClamped()
    {
        var map = Map.Felucca;
        var width = map.Width;
        var height = map.Height;

        var center = new Point3D(width - 2, height - 2, 0);
        const int range = Map.SectorSize * 2;

        var Items = new TestItem[1];
        try
        {
            Items[0] = CreateItem(map, center);

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
            }

            Assert.Single(found);
            Assert.Equal(Items[0], found[0]);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_NegativeRangeIsZero()
    {
        var map = Map.Felucca;
        var center = new Point3D(1000, 1000, 0);
        const int range = -5;

        var Items = new TestItem[2];
        try
        {
            Items[0] = CreateItem(map, center);
            Items[1] = CreateItem(map, new Point3D(1001, 1000, 0)); // 1 tile away

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
            }

            // With negative range creating a 1x1 bounds, only exact center matches
            Assert.Single(found);
            Assert.Equal(Items[0], found[0]);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_MultipleRings()
    {
        var map = Map.Felucca;
        var center = new Point3D(1100, 1100, 0);
        const int range = Map.SectorSize * 3;

        var Items = new List<TestItem>();
        try
        {
            // Create a grid of Items across multiple sectors
            for (var ringOffset = 0; ringOffset <= 2; ringOffset++)
            {
                for (var side = 0; side < 4; side++)
                {
                    var offset = ringOffset * Map.SectorSize;
                    var pos = side switch
                    {
                        0 => new Point3D(center.X + offset, center.Y, 0),
                        1 => new Point3D(center.X, center.Y + offset, 0),
                        2 => new Point3D(center.X - offset, center.Y, 0),
                        _ => new Point3D(center.X, center.Y - offset, 0)
                    };
                    Items.Add(CreateItem(map, pos));
                }
            }

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInRangeByDistance(center, range))
            {
                found.Add(Item);
            }

            // Should find all Items within range
            Assert.True(found.Count > 0);
            Assert.All(found, Item =>
            {
                var dx = Item.X - center.X;
                var dy = Item.Y - center.Y;
                var distSq = dx * dx + dy * dy;
                Assert.True(distSq <= range * range);
            });
        }
        finally
        {
            foreach (var Item in Items)
            {
                Item?.Delete();
            }
        }
    }

    private static TestItem CreateItem(Map map, Point3D location)
    {
        var Item = new TestItem(World.NewItem);
        Item.MoveToWorld(location, map);
        return Item;
    }

    private static void DeleteAll(TestItem[] Items)
    {
        for (var i = 0; i < Items.Length; i++)
        {
            Items[i]?.Delete();
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_Bounds_FindsItemsInBounds()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(100, 100, 50, 50);

        var Items = new TestItem[3];
        try
        {
            // Item inside bounds
            Items[0] = CreateItem(map, new Point3D(120, 120, 0));
            // Item at edge of bounds
            Items[1] = CreateItem(map, new Point3D(149, 149, 0));
            // Item outside bounds
            Items[2] = CreateItem(map, new Point3D(200, 200, 0));

            var found = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInBoundsByDistance<Item>(bounds))
            {
                found.Add(Item);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(Items[0], found);
            Assert.Contains(Items[1], found);
            Assert.DoesNotContain(Items[2], found);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_Bounds_MakeBoundsInclusive()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(100, 100, 50, 50);

        var Items = new TestItem[2];
        try
        {
            // Item at edge (inclusive)
            Items[0] = CreateItem(map, new Point3D(149, 149, 0));
            // Item just outside edge (will be included with makeBoundsInclusive)
            Items[1] = CreateItem(map, new Point3D(150, 150, 0));

            var foundWithoutInclusive = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInBoundsByDistance<Item>(bounds))
            {
                foundWithoutInclusive.Add(Item);
            }

            var foundWithInclusive = new List<Item>();
            foreach (var (Item, _) in map.GetItemsInBoundsByDistance<Item>(bounds, true))
            {
                foundWithInclusive.Add(Item);
            }

            Assert.Single(foundWithoutInclusive);
            Assert.Contains(Items[0], foundWithoutInclusive);

            Assert.Equal(2, foundWithInclusive.Count);
            Assert.Contains(Items[0], foundWithInclusive);
            Assert.Contains(Items[1], foundWithInclusive);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_Bounds_ReturnsMinDistance()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(300, 300, 20, 20);

        var Items = new TestItem[2];
        try
        {
            Items[0] = CreateItem(map, new Point3D(305, 305, 0));
            Items[1] = CreateItem(map, new Point3D(315, 315, 0));

            var foundWithDistance = new List<(Item, int)>();
            foreach (var result in map.GetItemsInBoundsByDistance<Item>(bounds))
            {
                foundWithDistance.Add(result);
            }

            Assert.Equal(2, foundWithDistance.Count);
            Assert.All(foundWithDistance, item => Assert.True(item.Item2 >= 0));
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    [Fact]
    public void ItemByDistanceEnumerator_Bounds_OrdersByProximityToCenter()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(500, 500, 64, 64);

        var Items = new TestItem[3];
        try
        {
            // Place Items at different distances from center
            Items[0] = CreateItem(map, new Point3D(532, 532, 0)); // At center
            Items[1] = CreateItem(map, new Point3D(548, 532, 0)); // 16 tiles away
            Items[2] = CreateItem(map, new Point3D(563, 563, 0)); // Far corner

            var found = new List<(Item, int)>();
            foreach (var result in map.GetItemsInBoundsByDistance<Item>(bounds))
            {
                found.Add(result);
            }

            Assert.Equal(3, found.Count);

            // Verify ordering by distance - closer Items should be found earlier (lower minDistance)
            var Item0Index = found.FindIndex(x => x.Item1 == Items[0]);
            var Item1Index = found.FindIndex(x => x.Item1 == Items[1]);
            var Item2Index = found.FindIndex(x => x.Item1 == Items[2]);

            // The minDistance should increase (or stay the same) as we go through the list
            Assert.True(found[Item0Index].Item2 <= found[Item1Index].Item2);
            Assert.True(found[Item1Index].Item2 <= found[Item2Index].Item2);
        }
        finally
        {
            DeleteAll(Items);
        }
    }

    // Test implementation of Item
    private class TestItem : Item
    {
        public TestItem(Serial serial) : base(serial)
        {
        }
    }

    private class TestItem2 : Item
    {
        public TestItem2(Serial serial) : base(serial)
        {
        }
    }
}

