/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StableRandom.cs                                                 *
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

namespace Server.Random;

public static class StableRandom
{
    // Returns the first long of a new Rng. Used for stable values given a specific set of inputs.
    public static long First(ulong seed, long maxValue)
    {
        Span<ulong> state = stackalloc ulong[4];

        var x = seed;
        state[0] = NextSplitMix(ref x);
        state[1] = NextSplitMix(ref x);
        state[2] = NextSplitMix(ref x);
        state[3] = NextSplitMix(ref x);

        return (long)NextUInt64(state, (ulong)maxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong NextUInt64(Span<ulong> state, ulong maxValue)
    {
        ulong randomProduct = Math.BigMul(maxValue, NextUInt64(ref state), out ulong lowPart);

        if (lowPart < maxValue)
        {
            ulong remainder = (0ul - maxValue) % maxValue;

            while (lowPart < remainder)
            {
                randomProduct = Math.BigMul(maxValue, NextUInt64(ref state), out lowPart);
            }
        }

        return randomProduct;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong NextUInt64(ref Span<ulong> state)
    {
        ulong result = BitOperations.RotateLeft(state[1] * 5, 7) * 9;
        ulong t = state[1] << 17;

        state[2] ^= state[0];
        state[3] ^= state[1];
        state[1] ^= state[2];
        state[0] ^= state[3];

        state[2] ^= t;
        state[3] = BitOperations.RotateLeft(state[3], 45);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong NextSplitMix(ref ulong x)
    {
        var z = x += 0x9e3779b97f4a7c15;
        z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9;
        z = (z ^ (z >> 27)) * 0x94d049bb133111eb;
        return z ^ (z >> 31);
    }
}
