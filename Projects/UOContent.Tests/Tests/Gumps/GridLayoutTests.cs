using System;
using Server.Gumps;
using Xunit;

namespace Server.Tests.Gumps;

public class GridLayoutTests
{
    [Fact]
    public void TestGridSizeSpec_ParseAbsolute()
    {
        var spec = GridSizeSpec.Parse("100");
        Assert.Equal(GridSizeType.Absolute, spec.Type);
        Assert.Equal(100, spec.Value);
    }

    [Fact]
    public void TestGridSizeSpec_ParseStar()
    {
        var spec = GridSizeSpec.Parse("*");
        Assert.Equal(GridSizeType.Star, spec.Type);
        Assert.Equal(1, spec.Value);
    }

    [Fact]
    public void TestGridSizeSpec_ParsePercent()
    {
        var spec = GridSizeSpec.Parse("10*");
        Assert.Equal(GridSizeType.Percent, spec.Type);
        Assert.Equal(10, spec.Value);
    }

    [Fact]
    public void TestGridSizeSpec_ParseAll()
    {
        Span<GridSizeSpec> specs = stackalloc GridSizeSpec[4];
        var count = GridSizeSpec.ParseAll("10* * 100 20*", specs);

        Assert.Equal(4, count);
        Assert.Equal(GridSizeType.Percent, specs[0].Type);
        Assert.Equal(10, specs[0].Value);
        Assert.Equal(GridSizeType.Star, specs[1].Type);
        Assert.Equal(GridSizeType.Absolute, specs[2].Type);
        Assert.Equal(100, specs[2].Value);
        Assert.Equal(GridSizeType.Percent, specs[3].Type);
        Assert.Equal(20, specs[3].Value);
    }

    [Fact]
    public void TestGridCalculator_UniformTracks()
    {
        Span<int> positions = stackalloc int[4];
        Span<int> sizes = stackalloc int[4];

        GridCalculator.ComputeUniformTrackSizes(4, 400, 0, positions, sizes);

        Assert.Equal(0, positions[0]);
        Assert.Equal(100, positions[1]);
        Assert.Equal(200, positions[2]);
        Assert.Equal(300, positions[3]);

        Assert.Equal(100, sizes[0]);
        Assert.Equal(100, sizes[1]);
        Assert.Equal(100, sizes[2]);
        Assert.Equal(100, sizes[3]);
    }

    [Fact]
    public void TestGridCalculator_UniformTracksWithOrigin()
    {
        Span<int> positions = stackalloc int[2];
        Span<int> sizes = stackalloc int[2];

        GridCalculator.ComputeUniformTrackSizes(2, 200, 50, positions, sizes);

        Assert.Equal(50, positions[0]);
        Assert.Equal(150, positions[1]);
        Assert.Equal(100, sizes[0]);
        Assert.Equal(100, sizes[1]);
    }

    [Fact]
    public void TestGridCalculator_MixedTracks()
    {
        // "10* * 100" = 10%, star, 100px absolute
        Span<int> positions = stackalloc int[3];
        Span<int> sizes = stackalloc int[3];

        var count = GridCalculator.ComputeFromSpec("10* * 100", 1000, 0, positions, sizes);

        Assert.Equal(3, count);

        // 10% of 1000 = 100
        Assert.Equal(0, positions[0]);
        Assert.Equal(100, sizes[0]);

        // 100px absolute
        Assert.Equal(900, positions[2]);
        Assert.Equal(100, sizes[2]);

        // Star gets remaining: 1000 - 100 - 100 = 800
        Assert.Equal(100, positions[1]);
        Assert.Equal(800, sizes[1]);
    }

    [Fact]
    public void TestGridCell_Properties()
    {
        var cell = new GridCell(10, 20, 100, 50);

        Assert.Equal(10, cell.X);
        Assert.Equal(20, cell.Y);
        Assert.Equal(100, cell.Width);
        Assert.Equal(50, cell.Height);
        Assert.Equal(110, cell.Right);
        Assert.Equal(70, cell.Bottom);
        Assert.Equal(60, cell.CenterX);
        Assert.Equal(45, cell.CenterY);
    }

    [Fact]
    public void TestGridCell_Inset()
    {
        var cell = new GridCell(10, 20, 100, 50);
        var inset = cell.Inset(5);

        Assert.Equal(15, inset.X);
        Assert.Equal(25, inset.Y);
        Assert.Equal(90, inset.Width);
        Assert.Equal(40, inset.Height);
    }

    [Fact]
    public void TestGridCell_Offset()
    {
        var cell = new GridCell(10, 20, 100, 50);
        var offset = cell.Offset(5, 10);

        Assert.Equal(15, offset.X);
        Assert.Equal(30, offset.Y);
        Assert.Equal(100, offset.Width);
        Assert.Equal(50, offset.Height);
    }

