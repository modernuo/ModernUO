/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: Random.cs - Created: 2019/12/30 - Updated: 2019/12/30           *
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

namespace Server
{
  /// <summary>
  ///   Handles random number generation.
  /// </summary>
  public static class RandomImpl
  {
    private static readonly IRandom _Random = new Xoshiro256PlusPlus();

    public static uint Next(uint c) => _Random.Next(c);

    public static bool NextBool() => _Random.NextBool();

    public static void NextBytes(Span<byte> b) => _Random.NextBytes(b);

    public static double NextDouble() => _Random.NextDouble();
  }

  public static class SecureRandomImpl
  {
    private static readonly IRandom _Random;

    static SecureRandomImpl()
    {
      try
      {
        _Random = new DRng64();
        if (_Random == null || _Random is IHardwareRNG rng && !rng.IsSupported())
          _Random = new CSPRng();
      }
      catch (Exception)
      {
        _Random = new CSPRng();
      }
    }

    public static bool IsHardwareRNG => _Random is IHardwareRNG;

    public static string Name => _Random.GetType().Name;

    public static uint Next(uint c) => _Random.Next(c);

    public static bool NextBool() => _Random.NextBool();

    public static void NextBytes(Span<byte> b) => _Random.NextBytes(b);

    public static double NextDouble() => _Random.NextDouble();
  }

  public interface IRandom
  {
    uint NextUInt32();
    uint Next(uint n);
    ulong NextUInt64();
    ulong Next(ulong n);
    bool NextBool();
    void NextBytes(Span<byte> b);
    double NextDouble();
  }

  public interface IHardwareRNG
  {
    bool IsSupported();
  }

  public abstract class BaseRandom : IRandom
  {
    internal abstract void GetBytes(Span<byte> b);
    internal abstract void GetBytes(byte[] b, int offset, int count);

    public virtual void NextBytes(Span<byte> b) => GetBytes(b);
    public uint NextUInt32() => throw new NotImplementedException();

    public virtual uint Next(uint c) => (uint)(c * NextDouble());
    public ulong NextUInt64() => throw new NotImplementedException();

    public ulong Next(ulong n) => throw new NotImplementedException();

    public virtual bool NextBool() => (NextByte() & 1) == 1;

    public virtual byte NextByte()
    {
      byte[] b = new byte[1];
      GetBytes(b, 0, 1);
      return b[0];
    }

    public virtual unsafe double NextDouble()
    {
      byte[] b = new byte[8];

      if (BitConverter.IsLittleEndian)
      {
        b[7] = 0;
        GetBytes(b, 0, 7);
      }
      else
      {
        b[0] = 0;
        GetBytes(b, 1, 7);
      }

      ulong r;
      fixed (byte* buf = b) r = *(ulong*)&buf[0] >> 3;

      /* double: 53 bits of significand precision
       * ulong.MaxValue >> 11 = 9007199254740991
       * 2^53 = 9007199254740992
       */

      return (double)r / 9007199254740992;
    }
  }
}
