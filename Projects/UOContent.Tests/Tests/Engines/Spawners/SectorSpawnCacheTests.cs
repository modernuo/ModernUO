using System.Collections.Generic;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests;

public class BitMask256Tests
{
    [Fact]
    public void BitMask256_InitialState_AllBitsZero()
    {
        var mask = BitMask256.AllClear();

        Assert.Equal(0, mask.PopCount());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(191)]
    [InlineData(192)]
    [InlineData(255)]
    public void BitMask256_SetBit_SetsCorrectBit(int bitIndex)
    {
        var mask = BitMask256.AllClear();

        mask.SetBit(bitIndex);

        Assert.True(mask.GetBit(bitIndex));
        Assert.Equal(1, mask.PopCount());
    }

    [Fact]
    public void BitMask256_SetMultipleBits_CountsCorrectly()
    {
        var mask = BitMask256.AllClear();

        mask.SetBit(0);
        mask.SetBit(64);
        mask.SetBit(128);
        mask.SetBit(192);

        Assert.Equal(4, mask.PopCount());
        Assert.True(mask.GetBit(0));
        Assert.True(mask.GetBit(64));
        Assert.True(mask.GetBit(128));
        Assert.True(mask.GetBit(192));
        Assert.False(mask.GetBit(1));
    }

    [Fact]
    public void BitMask256_ClearBit_ClearsCorrectBit()
    {
        var mask = BitMask256.AllClear();

        mask.SetBit(50);
        mask.SetBit(100);
        Assert.Equal(2, mask.PopCount());

        mask.ClearBit(50);

        Assert.False(mask.GetBit(50));
        Assert.True(mask.GetBit(100));
        Assert.Equal(1, mask.PopCount());
    }

    [Theory]
    [InlineData(0, 0)]   // First bit of first ulong
    [InlineData(1, 64)]  // First bit of second ulong
    [InlineData(2, 128)] // First bit of third ulong
    [InlineData(3, 192)] // First bit of fourth ulong
    public void BitMask256_GetNthSetBit_FirstBitInEachUlong(int n, int expectedPosition)
    {
        var mask = BitMask256.AllClear();

        // Set first bit in each ulong
        mask.SetBit(0);
        mask.SetBit(64);
        mask.SetBit(128);
        mask.SetBit(192);

        Assert.Equal(expectedPosition, mask.GetNthSetBit(n));
    }

    [Fact]
    public void BitMask256_GetNthSetBit_WithinSingleUlong()
    {
        var mask = BitMask256.AllClear();

        mask.SetBit(5);
        mask.SetBit(10);
        mask.SetBit(20);

        Assert.Equal(5, mask.GetNthSetBit(0));
        Assert.Equal(10, mask.GetNthSetBit(1));
        Assert.Equal(20, mask.GetNthSetBit(2));
    }
}

[Collection("Sequential UOContent Tests")]
public class SectorSpawnCacheManagerTests
{
    public SectorSpawnCacheManagerTests()
    {
        // Clear cache before each test
        SectorSpawnCacheManager.ClearAll();
    }

    [Fact]
    public void ClearAll_ResetsCache()
    {
        SectorSpawnCacheManager.ClearAll();
        Assert.Equal(0, SectorSpawnCacheManager.CachedSectorCount);
        Assert.Equal(0, SectorSpawnCacheManager.LandCacheCount);
        Assert.Equal(0, SectorSpawnCacheManager.WaterCacheCount);
    }

    [Fact]
    public void SetValid_CreatesSectorCache()
    {
        var map = Map.Felucca;
        var pos = new Point3D(100, 100, 0);

        SectorSpawnCacheManager.SetValid(map, pos, isWater: false);

        Assert.Equal(1, SectorSpawnCacheManager.CachedSectorCount);
        Assert.Equal(1, SectorSpawnCacheManager.LandCacheCount);
        Assert.Equal(0, SectorSpawnCacheManager.WaterCacheCount);
    }

    [Fact]
    public void SetValid_Water_CreatesWaterCache()
    {
        var map = Map.Felucca;
        var pos = new Point3D(100, 100, 0);

        SectorSpawnCacheManager.SetValid(map, pos, isWater: true);

        Assert.Equal(1, SectorSpawnCacheManager.CachedSectorCount);
        Assert.Equal(0, SectorSpawnCacheManager.LandCacheCount);
        Assert.Equal(1, SectorSpawnCacheManager.WaterCacheCount);
    }

    [Fact]
    public void SetValid_LandAndWater_SeparateCaches()
    {
        var map = Map.Felucca;

        // Same sector, different cache types
        SectorSpawnCacheManager.SetValid(map, new Point3D(100, 100, 0), isWater: false);
        SectorSpawnCacheManager.SetValid(map, new Point3D(105, 105, 0), isWater: true);

        Assert.Equal(2, SectorSpawnCacheManager.CachedSectorCount);
        Assert.Equal(1, SectorSpawnCacheManager.LandCacheCount);
        Assert.Equal(1, SectorSpawnCacheManager.WaterCacheCount);
    }

