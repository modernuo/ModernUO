/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SecureRandom.cs - Created: 2020/01/09 - Updated: 2020/07/25     *
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
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Server.Random;

namespace Server
{
    public class SecureRandom : BaseRandomSource
    {
        private RandomNumberGenerator m_Random;

        public RandomNumberGenerator Generator => m_Random ??= new RNGCryptoServiceProvider();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ulong NextULong()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            Generator.GetBytes(buffer);
            return BinaryPrimitives.ReadUInt64BigEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void NextBytes(Span<byte> buffer) => Generator.GetBytes(buffer);
    }
}
