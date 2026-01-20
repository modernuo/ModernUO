using System;
using System.Collections.Generic;
using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class MobileByDistanceEnumeratorTests
{
    [Fact]
    public void MobileByDistanceEnumerator_ReturnsNearbyMobiles()
    {
        var map = Map.Felucca;
        var center = new Point3D(100, 100, 0);
        const int range = 5;

        var mobiles = new TestMobile[3];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(102, 102, 0)); // Within range
            mobiles[1] = CreateMobile(map, new Point3D(98, 98, 0));   // Within range
            mobiles[2] = CreateMobile(map, new Point3D(110, 110, 0)); // Outside range

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
            {
                found.Add(mobile);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(mobiles[0], found);
            Assert.Contains(mobiles[1], found);
            Assert.DoesNotContain(mobiles[2], found);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_DeletedMobilesAreSkipped()
    {
        var map = Map.Felucca;
        var center = new Point3D(200, 200, 0);
        const int range = 5;

        var mobiles = new TestMobile[3];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(202, 202, 0));
            mobiles[1] = CreateMobile(map, new Point3D(203, 202, 0));
            mobiles[2] = CreateMobile(map, new Point3D(204, 202, 0));

            mobiles[1].Delete();

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
            {
                found.Add(mobile);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(mobiles[0], found);
            Assert.Contains(mobiles[2], found);
            Assert.DoesNotContain(mobiles[1], found);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_ReturnsMinDistance()
    {
        var map = Map.Felucca;
        var center = new Point3D(300, 300, 0);
        const int range = 10;

        var mobiles = new TestMobile[2];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(305, 305, 0));
            mobiles[1] = CreateMobile(map, new Point3D(302, 302, 0));

            var foundWithDistance = new List<(Mobile, int)>();
            foreach (var result in map.GetMobilesInRangeByDistance(center, range))
            {
                foundWithDistance.Add(result);
            }

            Assert.Equal(2, foundWithDistance.Count);
            // Each mobile should have a non-negative min distance
            Assert.All(foundWithDistance, item => Assert.True(item.Item2 >= 0));
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_OrderedBySector()
    {
        var map = Map.Felucca;
        var center = new Point3D(400, 400, 0);
        const int range = Map.SectorSize * 2;

        var mobiles = new TestMobile[3];
        try
        {
            // Place mobiles in different sectors
            mobiles[0] = CreateMobile(map, new Point3D(center.X + 2, center.Y + 2, 0));
            mobiles[1] = CreateMobile(map, new Point3D(center.X + Map.SectorSize + 2, center.Y + 2, 0));
            mobiles[2] = CreateMobile(map, new Point3D(center.X + 2, center.Y + Map.SectorSize + 2, 0));

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
            {
                found.Add(mobile);
            }

            Assert.Equal(3, found.Count);
            Assert.Contains(mobiles[0], found);
            Assert.Contains(mobiles[1], found);
            Assert.Contains(mobiles[2], found);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_MapNullYieldsEmpty()
    {
        var center = new Point2D(0, 0);
        var bounds = new Rectangle2D(center.m_X - 10, center.m_Y - 10, 21, 21);
        var enumerator = new Map.MobileDistanceEnumerable<Mobile>(null, bounds, center, false).GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void MobileByDistanceEnumerator_ThrowsOnVersionChange()
    {
        var map = Map.Felucca;
        var center = new Point3D(500, 500, 0);
        const int range = 5;

        var mobiles = new[]
        {
            CreateMobile(map, new Point3D(502, 502, 0)),
            CreateMobile(map, new Point3D(503, 502, 0))
        };

        try
        {
            var enumerator = map.GetMobilesInRangeByDistance(center, range).GetEnumerator();
            Assert.True(enumerator.MoveNext());

            mobiles[1].Delete();

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
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_ZeroRangeReturnsOnlyCenter()
    {
        var map = Map.Felucca;
        var center = new Point3D(600, 600, 0);
        const int range = 0;

        var mobiles = new TestMobile[2];
        try
        {
            mobiles[0] = CreateMobile(map, center);              // Exact center
            mobiles[1] = CreateMobile(map, new Point3D(601, 600, 0)); // 1 tile away

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
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
    public void MobileByDistanceEnumerator_FiltersByType()
    {
        var map = Map.Felucca;
        var center = new Point3D(700, 700, 0);
        const int range = 5;

        var player = new TestPlayerMobile(World.NewMobile);
        var npc = new TestMobile(World.NewMobile);

        try
        {
            player.DefaultMobileInit();
            npc.DefaultMobileInit();
            player.MoveToWorld(new Point3D(702, 702, 0), map);
            npc.MoveToWorld(new Point3D(703, 702, 0), map);

            var foundPlayers = new List<TestPlayerMobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance<TestPlayerMobile>(center, range))
            {
                foundPlayers.Add(mobile);
            }

            Assert.Single(foundPlayers);
            Assert.Equal(player, foundPlayers[0]);
        }
        finally
        {
            player.Delete();
            npc.Delete();
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_UsesDifferentPointOverloads()
    {
        var map = Map.Felucca;
        var center = new Point3D(800, 800, 5);
        const int range = 5;

        var mobiles = new TestMobile[1];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(802, 802, 0));

            // Test Point3D overload
            var found1 = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
            {
                found1.Add(mobile);
            }

            // Test (int, int) overload
            var found2 = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance<Mobile>(center.X, center.Y, range))
            {
                found2.Add(mobile);
            }

            // Test Point2D overload
            var found3 = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(new Point2D(center.X, center.Y), range))
            {
                found3.Add(mobile);
            }

            Assert.Single(found1);
            Assert.Equal(mobiles[0], found1[0]);
            Assert.Equal(found1, found2);
            Assert.Equal(found1, found3);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_RingTraversal()
    {
        var map = Map.Felucca;
        var center = new Point3D(900, 900, 0);
        const int range = Map.SectorSize * 2;

        var mobiles = new TestMobile[4];
        try
        {
            // Place mobiles in different rings around the center
            mobiles[0] = CreateMobile(map, center); // Ring 0 (center sector)
            mobiles[1] = CreateMobile(map, new Point3D(center.X + Map.SectorSize, center.Y, 0)); // Ring 1
            mobiles[2] = CreateMobile(map, new Point3D(center.X, center.Y + Map.SectorSize, 0)); // Ring 1
            mobiles[3] = CreateMobile(map, new Point3D(center.X + Map.SectorSize * 2 - 1, center.Y, 0)); // Ring 2

            var found = new List<Mobile>();
            var distances = new List<int>();
            foreach (var (mobile, minDistance) in map.GetMobilesInRangeByDistance(center, range))
            {
                found.Add(mobile);
                distances.Add(minDistance);
            }

            // All mobiles should be found
            Assert.Equal(4, found.Count);
            Assert.Contains(mobiles[0], found);
            Assert.Contains(mobiles[1], found);
            Assert.Contains(mobiles[2], found);
            Assert.Contains(mobiles[3], found);

            // Mobiles should be processed by sector distance (ring-based)
            // The center mobile should have distance 0
            var centerIndex = found.IndexOf(mobiles[0]);
            Assert.Equal(0, distances[centerIndex]);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_MapBoundsAreClamped()
    {
        var map = Map.Felucca;
        var width = map.Width;
        var height = map.Height;

        var center = new Point3D(width - 2, height - 2, 0);
        const int range = Map.SectorSize * 2;

        var mobiles = new TestMobile[1];
        try
        {
            mobiles[0] = CreateMobile(map, center);

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
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
    public void MobileByDistanceEnumerator_NegativeRangeIsZero()
    {
        var map = Map.Felucca;
        var center = new Point3D(1000, 1000, 0);
        const int range = -5;

        var mobiles = new TestMobile[2];
        try
        {
            mobiles[0] = CreateMobile(map, center);
            mobiles[1] = CreateMobile(map, new Point3D(1001, 1000, 0));

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
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

    [Fact]
    public void MobileByDistanceEnumerator_MultipleRings()
    {
        var map = Map.Felucca;
        var center = new Point3D(1100, 1100, 0);
        const int range = Map.SectorSize * 3;

        var mobiles = new List<TestMobile>();
        try
        {
            // Create a grid of mobiles across multiple sectors
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
                    mobiles.Add(CreateMobile(map, pos));
                }
            }

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInRangeByDistance(center, range))
            {
                found.Add(mobile);
            }

            // Should find all mobiles within range
            Assert.True(found.Count > 0);
            Assert.All(found, mobile =>
            {
                var dx = mobile.X - center.X;
                var dy = mobile.Y - center.Y;
                var distSq = dx * dx + dy * dy;
                Assert.True(distSq <= range * range);
            });
        }
        finally
        {
            foreach (var mobile in mobiles)
            {
                mobile?.Delete();
            }
        }
    }

    private static TestMobile CreateMobile(Map map, Point3D location)
    {
        var mobile = new TestMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }

    private static void DeleteAll(TestMobile[] mobiles)
    {
        for (var i = 0; i < mobiles.Length; i++)
        {
            mobiles[i]?.Delete();
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_Bounds_FindsMobilesInBounds()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(100, 100, 50, 50);

        var mobiles = new TestMobile[3];
        try
        {
            // Mobile inside bounds
            mobiles[0] = CreateMobile(map, new Point3D(120, 120, 0));
            // Mobile at edge of bounds
            mobiles[1] = CreateMobile(map, new Point3D(149, 149, 0));
            // Mobile outside bounds
            mobiles[2] = CreateMobile(map, new Point3D(200, 200, 0));

            var found = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInBoundsByDistance<Mobile>(bounds))
            {
                found.Add(mobile);
            }

            Assert.Equal(2, found.Count);
            Assert.Contains(mobiles[0], found);
            Assert.Contains(mobiles[1], found);
            Assert.DoesNotContain(mobiles[2], found);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_Bounds_MakeBoundsInclusive()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(100, 100, 50, 50);

        var mobiles = new TestMobile[2];
        try
        {
            // Mobile at edge (inclusive)
            mobiles[0] = CreateMobile(map, new Point3D(149, 149, 0));
            // Mobile just outside edge (will be included with makeBoundsInclusive)
            mobiles[1] = CreateMobile(map, new Point3D(150, 150, 0));

            var foundWithoutInclusive = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInBoundsByDistance<Mobile>(bounds))
            {
                foundWithoutInclusive.Add(mobile);
            }

            var foundWithInclusive = new List<Mobile>();
            foreach (var (mobile, _) in map.GetMobilesInBoundsByDistance<Mobile>(bounds, true))
            {
                foundWithInclusive.Add(mobile);
            }

            Assert.Single(foundWithoutInclusive);
            Assert.Contains(mobiles[0], foundWithoutInclusive);

            Assert.Equal(2, foundWithInclusive.Count);
            Assert.Contains(mobiles[0], foundWithInclusive);
            Assert.Contains(mobiles[1], foundWithInclusive);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_Bounds_ReturnsMinDistance()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(300, 300, 20, 20);

        var mobiles = new TestMobile[2];
        try
        {
            mobiles[0] = CreateMobile(map, new Point3D(305, 305, 0));
            mobiles[1] = CreateMobile(map, new Point3D(315, 315, 0));

            var foundWithDistance = new List<(Mobile, int)>();
            foreach (var result in map.GetMobilesInBoundsByDistance<Mobile>(bounds))
            {
                foundWithDistance.Add(result);
            }

            Assert.Equal(2, foundWithDistance.Count);
            Assert.All(foundWithDistance, item => Assert.True(item.Item2 >= 0));
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    [Fact]
    public void MobileByDistanceEnumerator_Bounds_OrdersByProximityToCenter()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle2D(500, 500, 64, 64);

        var mobiles = new TestMobile[3];
        try
        {
            // Place mobiles at different distances from center
            mobiles[0] = CreateMobile(map, new Point3D(532, 532, 0)); // At center
            mobiles[1] = CreateMobile(map, new Point3D(548, 532, 0)); // 16 tiles away
            mobiles[2] = CreateMobile(map, new Point3D(563, 563, 0)); // Far corner

            var found = new List<(Mobile, int)>();
            foreach (var result in map.GetMobilesInBoundsByDistance<Mobile>(bounds))
            {
                found.Add(result);
            }

            Assert.Equal(3, found.Count);

            // Verify ordering by distance - closer mobiles should be found earlier (lower minDistance)
            var mobile0Index = found.FindIndex(x => x.Item1 == mobiles[0]);
            var mobile1Index = found.FindIndex(x => x.Item1 == mobiles[1]);
            var mobile2Index = found.FindIndex(x => x.Item1 == mobiles[2]);

            // The minDistance should increase (or stay the same) as we go through the list
            Assert.True(found[mobile0Index].Item2 <= found[mobile1Index].Item2);
            Assert.True(found[mobile1Index].Item2 <= found[mobile2Index].Item2);
        }
        finally
        {
            DeleteAll(mobiles);
        }
    }

    // Test implementation of Mobile
    private class TestMobile : Mobile
    {
        public TestMobile(Serial serial) : base(serial)
        {
        }
    }

    private class TestPlayerMobile : Mobile
    {
        public TestPlayerMobile(Serial serial) : base(serial)
        {
        }
    }
}

