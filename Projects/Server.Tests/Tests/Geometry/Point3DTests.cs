using System;
using Xunit;

namespace Server.Tests;

public class Point3DTests
{
    [Theory]
    [InlineData("(709, 2236, -2)", 709, 2236, -2)]
    public void TestPoint3DRegressions(string text, int x, int y, int z)
    {
        var successful = Point3D.TryParse(text, null, out var p);
        Assert.True(successful);
        Assert.Equal(new Point3D(x, y, z), p);

        Assert.Equal(new Point3D(x, y, z), Point3D.Parse(text, null));
    }

    [Fact]
    public void TestPoint3DToString()
    {
        const int max = int.MaxValue;
        const int min = int.MinValue;
        Assert.Equal("(0, 0, 0)", new Point3D(0, 0, 0).ToString());
        Assert.Equal("(1, 1, 1)", new Point3D(1, 1, 1).ToString());
        Assert.Equal($"({max}, {max}, {max})", new Point3D(max, max, max).ToString());
        Assert.Equal($"({min}, {min}, {min})", new Point3D(min, min, min).ToString());
    }

    [Fact]
    public void TestPoint3DTryFormatSucceeds()
    {
        const int max = int.MaxValue;
        const int min = int.MinValue;
        char[] array = new char[128];

        var p1 = new Point3D(0, 0, 0);
        Assert.True(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(6 + 3, cp1);
        Array.Clear(array);

        var p2 = new Point3D(1, 1, 1);
        Assert.True(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(6 + 3, cp2);
        Array.Clear(array);

        var p3 = new Point3D(max, max, max);
        Assert.True(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(6 + 30, cp3);
        Array.Clear(array);

        var p4 = new Point3D(min, min, min);
        Assert.True(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(6 + 33, cp4);
    }

    [Fact]
    public void TestPoint3DTryFormatFails()
    {
        const int max = int.MaxValue;
        const int min = int.MinValue;
        char[] array = new char[1];

        var p1 = new Point3D(0, 0, 0);
        Assert.False(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(0, cp1);
        Array.Clear(array);

        var p2 = new Point3D(1, 1, 1);
        Assert.False(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(0, cp2);
        Array.Clear(array);

        var p3 = new Point3D(max, max, max);
        Assert.False(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(0, cp3);
        Array.Clear(array);

        var p4 = new Point3D(min, min, min);
        Assert.False(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(0, cp4);
    }

    [Fact]
    public void TestPoint3DTryParse()
    {
        // Happy Path
        Assert.True(Point3D.TryParse("(101, 23, 55)", null, out var p));
        Assert.Equal(new Point3D(101, 23, 55), p);
        Assert.Equal(new Point3D(101, 23, 55), Point3D.Parse("(101, 23, 55)", null));

        // Trimming
        Assert.True(Point3D.TryParse(" (101,23 ,55)  ", null, out p));
        Assert.Equal(new Point3D(101, 23, 55), p);
        Assert.Equal(new Point3D(101, 23, 55), Point3D.Parse(" (101,23 ,55)  ", null));

        // No parenthesis
        Assert.False(Point3D.TryParse("101, 23 55)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("101, 23, 55)", null));

        Assert.False(Point3D.TryParse("(101, 23, 55", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(101, 23, 55", null));

        // No numbers
        Assert.False(Point3D.TryParse("()", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("()", null));

        Assert.False(Point3D.TryParse("(,)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(,)", null));

        Assert.False(Point3D.TryParse("(|)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(|)", null));

        Assert.False(Point3D.TryParse("(101)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(101)", null));

        Assert.False(Point3D.TryParse("(101,)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(101,)", null));

        Assert.False(Point3D.TryParse("(,23)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(,23)", null));

        Assert.False(Point3D.TryParse("(,23,55)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(,23,55)", null));

        Assert.False(Point3D.TryParse("(,23,)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(,23,)", null));

        Assert.False(Point3D.TryParse("(23,,-1)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Point3D.Parse("(23,,-1)", null));
    }
}
