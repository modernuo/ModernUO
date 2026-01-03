/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridSizeSpec.cs                                                 *
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

namespace Server.Gumps;

/// <summary>
/// Specifies how a grid column or row should be sized.
/// Supports absolute pixels, percentage, and star (proportional) sizing.
/// </summary>
public readonly struct GridSizeSpec
{
    /// <summary>
    /// The sizing type.
    /// </summary>
    public readonly GridSizeType Type;

    /// <summary>
    /// The value - meaning depends on Type:
    /// - Absolute: pixel size
    /// - Percent: percentage of total size (1-100)
    /// - Star: proportional weight (1 = equal share)
    /// </summary>
    public readonly int Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GridSizeSpec(GridSizeType type, int value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Creates an absolute pixel size specification.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridSizeSpec Absolute(int pixels) => new(GridSizeType.Absolute, pixels);

    /// <summary>
    /// Creates a star (proportional) size specification.
    /// Star-sized tracks share the remaining space equally (after absolute/percent are allocated).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridSizeSpec Star() => new(GridSizeType.Star, 1);

    /// <summary>
    /// Creates a percentage size specification.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridSizeSpec Percent(int percent) => new(GridSizeType.Percent, percent);

    /// <summary>
    /// Parses a single size specification token.
    /// Formats:
    /// - "*" = star (equal share of remaining space)
    /// - "10*" = 10% of total size
    /// - "100" = 100 pixels absolute
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridSizeSpec Parse(ReadOnlySpan<char> token)
    {
        token = token.Trim();

        if (token.IsEmpty)
        {
            return Star();
        }

        // Check for star notation
        var starIndex = token.IndexOf('*');

        if (starIndex == -1)
        {
            // No star - absolute pixel value
            if (int.TryParse(token, out var pixels))
            {
                return Absolute(pixels);
            }

            return Star();
        }

        if (starIndex == 0 && token.Length == 1)
        {
            // Just "*" - equal share star
            return Star();
        }

        // "N*" format - percentage
        var percentPart = token[..starIndex];
        if (int.TryParse(percentPart, out var percent))
        {
            return Percent(percent);
        }

        return Star();
    }

    /// <summary>
    /// Parses a space-separated size specification string into a span of GridSizeSpec values.
    /// Returns the number of specs parsed.
    /// </summary>
    public static int ParseAll(ReadOnlySpan<char> sizeSpec, Span<GridSizeSpec> destination)
    {
        var count = 0;
        var remaining = sizeSpec;

        while (!remaining.IsEmpty && count < destination.Length)
        {
            // Skip leading whitespace
            remaining = remaining.TrimStart();
            if (remaining.IsEmpty)
            {
                break;
            }

            // Find end of current token
            var spaceIndex = remaining.IndexOf(' ');
            ReadOnlySpan<char> token;

            if (spaceIndex < 0)
            {
                token = remaining;
                remaining = default;
            }
            else
            {
                token = remaining[..spaceIndex];
                remaining = remaining[(spaceIndex + 1)..];
            }

            destination[count++] = Parse(token);
        }

        return count;
    }

    /// <summary>
    /// Counts the number of tokens in a space-separated size specification string.
    /// </summary>
    public static int CountTokens(ReadOnlySpan<char> sizeSpec)
    {
        var count = 0;
        var inToken = false;

        for (var i = 0; i < sizeSpec.Length; i++)
        {
            var isSpace = char.IsWhiteSpace(sizeSpec[i]);
            if (!isSpace && !inToken)
            {
                count++;
                inToken = true;
            }
            else if (isSpace)
            {
                inToken = false;
            }
        }

        return count;
    }
}

/// <summary>
/// Specifies the type of grid size specification.
/// </summary>
public enum GridSizeType : byte
{
    /// <summary>
    /// Fixed pixel size.
    /// </summary>
    Absolute,

    /// <summary>
    /// Star sizing - shares remaining space equally with other star-sized tracks.
    /// </summary>
    Star,

    /// <summary>
    /// Percentage of total available size.
    /// </summary>
    Percent
}
