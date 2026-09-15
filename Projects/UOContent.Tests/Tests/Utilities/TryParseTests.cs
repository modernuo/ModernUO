using System;
using Xunit;

namespace Server.Tests.Utility;

public class TryParseTests
{
    [Theory]
    [InlineData("True", null, true)]
    [InlineData("False", null, false)]
    [InlineData("Alakazam", "Not a valid boolean string.", true)]
    public void TestTryParseBool(string value, string returned, bool parsedAs)
    {
        var actualReturned = Server.Types.TryParse(typeof(bool), value, out var constructed);
        Assert.Equal(returned, actualReturned);

        if (returned == null)
        {
            Assert.Equal(parsedAs, constructed);
        }
    }

    [Theory]
    // Parsed directly into the target type (INumber<T>.TryParse), not via ulong + Convert.ChangeType.
    [InlineData(typeof(int), "42", true, 42)]
    [InlineData(typeof(int), "-5", true, -5)]              // signed values parse directly now
    [InlineData(typeof(int), "0xFF", true, 255)]          // hex
    [InlineData(typeof(int), "notanumber", false, null)]
    [InlineData(typeof(byte), "255", true, (byte)255)]
    [InlineData(typeof(byte), "256", false, null)]        // out of the byte range
    [InlineData(typeof(uint), "4294967295", true, 4294967295u)]
    [InlineData(typeof(long), "-9000000000", true, -9000000000L)]
    public void TestTryParseNumeric(Type type, string value, bool success, object expected)
    {
        var error = Server.Types.TryParse(type, value, out var constructed);

        if (success)
        {
            Assert.Null(error);
            Assert.Equal(expected, constructed);
        }
        else
        {
            Assert.NotNull(error);
        }
    }
}

// Interface-typed properties (BaseCreature.TargetLocation : IPoint2D) have no static Parse, so
// without these branches the parser fell through to Convert.ChangeType and reported the value as
// "not properly formatted".
public class InterfacePointParseTests
{
    [Theory]
    [InlineData(typeof(IPoint3D), "(1, 2, 3)", 1, 2, 3)]
    [InlineData(typeof(IPoint2D), "(1, 2, 3)", 1, 2, 3)]  // a 3-tuple is a valid IPoint2D
    public void TuplesParseIntoPoint3D(Type type, string value, int x, int y, int z)
    {
        Assert.Null(Server.Types.TryParse(type, value, out var constructed));
        Assert.Equal(new Point3D(x, y, z), constructed);
    }

    [Fact]
    public void PairParsesIntoPoint2D()
    {
        Assert.Null(Server.Types.TryParse(typeof(IPoint2D), "(4, 5)", out var constructed));
        Assert.Equal(new Point2D(4, 5), constructed);
    }

    [Fact]
    public void PairIsNotAPoint3D()
    {
        Assert.NotNull(Server.Types.TryParse(typeof(IPoint3D), "(4, 5)", out _));
    }

    [Fact]
    public void NullSentinelClearsAnInterfaceProperty()
    {
        Assert.Null(Server.Types.TryParse(typeof(IPoint2D), "(-null-)", out var constructed));
        Assert.Null(constructed);
    }
}
