/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: RandomProviders.cs - Created: 2020/05/24 - Updated: 2020/05/24  *
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
  public interface IRandomProvider
  {
    public uint Next(uint c);
    public bool NextBool();
    public void GetBytes(Span<byte> b);
    public double NextDouble();
  }

  public static class RandomProviders
  {
    private static IRandomProvider m_Provider;
    private static IRandomProvider m_SecureProvider;

    public static IRandomProvider Provider => m_Provider ??= new Xoshiro256PlusPlus();
    public static IRandomProvider SecureProvider => m_SecureProvider ??= new SecureRandom();
  }
}
