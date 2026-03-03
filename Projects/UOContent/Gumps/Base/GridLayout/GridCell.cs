/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridCell.cs                                                     *
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

namespace Server.Gumps;

/// <summary>
/// Represents a pre-calculated cell position within a grid layout.
/// This is a value type with zero heap allocations.
/// </summary>
public ref struct GridCell
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the right edge X coordinate (X + Width).
    /// </summary>
    public int Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => X + Width;
    }

    /// <summary>
    /// Gets the bottom edge Y coordinate (Y + Height).
    /// </summary>
    public int Bottom
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Y + Height;
    }

    /// <summary>
    /// Gets the horizontal center X coordinate.
    /// </summary>
    public int CenterX
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => X + Width / 2;
    }

    /// <summary>
    /// Gets the vertical center Y coordinate.
    /// </summary>
    public int CenterY
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Y + Height / 2;
    }

    /// <summary>
    /// Creates a new cell with uniform margin inset on all sides.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell Inset(int margin) =>
        new(X + margin, Y + margin, Width - margin * 2, Height - margin * 2);

    /// <summary>
    /// Creates a new cell with separate horizontal and vertical margins.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell Inset(int horizontal, int vertical) =>
        new(X + horizontal, Y + vertical, Width - horizontal * 2, Height - vertical * 2);

    /// <summary>
    /// Creates a new cell with individual margins for each side.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell Inset(int left, int top, int right, int bottom) =>
        new(X + left, Y + top, Width - left - right, Height - top - bottom);

    /// <summary>
    /// Creates a new cell with an offset applied to the position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell Offset(int offsetX, int offsetY) => new(X + offsetX, Y + offsetY, Width, Height);

    /// <summary>
    /// Creates a new cell with a different width.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell WithWidth(int width) => new(X, Y, width, Height);

    /// <summary>
    /// Creates a new cell with a different height.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell WithHeight(int height) => new(X, Y, Width, height);
}
