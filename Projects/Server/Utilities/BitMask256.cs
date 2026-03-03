/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BitMask256.cs                                                   *
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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Server;

/// <summary>
/// A 256-bit bitmask stored as 4 ulongs.
/// Useful for tracking 256 discrete states such as:
/// - Z levels in UO (-128 to 127 = 256 values)
/// - Sector positions (16x16 = 256 tiles)
/// Uses BMI2 for efficient bit selection when available.
/// </summary>
public struct BitMask256
{
    public ulong Bits0, Bits1, Bits2, Bits3;

    /// <summary>
    /// Creates a mask with all 256 bits set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256 AllSet() => new()
    {
        Bits0 = ulong.MaxValue,
        Bits1 = ulong.MaxValue,
        Bits2 = ulong.MaxValue,
        Bits3 = ulong.MaxValue
    };

    /// <summary>
    /// Creates a mask with all bits cleared (default state).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256 AllClear() => default;

    /// <summary>
    /// Sets a single bit at the specified index (0-255).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(int index)
    {
        if ((uint)index >= 256)
        {
            return;
        }

        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        switch (segment)
        {
            case 0:
                {
                    Bits0 |= mask; break;
                }
            case 1:
                {
                    Bits1 |= mask; break;
                }
            case 2:
                {
                    Bits2 |= mask; break;
                }
            case 3:
                {
                    Bits3 |= mask; break;
                }
        }
    }

    /// <summary>
    /// Clears a single bit at the specified index (0-255).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBit(int index)
    {
        if ((uint)index >= 256)
        {
            return;
        }

        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        switch (segment)
        {
            case 0:
                {
                    Bits0 &= ~mask; break;
                }
            case 1:
                {
                    Bits1 &= ~mask; break;
                }
            case 2:
                {
                    Bits2 &= ~mask; break;
                }
            case 3:
                {
                    Bits3 &= ~mask; break;
                }
        }
    }

    /// <summary>
    /// Gets the value of a single bit at the specified index (0-255).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool GetBit(int index)
    {
        if ((uint)index >= 256)
        {
            return false;
        }

        var segment = index >> 6;
        var localBit = index & 0x3F;
        var mask = 1UL << localBit;

        return segment switch
        {
            0 => (Bits0 & mask) != 0,
            1 => (Bits1 & mask) != 0,
            2 => (Bits2 & mask) != 0,
            3 => (Bits3 & mask) != 0,
            _ => false
        };
    }

    /// <summary>
    /// Sets all bits in the inclusive range [start, end].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRange(int start, int end)
    {
        if (end < 0 || start > 255 || start > end)
        {
            return;
        }

        start = Math.Max(0, start);
        end = Math.Min(255, end);

        Bits0 |= CreateSegmentMask(start, end, 0);
        Bits1 |= CreateSegmentMask(start, end, 64);
        Bits2 |= CreateSegmentMask(start, end, 128);
        Bits3 |= CreateSegmentMask(start, end, 192);
    }

    /// <summary>
    /// Clears all bits in the inclusive range [start, end].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRange(int start, int end)
    {
        if (end < 0 || start > 255 || start > end)
        {
            return;
        }

        start = Math.Max(0, start);
        end = Math.Min(255, end);

        Bits0 &= ~CreateSegmentMask(start, end, 0);
        Bits1 &= ~CreateSegmentMask(start, end, 64);
        Bits2 &= ~CreateSegmentMask(start, end, 128);
        Bits3 &= ~CreateSegmentMask(start, end, 192);
    }

    /// <summary>
    /// Creates a bitmask for the portion of [start, end] that falls within a 64-bit segment.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateSegmentMask(int start, int end, int segmentOffset)
    {
        var localStart = start - segmentOffset;
        var localEnd = end - segmentOffset;

        localStart = Math.Max(0, localStart);
        localEnd = Math.Min(63, localEnd);

        if (localStart > 63 || localEnd < 0 || localStart > localEnd)
        {
            return 0;
        }

        var bitCount = localEnd - localStart + 1;
        var mask = bitCount >= 64 ? ulong.MaxValue : (1UL << bitCount) - 1;
        return mask << localStart;
    }