    [Fact]
    public void TestListViewLayout_Pagination()
    {
        Span<int> colPos = stackalloc int[3];
        Span<int> colWidths = stackalloc int[3];

        // 100 items, row height 20, header 10, total height 210 (10 items per page)
        var layout = ListViewLayout.Create(
            0, 0, 300, 210,
            100, // total items
            0,   // current page
            20,  // row height
            10,  // header height
            "* * *",
            colPos, colWidths);

        Assert.Equal(100, layout.TotalItems);
        Assert.Equal(10, layout.ItemsPerPage);
        Assert.Equal(0, layout.CurrentPage);
        Assert.Equal(10, layout.TotalPages);
        Assert.Equal(0, layout.StartIndex);
        Assert.Equal(10, layout.VisibleCount);
        Assert.False(layout.CanGoBack);
        Assert.True(layout.CanGoNext);
    }

    [Fact]
    public void TestListViewLayout_MiddlePage()
    {
        Span<int> colPos = stackalloc int[3];
        Span<int> colWidths = stackalloc int[3];

        var layout = ListViewLayout.Create(
            0, 0, 300, 210,
            100, // total items
            5,   // current page (middle)
            20,  // row height
            10,  // header height
            "* * *",
            colPos, colWidths);

        Assert.Equal(5, layout.CurrentPage);
        Assert.Equal(50, layout.StartIndex);
        Assert.Equal(10, layout.VisibleCount);
        Assert.True(layout.CanGoBack);
        Assert.True(layout.CanGoNext);
    }

    [Fact]
    public void TestListViewLayout_LastPage()
    {
        Span<int> colPos = stackalloc int[3];
        Span<int> colWidths = stackalloc int[3];

        // 95 items, 10 per page = 10 pages, last page has 5 items
        var layout = ListViewLayout.Create(
            0, 0, 300, 210,
            95,  // total items
            9,   // last page
            20,  // row height
            10,  // header height
            "* * *",
            colPos, colWidths);

        Assert.Equal(9, layout.CurrentPage);
        Assert.Equal(90, layout.StartIndex);
        Assert.Equal(5, layout.VisibleCount); // Only 5 items on last page
        Assert.True(layout.CanGoBack);
        Assert.False(layout.CanGoNext);
    }

    [Fact]
    public void TestListViewLayout_EmptyList()
    {
        Span<int> colPos = stackalloc int[3];
        Span<int> colWidths = stackalloc int[3];

        var layout = ListViewLayout.Create(
            0, 0, 300, 210,
            0,   // no items
            0,   // current page
            20,  // row height
            10,  // header height
            "* * *",
            colPos, colWidths);

        Assert.Equal(0, layout.TotalItems);
        Assert.Equal(1, layout.TotalPages);
        Assert.Equal(0, layout.StartIndex);
        Assert.Equal(0, layout.VisibleCount);
        Assert.False(layout.CanGoBack);
        Assert.False(layout.CanGoNext);
    }

    [Fact]
    public void TestListViewLayout_GetHeaderCell()
    {
        Span<int> colPos = stackalloc int[3];
        Span<int> colWidths = stackalloc int[3];

        var layout = ListViewLayout.Create(
            10, 20, 300, 200,
            50, 0, 20, 30,
            "100 100 100",
            colPos, colWidths);

        var headerCell = layout.GetHeaderCell(1, colPos, colWidths);

        Assert.Equal(110, headerCell.X); // 10 (origin) + 100 (col 0 width)
        Assert.Equal(20, headerCell.Y);  // origin Y
        Assert.Equal(100, headerCell.Width);
        Assert.Equal(30, headerCell.Height); // header height
    }

    [Fact]
    public void TestListViewLayout_GetRowCell()
    {
        Span<int> colPos = stackalloc int[3];
        Span<int> colWidths = stackalloc int[3];

        var layout = ListViewLayout.Create(
            0, 0, 300, 200,
            50, 0, 20, 30,
            "100 100 100",
            colPos, colWidths);

        var rowCell = layout.GetRowCell(2, 1, colPos, colWidths);

        Assert.Equal(100, rowCell.X); // column 1 position
        Assert.Equal(70, rowCell.Y);  // 0 (origin) + 30 (header) + 2*20 (rows)
        Assert.Equal(100, rowCell.Width);
        Assert.Equal(20, rowCell.Height);
    }

    [Fact]
    public void TestListViewLayout_ColumnSpec()
    {
        Span<int> colPos = stackalloc int[12];
        Span<int> colWidths = stackalloc int[12];

        // The SpawnerControllerGump column spec
        var layout = ListViewLayout.Create(
            0, 80, 1000, 620,
            100, 0, 45, 30,
            "6* 8* 18* 6* 12* 6* 5* 5* 8* 8* 9* *",
            colPos, colWidths);

        Assert.Equal(12, layout.ColumnCount);

        // First column: 6% of 1000 = 60
        Assert.Equal(0, colPos[0]);
        Assert.Equal(60, colWidths[0]);

        // Second column: 8% = 80
        Assert.Equal(60, colPos[1]);
        Assert.Equal(80, colWidths[1]);
    }
}
