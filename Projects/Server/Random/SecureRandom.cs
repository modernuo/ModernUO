/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SecureRandom.cs                                                 *
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
using System.Security.Cryptography;
using Server.Random;

namespace Server;

public class SecureRandom : BaseRandomSource
{
    private RandomNumberGenerator m_Random;

    public RandomNumberGenerator Generator => m_Random ??= RandomNumberGenerator.Create();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ulong NextULong()
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        NextBytes(buffer);
        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void NextBytes(Span<byte> buffer) => Generator.GetBytes(buffer);
}