    /// <summary>
    /// Returns the bitwise AND of this mask with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 And(in BitMask256 other) => new()
    {
        Bits0 = Bits0 & other.Bits0,
        Bits1 = Bits1 & other.Bits1,
        Bits2 = Bits2 & other.Bits2,
        Bits3 = Bits3 & other.Bits3
    };

    /// <summary>
    /// Returns the bitwise OR of this mask with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 Or(in BitMask256 other) => new()
    {
        Bits0 = Bits0 | other.Bits0,
        Bits1 = Bits1 | other.Bits1,
        Bits2 = Bits2 | other.Bits2,
        Bits3 = Bits3 | other.Bits3
    };

    /// <summary>
    /// Returns the bitwise XOR of this mask with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 Xor(in BitMask256 other) => new()
    {
        Bits0 = Bits0 ^ other.Bits0,
        Bits1 = Bits1 ^ other.Bits1,
        Bits2 = Bits2 ^ other.Bits2,
        Bits3 = Bits3 ^ other.Bits3
    };

    /// <summary>
    /// Returns the bitwise AND-NOT (this &amp; ~other).
    /// Clears bits in this mask that are set in the other mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 AndNot(in BitMask256 other) => new()
    {
        Bits0 = Bits0 & ~other.Bits0,
        Bits1 = Bits1 & ~other.Bits1,
        Bits2 = Bits2 & ~other.Bits2,
        Bits3 = Bits3 & ~other.Bits3
    };

    /// <summary>
    /// Returns the bitwise NOT of this mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 Not() => new()
    {
        Bits0 = ~Bits0,
        Bits1 = ~Bits1,
        Bits2 = ~Bits2,
        Bits3 = ~Bits3
    };

    /// <summary>
    /// Returns the total number of bits set (population count).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int PopCount() =>
        BitOperations.PopCount(Bits0) +
        BitOperations.PopCount(Bits1) +
        BitOperations.PopCount(Bits2) +
        BitOperations.PopCount(Bits3);

    /// <summary>
    /// Returns true if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsEmpty() => (Bits0 | Bits1 | Bits2 | Bits3) == 0;

    /// <summary>
    /// Returns true if all 256 bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsFull() =>
        Bits0 == ulong.MaxValue &&
        Bits1 == ulong.MaxValue &&
        Bits2 == ulong.MaxValue &&
        Bits3 == ulong.MaxValue;

    /// <summary>
    /// Returns the index of the lowest set bit (0-255), or -1 if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int LowestSetBit()
    {
        if (Bits0 != 0)
        {
            return BitOperations.TrailingZeroCount(Bits0);
        }

        if (Bits1 != 0)
        {
            return 64 + BitOperations.TrailingZeroCount(Bits1);
        }

        if (Bits2 != 0)
        {
            return 128 + BitOperations.TrailingZeroCount(Bits2);
        }

        if (Bits3 != 0)
        {
            return 192 + BitOperations.TrailingZeroCount(Bits3);
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of the highest set bit (0-255), or -1 if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int HighestSetBit()
    {
        if (Bits3 != 0)
        {
            return 255 - BitOperations.LeadingZeroCount(Bits3);
        }

        if (Bits2 != 0)
        {
            return 191 - BitOperations.LeadingZeroCount(Bits2);
        }

        if (Bits1 != 0)
        {
            return 127 - BitOperations.LeadingZeroCount(Bits1);
        }

        if (Bits0 != 0)
        {
            return 63 - BitOperations.LeadingZeroCount(Bits0);
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of the Nth set bit (0-indexed), or -1 if fewer than N+1 bits are set.
    /// Uses BMI2 PDEP for O(1) performance when available, otherwise O(popcount) fallback.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetNthSetBit(int n)
    {
        if (n < 0)
        {
            return -1;
        }

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

        var count3 = BitOperations.PopCount(Bits3);
        if (n < count3)
        {
            return 192 + GetNthBitInUlong(Bits3, n);
        }

        return -1;
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
            bits &= bits - 1;
            n--;
        }

        return bits == 0 ? -1 : BitOperations.TrailingZeroCount(bits);
    }
}
