/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpGrid.cs                                                     *
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
using System.Collections.Generic;
using Server.Collections;

namespace Server.Gumps;

public class GumpGrid : Gump
{
    private Dictionary<string, Grid> _grids = new();

    public GumpGrid(int x, int y) : base(x, y)
    {
    }

    private void AddColumn(string name, int x, int width)
    {
        _grids[name].Columns.Add(new Col { X = x, Width = width, });
    }

    private void AddRow(string name, int y, int height)
    {
        _grids[name].Rows.Add(new Row { Y = y, Height = height });
    }

    private void InitColumn(string name, int column, int w, int x)
    {
        var width = w / column;
        for (int i = 0; i < column; i++)
        {
            AddColumn(name, i * width + x, width);
        }
    }

    private void InitRow(string name, int row, int h, int y)
    {
        var height = h / row;
        for (int i = 0; i < row; i++)
        {
            AddRow(name, i * height + y, height);
        }
    }

    private static Swap Exist(ref PooledRefList<Swap> list, int index)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (item.Index == index)
            {
                return item;
            }
        }

        return null;
    }

    private static int CalculateCustom(int column, int len, string[] sizes, ref PooledRefList<Swap> list)
    {
        var cropSize = 0;

        for (int i = 0; i < column; i++)
        {
            if (sizes[i] == "*")
            {
                continue;
            }

            //percent
            var percent = sizes[i].Replace("*", "");
            var size = sizes[i] != percent ? len / 100 * int.Parse(percent) : int.Parse(percent);

            list.Add(new Swap { Index = i, Size = size });
            cropSize += size;
        }

        return cropSize;
    }
    private void InitCustomSizeRow(string name, int height, int row, string[] sizes, int y)
    {
        var list = PooledRefList<Swap>.Create();
        var cropHeight = CalculateCustom(row, height, sizes, ref list);
        var Height = height - cropHeight;
        var factor = row - list.Count;
        for (int i = 0; i < row; i++)
        {
            var swap = Exist(ref list, i);
            if (swap != null)
            {
                var size = swap.Size;
                if (i == 0)
                {
                    AddRow(name, y, size);
                }
                else
                {
                    var col = _grids[name].Rows[i - 1];
                    AddRow(name, col.Height + col.Y + y, size);
                }
            }
            else
            {
                var size = Height / factor;
                if (i == 0)
                {
                    AddRow(name, y, size);
                }
                else
                {
                    var col = _grids[name].Rows[i - 1];
                    AddRow(name, col.Height + col.Y + y, size);
                }
            }
        }

        list.Dispose();
    }

    private void InitCustomSizeColumn(string name, int width, int column, string[] sizes, int x)
    {
        var list = PooledRefList<Swap>.Create();
        var cropWidth = CalculateCustom(column, width, sizes, ref list);
        var Width = width - cropWidth;
        var factor = column - list.Count;

        for (int i = 0; i < column; i++)
        {
            var swap = Exist(ref list, i);
            if (swap != null)
            {
                var size = swap.Size;
                if (i == 0)
                {
                    AddColumn(name, x, size);
                }
                else
                {
                    var col = _grids[name].Columns[i - 1];
                    AddColumn(name, col.Width + col.X + x, size);
                }
            }
            else
            {
                var size = Width / factor;
                if (i == 0)
                {
                    AddColumn(name, x, size);
                }
                else
                {
                    var col = _grids[name].Columns[i - 1];
                    AddColumn(name, col.Width + col.X + x, size);
                }
            }
        }
    }

    public Grid SubGrid(
        string name,
        string destGridName,
        int columnStart,
        int rowStart,
        int createColumnsCount,
        int createRowsCount,
        int columnSpan = 0,
        int rowSpan = 0,
        string createColumnSize = "",
        string createRowSize = "",
        int marginX = 0,
        int marginY = 0
    )
    {
        if (CalculateCord(
                destGridName,
                columnStart,
                rowStart,
                columnSpan,
                rowSpan,
                out var x,
                out var y,
                out var width,
                out var height
            ))
        {
            Grid(
                name,
                width,
                height,
                createColumnsCount,
                createRowsCount,
                createColumnSize,
                createRowSize,
                x + marginX,
                y + marginY
            );
        }

        return CreateGrid(name, 0, 0);
    }

    public Grid CreateGrid(string name, int width, int height)
    {
        if (!_grids.TryGetValue(name, out var grid))
        {
            grid = new Grid { Name = name, Width = width, Height = height };
            _grids.Add(name, grid);
        }

        return grid;
    }

    public Grid Grid(
        string name,
        int width,
        int height,
        int columns,
        int rows,
        string columnSize = "",
        string rowSize = "",
        int x = 0,
        int y = 0
    )
    {
        if (columns < 1)
        {
            throw new Exception(nameof(columns));
        }

        if (rows < 1)
        {
            throw new Exception(nameof(columns));
        }

        var Grid = CreateGrid(name, width, height);

        if (columnSize.Length > 0)
        {
            var buffer = columnSize.Split(' ');
            if (buffer.Length == columns)
            {
                InitCustomSizeColumn(name, width, columns, buffer, x);
            }
            else
            {
                throw new Exception(nameof(columnSize));
            }
        }
        else
        {
            InitColumn(name, columns, width, x);
        }

        if (rowSize.Length > 0)
        {
            var buffer = rowSize.Split(' ');
            if (buffer.Length == rows)
            {
                InitCustomSizeRow(name, height, rows, buffer, y);
            }
            else
            {
                throw new Exception(nameof(rowSize));
            }
        }
        else
        {
            InitRow(name, rows, height, y);
        }

        return Grid;
    }

    public bool CalculateCord(
        string name,
        int column,
        int row,
        int columnSpan,
        int rowSpan,
        out int x,
        out int y,
        out int width,
        out int height
    )
    {
        x = 0; y = 0; width = 0; height = 0;
        if (_grids.TryGetValue(name, out var grid))
        {
            var count = columnSpan == 0 ? column + 1 : row + columnSpan;
            for (int i = column; i < count; i++)
            {
                if (i >= grid.Columns.Count)
                {
                    break;
                }

                var dcol = grid.Columns[i];
                if (i == column)
                {
                    x = dcol.X;
                }

                width += dcol.Width;
            }

            count = rowSpan == 0 ? row + 1 : row + rowSpan;
            for (int i = row; i < count; i++)
            {
                if (i >= grid.Rows.Count)
                {
                    break;
                }

                var drow = grid.Rows[i];
                if (i == row)
                {
                    y = drow.Y;
                }

                height += drow.Height;
            }

            if (width == 0)
            {
                width = grid.Width;
            }

            if (height == 0)
            {
                height = grid.Height;
            }

            return true;

        }

        return false;
    }

    public class ListView
    {
        public int LineCount { get; }
        public int ItemsCount { get; }
        public ListItem[] Items { get; }
        public ListItem Header { get; }
        public int Page { get; }
        public int TotalPages { get; }
        public int ColHeight { get; }
        public bool CanNext { get; }
        public bool CanBack { get; }
        public int ColsCount { get; }

        public ListView(
            int itemsCount,
            int columns,
            int heightPerItem,
            int page,
            int x,
            int y,
            int height,
            int pageItemCount,
            int[] ColWidth,
            int marginX = 0,
            int marginY = 0,
            int headerHeight = 0
        )
        {
            ItemsCount = itemsCount;
            var itemsPerPageCount = pageItemCount == 0 ? (height - marginY) / heightPerItem : pageItemCount;
            LineCount = itemsPerPageCount;

            if (itemsCount > 0)
            {
                TotalPages = itemsCount / itemsPerPageCount;
                if (itemsCount % itemsPerPageCount == 0)
                {
                    TotalPages--;
                }
            }

            Page = page > TotalPages ? TotalPages : page;
            ColsCount = columns;
            ColHeight = heightPerItem;

            var index = Page * itemsPerPageCount > 0 ? Page * itemsPerPageCount : 0;
            var nowPageItems = index + itemsPerPageCount;

            if (index > 0)
            {
                CanBack = true;
            }

            if (nowPageItems < itemsCount)
            {
                CanNext = true;
            }

            if (nowPageItems > itemsCount)
            {
                itemsPerPageCount = Page > 0 ? itemsCount - Page * itemsPerPageCount : itemsCount;
            }

            //header
            Header = new ListItem
            {
                X = x,
                Y = y + marginY,
                Index = -1,
                Cols = new Col[columns],
            };

            var length = x + marginX;
            for (int col = 0; col < columns; col++)
            {
                Header.Cols[col] = new Col();
                Header.Cols[col].Width = ColWidth[col];
                Header.Cols[col].X = length;
                length += ColWidth[col];
            }

            Items = new ListItem[itemsPerPageCount];

            for (int i = 0; i < itemsPerPageCount; i++)
            {
                Items[i] = new ListItem
                {
                    X = x,
                    Y = y + i * heightPerItem + marginY + headerHeight,
                    Index = index + i,
                    Cols = Header.Cols
                };
            }
        }
    }

    public ListView AddListView(
        string name,
        int column,
        int row,
        int itemsCount,
        int page,
        int heightPerItem,
        int createColumn,
        int columnSpan = 0,
        int rowSpan = 0,
        int width = 0,
        int height = 0,
        int pageItemCount = 0,
        int marginX = 0,
        int marginY = 0,
        int headerHeight = 0,
        string colSize = ""
    )
    {
        if (CalculateCord(name, column, row, columnSpan, rowSpan, out var x, out var y, out var w, out var h))
        {
            w = width > 0 ? width : w;
            h = height > 0 ? height : h;

            int[] colWidth = new int[createColumn];

            if (colSize == string.Empty)
            {
                for (int i = 0; i < createColumn; i++)
                {
                    colWidth[i] = w / createColumn;
                }
            }
            else
            {
                var buffer = colSize.Split(' ');

                if (buffer.Length > 0)
                {
                    const string listName = "listView";
                    _grids.Add(listName, new Grid());

                    if (createColumn < buffer.Length)
                    {
                        Array.Resize(ref buffer, createColumn);
                    }

                    InitCustomSizeColumn(listName, w, createColumn, buffer, x);
                    var cols = _grids[listName].Columns;
                    for (var i = 0; i < createColumn; i++)
                    {
                        colWidth[i] = cols[i].Width;
                    }

                    _grids.Remove(listName);
                }
            }

            return new ListView(
                itemsCount,
                createColumn,
                heightPerItem,
                page,
                x,
                y,
                h,
                pageItemCount,
                colWidth,
                marginX,
                marginY,
                headerHeight
            );
        }

        return null;
    }
}
