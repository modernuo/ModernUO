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
