using Xunit;

namespace Server.Tests.Maps;

/// <summary>
/// Tests for CanSpawnMobile that don't require TileData (client files).
/// For tests that require client files, see CanSpawnMobileTileDataTests.
/// </summary>
[Collection("Sequential Server Tests")]
public class CanSpawnMobileTests
{
    [Fact]
    public void CanSpawnMobile_InternalMapReturnsFalse()
    {
        var result = Map.Internal.CanSpawnMobile(100, 100, -128, 127, false, false, out var spawnZ);

        Assert.False(result);
        Assert.Equal(0, spawnZ);
    }

    [Fact]
    public void CanSpawnMobile_InvalidCoordinatesReturnsFalse()
    {
        var map = Map.Felucca;

        Assert.False(map.CanSpawnMobile(-1, 100, -128, 127, false, false, out _));
        Assert.False(map.CanSpawnMobile(100, -1, -128, 127, false, false, out _));
        Assert.False(map.CanSpawnMobile(map.Width + 1, 100, -128, 127, false, false, out _));
        Assert.False(map.CanSpawnMobile(100, map.Height + 1, -128, 127, false, false, out _));
    }

    // Use coordinates near Britain which should be walkable land in both test and real data
    private const int TestLandX = 1500;
    private const int TestLandY = 1600;

    [Fact]
    public void CanSpawnMobile_EmptyLocationWithLandReturnsTrue()
    {
        var map = Map.Felucca;
        map.GetAverageZ(TestLandX, TestLandY, out _, out var landZ, out _);

        // Test with full Z range to find land
        var result = map.CanSpawnMobile(TestLandX, TestLandY, -128, 127, false, false, out var spawnZ);

        // Should find a valid spawn point (land surface)
        Assert.True(result, $"Expected to find valid spawn point on land at ({TestLandX}, {TestLandY})");
    }

    [Fact]
    public void CanSpawnMobile_ZRangeExcludingLandFails()
    {
        var map = Map.Felucca;
        map.GetAverageZ(TestLandX, TestLandY, out _, out var avgZ, out _);

        // Use a Z range that excludes the land surface (50+ units above land)
        var result = map.CanSpawnMobile(TestLandX, TestLandY, avgZ + 50, avgZ + 100, false, false, out var spawnZ);

        Assert.False(result, $"Expected no spawn point above land at Z={avgZ}");
    }

    [Fact]
    public void CanSpawnMobile_CantWalkSkipsLandSurface()
    {
        var map = Map.Felucca;
        // cantWalk=true means the mob can only swim, so land shouldn't be valid
        var result = map.CanSpawnMobile(TestLandX, TestLandY, -128, 127, false, true, out var spawnZ);

        Assert.False(result, "cantWalk=true should not find land as valid surface");
    }

    [Fact]
    public void CanSpawnMobile_MobileBlocksSpawn()
    {
        var map = Map.Felucca;
        // Use the same test coordinates
        map.GetAverageZ(TestLandX, TestLandY, out _, out var landZ, out _);

        Mobile blockingMobile = null;
        try
        {
            // Create a mobile at the land surface
            blockingMobile = new TestMobile();
            blockingMobile.MoveToWorld(new Point3D(TestLandX, TestLandY, landZ), map);

            // The mobile should block spawning at its location and Z
            var result = map.CanSpawnMobile(TestLandX, TestLandY, landZ - 16, landZ + 16, false, false, out var spawnZ);

            // Land surface is blocked by mobile at the same Z
            Assert.False(result, "Mobile should block spawn at same location");
        }
        finally
        {
            blockingMobile?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_HiddenGMDoesNotBlock()
    {
        var map = Map.Felucca;
        // Use slightly different coordinates to avoid test interference
        const int x = TestLandX + 10;
        const int y = TestLandY + 10;
        map.GetAverageZ(x, y, out _, out var landZ, out _);

        Mobile gm = null;
        try
        {
            // Create a hidden GM mobile
            gm = new TestMobile
            {
                AccessLevel = AccessLevel.GameMaster,
                Hidden = true
            };
            gm.MoveToWorld(new Point3D(x, y, landZ), map);

            // Hidden GM should not block spawning
            var result = map.CanSpawnMobile(x, y, landZ - 16, landZ + 16, false, false, out var spawnZ);

            Assert.True(result, "Hidden GM should not block spawn");
        }
        finally
        {
            gm?.Delete();
        }
    }

    [Fact]
    public void CanSpawnMobile_MobileAtDifferentZDoesNotBlock()
    {
        var map = Map.Felucca;
        // Use slightly different coordinates to avoid test interference
        const int x = TestLandX + 20;
        const int y = TestLandY + 20;
        map.GetAverageZ(x, y, out _, out var landZ, out _);

        Mobile blockingMobile = null;
        try
        {
            // Create a mobile at Z=landZ+50 (far above the land)
            blockingMobile = new TestMobile();
            blockingMobile.MoveToWorld(new Point3D(x, y, landZ + 50), map);

            // Land should still be valid (mobile above doesn't block ground level)
            var result = map.CanSpawnMobile(x, y, landZ - 16, landZ + 16, false, false, out var spawnZ);

            Assert.True(result, "Mobile at different Z should not block");
        }
        finally
        {
            blockingMobile?.Delete();
        }
    }

    private class TestMobile : Mobile
    {
        public TestMobile()
        {
            AccessLevel = AccessLevel.Player;
            Hidden = false;
        }
    }
}
