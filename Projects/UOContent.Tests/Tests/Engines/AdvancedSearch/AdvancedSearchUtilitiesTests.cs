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
        // State is unused here; the leaf evaluator just checks the span equals "T".
        var result = AdvancedSearchUtilities.EvaluateBoolean(expr, 0, static (_, leaf) => leaf.SequenceEqual("T"));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CompareValues_ReferenceType_EqualityByString_NoThrow()
    {
        // A reference-typed property (e.g. RootParent name-ish) compared with "=" should not throw,
        // and ordering operators must return false rather than throwing.
        var ex = Record.Exception(() =>
        {
            Assert.False(AdvancedSearchUtilities.CompareValues(typeof(object), new object(), "whatever", ">"));
        });
        Assert.Null(ex);
    }

    [Fact]
    public void CompareValues_TimeSpan_ParsesViaSpanParsable()
    {
        // TimeSpan is not IConvertible, so the old Convert.ChangeType fallback threw and silently
        // returned no-match. ISpanParsable<TimeSpan> parses it correctly.
        var prop = TimeSpan.FromMinutes(5);
        Assert.True(AdvancedSearchUtilities.CompareValues(typeof(TimeSpan), prop, "00:05:00", "="));
        Assert.False(AdvancedSearchUtilities.CompareValues(typeof(TimeSpan), prop, "00:10:00", "="));
        Assert.True(AdvancedSearchUtilities.CompareValues(typeof(TimeSpan), prop, "00:01:00", ">"));
    }

    [Fact]
    public void CompareValues_TimeSpan_BadInput_ReturnsFalse_NoThrow()
    {
        var ex = Record.Exception(() =>
            Assert.False(AdvancedSearchUtilities.CompareValues(typeof(TimeSpan), TimeSpan.Zero, "notaspan", "=")));
        Assert.Null(ex);
    }
}