    [Fact]
    public void SetValid_MultipleSameSector_OnlyOneCacheEntry()
    {
        var map = Map.Felucca;

        // All positions in same 16x16 sector (sector 6,6 for coords 96-111)
        SectorSpawnCacheManager.SetValid(map, new Point3D(96, 96, 0), isWater: false);
        SectorSpawnCacheManager.SetValid(map, new Point3D(100, 100, 0), isWater: false);
        SectorSpawnCacheManager.SetValid(map, new Point3D(111, 111, 0), isWater: false);

        Assert.Equal(1, SectorSpawnCacheManager.CachedSectorCount);
    }

    [Fact]
    public void SetValid_DifferentSectors_MultipleCacheEntries()
    {
        var map = Map.Felucca;

        // Different sectors
        SectorSpawnCacheManager.SetValid(map, new Point3D(0, 0, 0), isWater: false);    // Sector 0,0
        SectorSpawnCacheManager.SetValid(map, new Point3D(16, 0, 0), isWater: false);   // Sector 1,0
        SectorSpawnCacheManager.SetValid(map, new Point3D(0, 16, 0), isWater: false);   // Sector 0,1

        Assert.Equal(3, SectorSpawnCacheManager.CachedSectorCount);
    }

    [Fact]
    public void TryGetRandomPosition_EmptyCache_ReturnsFalse()
    {
        var map = Map.Felucca;
        var bounds = new Rectangle3D(0, 0, 0, 100, 100, 20);

        var result = SectorSpawnCacheManager.TryGetRandomPosition(map, bounds, isWater: false, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetRandomPosition_WithCachedPosition_ReturnsTrue()
    {
        var map = Map.Felucca;
        var cachedPos = new Point3D(50, 50, 0);
        var bounds = new Rectangle3D(0, 0, -128, 100, 100, 256);

        SectorSpawnCacheManager.SetValid(map, cachedPos, isWater: false);

        var result = SectorSpawnCacheManager.TryGetRandomPosition(map, bounds, isWater: false, out var pos);

        Assert.True(result);
        Assert.Equal(cachedPos.X, pos.X);
        Assert.Equal(cachedPos.Y, pos.Y);
    }

    [Fact]
    public void TryGetRandomPosition_WaterVsLand_Separated()
    {
        var map = Map.Felucca;

        var landPos = new Point3D(50, 50, 0);
        var waterPos = new Point3D(60, 60, 0);
        var bounds = new Rectangle3D(0, 0, -128, 100, 100, 256);

        SectorSpawnCacheManager.SetValid(map, landPos, isWater: false);
        SectorSpawnCacheManager.SetValid(map, waterPos, isWater: true);

        // Request land position
        var landResult = SectorSpawnCacheManager.TryGetRandomPosition(map, bounds, isWater: false, out var pos1);
        Assert.True(landResult);
        Assert.Equal(landPos.X, pos1.X);
        Assert.Equal(landPos.Y, pos1.Y);

        // Request water position
        var waterResult = SectorSpawnCacheManager.TryGetRandomPosition(map, bounds, isWater: true, out var pos2);
        Assert.True(waterResult);
        Assert.Equal(waterPos.X, pos2.X);
        Assert.Equal(waterPos.Y, pos2.Y);
    }

    [Fact]
    public void TryGetRandomPosition_OutsideBounds_ReturnsFalse()
    {
        var map = Map.Felucca;
        var cachedPos = new Point3D(200, 200, 0);
        var bounds = new Rectangle3D(0, 0, -128, 100, 100, 256); // Only covers 0-99

        SectorSpawnCacheManager.SetValid(map, cachedPos, isWater: false);

        var result = SectorSpawnCacheManager.TryGetRandomPosition(map, bounds, isWater: false, out _);

        Assert.False(result);
    }

    [Fact]
    public void InvalidateSectors_RemovesCachedData()
    {
        var map = Map.Felucca;

        SectorSpawnCacheManager.SetValid(map, new Point3D(50, 50, 0), isWater: false);
        Assert.Equal(1, SectorSpawnCacheManager.CachedSectorCount);

        SectorSpawnCacheManager.InvalidateSectors(map, new Rectangle2D(0, 0, 100, 100));

        Assert.Equal(0, SectorSpawnCacheManager.CachedSectorCount);
    }

    [Fact]
    public void InvalidateSectors_RemovesBothLandAndWater()
    {
        var map = Map.Felucca;

        // Same sector, both land and water
        SectorSpawnCacheManager.SetValid(map, new Point3D(50, 50, 0), isWater: false);
        SectorSpawnCacheManager.SetValid(map, new Point3D(55, 55, 0), isWater: true);
        Assert.Equal(2, SectorSpawnCacheManager.CachedSectorCount);

        SectorSpawnCacheManager.InvalidateSectors(map, new Rectangle2D(0, 0, 100, 100));

        Assert.Equal(0, SectorSpawnCacheManager.CachedSectorCount);
        Assert.Equal(0, SectorSpawnCacheManager.LandCacheCount);
        Assert.Equal(0, SectorSpawnCacheManager.WaterCacheCount);
    }

    [Fact]
    public void InvalidateSectors_OnlyAffectsSpecifiedArea()
    {
        var map = Map.Felucca;

        // Cache in two different areas
        SectorSpawnCacheManager.SetValid(map, new Point3D(50, 50, 0), isWater: false);   // Sector 3,3
        SectorSpawnCacheManager.SetValid(map, new Point3D(500, 500, 0), isWater: false); // Sector 31,31

        Assert.Equal(2, SectorSpawnCacheManager.CachedSectorCount);

        // Only invalidate first area
        SectorSpawnCacheManager.InvalidateSectors(map, new Rectangle2D(0, 0, 100, 100));

        Assert.Equal(1, SectorSpawnCacheManager.CachedSectorCount);
    }

    [Fact]
    public void InvalidateSectors_DifferentMaps_Independent()
    {
        var map1 = Map.Felucca;
        var map2 = Map.Internal;

        SectorSpawnCacheManager.SetValid(map1, new Point3D(50, 50, 0), isWater: false);
        SectorSpawnCacheManager.SetValid(map2, new Point3D(50, 50, 0), isWater: false);

        Assert.Equal(2, SectorSpawnCacheManager.CachedSectorCount);

        // Only invalidate first map
        SectorSpawnCacheManager.InvalidateSectors(map1, new Rectangle2D(0, 0, 100, 100));

        Assert.Equal(1, SectorSpawnCacheManager.CachedSectorCount);
    }
}

[Collection("SectorSpawnCache")]
public class SpiralScanTests
{
    public SpiralScanTests()
    {
        SectorSpawnCacheManager.ClearAll();
    }

    [Theory]
    [InlineData(1, 0, -1, -1)] // Ring 1, position 0: top-left
    [InlineData(1, 1, 0, -1)]  // Ring 1, position 1: top
    [InlineData(1, 2, 1, -1)]  // Ring 1, position 2: top-right (end of top edge)
    [InlineData(1, 3, 1, 0)]   // Ring 1, position 3: right
    [InlineData(1, 4, 1, 1)]   // Ring 1, position 4: bottom-right (end of right edge)
    [InlineData(1, 5, 0, 1)]   // Ring 1, position 5: bottom
    [InlineData(1, 6, -1, 1)]  // Ring 1, position 6: bottom-left (end of bottom edge)
    [InlineData(1, 7, -1, 0)]  // Ring 1, position 7: left
    public void GetSpiralOffset_Ring1_CorrectOffsets(int ring, int position, int expectedDx, int expectedDy)
    {
        var (dx, dy) = SectorSpawnCacheManager.GetSpiralOffset(ring, position);

        Assert.Equal(expectedDx, dx);
        Assert.Equal(expectedDy, dy);
    }

    [Fact]
    public void SpiralPattern_Ring1Has8Positions()
    {
        // Ring N has 8*N positions
        const int ring1Positions = 1 * 8;
        Assert.Equal(8, ring1Positions);
    }

    [Fact]
    public void SpiralPattern_Ring2Has16Positions()
    {
        const int ring2Positions = 2 * 8;
        Assert.Equal(16, ring2Positions);
    }

    [Fact]
    public void SpiralPattern_Ring10Has80Positions()
    {
        const int ring10Positions = 10 * 8;
        Assert.Equal(80, ring10Positions);
    }

    [Fact]
    public void SpiralPattern_Ring1_CoversAllAdjacentTiles()
    {
        // Ring 1 should cover all 8 adjacent tiles
        var expectedOffsets = new HashSet<(int dx, int dy)>
        {
            (-1, -1), (0, -1), (1, -1),  // Top row
            (1, 0),                       // Right
            (1, 1), (0, 1), (-1, 1),      // Bottom row
            (-1, 0)                       // Left
        };

        var actualOffsets = new HashSet<(int dx, int dy)>();

        for (var position = 0; position < 8; position++)
        {
            actualOffsets.Add(SectorSpawnCacheManager.GetSpiralOffset(1, position));
        }

        Assert.Equal(expectedOffsets.Count, actualOffsets.Count);
        foreach (var expected in expectedOffsets)
        {
            Assert.Contains(expected, actualOffsets);
        }
    }

    [Fact]
    public void SpiralPattern_Ring2_CoversAllExpectedTiles()
    {
        // Ring 2 should cover all tiles at distance 2
        var actualOffsets = new HashSet<(int dx, int dy)>();

        for (var position = 0; position < 16; position++)
        {
            actualOffsets.Add(SectorSpawnCacheManager.GetSpiralOffset(2, position));
        }

        // Should have 16 unique positions
        Assert.Equal(16, actualOffsets.Count);

        // All positions should be at Chebyshev distance 2
        foreach (var (dx, dy) in actualOffsets)
        {
            var chebyshevDist = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
            Assert.Equal(2, chebyshevDist);
        }
    }
}

public class SpawnPositionStateTests
{
    [Fact]
    public void SpawnPositionState_InitialState_ZeroCounts()
    {
        var state = new SpawnPositionState();

        Assert.False(state.SpiralComplete);
        Assert.Equal(0, state.SpiralRing);
        Assert.Equal(0, state.SpiralRingPosition);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var state = new SpawnPositionState();
        state.SpiralRing = 5;
        state.SpiralRingPosition = 10;
        state.SpiralComplete = true;
        state.RecordNonTransientFailure();

        state.Reset();

        Assert.False(state.SpiralComplete);
        Assert.Equal(0, state.SpiralRing);
        Assert.Equal(0, state.SpiralRingPosition);
    }

    [Fact]
    public void ShouldCachePositions_Enabled_AlwaysTrue()
    {
        var state = new SpawnPositionState();

        Assert.True(state.ShouldCachePositions(SpawnPositionMode.Enabled));
    }

    [Fact]
    public void ShouldCachePositions_Disabled_AlwaysFalse()
    {
        var state = new SpawnPositionState();
        state.RecordNonTransientFailure();

        Assert.False(state.ShouldCachePositions(SpawnPositionMode.Disabled));
    }

    [Fact]
    public void ShouldCachePositions_Automatic_FalseWithNoFailures()
    {
        var state = new SpawnPositionState();

        Assert.False(state.ShouldCachePositions(SpawnPositionMode.Automatic));
    }

    [Fact]
    public void ShouldCachePositions_Automatic_TrueAfterThresholdFailures()
    {
        var state = new SpawnPositionState();

        // FailureThreshold is 5, so need more than 5 failures
        for (var i = 0; i < 6; i++)
        {
            state.RecordNonTransientFailure();
        }

        Assert.True(state.ShouldCachePositions(SpawnPositionMode.Automatic));
    }

    [Fact]
    public void ShouldCachePositions_Automatic_FalseWithFewerThanThresholdFailures()
    {
        var state = new SpawnPositionState();

        // FailureThreshold is 5, so 5 or fewer failures should not trigger caching
        for (var i = 0; i < 5; i++)
        {
            state.RecordNonTransientFailure();
        }

        Assert.False(state.ShouldCachePositions(SpawnPositionMode.Automatic));
    }

    [Fact]
    public void ShouldAbandon_FalseWhenSpiralNotComplete()
    {
        var state = new SpawnPositionState();

        // Record 25 useless results
        for (var i = 0; i < 25; i++)
        {
            state.RecordUselessResult();
        }

        Assert.False(state.ShouldAbandon()); // Spiral not complete
    }

    [Fact]
    public void ShouldAbandon_TrueAfterThresholdUselessResults()
    {
        var state = new SpawnPositionState();
        state.SpiralComplete = true;

        // Record 25 useless results (cache miss or Location-only)
        for (var i = 0; i < 25; i++)
        {
            state.RecordUselessResult();
        }

        Assert.True(state.ShouldAbandon());
    }

    [Fact]
    public void ShouldAbandon_FalseWithUsefulCacheHit()
    {
        var state = new SpawnPositionState();
        state.SpiralComplete = true;

        // Record 24 useless results
        for (var i = 0; i < 24; i++)
        {
            state.RecordUselessResult();
        }

        // Record a useful cache hit - resets counter
        state.RecordUsefulCacheHit();

        // Record a few more useless results
        for (var i = 0; i < 5; i++)
        {
            state.RecordUselessResult();
        }

        Assert.False(state.ShouldAbandon()); // Counter was reset, only 5 now
    }

    [Fact]
    public void RecordUsefulCacheHit_ResetsUselessCounter()
    {
        var state = new SpawnPositionState();
        state.SpiralComplete = true;

        // Record 20 useless results
        for (var i = 0; i < 20; i++)
        {
            state.RecordUselessResult();
        }

        // Useful cache hit resets counter
        state.RecordUsefulCacheHit();

        // Need 25 more to trigger abandon
        for (var i = 0; i < 24; i++)
        {
            state.RecordUselessResult();
        }

        Assert.False(state.ShouldAbandon()); // Only 24 after reset

        state.RecordUselessResult(); // 25th
        Assert.True(state.ShouldAbandon());
    }
}
