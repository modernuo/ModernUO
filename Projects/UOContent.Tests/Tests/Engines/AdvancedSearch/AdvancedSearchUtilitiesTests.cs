using System;
using Server;
using Server.Engines.AdvancedSearch;
using Xunit;

namespace UOContent.Tests;

public class AdvancedSearchUtilitiesTests
{
    [Theory]
    [InlineData("abc")]           // not a number -> was FormatException
    [InlineData("99999999999")]   // overflows int -> was OverflowException
    [InlineData("0xZZ")]          // bad hex -> was FormatException
    public void CompareValues_BadNumeric_ReturnsFalse_DoesNotThrow(string value)
    {
        var ex = Record.Exception(() =>
        {
            var result = AdvancedSearchUtilities.CompareValues(typeof(int), 5, value, ">");
            Assert.False(result);
        });
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("Bogus")]         // not a member -> was ArgumentException
    [InlineData("onehandedxyz")]  // not a member, even case-insensitively -> was ArgumentException
    public void CompareValues_BadEnum_ReturnsFalse_DoesNotThrow(string value)
    {
        var ex = Record.Exception(() =>
        {
            var result = AdvancedSearchUtilities.CompareValues(typeof(Layer), (byte)Layer.OneHanded, value, "=");
            Assert.False(result);
        });
        Assert.Null(ex);
    }

    [Fact]
    public void CompareValues_ValidEnum_IgnoresCase()
    {
        Assert.True(AdvancedSearchUtilities.CompareValues(typeof(Layer), (byte)Layer.OneHanded, "onehanded", "="));
    }
}
