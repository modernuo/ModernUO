/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SectorSpawnCache.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Server.Collections;
using Server.Regions;

namespace Server.Engines.Spawners;

/// <summary>
/// Cached spawn position data for a 16x16 sector.
/// Uses a bitmap to track valid spawn positions (256 bits = 4 ulongs = 32 bytes).
/// </summary>
public struct SectorSpawnCache
{
    public ulong Bits0;
    public ulong Bits1;
    public ulong Bits2;
    public ulong Bits3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetCount() =>
        BitOperations.PopCount(Bits0) +
        BitOperations.PopCount(Bits1) +
        BitOperations.PopCount(Bits2) +
        BitOperations.PopCount(Bits3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(int bitIndex)
    {
        var ulongIndex = bitIndex >> 6; // / 64
        var bitPosition = bitIndex & 0x3F; // % 64

        switch (ulongIndex)
        {
            case 0: Bits0 |= 1UL << bitPosition; break;
            case 1: Bits1 |= 1UL << bitPosition; break;
            case 2: Bits2 |= 1UL << bitPosition; break;
            case 3: Bits3 |= 1UL << bitPosition; break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool GetBit(int bitIndex)
    {
        var ulongIndex = bitIndex >> 6;
        var bitPosition = bitIndex & 0x3F;

        return ulongIndex switch
        {
            0 => (Bits0 & (1UL << bitPosition)) != 0,
            1 => (Bits1 & (1UL << bitPosition)) != 0,
            2 => (Bits2 & (1UL << bitPosition)) != 0,
            3 => (Bits3 & (1UL << bitPosition)) != 0,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBit(int bitIndex)
    {
        var ulongIndex = bitIndex >> 6;
        var bitPosition = bitIndex & 0x3F;

        switch (ulongIndex)
        {
            case 0: Bits0 &= ~(1UL << bitPosition); break;
            case 1: Bits1 &= ~(1UL << bitPosition); break;
            case 2: Bits2 &= ~(1UL << bitPosition); break;
            case 3: Bits3 &= ~(1UL << bitPosition); break;
        }
    }

    /// <summary>
    /// Gets the Nth set bit position (0-indexed) from the bitmap.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetNthBitPosition(int n)
    {
        var count0 = BitOperations.PopCount(Bits0);
        if (n < count0)
        {
            return GetNthBitInUlong(Bits0, n);
        }
        n -= count0;

        var count1 = BitOperations.PopCount(Bits1);
        if (n < count1)
        {
            return 64 + GetNthBitInUlong(Bits1, n);
        }
        n -= count1;

        var count2 = BitOperations.PopCount(Bits2);
        if (n < count2)
        {
            return 128 + GetNthBitInUlong(Bits2, n);
        }
        n -= count2;

        return 192 + GetNthBitInUlong(Bits3, n);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNthBitInUlong(ulong bits, int n)
    {
        // BMI2 PDEP: O(1) - deposits the nth selector bit into the position of the nth set bit
        if (Bmi2.X64.IsSupported)
        {
            var deposited = Bmi2.X64.ParallelBitDeposit(1UL << n, bits);
            return BitOperations.TrailingZeroCount(deposited);
        }

        // Fallback: O(popcount) - clear n set bits, then find position of next one
        while (n > 0 && bits != 0)
        {
            bits &= bits - 1; // Clear lowest set bit
            n--;
        }

        return bits == 0 ? -1 : BitOperations.TrailingZeroCount(bits);
    }
}

/// <summary>
/// Global manager for sector-based spawn position caching.
/// Shared across all spawners for efficient memory usage and house invalidation.
/// Uses separate caches for land and water to minimize memory usage since most
/// sectors are either all land or all water.
/// </summary>
public static class SectorSpawnCacheManager
{
    private static readonly Dictionary<(Map, int, int), SectorSpawnCache> _landCaches = [];
    private static readonly Dictionary<(Map, int, int), SectorSpawnCache> _waterCaches = [];

    /// <summary>
    /// Marks a position as valid for spawning in the global cache.
    /// </summary>
    /// <param name="map">The map containing the position</param>
    /// <param name="pos">The valid spawn position</param>
    /// <param name="isWater">True for water mob, false for land mob</param>
    public static void SetValid(Map map, Point3D pos, bool isWater)
    {
        var sectorX = pos.X >> Map.SectorShift;
        var sectorY = pos.Y >> Map.SectorShift;
        var bitIndex = (pos.X & (Map.SectorSize - 1)) + ((pos.Y & (Map.SectorSize - 1)) << Map.SectorShift);

        var key = (map, sectorX, sectorY);
        var caches = isWater ? _waterCaches : _landCaches;

        ref var cache = ref CollectionsMarshal.GetValueRefOrAddDefault(caches, key, out _);
        cache.SetBit(bitIndex);
    }

    /// <summary>
    /// Attempts to get a random valid position from cached sectors within the specified bounds.
    /// </summary>
    /// <param name="map">The map to search</param>
    /// <param name="bounds">The spawn bounds to search within</param>
    /// <param name="isWater">True for water mob, false for land mob</param>
    /// <param name="pos">The selected position (X, Y only - caller must verify Z)</param>
    /// <returns>True if a cached position was found</returns>
    public static bool TryGetRandomPosition(Map map, Rectangle3D bounds, bool isWater, out Point2D pos)
    {
        pos = Point2D.Zero;

        var caches = isWater ? _waterCaches : _landCaches;

        var startSectorX = bounds.Start.X >> Map.SectorShift;
        var startSectorY = bounds.Start.Y >> Map.SectorShift;
        var endSectorX = (bounds.End.X - 1) >> Map.SectorShift;
        var endSectorY = (bounds.End.Y - 1) >> Map.SectorShift;

        // First pass: count total valid positions across all sectors in bounds
        var totalPositions = 0;
        for (var sx = startSectorX; sx <= endSectorX; sx++)
        {
            for (var sy = startSectorY; sy <= endSectorY; sy++)
            {
                if (caches.TryGetValue((map, sx, sy), out var cache))
                {
                    totalPositions += cache.GetCount();
                }
            }
        }

        if (totalPositions == 0)
        {
            return false;
        }

        // Pick a random position
        var targetIndex = Utility.Random(totalPositions);

        // Second pass: find the sector containing that index
        var currentIndex = 0;
        for (var sx = startSectorX; sx <= endSectorX; sx++)
        {
            for (var sy = startSectorY; sy <= endSectorY; sy++)
            {
                if (!caches.TryGetValue((map, sx, sy), out var cache))
                {
                    continue;
                }

                var sectorCount = cache.GetCount();
                if (targetIndex < currentIndex + sectorCount)
                {
                    // Target is in this sector
                    var indexInSector = targetIndex - currentIndex;
                    var bitPosition = cache.GetNthBitPosition(indexInSector);

                    var localX = bitPosition & (Map.SectorSize - 1);
                    var localY = bitPosition >> Map.SectorShift;

                    pos = new Point2D((sx << Map.SectorShift) + localX, (sy << Map.SectorShift) + localY);

                    // Verify position is within bounds (sector may extend beyond spawn bounds)
                    return pos.X >= bounds.Start.X && pos.X < bounds.End.X &&
                           pos.Y >= bounds.Start.Y && pos.Y < bounds.End.Y;
                }

                currentIndex += sectorCount;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get a random valid position from cached sectors across multiple bounds.
    /// Deduplicates overlapping sectors for uniform distribution.
    /// </summary>
    /// <param name="map">The map to search</param>
    /// <param name="allBounds">All spawn bounds to search within</param>
    /// <param name="isWater">True for water mob, false for land mob</param>
    /// <param name="pos">The selected position (X, Y only - caller must verify Z)</param>
    /// <param name="containingBounds">The bounds rectangle containing the selected position</param>
    /// <returns>True if a cached position was found</returns>
    public static bool TryGetRandomPosition(
        Map map,
        IReadOnlyList<Rectangle3D> allBounds,
        bool isWater,
        out Point2D pos,
        out Rectangle3D containingBounds)
    {
        pos = Point2D.Zero;
        containingBounds = default;

        if (allBounds.Count == 0)
        {
            return false;
        }

        // Fast path for single bounds
        if (allBounds.Count == 1)
        {
            containingBounds = allBounds[0];
            return TryGetRandomPosition(map, containingBounds, isWater, out pos);
        }

        var caches = isWater ? _waterCaches : _landCaches;

        // Collect unique sectors and their counts
        using var sectorList = PooledRefList<(int sx, int sy, int count)>.Create();
        var totalPositions = 0;

        for (var i = 0; i < allBounds.Count; i++)
        {
            var bounds = allBounds[i];
            var startSectorX = bounds.Start.X >> Map.SectorShift;
            var startSectorY = bounds.Start.Y >> Map.SectorShift;
            var endSectorX = (bounds.End.X - 1) >> Map.SectorShift;
            var endSectorY = (bounds.End.Y - 1) >> Map.SectorShift;

            for (var sx = startSectorX; sx <= endSectorX; sx++)
            {
                for (var sy = startSectorY; sy <= endSectorY; sy++)
                {
                    // Check if we've already added this sector
                    var alreadyAdded = false;
                    for (var j = 0; j < sectorList.Count; j++)
                    {
                        var (checkSX, checkSY, _) = sectorList[j];
                        if (checkSX == sx && checkSY == sy)
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }

                    if (alreadyAdded)
                    {
                        continue;
                    }

                    if (caches.TryGetValue((map, sx, sy), out var cache))
                    {
                        var count = cache.GetCount();
                        if (count > 0)
                        {
                            sectorList.Add((sx, sy, count));
                            totalPositions += count;
                        }
                    }
                }
            }
        }

        if (totalPositions == 0)
        {
            return false;
        }

        // Pick a random position
        var targetIndex = Utility.Random(totalPositions);

        // Find the sector containing that index
        var currentIndex = 0;
        for (var i = 0; i < sectorList.Count; i++)
        {
            var (sx, sy, sectorCount) = sectorList[i];
            if (targetIndex < currentIndex + sectorCount)
            {
                // Target is in this sector - look up cache again
                if (!caches.TryGetValue((map, sx, sy), out var cache))
                {
                    return false; // Should not happen
                }

                var indexInSector = targetIndex - currentIndex;
                var bitPosition = cache.GetNthBitPosition(indexInSector);

                var localX = bitPosition & (Map.SectorSize - 1);
                var localY = bitPosition >> Map.SectorShift;

                pos = new Point2D((sx << Map.SectorShift) + localX, (sy << Map.SectorShift) + localY);

                // Find which bounds contains this position
                for (var j = 0; j < allBounds.Count; j++)
                {
                    var bounds = allBounds[j];
                    if (pos.X >= bounds.Start.X && pos.X < bounds.End.X &&
                        pos.Y >= bounds.Start.Y && pos.Y < bounds.End.Y)
                    {
                        containingBounds = bounds;
                        return true;
                    }
                }

                // Position not in any bounds (edge of sector outside all bounds)
                return false;
            }

            currentIndex += sectorCount;
        }

        return false;
    }

    /// <summary>
    /// Checks if a position is blocked by a private house.
    /// Public AoS houses (with unlocked doors) allow spawning.
    /// </summary>
    /// <param name="map">The map to check</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>True if blocked by a private house</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBlockedByHouse(Map map, int x, int y, int z)
    {
        if (Region.Find(new Point3D(x, y, z), map) is HouseRegion houseRegion)
        {
            var house = houseRegion.House;
            // Allow spawning in public AoS houses (unlocked doors, free entry)
            return !(house.IsAosRules && house.Public);
        }

        return false;
    }

    /// <summary>
    /// Invalidates all cached data for sectors within the specified bounds.
    /// Called when houses are placed or demolished.
    /// </summary>
    /// <param name="map">The map to invalidate</param>
    /// <param name="bounds">The affected area</param>
    public static void InvalidateSectors(Map map, Rectangle2D bounds)
    {
        var startSectorX = bounds.Start.X >> Map.SectorShift;
        var startSectorY = bounds.Start.Y >> Map.SectorShift;
        var endSectorX = bounds.End.X >> Map.SectorShift;
        var endSectorY = bounds.End.Y >> Map.SectorShift;

        for (var sx = startSectorX; sx <= endSectorX; sx++)
        {
            for (var sy = startSectorY; sy <= endSectorY; sy++)
            {
                var key = (map, sx, sy);
                _landCaches.Remove(key);
                _waterCaches.Remove(key);
            }
        }
    }

    /// <summary>
    /// Performs incremental spiral scanning to find and cache valid spawn positions.
    /// </summary>
    /// <param name="map">The map to scan</param>
    /// <param name="center">The center point to spiral from</param>
    /// <param name="bounds">The spawn bounds to stay within</param>
    /// <param name="minZ">Minimum Z for spawn checks</param>
    /// <param name="maxZ">Maximum Z for spawn checks</param>
    /// <param name="canSwim">Whether to find water positions</param>
    /// <param name="cantWalk">Whether the mob can't walk (water-only)</param>
    /// <param name="currentRing">Current ring being scanned (updated on return)</param>
    /// <param name="ringPosition">Position within current ring (updated on return)</param>
    /// <param name="ringsPerTick">Number of rings to scan per call</param>
    /// <returns>True if scan is complete (exhausted bounds)</returns>
    public static bool ContinueSpiralScan(
        Map map,
        Point3D center,
        Rectangle3D bounds,
        int minZ,
        int maxZ,
        bool canSwim,
        bool cantWalk,
        ref int currentRing,
        ref int ringPosition,
        int ringsPerTick = 3)
    {
        var maxRing = Math.Max(bounds.Width, bounds.Height) / 2 + 1;
        var ringsToScan = Math.Min(ringsPerTick, maxRing - currentRing);

        for (var r = 0; r < ringsToScan; r++)
        {
            var ring = currentRing + r;
            if (ring > maxRing)
            {
                currentRing = ring;
                return true; // Scan complete
            }

            // Ring 0 is just the center point
            if (ring == 0)
            {
                CheckAndCachePosition(map, center.X, center.Y, minZ, maxZ, bounds, canSwim, cantWalk);
                continue;
            }

            // Ring N has 8*N positions
            var positionsInRing = ring * 8;
            for (var p = 0; p < positionsInRing; p++)
            {
                var (dx, dy) = GetSpiralOffset(ring, p);
                var x = center.X + dx;
                var y = center.Y + dy;

                CheckAndCachePosition(map, x, y, minZ, maxZ, bounds, canSwim, cantWalk);
            }
        }

        currentRing += ringsToScan;
        ringPosition = 0;

        return currentRing > maxRing;
    }

    private static void CheckAndCachePosition(
        Map map,
        int x, int y,
        int minZ, int maxZ,
        Rectangle3D bounds,
        bool canSwim,
        bool cantWalk)
    {
        // Check bounds
        if (x < bounds.Start.X || x >= bounds.End.X ||
            y < bounds.Start.Y || y >= bounds.End.Y)
        {
            return;
        }

        // Check if position is valid for spawning
        if (map.CanSpawnMobile(x, y, minZ, maxZ, canSwim, cantWalk, out var spawnZ))
        {
            // Skip positions inside private houses
            if (IsBlockedByHouse(map, x, y, spawnZ))
            {
                return;
            }

            var pos = new Point3D(x, y, spawnZ);
            var isWater = canSwim && cantWalk;
            SetValid(map, pos, isWater);
        }
    }

    /// <summary>
    /// Gets the X,Y offset for a position in a spiral ring.
    /// Ring 0 = center (no offset)
    /// Ring 1 = 8 positions around center
    /// Ring N = 8*N positions
    /// </summary>
    public static (int dx, int dy) GetSpiralOffset(int ring, int position)
    {
        // Each ring has 4 sides, each side has ring*2 positions
        var sideLength = ring * 2;
        var side = position / sideLength;
        var sidePos = position % sideLength;

        return side switch
        {
            0 => (-ring + sidePos, -ring), // Top edge, left to right
            1 => (ring, -ring + sidePos),  // Right edge, top to bottom
            2 => (ring - sidePos, ring),   // Bottom edge, right to left
            3 => (-ring, ring - sidePos),  // Left edge, bottom to top
            _ => (0, 0)
        };
    }

    /// <summary>
    /// Clears all cached data. Used for testing or server restart.
    /// </summary>
    public static void ClearAll()
    {
        _landCaches.Clear();
        _waterCaches.Clear();
    }

    /// <summary>
    /// Gets the number of sectors currently cached (land + water).
    /// </summary>
    public static int CachedSectorCount => _landCaches.Count + _waterCaches.Count;

    /// <summary>
    /// Gets the number of land sectors currently cached.
    /// </summary>
    public static int LandCacheCount => _landCaches.Count;

    /// <summary>
    /// Gets the number of water sectors currently cached.
    /// </summary>
    public static int WaterCacheCount => _waterCaches.Count;
}
