/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BuiltInRng.cs                                                   *
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
using System.Runtime.CompilerServices;

namespace Server.Random;

public static class BuiltInRng
{
    public static System.Random Generator { get; private set; } = new();

    public static void Reset() => Generator = new System.Random();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next() => Generator.Next();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next(int maxValue) => Generator.Next(maxValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next(int minValue, int count) => minValue + Generator.Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Next(long maxValue) => Generator.NextInt64(maxValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Next(long minValue, long count) => minValue + Generator.NextInt64(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NextDouble() => Generator.NextDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextBytes(Span<byte> buffer) => Generator.NextBytes(buffer);
}
