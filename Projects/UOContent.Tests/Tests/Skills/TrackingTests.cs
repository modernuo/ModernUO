using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.SkillHandlers;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class TrackingTests
{
    /// <summary>
    /// Tests that tracking correctly finds the closest mobiles when there are more than 12 available.
    /// This validates the GetClosestMobs logic, especially the early exit optimization.
    /// </summary>
    [Fact]
    public void Tracking_FindsClosestMobiles_WhenManyAvailable()
    {
        var map = Map.Felucca;
        var center = new Point3D(1000, 1000, 0);

        var tracker = CreatePlayerMobile(map, center);
        tracker.Skills.Tracking.BaseFixedPoint = 1000; // 100.0 skill = 110 range

        var mobiles = new List<TestAnimal>();

        try
        {
            // Create 20 animals at various distances
            // First 12 should be the closest ones we find
            for (var i = 0; i < 20; i++)
            {
                var distance = i + 1; // Distance from 1 to 20
                var location = new Point3D(center.X + distance, center.Y, 0);
                var animal = CreateAnimal(map, location);
                mobiles.Add(animal);
            }

            // Invoke tracking through the skill system
            // We can't directly test GetClosestMobs since it's private, but we can verify
            // the behavior by checking what the gump would show

            // Use reflection to test the private method
            var method = typeof(TrackWhoGump).GetMethod(
                "GetClosestMobs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );

            Assert.NotNull(method);

            var range = Math.Clamp(10 + (int)tracker.Skills.Tracking.Value, 0, 100);
            var result = (Mobile[])method.Invoke(null, new object[] { tracker, range, 0 }); // 0 = animals

            // Should return at most 12 mobiles
            Assert.True(result.Length <= 12);

            // Should return the 12 closest (distances 1-12)
            Assert.Equal(12, result.Length);

            // Verify they are sorted by distance
            for (var i = 0; i < result.Length - 1; i++)
            {
                var dist1 = result[i].GetDistanceToSqrt(center);
                var dist2 = result[i + 1].GetDistanceToSqrt(center);
                Assert.True(dist1 <= dist2, $"Mobiles not sorted: {dist1} > {dist2}");
            }

            // Verify the first mobile is the closest (distance 1)
            Assert.Equal(mobiles[0], result[0]);

            // Verify the last mobile is at distance 12
            Assert.Equal(mobiles[11], result[11]);

            // Verify mobile at distance 13 is NOT included
            Assert.DoesNotContain(mobiles[12], result);
        }
        finally
        {
            tracker?.Delete();
            foreach (var mob in mobiles)
            {
                mob?.Delete();
            }
        }
    }

    /// <summary>
    /// Tests that tracking correctly handles the case where there are exactly 12 mobiles available.
    /// </summary>
    [Fact]
    public void Tracking_FindsAllMobiles_WhenExactly12Available()
    {
        var map = Map.Felucca;
        var center = new Point3D(2000, 2000, 0);

        var tracker = CreatePlayerMobile(map, center);
        tracker.Skills.Tracking.BaseFixedPoint = 1000; // 100.0 skill = 110 range

        var mobiles = new List<TestAnimal>();

        try
        {
            // Create exactly 12 animals
            for (var i = 0; i < 12; i++)
            {
                var distance = i + 1;
                var location = new Point3D(center.X + distance, center.Y, 0);
                var animal = CreateAnimal(map, location);
                mobiles.Add(animal);
            }

            var method = typeof(TrackWhoGump).GetMethod(
                "GetClosestMobs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );

            var range = Math.Clamp(10 + (int)tracker.Skills.Tracking.Value, 0, 100);
            var result = (Mobile[])method.Invoke(null, new object[] { tracker, range, 0 });

            Assert.Equal(12, result.Length);

            // Verify all mobiles are included
            foreach (var mob in mobiles)
            {
                Assert.Contains(mob, result);
            }
        }
        finally
        {
            tracker?.Delete();
            foreach (var mob in mobiles)
            {
                mob?.Delete();
            }
        }
    }

    /// <summary>
    /// Tests that tracking correctly handles the case where there are fewer than 12 mobiles available.
    /// </summary>
    [Fact]
    public void Tracking_FindsAllMobiles_WhenFewerThan12Available()
    {
        var map = Map.Felucca;
        var center = new Point3D(3000, 3000, 0);

        var tracker = CreatePlayerMobile(map, center);
        tracker.Skills.Tracking.BaseFixedPoint = 1000;

        var mobiles = new List<TestAnimal>();

        try
        {
            // Create only 5 animals
            for (var i = 0; i < 5; i++)
            {
                var distance = i + 1;
                var location = new Point3D(center.X + distance, center.Y, 0);
                var animal = CreateAnimal(map, location);
                mobiles.Add(animal);
            }

            var method = typeof(TrackWhoGump).GetMethod(
                "GetClosestMobs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );

            var range = Math.Clamp(10 + (int)tracker.Skills.Tracking.Value, 0, 100);
            var result = (Mobile[])method.Invoke(null, new object[] { tracker, range, 0 });

            Assert.Equal(5, result.Length);

            // Verify all mobiles are included
            foreach (var mob in mobiles)
            {
                Assert.Contains(mob, result);
            }
        }
        finally
        {
            tracker?.Delete();
            foreach (var mob in mobiles)
            {
                mob?.Delete();
            }
        }
    }

    /// <summary>
    /// Tests that the early exit optimization works correctly when mobiles in farther sectors
    /// are closer than mobiles in nearer sectors (worst case for the optimization).
    /// </summary>
    [Fact]
    public void Tracking_EarlyExitWorksCorrectly_WithFarSectorNearMobiles()
    {
        var map = Map.Felucca;
        // Use a location that puts mobiles in different sectors
        var center = new Point3D(1500, 1500, 0);

        var tracker = CreatePlayerMobile(map, center);
        tracker.Skills.Tracking.BaseFixedPoint = 1000;

        var mobiles = new List<TestAnimal>();

        try
        {
            // Create 15 animals where some farther ones might be in closer proximity
            // but in different sectors
            for (var i = 0; i < 15; i++)
            {
                var distance = i + 1;
                // Alternate between X and Y to potentially cross sector boundaries
                var location = i % 2 == 0
                    ? new Point3D(center.X + distance, center.Y, 0)
                    : new Point3D(center.X, center.Y + distance, 0);
                var animal = CreateAnimal(map, location);
                mobiles.Add(animal);
            }

            var method = typeof(TrackWhoGump).GetMethod(
                "GetClosestMobs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );

            var range = Math.Clamp(10 + (int)tracker.Skills.Tracking.Value, 0, 100);
            var result = (Mobile[])method.Invoke(null, new object[] { tracker, range, 0 });

            Assert.True(result.Length <= 12);

            // Get the actual 12 closest by brute force
            var allDistances = mobiles
                .Select(m => (Mobile: m, Distance: m.GetDistanceToSqrt(center)))
                .OrderBy(x => x.Distance)
                .ToList();

            var expected12Closest = allDistances.Take(12).ToList();

            // The result should match the actual 12 closest
            Assert.Equal(expected12Closest.Count, result.Length);

            // Verify all returned mobiles are in the expected 12 closest
            foreach (var mob in result)
            {
                Assert.Contains(mob, expected12Closest.Select(x => x.Mobile));
            }

            // Verify they are sorted by distance (main invariant)
            for (var i = 0; i < result.Length - 1; i++)
            {
                var dist1 = result[i].GetDistanceToSqrt(center);
                var dist2 = result[i + 1].GetDistanceToSqrt(center);
                Assert.True(dist1 <= dist2, $"Mobiles not sorted by distance: {dist1} > {dist2}");
            }

            // Verify we got the closest mobiles (not just any 12)
            var maxResultDistance = result.Max(m => m.GetDistanceToSqrt(center));
            var minExcludedDistance = allDistances.Skip(12).Any()
                ? allDistances.Skip(12).Min(x => x.Distance)
                : double.MaxValue;
            Assert.True(maxResultDistance <= minExcludedDistance,
                $"Found a closer excluded mobile: max in result={maxResultDistance}, min excluded={minExcludedDistance}");
        }
        finally
        {
            tracker?.Delete();
            foreach (var mob in mobiles)
            {
                mob?.Delete();
            }
        }
    }

    private static PlayerMobile CreatePlayerMobile(Map map, Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }

    private static TestAnimal CreateAnimal(Map map, Point3D location)
    {
        var animal = new TestAnimal(World.NewMobile);
        animal.DefaultMobileInit();
        animal.MoveToWorld(location, map);
        return animal;
    }

    private class TestAnimal : BaseCreature
    {
        public TestAnimal(Serial serial) : base(serial)
        {
            Body = 0xD8; // Llama body - an animal body
        }
    }
}

