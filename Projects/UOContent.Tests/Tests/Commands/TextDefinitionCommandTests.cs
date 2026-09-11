using Server;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands;

// The command layer reaches a TextDefinition through four doors -- [set, [add, spawner Props and
// the props gump -- and all four funnel through Types.TryParse. Commands.Split has already thrown
// the caller's quotes away by then, so "0" and 0 arrive identical; the only way to say which one
// you meant is an in-band marker.
public class TextDefinitionParsingTests
{
    private static TextDefinition Parse(string value)
    {
        Assert.Null(Types.TryParse(typeof(TextDefinition), value, out var parsed));
        return parsed as TextDefinition;
    }

    [Theory]
    [InlineData("1060847", 1060847)]
    [InlineData("#1060847", 1060847)]
    public void ClilocsParseToNumber(string entered, int expected)
    {
        var td = Parse(entered);

        Assert.NotNull(td);
        Assert.Equal(expected, td.Number);
        Assert.Null(td.String);
    }

    // '#' is what ToString() already emits for a cliloc, so this closes the round trip.
    [Fact]
    public void ClilocMarkerRoundTripsThroughToString()
    {
        var td = TextDefinition.Of(1060847);

        Assert.Equal("#1060847", td.ToString());
        Assert.Equal(td, Parse(td.ToString()));
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData(@"@""0""", "0")]
    [InlineData(@"@""1060847""", "1060847")]
    [InlineData(@"@""null""", "null")]
    [InlineData(@"@""#5""", "#5")]
    public void QuotedLiteralsForceAString(string entered, string expected)
    {
        var td = Parse(entered);

        Assert.NotNull(td);
        Assert.Equal(0, td.Number);
        Assert.Equal(expected, td.String);
    }

    // Bare integers keep meaning "cliloc" so existing spawners and scripts are unaffected.
    [Fact]
    public void BareZeroStaysAnEmptyCliloc()
    {
        var td = Parse("0");

        Assert.NotNull(td);
        Assert.Equal(0, td.Number);
        Assert.Null(td.String);
    }

    // GetValue() fills the props-gump edit box. A string that would read back as something else
    // has to come out quoted or pressing OK silently converts it.
    [Theory]
    [InlineData("1060847")]
    [InlineData("0")]
    [InlineData("#5")]
    public void AmbiguousStringsAreEmittedQuotedSoTheyReadBack(string text)
    {
        var round = Parse(TextDefinition.Of(text).GetValue());

        Assert.NotNull(round);
        Assert.Equal(0, round.Number);
        Assert.Equal(text, round.String);
    }

    [Fact]
    public void UnambiguousValuesAreEmittedPlain()
    {
        Assert.Equal("Hail, traveller.", TextDefinition.Of("Hail, traveller.").GetValue());
        Assert.Equal("1060847", TextDefinition.Of(1060847).GetValue());
    }

    [Fact]
    public void NullSentinelClearsTheValue()
    {
        Assert.Null(Types.TryParse(typeof(TextDefinition), "(-null-)", out var parsed));
        Assert.Null(parsed);
    }
}

// [get emits @"null" for a string whose value is literally "null" so the two can be told apart.
// [set has to decode it or the value you copy out of [get is not the value you can paste back in.
[Collection("Sequential UOContent Tests")]
public class StringEscapeRoundTripTests
{
    [Fact]
    public void QuotedNullSetsTheLiteralStringNull()
    {
        Assert.Null(Types.TryParse(typeof(string), @"@""null""", out var parsed));

        Assert.Equal("null", parsed);
    }

    [Fact]
    public void BareNullSentinelStillClearsTheValue()
    {
        Assert.Null(Types.TryParse(typeof(string), "(-null-)", out var parsed));

        Assert.Null(parsed);
    }

    [Fact]
    public void GetOutputPastesBackIntoSet()
    {
        var item = new Static(0x1F13) { Name = "null" };

        try
        {
            var from = new Mobile { AccessLevel = AccessLevel.Developer };
            var shown = Properties.GetValue(from, item, "Name");

            // "Name = @"null"" -> the value half is what a GM copies.
            var value = shown[(shown.IndexOf('=') + 2)..];
            Assert.Equal(@"@""null""", value);

            item.Name = "changed";
            Properties.SetValue(from, item, "Name", value);

            Assert.Equal("null", item.Name);
        }
        finally
        {
            item.Delete();
        }
    }
}
