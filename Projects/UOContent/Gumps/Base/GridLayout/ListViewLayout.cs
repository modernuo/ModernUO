/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ListViewLayout.cs                                               *
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
/// Calculates pagination and provides helper methods for a paginated list view.
/// This struct does NOT hold column data - column positions/widths are passed
/// separately via spans to avoid ref struct limitations.
/// </summary>
public ref struct ListViewLayout
{
    private readonly int _originY;
    private readonly int _headerHeight;
    private readonly int _rowHeight;

    /// <summary>
    /// The total number of items in the list.
    /// </summary>
    public readonly int TotalItems;

    /// <summary>
    /// The number of items that can be displayed per page.
    /// </summary>
    public readonly int ItemsPerPage;

    /// <summary>
    /// The current page index (0-based).
    /// </summary>
    public readonly int CurrentPage;

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public readonly int TotalPages;

    /// <summary>
    /// The index of the first item on the current page.
    /// </summary>
    public readonly int StartIndex;

    /// <summary>
    /// The number of items visible on the current page.
    /// </summary>
    public readonly int VisibleCount;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public readonly bool CanGoBack;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public readonly bool CanGoNext;

    /// <summary>
    /// The number of columns in the list.
    /// </summary>
    public readonly int ColumnCount;

    /// <summary>
    /// The height of each data row.
    /// </summary>
    public int RowHeight
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _rowHeight;
    }

    /// <summary>
    /// The height of the header row.
    /// </summary>
    public int HeaderHeight
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _headerHeight;
    }

    /// <summary>
    /// Creates a new ListViewLayout and computes column positions into the provided spans.
    /// </summary>
    /// <param name="originX">The X origin of the list area.</param>
    /// <param name="originY">The Y origin of the list area.</param>
    /// <param name="width">The total width of the list area.</param>
    /// <param name="height">The total height of the list area.</param>
    /// <param name="totalItems">The total number of items in the data source.</param>
    /// <param name="currentPage">The current page index (0-based).</param>
    /// <param name="rowHeight">The height of each data row.</param>
    /// <param name="headerHeight">The height of the header row (0 for no header).</param>
    /// <param name="columnSpec">Space-separated column size specification.</param>
    /// <param name="columnPositions">Span to receive calculated column X positions.</param>
    /// <param name="columnWidths">Span to receive calculated column widths.</param>
    /// <returns>The configured ListViewLayout.</returns>
    public static ListViewLayout Create(
        int originX,
        int originY,
        int width,
        int height,
        int totalItems,
        int currentPage,
        int rowHeight,
        int headerHeight,
        ReadOnlySpan<char> columnSpec,
        Span<int> columnPositions,
        Span<int> columnWidths
    )
    {
        // Parse and compute column widths
        var columnCount = GridCalculator.ComputeFromSpec(columnSpec, width, originX, columnPositions, columnWidths);

        return new ListViewLayout(
            originY,
            headerHeight, rowHeight,
            totalItems, currentPage,
            height,
            columnCount
        );
    }

    private ListViewLayout(
        int originY,
        int headerHeight,
        int rowHeight,
        int totalItems,
        int currentPage,
        int totalHeight,
        int columnCount
    )
    {
        _originY = originY;
        _headerHeight = headerHeight;
        _rowHeight = rowHeight;
        ColumnCount = columnCount;

        TotalItems = totalItems;

        // Calculate items per page based on available height
        var contentHeight = totalHeight - headerHeight;
        ItemsPerPage = contentHeight / rowHeight;

        // Calculate pagination
        if (totalItems > 0 && ItemsPerPage > 0)
        {
            TotalPages = (totalItems + ItemsPerPage - 1) / ItemsPerPage;
            CurrentPage = Math.Clamp(currentPage, 0, TotalPages - 1);
            StartIndex = CurrentPage * ItemsPerPage;
            VisibleCount = Math.Min(ItemsPerPage, totalItems - StartIndex);
            CanGoBack = CurrentPage > 0;
            CanGoNext = CurrentPage < TotalPages - 1;
        }
        else
        {
            TotalPages = 1;
            CurrentPage = 0;
            StartIndex = 0;
            VisibleCount = 0;
            CanGoBack = false;
            CanGoNext = false;
        }
    }

    /// <summary>
    /// Gets the header cell for the specified column using pre-computed column data.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell GetHeaderCell(int column, ReadOnlySpan<int> columnPositions, ReadOnlySpan<int> columnWidths) =>
        new(columnPositions[column], _originY, columnWidths[column], _headerHeight);

    /// <summary>
    /// Gets the data cell for the specified visible row and column.
    /// </summary>
    /// <param name="visibleRowIndex">The 0-based index within visible rows (not data index).</param>
    /// <param name="column">The column index.</param>
    /// <param name="columnPositions">Pre-computed column X positions.</param>
    /// <param name="columnWidths">Pre-computed column widths.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridCell GetRowCell(
        int visibleRowIndex, int column, ReadOnlySpan<int> columnPositions, ReadOnlySpan<int> columnWidths
    ) => new(
        columnPositions[column],
        _originY + _headerHeight + visibleRowIndex * _rowHeight,
        columnWidths[column],
        _rowHeight
    );

    /// <summary>
    /// Gets the data index for a visible row.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetDataIndex(int visibleRowIndex) => StartIndex + visibleRowIndex;

    /// <summary>
    /// Gets the Y position for a visible row.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetRowY(int visibleRowIndex) => _originY + _headerHeight + visibleRowIndex * _rowHeight;

    /// <summary>
    /// Gets the X position of a column.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetColumnX(ReadOnlySpan<int> columnPositions, int column) => columnPositions[column];

    /// <summary>
    /// Gets the width of a column.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetColumnWidth(ReadOnlySpan<int> columnWidths, int column) => columnWidths[column];

    /// <summary>
    /// Gets the horizontal center of a column.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetColumnCenterX(ReadOnlySpan<int> columnPositions, ReadOnlySpan<int> columnWidths, int column) =>
        columnPositions[column] + columnWidths[column] / 2;

    /// <summary>
    /// Gets the right edge of a column.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetColumnRightX(ReadOnlySpan<int> columnPositions, ReadOnlySpan<int> columnWidths, int column) =>
        columnPositions[column] + columnWidths[column];
}
