/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Random.cs - Created: 2019/12/30 - Updated: 2020/05/23           *
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
  public class SecureRandom : RandomNumberGenerator, IRandomProvider
  {
    private readonly RandomNumberGenerator m_Random = new RNGCryptoServiceProvider();

    public override void GetBytes(byte[] data) => m_Random.GetBytes(data);

    public override void GetBytes(Span<byte> data) => m_Random.GetBytes(data);

    public uint Next(uint c) => throw new NotImplementedException();

    public bool NextBool() => throw new NotImplementedException();

    public double NextDouble() => throw new NotImplementedException();
  }
}
