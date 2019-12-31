/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: Xoshiro256PlusPlus.cs                                           *
 * Created: 2019/12/29 - Updated: 2019/12/30                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Server
{
  public class Xoshiro256PlusPlus : IRandom
  {
    private ulong _s0, _s1, _s2, _s3;

    public Xoshiro256PlusPlus() : this((ulong)Environment.TickCount64)
    {
    }

    public Xoshiro256PlusPlus(ulong seed)
    {
      var mix = new SplitMix64(seed);
      ulong[] state = new ulong[4];
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
    public uint Next(uint max)
    {
      if (max <= 1u << 12)
      {
        if (max == 0) throw new ArgumentOutOfRangeException();
        return (uint)(((ulong)NextUInt32() * max) >> 32);
      }

      uint r, v, limit = (uint)-(int)max;
      do
      {
        r = NextUInt32();
        v = r % max;
      } while (r - v > limit);

      return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NextUInt32() => (uint)NextUInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextUInt64()
    {
      ulong r1 = (_s1 << 2) + _s1;
      ulong r2 = (r1 << 7) | (r1 >> 57);
      ulong rslt = (r2 << 3) + r2;

      ulong t = _s1 << 17;

      _s2 ^= _s0;
      _s3 ^= _s1;
      _s1 ^= _s2;
      _s0 ^= _s3;

      _s2 ^= t;

      _s3 = (_s3 << 45) | (_s3 >> 19);

      return rslt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Next(ulong max)
    {
      if (max <= uint.MaxValue) return Next((uint)max);

      if (max <= 1ul << 38) return ((NextUInt32() * max >> 32) + (NextUInt32() & ((1u << 26) - 1)) * max) >> 26;

      ulong r, v, limit = (ulong)-(long)max;
      do
      {
        r = NextUInt64();
        v = r % max;
      } while (r - v > limit);

      return v;
    }

    public bool NextBool() => NextUInt64() < 1ul << 63;

    public unsafe void NextBytes(Span<byte> b)
    {
      ulong s0 = _s0;
      ulong s1 = _s1;
      ulong s2 = _s2;
      ulong s3 = _s3;

      int i = 0;

      fixed (byte* pBuffer = b)
      {
        ulong* pULong = (ulong*)pBuffer;

        for (int bound = b.Length / 8; i < bound; i++)
        {
          ulong r1 = (s1 << 2) + s1;
          ulong r2 = (r1 << 7) | (r1 >> 57);
          pULong[i] = (r2 << 3) + r2;

          ulong t = s1 << 17;
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
        ulong r1 = (s1 << 2) + s1;
        ulong r2 = (r1 << 7) | (r1 >> 57);
        ulong rslt = (r2 << 3) + r2;

        ulong t = s1 << 17;

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

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1ul << 53));

    private static readonly ulong[] JUMP =
      {0x180ec6d33cfd0aba, 0xd5a61266f0c9392c, 0xa9582618e03fc9aa, 0x39abdc4529b1661c};

    private static readonly ulong[] LONG_JUMP =
      {0x76e15d3efefdcbbf, 0xc5004e441c522fb3, 0x77710069854ee241, 0x39109bb02acbe635};

    public void Jump() => Jump(JUMP);

    public void LongJump() => Jump(LONG_JUMP);

    private void Jump(in ulong[] jumps)
    {
      ulong s0 = 0;
      ulong s1 = 0;
      ulong s2 = 0;
      ulong s3 = 0;

      for (int i = 0; i < jumps.Length; i++)
      for (int b = 0; b < 64; b++)
      {
        if ((jumps[i] & 1ul << b) != 0)
        {
          s0 ^= _s0;
          s1 ^= _s1;
          s2 ^= _s2;
          s3 ^= _s3;
        }

        NextUInt64();
      }

      _s0 = s0;
      _s1 = s1;
      _s2 = s2;
      _s3 = s3;
    }

    public Xoshiro256PlusPlus Split()
    {
      Xoshiro256PlusPlus rng = new Xoshiro256PlusPlus(_s0, _s1, _s2, _s3);
      rng.Jump();
      return rng;
    }

    public Xoshiro256PlusPlus LongSplit()
    {
      Xoshiro256PlusPlus rng = new Xoshiro256PlusPlus(_s0, _s1, _s2, _s3);
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
      ulong z = x += 0x9e3779b97f4a7c15;
      z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9;
      z = (z ^ (z >> 27)) * 0x94d049bb133111eb;
      return z ^ (z >> 31);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillArray(ulong[] arr)
    {
      for (int i = 0; i < arr.Length; i++) arr[i] = Next();
    }
  }
}
