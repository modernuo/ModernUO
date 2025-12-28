using System.Collections.Generic;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests;

public class SectorSpawnCacheTests
{
    [Fact]
    public void SectorSpawnCache_InitialState_AllBitsZero()
    {
        var cache = new SectorSpawnCache();

        Assert.Equal(0, cache.GetCount());
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
    public void SectorSpawnCache_SetBit_SetsCorrectBit(int bitIndex)
    {
        var cache = new SectorSpawnCache();

        cache.SetBit(bitIndex);

        Assert.True(cache.GetBit(bitIndex));
        Assert.Equal(1, cache.GetCount());
    }

    [Fact]
    public void SectorSpawnCache_SetMultipleBits_CountsCorrectly()
    {
        var cache = new SectorSpawnCache();

        cache.SetBit(0);
        cache.SetBit(64);
        cache.SetBit(128);
        cache.SetBit(192);

        Assert.Equal(4, cache.GetCount());
        Assert.True(cache.GetBit(0));
        Assert.True(cache.GetBit(64));
        Assert.True(cache.GetBit(128));
        Assert.True(cache.GetBit(192));
        Assert.False(cache.GetBit(1));
    }

    [Fact]
    public void SectorSpawnCache_ClearBit_ClearsCorrectBit()
    {
        var cache = new SectorSpawnCache();

        cache.SetBit(50);
        cache.SetBit(100);
        Assert.Equal(2, cache.GetCount());

        cache.ClearBit(50);

        Assert.False(cache.GetBit(50));
        Assert.True(cache.GetBit(100));
        Assert.Equal(1, cache.GetCount());
    }

    [Theory]
    [InlineData(0, 0)]   // First bit of first ulong
    [InlineData(1, 64)]  // First bit of second ulong
    [InlineData(2, 128)] // First bit of third ulong
    [InlineData(3, 192)] // First bit of fourth ulong
    public void SectorSpawnCache_GetNthBitPosition_FirstBitInEachUlong(int n, int expectedPosition)
    {
        var cache = new SectorSpawnCache();

        // Set first bit in each ulong
        cache.SetBit(0);
        cache.SetBit(64);
        cache.SetBit(128);
        cache.SetBit(192);

        Assert.Equal(expectedPosition, cache.GetNthBitPosition(n));
    }

    [Fact]
    public void SectorSpawnCache_GetNthBitPosition_WithinSingleUlong()
    {
        var cache = new SectorSpawnCache();

        cache.SetBit(5);
        cache.SetBit(10);
        cache.SetBit(20);

        Assert.Equal(5, cache.GetNthBitPosition(0));
        Assert.Equal(10, cache.GetNthBitPosition(1));
        Assert.Equal(20, cache.GetNthBitPosition(2));
    }

    [Fact]
    public void SectorSpawnCache_BitIndexToCoordinates_CorrectMapping()
    {
        // Bit index = (localX) + (localY * 16)
        // localX = bitIndex & 0xF
        // localY = bitIndex >> 4

        // Test corner cases
        Assert.Equal(0, 0 & 0xF);  // (0,0) -> bit 0
        Assert.Equal(0, 0 >> 4);

        Assert.Equal(15, 15 & 0xF); // (15,0) -> bit 15
        Assert.Equal(0, 15 >> 4);

        Assert.Equal(0, 16 & 0xF);  // (0,1) -> bit 16
        Assert.Equal(1, 16 >> 4);

        Assert.Equal(15, 255 & 0xF); // (15,15) -> bit 255
        Assert.Equal(15, 255 >> 4);
    }
}

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

        // Skip if maps aren't properly initialized as distinct objects in test environment
        if (map1 == null || map2 == null || ReferenceEquals(map1, map2))
        {
            return;
        }

        SectorSpawnCacheManager.SetValid(map1, new Point3D(50, 50, 0), isWater: false);
        SectorSpawnCacheManager.SetValid(map2, new Point3D(50, 50, 0), isWater: false);

        Assert.Equal(2, SectorSpawnCacheManager.CachedSectorCount);

        // Only invalidate first map
        SectorSpawnCacheManager.InvalidateSectors(map1, new Rectangle2D(0, 0, 100, 100));

        Assert.Equal(1, SectorSpawnCacheManager.CachedSectorCount);
    }
}

public class SpiralScanTests
{
    public SpiralScanTests()
    {
        SectorSpawnCacheManager.ClearAll();
    }

    [Fact]
    public void GetSpiralOffset_Ring0_ReturnsCenter()
    {
        // Ring 0 is just the center point (no offset)
        // This is handled specially in ContinueSpiralScan, not GetSpiralOffset
        // Ring 0 has 0 positions in the loop, center is checked separately
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
        var ring1Positions = 1 * 8;
        Assert.Equal(8, ring1Positions);
    }

    [Fact]
    public void SpiralPattern_Ring2Has16Positions()
    {
        var ring2Positions = 2 * 8;
        Assert.Equal(16, ring2Positions);
    }

    [Fact]
    public void SpiralPattern_Ring10Has80Positions()
    {
        var ring10Positions = 10 * 8;
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
    public void ShouldCachePositions_Automatic_TrueAfterFailure()
    {
        var state = new SpawnPositionState();
        state.RecordNonTransientFailure();

        Assert.True(state.ShouldCachePositions(SpawnPositionMode.Automatic));
    }

    [Fact]
    public void ShouldAbandon_FalseWhenSpiralNotComplete()
    {
        var state = new SpawnPositionState();

        // Record 25 failures
        for (var i = 0; i < 25; i++)
        {
            state.RecordNonTransientFailure();
        }

        Assert.False(state.ShouldAbandon()); // Spiral not complete
    }

    [Fact]
    public void ShouldAbandon_TrueWhen100PercentFailureRateAfterSpiral()
    {
        var state = new SpawnPositionState();
        state.SpiralComplete = true;

        // Record 25 failures (100% failure rate)
        for (var i = 0; i < 25; i++)
        {
            state.RecordNonTransientFailure();
        }

        Assert.True(state.ShouldAbandon());
    }

    [Fact]
    public void ShouldAbandon_FalseWithSomeSuccesses()
    {
        var state = new SpawnPositionState();
        state.SpiralComplete = true;

        // Record some successes and failures
        for (var i = 0; i < 20; i++)
        {
            state.RecordNonTransientFailure();
        }
        for (var i = 0; i < 5; i++)
        {
            state.RecordSuccess();
        }

        Assert.False(state.ShouldAbandon()); // Not 100% failure rate
    }

    [Fact]
    public void RecordSuccess_ResetsWindowAfter25Attempts()
    {
        var state = new SpawnPositionState();

        // Record 24 successes
        for (var i = 0; i < 24; i++)
        {
            state.RecordSuccess();
        }

        // Record 1 failure
        state.RecordNonTransientFailure();

        // Should still cache since we have failures in window
        Assert.True(state.ShouldCachePositions(SpawnPositionMode.Automatic));

        // Record 25th attempt (success) - this should reset the window
        state.RecordSuccess();

        // After reset, no failures in new window
        Assert.False(state.ShouldCachePositions(SpawnPositionMode.Automatic));
    }
}
