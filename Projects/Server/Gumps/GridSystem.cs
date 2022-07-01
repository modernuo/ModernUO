using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Gumps
{


    public partial class Gump
    {
        private Dictionary<string, Grid> Grids = new();

        private void AddColumn(string name, int x, int y, int width, int height)
        {
            Grids[name].Columns.Add(new Col() { X = x, Width = width, });
        }
        private void AddRow(string name, int x, int y, int width, int height)
        {
            Grids[name].Rows.Add(new Row() { Y = y, Height = height });
        }
        private void InitColumn(string name, int column, int Width, int Height, int x, int y)
        {
            var width = Width / column;
            var height = Height;
            for (int i = 0; i < column; i++)
            {
                AddColumn(name, i * width + x, y, width, height);
            }
        }
        private void InitRow(string name, int row, int Width, int Height, int x, int y)
        {
            var height = Height / row;
            for (int i = 0; i < row; i++)
            {
                AddRow(name, x, i * height + y, Width, height);
            }
        }
        private Swap Exist(List<Swap> list, int index)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.Index == index)
                    return item;
            }

            return null;
        }
        private int CalculateCustom(int column, int len, string[] sizes, out List<Swap> arraySizes)
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
                        var size = (len / 100) * Convert.ToInt32(percente);
                        arraySizes.Add(new Swap() { Index = i, Size = size });
                        cropSize += size;
                    }
                    else // number for example 50px
                    {
                        var size = Convert.ToInt32(percente);
                        arraySizes.Add(new Swap() { Index = i, Size = size });
                        cropSize += size;
                    }
                }
            }
            return cropSize;
        }
        private void InitCustomSizeRow(string name, int width, int height, int row, string[] sizes, int x, int y)
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
                        AddRow(name, x, y, width, size);
                    else
                    {
                        var col = Grids[name].Rows[i - 1];
                        AddRow(name, x, col.Height + col.Y + y, width, size);
                    }
                }
                else
                {
                    var size = Height / factor;
                    if (i == 0)
                        AddRow(name, x, y, width, size);
                    else
                    {
                        var col = Grids[name].Rows[i - 1];
                        AddRow(name, x, col.Height + col.Y + y, width, size);
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
                        AddColumn(name, x, y, size, height);
                    else
                    {
                        var col = Grids[name].Columns[i - 1];
                        AddColumn(name, col.Width + col.X + x, y, size, height);
                    }
                }
                else
                {
                    var size = Width / factor;
                    if (i == 0)
                        AddColumn(name, x, y, size, height);
                    else
                    {
                        var col = Grids[name].Columns[i - 1];
                        AddColumn(name, col.Width + col.X + x, y, size, height);
                    }
                }
            }
        }
        public Grid SubGrid(string name, string destGridName, int columnStart, int rowStart,
            int createColumnsCount, int createRowsCount, int columnSpan = 0, int rowSpan = 0, string createColumnSize = "", string createRowSize = "", int marginX = 0, int marginY = 0)

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
            Grid Grid;
            if (!Grids.ContainsKey(name))
            {
                Grid = new Grid() { Name = name, Width = width, Height = height };
                Grids.Add(name, Grid);
            }
            else
            {
                Grid = Grids[name];
            }
            return Grid;
        }

        public Grid Grid(string name, int width, int height, int columns, int rows, string columnSize = "", string rowSize = "", int x = 0, int y = 0)
        {
            if (columns < 1)
                throw new Exception(nameof(columns));
            if (rows < 1)
                throw new Exception(nameof(columns));

            var Grid = CreateGreed(name, width, height);

            if (columnSize.Length > 0)
            {
                var buffer = columnSize.Split(' ');
                if (buffer.Length == columns) InitCustomSizeColumn(name, width, height, columns, buffer, x, y);
                else throw new Exception(nameof(columnSize));
            }
            else
            {
                InitColumn(name, columns, width, height, x, y);
            }

            if (rowSize.Length > 0)
            {
                var buffer = rowSize.Split(' ');
                if (buffer.Length == rows) InitCustomSizeRow(name, width, height, rows, buffer, x, y);
                else throw new Exception(nameof(rowSize));
            }
            else
            {
                InitRow(name, rows, width, height, x, y);
            }

            return Grid;
        }

        public StackItems InitStack(int coord, int delta) => new(coord, delta);

        public bool CalculateCord(string name, int column, int row, int columnSpan, int rowSpan,
            out int x, out int y, out int width, out int height)
        {
            x = 0; y = 0; width = 0; height = 0;
            if (Grids.TryGetValue(name, out var grid))
            {
                var count = columnSpan == 0 ? column + 1 : row + columnSpan;

                for (int i = column; i < count; i++)
                {
                    if (i >= grid.Columns.Count)
                        break;

                    var dcol = grid.Columns[i];
                    if (i == column) x = dcol.X;
                    width += dcol.Width;
                }
                count = rowSpan == 0 ? row + 1 : row + rowSpan;
                for (int i = row; i < count; i++)
                {
                    if (i >= grid.Rows.Count)
                        break;

                    var drow = grid.Rows[i];
                    if (i == row) y = drow.Y;
                    height += drow.Height;
                }

                if (width == 0)
                    width = grid.Width;

                if (height == 0)
                    height = grid.Height;

                return true;

            }

            return false;
        }

        public class ListView
        {
            public readonly int LineCount;
            public readonly int ItemsCount;
            public readonly List<ListItem> Items = new();
            public readonly ListItem Header;
            public readonly int Page;
            public readonly int TotalPages;
            public readonly int ColHeight;
            public readonly bool CanNext;
            public readonly bool CanBack;
            public readonly int ColsCount;

            public ListView(int itemsCount, int colums, int heightPerItem, int page, int x, int y,
                int width, int height, int pageItemCount, int[] ColWidth, int marginX = 0, int marginY = 0, int headerHeight = 0)
            {
                ItemsCount = itemsCount;
                var ItemsPerPageCount = pageItemCount == 0 ? (height - marginY) / heightPerItem : pageItemCount;
                LineCount = ItemsPerPageCount;

                if (itemsCount > 0)
                {
                    TotalPages = itemsCount / ItemsPerPageCount;
                    if (itemsCount % ItemsPerPageCount == 0)
                        TotalPages--;
                }

                Page = page > TotalPages ? TotalPages : page;
                ColsCount = colums;
                ColHeight = heightPerItem;

                var index = (Page * ItemsPerPageCount) > 0 ? (Page * ItemsPerPageCount) : 0;
                var nowPageItems = (index + ItemsPerPageCount);

                if (index > 0)
                    CanBack = true;

                if (nowPageItems < itemsCount)
                    CanNext = true;

                if (nowPageItems > itemsCount)
                    ItemsPerPageCount = Page > 0 ? itemsCount - (Page * ItemsPerPageCount) : itemsCount;

                //header
                Header = new ListItem()
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

                for (int i = 0; i < ItemsPerPageCount; i++)
                {
                    var item = new ListItem()
                    {
                        X = x,
                        Y = y + (i * heightPerItem) + marginY + headerHeight,
                        Index = index + i,
                        Cols = Header.Cols
                    };

                    Items.Add(item);
                }
            }
        }

        public ListView AddListView(string name, int column, int row, int itemsCount, int page, int heightPerItem, int createColumn,
             int columnSpan = 0, int rowSpan = 0, int width = 0, int height = 0, int pageItemCount = 0,
              int marginX = 0, int marginY = 0, int headerHeight = 0, string colSize = "")
        {
            if (CalculateCord(name, column, row, columnSpan, rowSpan,
               out var x, out var y, out var Width, out var Height))
            {
                var w = width > 0 ? width : Width;
                var h = height > 0 ? height : Height;
                int[] colWidtdh = null;

                if (colSize == string.Empty)
                {
                    colWidtdh = new int[createColumn];
                    for (int i = 0; i < createColumn; i++)
                        colWidtdh[i] = w / createColumn;
                }
                else if (colSize.Split(' ') is string[] buffer && buffer.Length > 0)
                {
                    var listNmae = "listView";
                    Grids.Add(listNmae, new Grid());

                    if (createColumn < buffer.Length)
                        buffer = buffer.Take(createColumn).ToArray();

                    InitCustomSizeColumn(listNmae, w, h, createColumn, buffer, x, y);
                    colWidtdh = Grids[listNmae].Columns.Select(_ => _.Width).ToArray();
                    Grids.Remove(listNmae);
                }
                else
                    throw new Exception(nameof(createColumn));

                return new ListView(itemsCount, createColumn, heightPerItem, page, x, y, w, h, pageItemCount, colWidtdh, marginX, marginY, headerHeight);

            }

            return null;
        }
    }
}

