/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Xoshiro256PlusPlus.cs                                           *
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
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Server.Random;

public class Xoshiro256PlusPlus : BaseRandomSource
{
    private static readonly ulong[] JUMP =
        { 0x180ec6d33cfd0aba, 0xd5a61266f0c9392c, 0xa9582618e03fc9aa, 0x39abdc4529b1661c };

    private static readonly ulong[] LONG_JUMP =
        { 0x76e15d3efefdcbbf, 0xc5004e441c522fb3, 0x77710069854ee241, 0x39109bb02acbe635 };

    private ulong _s0, _s1, _s2, _s3;

    public Xoshiro256PlusPlus()
    {
        Span<byte> states = stackalloc byte[32];
        RandomSources.SecureSource.NextBytes(states);
        _s0 = BinaryPrimitives.ReadUInt64LittleEndian(states[..8]);
        _s1 = BinaryPrimitives.ReadUInt64LittleEndian(states[8..16]);
        _s2 = BinaryPrimitives.ReadUInt64LittleEndian(states[16..24]);
        _s3 = BinaryPrimitives.ReadUInt64LittleEndian(states[24..32]);
    }

    public Xoshiro256PlusPlus(ulong seed)
    {
        var mix = new SplitMix64(seed);
        Span<ulong> state = stackalloc ulong[4];
        mix.FillArray(state);
        _s0 = state[0];
        _s1 = state[1];
        _s2 = state[2];
        _s3 = state[3];
    }

    private Xoshiro256PlusPlus(ulong s0, ulong s1, ulong s2, ulong s3)
    {
        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ulong NextULong()
    {
        var r1 = (_s1 << 2) + _s1;
        var r2 = (r1 << 7) | (r1 >> 57);
        var rslt = (r2 << 3) + r2;

        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;

        _s2 ^= t;

        _s3 = (_s3 << 45) | (_s3 >> 19);

        return rslt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe void NextBytes(Span<byte> b)
    {
        if (b.Length == 0)
        {
            return;
        }

        var s0 = _s0;
        var s1 = _s1;
        var s2 = _s2;
        var s3 = _s3;

        var i = 0;

        fixed (byte* pBuffer = b)
        {
            var pULong = (ulong*)pBuffer;

            for (var bound = b.Length / sizeof(ulong); i < bound; i++)
            {
                var r1 = (s1 << 2) + s1;
                var r2 = (r1 << 7) | (r1 >> 57);
                pULong[i] = (r2 << 3) + r2;

                var t = s1 << 17;
                s2 ^= s0;
                s3 ^= s1;
                s1 ^= s2;
                s0 ^= s3;

                s2 ^= t;

                s3 = (s3 << 45) | (s3 >> 19);
            }
        }

        i *= 8;

        if (i < b.Length)
        {
            var r1 = (s1 << 2) + s1;
            var r2 = (r1 << 7) | (r1 >> 57);
            var rslt = (r2 << 3) + r2;

            var t = s1 << 17;

            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;

            s2 ^= t;

            s3 = (s3 << 45) | (s3 >> 19);

            while (i < b.Length)
            {
                b[i++] = (byte)rslt;
                rslt >>= 8;
            }
        }

        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
    }

    public void Jump() => Jump(JUMP);

    public void LongJump() => Jump(LONG_JUMP);

    private void Jump(in ulong[] jumps)
    {
        ulong s0 = 0;
        ulong s1 = 0;
        ulong s2 = 0;
        ulong s3 = 0;

        for (var i = 0; i < jumps.Length; i++)
        {
            for (var b = 0; b < 64; b++)
            {
                if ((jumps[i] & (1ul << b)) != 0)
                {
                    s0 ^= _s0;
                    s1 ^= _s1;
                    s2 ^= _s2;
                    s3 ^= _s3;
                }

                NextULong();
            }
        }

        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
    }

    public Xoshiro256PlusPlus Split()
    {
        var rng = new Xoshiro256PlusPlus(_s0, _s1, _s2, _s3);
        rng.Jump();
        return rng;
    }

    public Xoshiro256PlusPlus LongSplit()
    {
        var rng = new Xoshiro256PlusPlus(_s0, _s1, _s2, _s3);
        rng.LongJump();
        return rng;
    }
}

public class SplitMix64
{
    private ulong x;

    public SplitMix64(ulong seed) => x = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Next()
    {
        var z = x += 0x9e3779b97f4a7c15;
        z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9;
        z = (z ^ (z >> 27)) * 0x94d049bb133111eb;
        return z ^ (z >> 31);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillArray(Span<ulong> arr)
    {
        for (var i = 0; i < arr.Length; i++)
        {
            arr[i] = Next();
        }
    }
}
