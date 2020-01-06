/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: Random.cs - Created: 2019/12/30 - Updated: 2019/01/05           *
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
using System.Security.Cryptography;

namespace Server
{
  /// <summary>
  ///   Handles random number generation.
  /// </summary>
  public static class RandomImpl
  {
    private static readonly Xoshiro256PlusPlus _Random = new Xoshiro256PlusPlus();

    public static uint Next(uint c) => _Random.Next(c);

    public static bool NextBool() => _Random.NextBool();

    public static void GetBytes(Span<byte> b) => _Random.GetBytes(b);

    public static double NextDouble() => _Random.NextDouble();
  }

  public static class SecureRandomImpl
  {
    public static readonly RandomNumberGenerator _Random;

    static SecureRandomImpl()
    {
      try
      {
        _Random = new DRng64();
        if (_Random is IHardwareRNG rng && rng?.IsSupported() != true)
          _Random = RandomNumberGenerator.Create();
      }
      catch (Exception)
      {
        _Random = RandomNumberGenerator.Create();
      }
    }

    public static bool IsHardwareRNG => _Random is IHardwareRNG;

    public static string Name => _Random.GetType().Name;

    public static void GetBytes(Span<byte> buffer) => _Random.GetBytes(buffer);
  }

  public interface IHardwareRNG
  {
    bool IsSupported();
  }
}
