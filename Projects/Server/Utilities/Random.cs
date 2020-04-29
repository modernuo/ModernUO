/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
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
    private static readonly Xoshiro256PlusPlus m_Random = new Xoshiro256PlusPlus();

    public static uint Next(uint c) => m_Random.Next(c);

    public static bool NextBool() => m_Random.NextBool();

    public static void GetBytes(Span<byte> b) => m_Random.GetBytes(b);

    public static double NextDouble() => m_Random.NextDouble();
  }

  public static class SecureRandomImpl
  {
    public static readonly RandomNumberGenerator m_Random;

    static SecureRandomImpl()
    {
      try
      {
        m_Random = new DRng64();
        if (m_Random is IHardwareRNG rng && rng?.IsSupported() != true)
          m_Random = new RNGCryptoServiceProvider();
      }
      catch (Exception)
      {
        m_Random = new RNGCryptoServiceProvider();
      }
    }

    public static bool IsHardwareRNG => m_Random is IHardwareRNG;

    public static string Name => m_Random.GetType().Name;

    public static void GetBytes(Span<byte> buffer) => m_Random.GetBytes(buffer);
  }

  public interface IHardwareRNG
  {
    bool IsSupported();
  }
}
