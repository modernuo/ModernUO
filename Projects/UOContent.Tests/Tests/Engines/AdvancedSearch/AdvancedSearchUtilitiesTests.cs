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

    [Theory]
    // leaf value is "T"/"F"; evalLeaf returns leaf=="T"
    [InlineData("T", true)]
    [InlineData("F", false)]
    [InlineData("F@F|T", true)]   // (F&&F)||T = T   (buggy code gave F&&(F||T)=F)
    [InlineData("T|F@F", true)]   // T||(F&&F) = T   (buggy code gave (T||F)&&F=F)
    [InlineData("T@F", false)]
    [InlineData("T@T", true)]
    [InlineData("F|F", false)]
    public void EvaluateBoolean_Precedence(string expr, bool expected)
    {
        var result = AdvancedSearchUtilities.EvaluateBoolean(expr, leaf => leaf == "T");
        Assert.Equal(expected, result);
    }
}
