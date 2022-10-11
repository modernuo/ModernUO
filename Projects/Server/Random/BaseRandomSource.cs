/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseRandomSource.cs                                             *
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
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Server.Random;

public abstract class BaseRandomSource : IRandomSource
{
    private const double INCR_DOUBLE = 1.0 / (1UL << 53);
    private const float INCR_FLOAT = 1f / (1U << 24);

    public abstract ulong NextULong();
    public abstract void NextBytes(Span<byte> buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next()
    {
        ulong rtn;
        do
        {
            rtn = NextULong() >> 33;
        } while (rtn == 0x7fff_ffffUL);

        return (int)rtn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next(int count)
    {
        Debug.Assert(count != 0, $"{nameof(count)} must not be 0");

        if (count is -1 or 0 or 1)
        {
            return 0;
        }

        var negative = count < 0;

        var max = negative ? -count : count;

        var bits = Log2((uint)max);

        int x;
        do
        {
            x = (int)(NextULong() >> (64 - bits));
        } while (x >= max);

        return negative ? -x : x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next(int minValue, int count) => minValue + Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Next(uint count)
    {
        Debug.Assert(count != 0, $"{nameof(count)} must not be 0");

        if (count is 0 or 1)
        {
            return 0;
        }

        var bits = Log2(count);

        uint x;
        do
        {
            x = (uint)(NextULong() >> (64 - bits));
        } while (x >= count);

        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Next(uint minValue, uint count) => minValue + Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Next(long count)
    {
        Debug.Assert(count != 0, $"{nameof(count)} must not be 0");

        if (count is -1 or 0 or 1)
        {
            return 0;
        }

        var negative = count < 0;

        var max = negative ? -count : count;

        var bits = Log2((ulong)max);

        long x;
        do
        {
            x = (long)(NextULong() >> (64 - bits));
        } while (x >= max);

        return negative ? -x : x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Next(long minValue, long count) => minValue + Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble() => (NextULong() >> 11) * INCR_DOUBLE;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextInt() => (int)(NextULong() >> 33);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NextUInt() => (uint)NextULong();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NextBool() => (NextULong() & 0x8000000000000000) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte NextByte() => (byte)(NextULong() >> 56);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat() => (NextULong() >> 40) * INCR_FLOAT;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloatNonZero() => NextFloat() + INCR_FLOAT;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDoubleNonZero() => NextDouble() + INCR_DOUBLE;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDoubleHighRes()
    {
        var exponent = -64;
        ulong significand;
        int shift;

        while ((significand = NextULong()) == 0)
        {
            exponent -= 64;

            if (exponent < -1074)
            {
                return 0;
            }
        }

        shift = BitOperations.LeadingZeroCount(significand);
        if (shift != 0)
        {
            exponent -= shift;
            significand <<= shift;
            significand |= NextULong() >> (64 - shift);
        }

        significand |= 1;

        return significand * Math.Pow(2, exponent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Log2(uint v)
    {
        var exp = BitOperations.Log2(v);
        return v == 1 << exp ? exp : exp + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Log2(ulong v)
    {
        var exp = BitOperations.Log2(v);
        return v == 1UL << exp ? exp : exp + 1;
    }
}
