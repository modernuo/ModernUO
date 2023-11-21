/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.IO.Hashing;
using System.Numerics;

namespace Server;

public static class HashUtility
{
    // *************** DO NOT CHANGE THIS NUMBER ****************
    // * Computed hashes might be serialized against this seed! *
    // **********************************************************
    private const ulong xxHash3Seed = 9609125370673258709ul; // Randomly generated 64-bit prime number
    private const uint xxHash1Seed = 665738807u; // Randomly generated 32-bit prime number

    [ThreadStatic]
    private static XxHash3 _xxHash3;

    [ThreadStatic]
    private static XxHash32 _xxHash32;

    public static unsafe ulong ComputeHash64(string? str)
    {
        if (str == null)
        {
            return 0;
        }

        var hasher = _xxHash3 ??= new XxHash3(unchecked((long)xxHash3Seed));

        fixed (char* src = &str.GetPinnableReference())
        {
            hasher.Append(new ReadOnlySpan<byte>(src, str.Length * 2));
        }

        var result = hasher.GetCurrentHashAsUInt64();
        hasher.Reset();

        return result;
    }

    public static unsafe uint ComputeHash32(string? str)
    {
        if (str == null)
        {
            return 0;
        }

        var hasher = _xxHash32 ??= new XxHash32(unchecked((int)xxHash1Seed));

        fixed (char* src = &str.GetPinnableReference())
        {
            hasher.Append(new ReadOnlySpan<byte>(src, str.Length * 2));
        }

        var result = hasher.GetCurrentHashAsUInt32();
        hasher.Reset();

        return result;
    }

    public static unsafe int GetNetFrameworkHashCode(this string? str)
    {
        if (str == null)
        {
            return 0;
        }

        fixed (char* src = &str.GetPinnableReference())
        {
            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            uint* ptr = (uint*)src;
            int length = str.Length;

            while (length > 2)
            {
                length -= 4;
                // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                ptr += 2;
            }

            if (length > 0)
            {
                // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
            }

            return (int)(hash1 + hash2 * 1566083941);
        }
    }
}
