/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: HashUtility.cs                                                  *
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
using Standart.Hash.xxHash;

namespace Server;

/// <summary>
/// Represents supported non-cryptographic fast hash algorithms.
/// </summary>
public enum FastHashAlgorithm
{
    None, // Used for collisions where full-data is serialized instead
    XXHash3_64, // xxHash3 64bit
}

public static class HashUtility
{
    // *************** DO NOT CHANGE THIS NUMBER ****************
    // * Computed hashes might be serialized against this seed! *
    // **********************************************************
    private const ulong xxHash3Seed = 9609125370673258709ul; // Randomly generated 64-bit prime number

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeHash64(string? data, FastHashAlgorithm algorithm = FastHashAlgorithm.XXHash3_64) =>
        algorithm switch
        {
            FastHashAlgorithm.XXHash3_64 => ComputeXXHash3_64(data),
            _                            => throw new NotSupportedException($"Hash {algorithm} is not supported.")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeXXHash3_64(string? data) => data == null ? 0 : xxHash3.ComputeHash(data, xxHash3Seed);
}
