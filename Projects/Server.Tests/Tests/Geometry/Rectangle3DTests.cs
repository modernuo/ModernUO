using System;
using Xunit;

namespace Server.Tests;

public class Rectangle3DTests
{
    [Fact]
    public void TestRectangle3DToString()
    {
        const int max = int.MaxValue;
        const int min = int.MinValue;
        Assert.Equal("(0, 0, 0)+(0, 0, 0)", new Rectangle3D(0, 0, 0, 0, 0, 0).ToString());
        Assert.Equal("(1, 1, 1)+(1, 1, 1)", new Rectangle3D(1, 1, 1, 1, 1, 1).ToString());
        Assert.Equal($"({max}, {max}, {max})+({max}, {max}, {max})", new Rectangle3D(max, max, max, max, max, max).ToString());
        Assert.Equal($"({min}, {min}, {min})+({min}, {min}, {min})", new Rectangle3D(min, min, min, min, min, min).ToString());
    }

    [Fact]
    public void TestRectangle3DTryFormatSucceeds()
    {
        const int max = int.MaxValue;
        const int min = int.MinValue;
        char[] array = new char[128];

        var p1 = new Rectangle3D(0, 0, 0, 0, 0, 0);
        Assert.True(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(13 + 6, cp1);
        Array.Clear(array);

        var p2 = new Rectangle3D(1, 1, 1, 1, 1, 1);
        Assert.True(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(13 + 6, cp2);
        Array.Clear(array);

        var p3 = new Rectangle3D(max, max, max, max, max, max);
        Assert.True(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(13 + 6 * 10, cp3);
        Array.Clear(array);

        var p4 = new Rectangle3D(min, min, min, min, min, min);
        Assert.True(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(13 + 6 * 11, cp4);
    }

    [Fact]
    public void TestRectangle3DTryFormatFails()
    {
        const int max = int.MaxValue;
        const int min = int.MinValue;
        char[] array = new char[1];

        var p1 = new Rectangle3D(0, 0, 0, 0, 0, 0);
        Assert.False(p1.TryFormat(array, out var cp1, null, null));
        Assert.Equal(0, cp1);
        Array.Clear(array);

        var p2 = new Rectangle3D(1, 1, 1, 1, 1, 1);
        Assert.False(p2.TryFormat(array, out var cp2, null, null));
        Assert.Equal(0, cp2);
        Array.Clear(array);

        var p3 = new Rectangle3D(max, max, max, max, max, max);
        Assert.False(p3.TryFormat(array, out var cp3, null, null));
        Assert.Equal(0, cp3);
        Array.Clear(array);

        var p4 = new Rectangle3D(min, min, min, min, min, min);
        Assert.False(p4.TryFormat(array, out var cp4, null, null));
        Assert.Equal(0, cp4);
    }

    [Fact]
    public void TestRectangle3DTryParse()
    {
        // Happy Path
        Assert.True(Rectangle3D.TryParse("(101, 23, 10)+(55, 89, 1)", null, out var p));
        Assert.Equal(new Rectangle3D(new Point3D(101, 23, 10), new Point3D(55, 89, 1)), p);
        Assert.Equal(new Rectangle3D(new Point3D(101, 23, 10), new Point3D(55, 89, 1)), Rectangle3D.Parse("(101, 23, 10)+(55, 89, 1)", null));

        // Trimming
        Assert.True(Rectangle3D.TryParse(" (101,23,10)+ (55, 89,1)  ", null, out p));
        Assert.Equal(new Rectangle3D(new Point3D(101, 23, 10), new Point3D(55, 89, 1)), p);
        Assert.Equal(new Rectangle3D(new Point3D(101, 23, 10), new Point3D(55, 89, 1)), Rectangle3D.Parse(" (101,23,10)+ (55, 89,1)  ", null));

        // No parenthesis
        Assert.False(Rectangle3D.TryParse("101, 23, 10)+( 55, 89, 1)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("101, 23, 10)+( 55, 89, 1)", null));

        Assert.False(Rectangle3D.TryParse("(101, 23, 10)+(55, 89, 1", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(101, 23, 10)+(55, 89, 1", null));

        // No numbers
        Assert.False(Rectangle3D.TryParse("()", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("()", null));

        Assert.False(Rectangle3D.TryParse("(,)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(,)", null));

        Assert.False(Rectangle3D.TryParse("(|)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(|)", null));

        Assert.False(Rectangle3D.TryParse("(101)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(101)", null));

        Assert.False(Rectangle3D.TryParse("(101,)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(101,)", null));

        Assert.False(Rectangle3D.TryParse("(101,23)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(101,23)", null));

        Assert.False(Rectangle3D.TryParse("(,23)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(,23)", null));

        Assert.False(Rectangle3D.TryParse("(,23)+(,55)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(,23)+(,55)", null));

        Assert.False(Rectangle3D.TryParse("(,23,)+(,89)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(,23,)+(,89)", null));

        Assert.False(Rectangle3D.TryParse("(101,,10)+(55, 89 1)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(,23,)+(,89)", null));

        Assert.False(Rectangle3D.TryParse("(,,)+(55,89, 1)", null, out p));
        Assert.Equal(default, p);
        Assert.Throws<FormatException>(() => Rectangle3D.Parse("(,,)+(55,89, 1)", null));
    }
}
