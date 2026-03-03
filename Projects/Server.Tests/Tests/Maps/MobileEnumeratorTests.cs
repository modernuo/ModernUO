using System;
using System.Collections.Generic;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class MobileEnumeratorTests
{
    [Fact]
    public void MobileEnumerator_FiltersByBoundsAndOrder()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(100, 100, 32, 32);

        var mobiles = new Mobile[3];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(105, 105, 0));
            mobiles[1] = CreateMobile(map, new Point3D(130, 130, 0));
            mobiles[2] = CreateMobile(map, new Point3D(90, 90, 0));

            var found = new List<Mobile>();
            foreach (var m in map.GetMobilesInBounds<Mobile>(rect))
            {
                found.Add(m);
            }

            Assert.Equal(2, found.Count);
            Assert.All(found, m => Assert.True(rect.Contains(m.Location)));
            Assert.Equal(new[] { mobiles[0], mobiles[1] }, found);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileEnumerator_DeletedMobilesAreSkipped()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(200, 200, 16, 16);

        var mobiles = new Mobile[3];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(205, 205, 0));
            mobiles[1] = CreateMobile(map, new Point3D(206, 205, 0));
            mobiles[2] = CreateMobile(map, new Point3D(207, 205, 0));

            mobiles[1].Delete();

            var found = new List<Mobile>();
            foreach (var m in map.GetMobilesInBounds<Mobile>(rect))
            {
                found.Add(m);
            }

            Assert.Equal(new[] { mobiles[0], mobiles[2] }, found);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileEnumerator_RespectsMakeBoundsInclusiveFlag()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(300, 300, 1, 1);

        var mobiles = new Mobile[1];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(301, 301, 0));

            var enumerator = map.GetMobilesInBounds<Mobile>(rect, makeBoundsInclusive: true).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(mobiles[0], enumerator.Current);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileEnumerator_MapNullYieldsEmpty()
    {
        var enumerator = new Map.MobileEnumerator<Mobile>(null, Rectangle2D.Empty, false);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void MobileEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(400, 400, 16, 16);

        var mobiles = new[]
        {
            CreateMobile(map, new Point3D(405, 405, 0)),
            CreateMobile(map, new Point3D(406, 405, 0))
        };

        try
        {
            var enumerator = map.GetMobilesInBounds<Mobile>(rect).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            mobiles[1].Delete();

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
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileEnumerator_StepsAcrossSectors()
    {
        var map = Map.Felucca;
        var rect = new Rectangle2D(500, 500, Map.SectorSize * 2, Map.SectorSize * 2);

        var mobiles = new[]
        {
            CreateMobile(map, new Point3D(rect.X + 1, rect.Y + 1, 0)),
            CreateMobile(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + 1, 0)),
            CreateMobile(map, new Point3D(rect.X + Map.SectorSize + 1, rect.Y + Map.SectorSize + 1, 0))
        };

        try
        {
            var result = new List<Mobile>();
            foreach (var m in map.GetMobilesInBounds<Mobile>(rect))
            {
                result.Add(m);
            }

            Assert.Equal(mobiles, result);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileEnumerator_MapBoundsAreClamped()
    {
        var map = Map.Felucca;
        var width = map.Width;
        var height = map.Height;

        var rect = new Rectangle2D(width - Map.SectorSize - 2, height - Map.SectorSize - 2, Map.SectorSize * 2, Map.SectorSize * 2);

        var mobiles = new[]
        {
            CreateMobile(map, new Point3D(width - 2, height - 2, 0))
        };

        try
        {
            var enumerator = map.GetMobilesInBounds<Mobile>(rect).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(mobiles[0], enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileEnumerator_ZeroRangeReturnsOnlyCenter()
    {
        var map = Map.Felucca;
        var center = new Point3D(700, 700, 0);
        const int range = 0;

        var mobiles = new Mobile[2];
        try
        {
            mobiles[0] = CreateMobile(map, center);              // Exact center
            mobiles[1] = CreateMobile(map, new Point3D(701, 700, 0)); // 1 tile away

            var found = new List<Mobile>();
            foreach (var mobile in map.GetMobilesInRange<Mobile>(center, range))
            {
                found.Add(mobile);
            }

            Assert.Single(found);
            Assert.Equal(mobiles[0], found[0]);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileEnumerator_NegativeRangeCreates1x1Bounds()
    {
        var map = Map.Felucca;
        var center = new Point3D(750, 750, 0);
        const int range = -5;

        var mobiles = new Mobile[2];
        try
        {
            mobiles[0] = CreateMobile(map, center);
            mobiles[1] = CreateMobile(map, new Point3D(751, 750, 0)); // 1 tile away

            var found = new List<Mobile>();
            foreach (var mobile in map.GetMobilesInRange<Mobile>(center, range))
            {
                found.Add(mobile);
            }

            // With negative range creating a 1x1 bounds, only exact center matches
            Assert.Single(found);
            Assert.Equal(mobiles[0], found[0]);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    private static Mobile CreateMobile(Map map, Point3D location)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }

    private static void DeleteAll(Mobile[] mobiles)
    {
        for (var i = 0; i < mobiles.Length; i++)
        {
            mobiles[i]?.Delete();
        }
    }
}
