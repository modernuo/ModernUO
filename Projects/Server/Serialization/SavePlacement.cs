/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SavePlacement.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;

namespace Server;

/// <summary>
/// Packs where an entity's record sits in the last committed save file into one
/// <see cref="long" /> so every entity can carry it: 40 bits of position (stored plus one,
/// so the zero-initialized field means "no placement") and 24 bits of length. A record too
/// large to pack has no placement and simply serializes on every save.
/// </summary>
public static class SavePlacement
{
    public const long None = 0;

    private const int LengthBits = 24;
    private const long LengthMask = (1L << LengthBits) - 1;
    private const long MaxStoredPosition = (1L << 40) - 1;

    /// <summary>Largest record length that can be placed. Longer records always re-serialize.</summary>
    public const int MaxLength = (int)LengthMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Encode(long position, int length)
    {
        if (position < 0 || length <= 0 || length > MaxLength || position > MaxStoredPosition - 1)
        {
            return None;
        }

        return ((position + 1) << LengthBits) | (uint)length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNone(long placement) => placement == None;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Position(long placement) => (placement >>> LengthBits) - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(long placement) => (int)(placement & LengthMask);
}
