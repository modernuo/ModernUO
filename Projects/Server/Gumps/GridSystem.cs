using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Gumps;

public partial class Gump
{
    private Dictionary<string, Grid> _grids = new();

    private void AddColumn(string name, int x, int width)
    {
        _grids[name].Columns.Add(new Col { X = x, Width = width,});
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

    private Swap Exist(List<Swap> list, int index)
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

    private int CalculateCustom(int column,int len, string[] sizes, out List<Swap> arraySizes)
    {
        arraySizes = new List<Swap>();
        var cropSize = 0;
        for (int i = 0; i < column; i++)
        {
            if (sizes[i] != "*")
            {
                //percente
                var percente = sizes[i].Replace("*", "");
                if (sizes[i] != percente)
                {
                    var size = len / 100 * Convert.ToInt32(percente);
                    arraySizes.Add(new Swap { Index = i, Size = size });
                    cropSize += size;
                }
                else // number for example 50px
                {
                    var size = Convert.ToInt32(percente);
                    arraySizes.Add(new Swap { Index = i, Size = size });
                    cropSize += size;
                }
            }
        }
        return cropSize;
    }
    private void InitCustomSizeRow(string name, int height, int row, string[] sizes, int y)
    {
        var cropHeight = CalculateCustom(row, height, sizes, out var arraySizes);
        var Height = height - cropHeight;
        var factor = row - arraySizes.Count;
        for (int i = 0; i < row; i++)
        {
            if (Exist(arraySizes, i) is Swap item)
            {
                var size = item.Size;
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
    }

    private void InitCustomSizeColumn(string name, int width, int height, int column, string[] sizes, int x, int y)
    {
        var cropWidth = CalculateCustom(column, width, sizes, out var arraySizes);
        var Width = width - cropWidth;
        var factor = column - arraySizes.Count;

        for (int i = 0; i < column; i++)
        {
            if (Exist(arraySizes, i) is Swap item)
            {
                var size = item.Size;
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
        if (CalculateCord(destGridName, columnStart, rowStart, columnSpan, rowSpan,
                out var x, out var y, out var width, out var height))
        {
            Grid(name, width, height, createColumnsCount, createRowsCount, createColumnSize, createRowSize, x + marginX, y + marginY);
        }

        return CreateGreed(name, 0, 0);
    }

    public Grid CreateGreed(string name, int width, int height)
    {
        if (!_grids.TryGetValue(name, out var grid))
        {
            grid = new Grid { Name = name, Width = width, Height = height };
            _grids.Add(name, grid);
        }

        return grid;
    }

    public Grid Grid(string name, int width, int height, int columns, int rows, string columnSize = "", string rowSize = "", int x = 0, int y = 0)
    {
        if (columns < 1)
        {
            throw new Exception(nameof(columns));
        }

        if (rows < 1)
        {
            throw new Exception(nameof(columns));
        }

        var grid = CreateGreed(name, width, height);

        if (columnSize.Length > 0)
        {
            var buffer = columnSize.Split(' ');

            if (buffer.Length == columns)
            {
                InitCustomSizeColumn(name, width, height, columns, buffer, x, y);
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
            InitRow(name, rows, height, x);
        }

        return grid;
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

        if (column + columnSpan > _grids[name].Columns.Count)
        {
            columnSpan = _grids[name].Columns.Count;
        }

        if (row + rowSpan > _grids[name].Rows.Count)
        {
            rowSpan = _grids[name].Rows.Count;
        }

        var count = columnSpan == 0 ? column + 1 : columnSpan;
        for (int i = column; i < count; i++)
        {
            var dcol = _grids[name].Columns[i];
            if (i == column)
            {
                x = dcol.X;
            }

            width += dcol.Width;
        }
        count = rowSpan == 0 ? row + 1 : rowSpan;
        for (int i = row; i < count; i++)
        {
            var drow = _grids[name].Rows[i];
            if (i == row)
            {
                y = drow.Y;
            }

            height += drow.Height;
        }

        return true;
    }

    public class GumpList
    {
        public int LineCount { get; }
        public int ItemsCount { get; }
        public List<ListItem> Items { get; } = new();
        public ListItem Header { get; }
        public int Page { get; }
        public int TotalPages { get; }
        public int ColHeight { get; }
        public bool CanNext { get; }
        public bool CanBack { get; }
        public int ColsCount { get; }

        public GumpList(
            int itemsCount,
            int colums,
            int heightPerItem,
            int page,
            int x,
            int y,
            int width,
            int height,
            int pageItemCount,
            int[] ColWidth,
            int marginX = 0,
            int marginY = 0,
            int headerHeight = 0
        )
        {
            ItemsCount = itemsCount;
            var itemsPerPageCount = pageItemCount == 0 ? height / heightPerItem : pageItemCount;
            LineCount = itemsPerPageCount;
            TotalPages = itemsCount > 0 ? itemsCount / itemsPerPageCount : 0;
            Page = page > TotalPages ? TotalPages : page;
            ColsCount = colums;
            ColHeight = heightPerItem;

            var index = page * itemsPerPageCount > 0 ? page * itemsPerPageCount : 0;
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
                Cols = new Col[colums],
            };
            var length = x + marginX;
            for (int col = 0; col < colums; col++)
            {
                Header.Cols[col] = new Col();
                Header.Cols[col].Width = ColWidth[col];
                Header.Cols[col].X = length;
                length += ColWidth[col];
            }

            for (int i = 0; i < itemsPerPageCount; i++)
            {
                var item = new ListItem
                {
                    X = x,
                    Y = y + i * heightPerItem + marginY + headerHeight,
                    Index = index + i,
                    Cols = Header.Cols
                };

                Items.Add(item);
            }
        }
    }

    public GumpList AddListView(
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
        int marginX = 0, int marginY = 0, int headerHeight = 0, string colSize = "")
    {
        if (CalculateCord(name, column, row, columnSpan, rowSpan,
                out var x, out var y, out var Width, out var Height))
        {
            var w = width > 0 ? width : Width;
            var h = height > 0 ? height : Height;
            int[] colWidth;

            if (colSize == string.Empty)
            {
                colWidth = new int[createColumn];
                for (int i = 0; i < createColumn; i++)
                {
                    colWidth[i] = w / createColumn;
                }
            }
            else
            {
                var buffer = colSize.Split(' ');
                if (buffer.Length == 0)
                {
                    return null;
                }

                const string listName = "listView";
                _grids.Add(listName, new Grid());

                if (createColumn < buffer.Length)
                {
                    buffer = buffer.Take(createColumn).ToArray();
                }

                InitCustomSizeColumn(listName, w, h, createColumn, buffer, x, y);
                colWidth = _grids[listName].Columns.Select(_ => _.Width).ToArray();
                _grids.Remove(listName);
            }

            return new GumpList(itemsCount, createColumn, heightPerItem, page, x, y, w, h, pageItemCount, colWidth, marginX, marginY, headerHeight);
        }

        return null;
    }
}
