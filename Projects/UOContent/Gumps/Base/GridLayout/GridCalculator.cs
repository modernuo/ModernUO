/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridCalculator.cs                                               *
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
/// Static helper for calculating grid cell positions without heap allocations.
/// All results are written to caller-provided spans using stackalloc.
/// </summary>
public static class GridCalculator
{
    /// <summary>
    /// Maximum supported columns or rows per grid.
    /// </summary>
    public const int MaxTracks = 32;

    /// <summary>
    /// Computes track sizes from size specifications.
    /// Writes positions and sizes to the provided spans.
    /// </summary>
    /// <param name="specs">The size specifications for each track.</param>
    /// <param name="totalSize">The total available size (width or height).</param>
    /// <param name="origin">The starting position (x or y offset).</param>
    /// <param name="positions">Output: position of each track.</param>
    /// <param name="sizes">Output: size of each track.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ComputeTrackSizes(
        ReadOnlySpan<GridSizeSpec> specs,
        int totalSize,
        int origin,
        Span<int> positions,
        Span<int> sizes
    )
    {
        var trackCount = specs.Length;

        // First pass: calculate absolute/percent sizes and count star tracks
        var remainingSize = totalSize;
        var starCount = 0;

        for (var i = 0; i < trackCount; i++)
        {
            var spec = specs[i];
            switch (spec.Type)
            {
                case GridSizeType.Absolute:
                    {
                        sizes[i] = spec.Value;
                        remainingSize -= spec.Value;
                        break;
                    }
                case GridSizeType.Percent:
                    {
                        sizes[i] = totalSize * spec.Value / 100;
                        remainingSize -= sizes[i];
                        break;
                    }
                case GridSizeType.Star:
                    {
                        starCount++;
                        sizes[i] = -1; // Mark as star for second pass
                        break;
                    }
            }
        }

        // Second pass: distribute remaining space to star tracks
        if (starCount > 0 && remainingSize > 0)
        {
            var starSize = remainingSize / starCount;
            for (var i = 0; i < trackCount; i++)
            {
                if (sizes[i] == -1)
                {
                    sizes[i] = starSize;
                }
            }
        }
        else
        {
            // No star tracks or no remaining space - set any marked stars to 0
            for (var i = 0; i < trackCount; i++)
            {
                if (sizes[i] < 0)
                {
                    sizes[i] = 0;
                }
            }
        }

        // Third pass: calculate positions
        var pos = origin;
        for (var i = 0; i < trackCount; i++)
        {
            positions[i] = pos;
            pos += sizes[i];
        }
    }

    /// <summary>
    /// Computes uniform track sizes (equal-sized cells).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ComputeUniformTrackSizes(
        int trackCount,
        int totalSize,
        int origin,
        Span<int> positions,
        Span<int> sizes
    )
    {
        var trackSize = totalSize / trackCount;
        var pos = origin;

        for (var i = 0; i < trackCount; i++)
        {
            positions[i] = pos;
            sizes[i] = trackSize;
            pos += trackSize;
        }
    }

    /// <summary>
    /// Parses a size specification string and computes track sizes.
    /// </summary>
    /// <param name="sizeSpec">Space-separated size specification (e.g., "10* * 100").</param>
    /// <param name="totalSize">The total available size.</param>
    /// <param name="origin">The starting position.</param>
    /// <param name="positions">Output: position of each track.</param>
    /// <param name="sizes">Output: size of each track.</param>
    /// <returns>The number of tracks computed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeFromSpec(
        ReadOnlySpan<char> sizeSpec,
        int totalSize,
        int origin,
        Span<int> positions,
        Span<int> sizes
    ) => ComputeFromSpec(sizeSpec, totalSize, origin, 0, positions, sizes);

    /// <summary>
    /// Parses a size specification string and computes track sizes with gaps between tracks.
    /// </summary>
    /// <param name="sizeSpec">Space-separated size specification (e.g., "10* * 100").</param>
    /// <param name="totalSize">The total available size.</param>
    /// <param name="origin">The starting position.</param>
    /// <param name="gap">The gap size between tracks.</param>
    /// <param name="positions">Output: position of each track.</param>
    /// <param name="sizes">Output: size of each track.</param>
    /// <returns>The number of tracks computed.</returns>
    public static int ComputeFromSpec(
        ReadOnlySpan<char> sizeSpec,
        int totalSize,
        int origin,
        int gap,
        Span<int> positions,
        Span<int> sizes)
    {
        Span<GridSizeSpec> specs = stackalloc GridSizeSpec[MaxTracks];
        var trackCount = GridSizeSpec.ParseAll(sizeSpec, specs);

        if (trackCount > 0)
        {
            // Subtract total gap space from available size before computing track sizes
            var totalGapSpace = gap * (trackCount - 1);
            var availableSize = totalSize - totalGapSpace;

            ComputeTrackSizes(specs[..trackCount], availableSize, origin, positions, sizes);

            // Adjust positions to account for gaps
            if (gap > 0)
            {
                for (var i = 1; i < trackCount; i++)
                {
                    positions[i] += gap * i;
                }
            }
        }

        return trackCount;
    }

    /// <summary>
    /// Gets a cell at the specified column and row from pre-computed grid arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridCell GetCell(
        ReadOnlySpan<int> columnPositions,
        ReadOnlySpan<int> columnWidths,
        ReadOnlySpan<int> rowPositions,
        ReadOnlySpan<int> rowHeights,
        int column,
        int row
    ) => new(columnPositions[column], rowPositions[row], columnWidths[column], rowHeights[row]);

    /// <summary>
    /// Gets a cell spanning multiple columns and/or rows from pre-computed grid arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GridCell GetCell(
        ReadOnlySpan<int> columnPositions,
        ReadOnlySpan<int> columnWidths,
        ReadOnlySpan<int> rowPositions,
        ReadOnlySpan<int> rowHeights,
        int column,
        int row,
        int columnSpan,
        int rowSpan
    )
    {
        var width = 0;
        var endCol = Math.Min(column + columnSpan, columnWidths.Length);
        for (var c = column; c < endCol; c++)
        {
            width += columnWidths[c];
        }

        var height = 0;
        var endRow = Math.Min(row + rowSpan, rowHeights.Length);
        for (var r = row; r < endRow; r++)
        {
            height += rowHeights[r];
        }

        return new GridCell(columnPositions[column], rowPositions[row], width, height);
    }
}
