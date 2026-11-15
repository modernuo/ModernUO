/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Server;

/// <summary>
/// A 256-bit bitmask backed by a single <see cref="Vector256{T}"/>.
/// Useful for tracking 256 discrete states such as:
/// - Z levels in UO (-128 to 127 = 256 values)
/// - Sector positions (16x16 = 256 tiles)
/// On x64 with AVX2 the mask lives in one YMM register; elsewhere the JIT splits it into two 128-bit halves.
/// </summary>
public struct BitMask256 : IEquatable<BitMask256>
{
    private Vector256<ulong> _value;

    // Scalar paths load lanes straight from the struct: extracting from a YMM register costs more than a load,
    // and a runtime lane index would otherwise spill the whole vector. Callers keep lane in 0-3.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ulong Lane(int lane) =>
        Unsafe.Add(ref Unsafe.As<Vector256<ulong>, ulong>(ref Unsafe.AsRef(in _value)), lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BitMask256(Vector256<ulong> value) => _value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitMask256(ulong bits0, ulong bits1, ulong bits2, ulong bits3) =>
        _value = Vector256.Create(bits0, bits1, bits2, bits3);

    public readonly ulong Bits0
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Lane(0);
    }

    public readonly ulong Bits1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Lane(1);
    }

    public readonly ulong Bits2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Lane(2);
    }

    public readonly ulong Bits3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Lane(3);
    }

    /// <summary>
    /// Creates a mask with all 256 bits set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256 AllSet() => new(Vector256<ulong>.AllBitsSet);

    /// <summary>
    /// Creates a mask with all bits cleared (default state).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256 AllClear() => default;

    /// <summary>
    /// Creates a mask with only the bit at <paramref name="index"/> (0-255) set.
    /// Out-of-range indices produce an empty mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256 SingleBit(int index)
    {
        if ((uint)index >= 256)
        {
            return default;
        }

        var laneSelector = Vector256.Equals(
            Vector256.Create(0UL, 1UL, 2UL, 3UL),
            Vector256.Create((ulong)((uint)index >> 6))
        );

        // Uniform shift count: a single vpsllq on AVX2, ushl on NEON
        return new BitMask256(Vector256.ShiftLeft(laneSelector & Vector256<ulong>.One, index & 0x3F));
    }

    /// <summary>
    /// Creates a mask with all bits in the inclusive range [start, end] set, clamped to 0-255.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitMask256 FromRange(int start, int end)
    {
        if (end < 0 || start > 255 || start > end)
        {
            return default;
        }

        start = Math.Max(0, start);
        end = Math.Min(255, end);

        if (Avx2.IsSupported)
        {
            var laneStart = Vector256.Create(0UL, 64UL, 128UL, 192UL);
            var laneEnd = laneStart + Vector256.Create(64UL);

            // Clamp to each lane before subtracting so nothing underflows: both values land in [0, 64]
            var localStart = Vector256.Min(Vector256.Max(Vector256.Create((ulong)start), laneStart), laneEnd) - laneStart;
            var localEndExcl = Vector256.Min(Vector256.Max(Vector256.Create((ulong)end + 1), laneStart), laneEnd) - laneStart;

            // An active lane has localStart <= 63 and localEndExcl >= 1, so its shift counts stay in [0, 63].
            // Inactive lanes may shift by 64 (vpsllvq/vpsrlvq yield 0) and are masked off regardless.
            var active = Vector256.GreaterThan(localEndExcl, localStart);
            var low = Avx2.ShiftLeftLogicalVariable(Vector256<ulong>.AllBitsSet, localStart);
            var high = Avx2.ShiftRightLogicalVariable(Vector256<ulong>.AllBitsSet, Vector256.Create(64UL) - localEndExcl);

            return new BitMask256(low & high & active);
        }

        return new BitMask256(
            CreateSegmentMask(start, end, 0),
            CreateSegmentMask(start, end, 64),
            CreateSegmentMask(start, end, 128),
            CreateSegmentMask(start, end, 192)
        );
    }

    /// <summary>
    /// Creates a bitmask for the portion of [start, end] that falls within a 64-bit segment.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateSegmentMask(int start, int end, int segmentOffset)
    {
        var localStart = Math.Max(0, start - segmentOffset);
        var localEnd = Math.Min(63, end - segmentOffset);

        if (localStart > 63 || localEnd < 0 || localStart > localEnd)
        {
            return 0;
        }

        return (ulong.MaxValue << localStart) & (ulong.MaxValue >> (63 - localEnd));
    }

    /// <summary>
    /// Sets a single bit at the specified index (0-255).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(int index) => _value |= SingleBit(index)._value;

    /// <summary>
    /// Clears a single bit at the specified index (0-255).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBit(int index) => _value = Vector256.AndNot(_value, SingleBit(index)._value);

    /// <summary>
    /// Sets all bits in the inclusive range [start, end].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRange(int start, int end) => _value |= FromRange(start, end)._value;

    /// <summary>
    /// Clears all bits in the inclusive range [start, end].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRange(int start, int end) => _value = Vector256.AndNot(_value, FromRange(start, end)._value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 WithBitSet(int index) => new(_value | SingleBit(index)._value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 WithBitCleared(int index) => new(Vector256.AndNot(_value, SingleBit(index)._value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 WithRangeSet(int start, int end) => new(_value | FromRange(start, end)._value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 WithRangeCleared(int start, int end) =>
        new(Vector256.AndNot(_value, FromRange(start, end)._value));

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

        return ((Lane(index >> 6) >> (index & 0x3F)) & 1UL) != 0;
    }

    /// <summary>
    /// Returns the bitwise AND of this mask with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 And(in BitMask256 other) => new(_value & other._value);

    /// <summary>
    /// Returns the bitwise OR of this mask with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 Or(in BitMask256 other) => new(_value | other._value);

    /// <summary>
    /// Returns the bitwise XOR of this mask with another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 Xor(in BitMask256 other) => new(_value ^ other._value);

    /// <summary>
    /// Returns the bitwise AND-NOT (this &amp; ~other).
    /// Clears bits in this mask that are set in the other mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 AndNot(in BitMask256 other) => new(Vector256.AndNot(_value, other._value));

    /// <summary>
    /// Returns the bitwise NOT of this mask.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitMask256 Not() => new(~_value);

    public static BitMask256 operator &(BitMask256 left, BitMask256 right) => new(left._value & right._value);

    public static BitMask256 operator |(BitMask256 left, BitMask256 right) => new(left._value | right._value);

    public static BitMask256 operator ^(BitMask256 left, BitMask256 right) => new(left._value ^ right._value);

    public static BitMask256 operator ~(BitMask256 value) => new(~value._value);

    /// <summary>
    /// Returns true if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsEmpty()
    {
        if (Vector256.IsHardwareAccelerated)
        {
            return _value == Vector256<ulong>.Zero;
        }

        return (_value.GetLower() | _value.GetUpper()) == Vector128<ulong>.Zero;
    }

    /// <summary>
    /// Returns true if all 256 bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsFull()
    {
        if (Vector256.IsHardwareAccelerated)
        {
            return _value == Vector256<ulong>.AllBitsSet;
        }

        return (_value.GetLower() & _value.GetUpper()) == Vector128<ulong>.AllBitsSet;
    }

    /// <summary>
    /// Returns the total number of bits set (population count).
    /// </summary>
    /// <remarks>
    /// Scalar on purpose: there is no portable vector popcount, and emulating one on AVX2 is slower than four popcnt.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int PopCount() =>
        BitOperations.PopCount(Lane(0)) +
        BitOperations.PopCount(Lane(1)) +
        BitOperations.PopCount(Lane(2)) +
        BitOperations.PopCount(Lane(3));

    /// <summary>
    /// Returns the index of the lowest set bit (0-255), or -1 if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int LowestSetBit() => LowestSetBit(_value);

    /// <summary>
    /// Returns the index of the highest set bit (0-255), or -1 if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int HighestSetBit() => HighestSetBit(_value);

    /// <summary>
    /// Returns the index of the set bit closest to <paramref name="target"/>, or -1 if no bits are set.
    /// On a tie (one set bit the same distance below and above) the lower one wins.
    /// <paramref name="target"/> is clamped to 0-255, so <c>NearestSetBit(0)</c> equals <see cref="LowestSetBit()"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int NearestSetBit(int target)
    {
        target = Math.Clamp(target, 0, 255);

        // Bits strictly below target: whole lanes under the target's lane, plus the low bits of its own lane.
        // Signed lane-index compares map to a single vpcmpgtq/vpcmpeqq; unsigned 64-bit compares are emulated on AVX2.
        var laneIndex = Vector256.Create(0L, 1L, 2L, 3L);
        var targetLane = Vector256.Create((long)(target >> 6));
        var strictlyBelow = Vector256.LessThan(laneIndex, targetLane).AsUInt64() |
                            (Vector256.Equals(laneIndex, targetLane).AsUInt64() &
                             Vector256.Create((1UL << (target & 0x3F)) - 1));

        var high = LowestSetBit(Vector256.AndNot(_value, strictlyBelow));
        if (high == target)
        {
            return high;
        }

        var low = HighestSetBit(_value & strictlyBelow);
        if (low < 0)
        {
            return high;
        }

        if (high < 0)
        {
            return low;
        }

        return target - low <= high - target ? low : high;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int LowestSetBit(Vector256<ulong> value)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            var nonZeroLanes = ~Vector256.Equals(value, Vector256<ulong>.Zero).ExtractMostSignificantBits() & 0xFu;
            if (nonZeroLanes == 0)
            {
                return -1;
            }

            // & 3 lets the JIT drop the GetElement range check; nonZeroLanes is 4 bits so it changes nothing
            var lane = BitOperations.TrailingZeroCount(nonZeroLanes) & 3;
            return (lane << 6) + BitOperations.TrailingZeroCount(value.GetElement(lane));
        }

        var lo = value.GetLower();
        if (lo != Vector128<ulong>.Zero)
        {
            var e0 = lo.GetElement(0);
            return e0 != 0
                ? BitOperations.TrailingZeroCount(e0)
                : 64 + BitOperations.TrailingZeroCount(lo.GetElement(1));
        }

        var hi = value.GetUpper();
        if (hi != Vector128<ulong>.Zero)
        {
            var e2 = hi.GetElement(0);
            return e2 != 0
                ? 128 + BitOperations.TrailingZeroCount(e2)
                : 192 + BitOperations.TrailingZeroCount(hi.GetElement(1));
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HighestSetBit(Vector256<ulong> value)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            var nonZeroLanes = ~Vector256.Equals(value, Vector256<ulong>.Zero).ExtractMostSignificantBits() & 0xFu;
            if (nonZeroLanes == 0)
            {
                return -1;
            }

            var lane = (31 - BitOperations.LeadingZeroCount(nonZeroLanes)) & 3;
            return (lane << 6) + 63 - BitOperations.LeadingZeroCount(value.GetElement(lane));
        }

        var hi = value.GetUpper();
        if (hi != Vector128<ulong>.Zero)
        {
            var e3 = hi.GetElement(1);
            return e3 != 0
                ? 255 - BitOperations.LeadingZeroCount(e3)
                : 191 - BitOperations.LeadingZeroCount(hi.GetElement(0));
        }

        var lo = value.GetLower();
        if (lo != Vector128<ulong>.Zero)
        {
            var e1 = lo.GetElement(1);
            return e1 != 0
                ? 127 - BitOperations.LeadingZeroCount(e1)
                : 63 - BitOperations.LeadingZeroCount(lo.GetElement(0));
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of the Nth set bit (0-indexed), or -1 if fewer than N+1 bits are set.
    /// Uses BMI2 PDEP for O(1) selection within a lane when available, otherwise O(popcount).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetNthSetBit(int n)
    {
        var c0 = BitOperations.PopCount(Lane(0));
        var c1 = BitOperations.PopCount(Lane(1));
        var c2 = BitOperations.PopCount(Lane(2));
        var p1 = c0;
        var p2 = p1 + c1;
        var p3 = p2 + c2;

        // Also rejects negative n
        if ((uint)n >= (uint)(p3 + BitOperations.PopCount(Lane(3))))
        {
            return -1;
        }

        // Callers pass a random n, so a lane walk mispredicts. Compares become setcc; the counts before the lane
        // are masked with -ge rather than selected with ?:, which the JIT turns back into branches inside loops.
        var ge1 = n >= p1 ? 1 : 0;
        var ge2 = n >= p2 ? 1 : 0;
        var ge3 = n >= p3 ? 1 : 0;
        var lane = ge1 + ge2 + ge3;
        var before = (c0 & -ge1) + (c1 & -ge2) + (c2 & -ge3);

        return (lane << 6) + GetNthBitInUlong(Lane(lane), n - before);
    }

    // Caller guarantees n < PopCount(bits)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNthBitInUlong(ulong bits, int n)
    {
        // PDEP deposits the nth selector bit into the position of the nth set bit
        if (Bmi2.X64.IsSupported)
        {
            return BitOperations.TrailingZeroCount(Bmi2.X64.ParallelBitDeposit(1UL << n, bits));
        }

        while (n > 0)
        {
            bits &= bits - 1;
            n--;
        }

        return BitOperations.TrailingZeroCount(bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(BitMask256 other) => _value == other._value;

    public readonly override bool Equals(object obj) => obj is BitMask256 other && Equals(other);

    public readonly override int GetHashCode() => _value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(BitMask256 left, BitMask256 right) => left._value == right._value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(BitMask256 left, BitMask256 right) => left._value != right._value;
}
