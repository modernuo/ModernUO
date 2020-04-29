/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
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

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Server
{
  public class Xoshiro256PlusPlus : RandomNumberGenerator
  {
    private ulong m_S0, m_S1, m_S2, m_S3;

    public Xoshiro256PlusPlus() : this((ulong)Environment.TickCount64)
    {
    }

    public Xoshiro256PlusPlus(ulong seed)
    {
      var mix = new SplitMix64(seed);
      var state = new ulong[4];
      mix.FillArray(state);
      m_S0 = state[0];
      m_S1 = state[1];
      m_S2 = state[2];
      m_S3 = state[3];
    }

    private Xoshiro256PlusPlus(ulong s0, ulong s1, ulong s2, ulong s3)
    {
      m_S0 = s0;
      m_S1 = s1;
      m_S2 = s2;
      m_S3 = s3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Next(uint max)
    {
      if (max <= 1u << 12)
      {
        if (max == 0) throw new ArgumentOutOfRangeException(nameof(max));
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
      var r1 = (m_S1 << 2) + m_S1;
      var r2 = (r1 << 7) | (r1 >> 57);
      var rslt = (r2 << 3) + r2;

      var t = m_S1 << 17;

      m_S2 ^= m_S0;
      m_S3 ^= m_S1;
      m_S1 ^= m_S2;
      m_S0 ^= m_S3;

      m_S2 ^= t;

      m_S3 = (m_S3 << 45) | (m_S3 >> 19);

      return rslt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Next(ulong max)
    {
      if (max <= uint.MaxValue) return Next((uint)max);

      if (max <= 1ul << 38) return (((NextUInt32() * max) >> 32) + (NextUInt32() & ((1u << 26) - 1)) * max) >> 26;

      ulong r, v, limit = (ulong)-(long)max;
      do
      {
        r = NextUInt64();
        v = r % max;
      } while (r - v > limit);

      return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NextBool() => NextUInt64() < 1ul << 63;

    public override void GetBytes(byte[] data)
    {
      if (data == null) throw new ArgumentNullException(nameof(data));
      GetBytes(new Span<byte>(data));
    }

    public override unsafe void GetBytes(Span<byte> b)
    {
      if (b.Length == 0) return;

      var s0 = m_S0;
      var s1 = m_S1;
      var s2 = m_S2;
      var s3 = m_S3;

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

      m_S0 = s0;
      m_S1 = s1;
      m_S2 = s2;
      m_S3 = s3;
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1ul << 53));

    private static readonly ulong[] JUMP =
      { 0x180ec6d33cfd0aba, 0xd5a61266f0c9392c, 0xa9582618e03fc9aa, 0x39abdc4529b1661c };

    private static readonly ulong[] LONG_JUMP =
      { 0x76e15d3efefdcbbf, 0xc5004e441c522fb3, 0x77710069854ee241, 0x39109bb02acbe635 };

    public void Jump() => Jump(JUMP);

    public void LongJump() => Jump(LONG_JUMP);

    private void Jump(in ulong[] jumps)
    {
      ulong s0 = 0;
      ulong s1 = 0;
      ulong s2 = 0;
      ulong s3 = 0;

      for (var i = 0; i < jumps.Length; i++)
        for (var b = 0; b < 64; b++)
        {
          if ((jumps[i] & (1ul << b)) != 0)
          {
            s0 ^= m_S0;
            s1 ^= m_S1;
            s2 ^= m_S2;
            s3 ^= m_S3;
          }

          NextUInt64();
        }

      m_S0 = s0;
      m_S1 = s1;
      m_S2 = s2;
      m_S3 = s3;
    }

    public Xoshiro256PlusPlus Split()
    {
      var rng = new Xoshiro256PlusPlus(m_S0, m_S1, m_S2, m_S3);
      rng.Jump();
      return rng;
    }

    public Xoshiro256PlusPlus LongSplit()
    {
      var rng = new Xoshiro256PlusPlus(m_S0, m_S1, m_S2, m_S3);
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
    public void FillArray(ulong[] arr)
    {
      for (var i = 0; i < arr.Length; i++) arr[i] = Next();
    }
  }
}
