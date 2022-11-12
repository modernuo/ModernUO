using System;
using Xunit;

namespace Server.Tests;

public class Point2DTests
{
    [Fact]
    public void TestPoint2DToString()
    {
        Assert.Equal("(0, 0)", new Point2D(0, 0).ToString());
        Assert.Equal("(1, 1)", new Point2D(1, 1).ToString());
        Assert.Equal($"({int.MaxValue}, {int.MaxValue})", new Point2D(int.MaxValue, int.MaxValue).ToString());
        Assert.Equal($"({int.MinValue}, {int.MaxValue})", new Point2D(int.MinValue, int.MaxValue).ToString());
        Assert.Equal($"({int.MinValue}, {int.MinValue})", new Point2D(int.MinValue, int.MinValue).ToString());
        Assert.Equal($"({int.MaxValue}, {int.MinValue})", new Point2D(int.MaxValue, int.MinValue).ToString());
    }

    [Fact]
    public void TestPoint2DTryFormatSucceeds()
    {
        char[] array = new char[128];

        var p1 = new Point2D(0, 0);
        Assert.True(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(6, cp1);
        Array.Clear(array);

        var p2 = new Point2D(1, 1);
        Assert.True(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(6, cp2);
        Array.Clear(array);

        var p3 = new Point2D(int.MaxValue, int.MaxValue);
        Assert.True(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(4 + 20, cp3);
        Array.Clear(array);

        var p4 = new Point2D(int.MinValue, int.MinValue);
        Assert.True(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(4 + 22, cp4);
    }

    [Fact]
    public void TestPoint2DTryFormatFails()
    {
        char[] array = new char[1];

        var p1 = new Point2D(0, 0);
        Assert.False(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(0, cp1);
        Array.Clear(array);

        var p2 = new Point2D(1, 1);
        Assert.False(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(0, cp2);
        Array.Clear(array);

        var p3 = new Point2D(int.MaxValue, int.MaxValue);
        Assert.False(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(0, cp3);
        Array.Clear(array);

        var p4 = new Point2D(int.MinValue, int.MinValue);
        Assert.False(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(0, cp4);
    }

    [Fact]
    public void TestPoint2DTryParse()
    {
        // Happy Path
        Assert.True(Point2D.TryParse("(101, 23)", null, out var p));
        Assert.Equal(new Point2D(101, 23), p);
        Assert.Equal(new Point2D(101, 23), Point2D.Parse("(101, 23)", null));

        // Trimming
        Assert.True(Point2D.TryParse(" (101,23)  ", null, out p));
        Assert.Equal(new Point2D(101, 23), p);
        Assert.Equal(new Point2D(101, 23), Point2D.Parse(" (101,23)  ", null));

        // No parenthesis
        Assert.False(Point2D.TryParse("101, 23)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point2D.Parse("101, 23)", null));

        Assert.False(Point2D.TryParse("(101, 23", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point2D.Parse("(101, 23", null));

        // No numbers
        Assert.False(Point2D.TryParse("()", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point2D.Parse("()", null));

        Assert.False(Point2D.TryParse("(101)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point2D.Parse("(101)", null));

        Assert.False(Point2D.TryParse("(101,)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point2D.Parse("(101,)", null));

        Assert.False(Point2D.TryParse("(,23)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point2D.Parse("(,23)", null));
    }
}
