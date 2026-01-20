using System;
using System.Collections.Generic;
using Server.Items;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class MultiEnumeratorTests
{
    [Fact]
    public void MultiEnumerator_FiltersByBoundsAndOrder()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(100, 100, 32, 32);

        var multis = new TestMulti[3];
        try
        {
            multis[0] = CreateMulti(map, new Point3D(105, 105, 0));
            multis[1] = CreateMulti(map, new Point3D(130, 130, 0));
            multis[2] = CreateMulti(map, new Point3D(90, 90, 0));

            var found = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInBounds<BaseMulti>(rect))
            {
                found.Add(multi);
            }

            Assert.Equal(2, found.Count);
            Assert.All(found, multi => Assert.True(rect.Contains(multi.Location)));
            Assert.Equal(new[] { multis[0], multis[1] }, found);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_DeletedMultisAreSkipped()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(200, 200, 16, 16);

        var multis = new TestMulti[3];
        try
        {
            multis[0] = CreateMulti(map, new Point3D(205, 205, 0));
            multis[1] = CreateMulti(map, new Point3D(206, 205, 0));
            multis[2] = CreateMulti(map, new Point3D(207, 205, 0));

            multis[1].Delete();

            var found = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInBounds<BaseMulti>(rect))
            {
                found.Add(multi);
            }

            Assert.Equal(new[] { multis[0], multis[2] }, found);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_RespectsMakeBoundsInclusiveFlag()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(300, 300, 1, 1);

        var multis = new TestMulti[1];
        try
        {
            multis[0] = CreateMulti(map, new Point3D(301, 301, 0));

            var enumerator = map.GetMultisInBounds<BaseMulti>(rect, makeBoundsInclusive: true).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(multis[0], enumerator.Current);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.MultiBoundsEnumerable<BaseMulti>(null, Rectangle2D.Empty, false).GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void MultiEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(400, 400, 16, 16);

        var multis = new[]
        {
            CreateMulti(map, new Point3D(405, 405, 0)),
            CreateMulti(map, new Point3D(406, 405, 0))
        };

        try
        {
            var enumerator = map.GetMultisInBounds<BaseMulti>(rect).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            multis[1].Delete();

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
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_StepsAcrossSectors()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(500, 500, Map.SectorSize * 2, Map.SectorSize * 2);

        var multis = new[]
        {
            CreateMulti(map, new Point3D(rect.X + 1, rect.Y + 1, 0)),
            CreateMulti(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + 1, 0)),
            CreateMulti(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + Map.SectorSize + 1, 0))
        };

        try
        {
            var result = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInBounds<BaseMulti>(rect))
            {
                result.Add(multi);
            }

            Assert.Equal(multis, result);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_MapBoundsAreClamped()
    {
        var map = Map.Felucca;
        var width = map.Width;
        var height = map.Height;

        var rect = new Rectangle2D(width - Map.SectorSize - 2, height - Map.SectorSize - 2, Map.SectorSize * 2, Map.SectorSize * 2);

        var multis = new[]
        {
            CreateMulti(map, new Point3D(width - 2, height - 2, 0))
        };

        try
        {
            var enumerator = map.GetMultisInBounds<BaseMulti>(rect).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(multis[0], enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_GetMultisInRange()
    {
        var map = Map.Felucca;
        var center = new Point3D(600, 600, 0);
        var range = 5;

        var multis = new TestMulti[3];
        try
        {
            multis[0] = CreateMulti(map, new Point3D(602, 602, 0)); // Within range
            multis[1] = CreateMulti(map, new Point3D(598, 598, 0)); // Within range
            multis[2] = CreateMulti(map, new Point3D(610, 610, 0)); // Outside range

            var found = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInRange<BaseMulti>(center, range))
            {
                found.Add(multi);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(multis[0], found);
            Assert.Contains(multis[1], found);
            Assert.DoesNotContain(multis[2], found);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiSectorEnumerator_FiltersToSingleSector()
    {
        var map = Map.Felucca;
        var location = new Point3D(700, 700, 0);

        var multis = new TestMulti[2];
        try
        {
            multis[0] = CreateMulti(map, location);
            multis[1] = CreateMulti(map, new Point3D(location.X + 1, location.Y, 0));

            var found = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInSector<BaseMulti>(location))
            {
                found.Add(multi);
            }

            // Both should be in the same sector
            Assert.Equal(2, found.Count);
            Assert.Contains(multis[0], found);
            Assert.Contains(multis[1], found);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiSectorEnumerator_DeletedMultisAreSkipped()
    {
        var map = Map.Felucca;
        var location = new Point3D(750, 750, 0);

        var multis = new TestMulti[3];
        try
        {
            multis[0] = CreateMulti(map, location);
            multis[1] = CreateMulti(map, new Point3D(location.X + 1, location.Y, 0));
            multis[2] = CreateMulti(map, new Point3D(location.X + 2, location.Y, 0));

            multis[1].Delete();

            var found = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInRange<BaseMulti>(location, 10))
            {
                found.Add(multi);
            }

            Assert.Equal(new[] { multis[0], multis[2] }, found);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiSectorEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.MultiSectorEnumerable<BaseMulti>(null, new Point2D(0, 0)).GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void MultiSectorEnumerator_UsesDifferentPointOverloads()
    {
        var map = Map.Felucca;
        var location = new Point3D(800, 800, 5);

        var multis = new TestMulti[1];
        try
        {
            multis[0] = CreateMulti(map, location);

            // Test Point3D overload
            var found1 = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInSector(location))
            {
                found1.Add(multi);
            }

            // Test (int, int) overload
            var found2 = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInSector(location.X, location.Y))
            {
                found2.Add(multi);
            }

            // Test Point2D overload
            var found3 = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInSector(new Point2D(location.X, location.Y)))
            {
                found3.Add(multi);
            }

            Assert.Single(found1);
            Assert.Equal(multis[0], found1[0]);
            Assert.Equal(found1, found2);
            Assert.Equal(found1, found3);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_ZeroRangeReturnsOnlyCenter()
    {
        var map = Map.Felucca;
        var center = new Point3D(800, 800, 0);
        const int range = 0;

        var multis = new TestMulti[2];
        try
        {
            multis[0] = CreateMulti(map, center);              // Exact center
            multis[1] = CreateMulti(map, new Point3D(801, 800, 0)); // 1 tile away

            var found = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInRange<BaseMulti>(center, range))
            {
                found.Add(multi);
            }

            Assert.Single(found);
            Assert.Equal(multis[0], found[0]);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    [Fact]
    public void MultiEnumerator_NegativeRangeCreates1x1Bounds()
    {
        var map = Map.Felucca;
        var center = new Point3D(850, 850, 0);
        const int range = -5;

        var multis = new TestMulti[2];
        try
        {
            multis[0] = CreateMulti(map, center);
            multis[1] = CreateMulti(map, new Point3D(851, 850, 0)); // 1 tile away

            var found = new List<BaseMulti>();
            foreach (var multi in map.GetMultisInRange<BaseMulti>(center, range))
            {
                found.Add(multi);
            }

            // With negative range creating a 1x1 bounds, only exact center matches
            Assert.Single(found);
            Assert.Equal(multis[0], found[0]);
        }
        finally
        {
            DeleteAll(multis);
        }
    }

    private static TestMulti CreateMulti(Map map, Point3D location)
    {
        var multi = new TestMulti();
        multi.MoveToWorld(location, map);
        return multi;
    }

    private static void DeleteAll(TestMulti[] multis)
    {
        for (var i = 0; i < multis.Length; i++)
        {
            multis[i]?.Delete();
        }
    }

    // Test implementation of BaseMulti
    private class TestMulti : BaseMulti
    {
        public TestMulti() : base(0x1)
        {
        }
    }
}

