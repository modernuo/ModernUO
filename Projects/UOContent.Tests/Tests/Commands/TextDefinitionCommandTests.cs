using Server;
using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands;

// Commands.Split strips quotes before any parser runs, so "0" and 0 arrive identical -- the
// markers are the only way to say which was meant. See dev-docs/generic-commands.md.
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

    // ToString() already emits '#', so this closes the round trip.
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

    // Bare integers stay clilocs, so existing content is unaffected.
    [Fact]
    public void BareZeroStaysAnEmptyCliloc()
    {
        var td = Parse("0");

        Assert.NotNull(td);
        Assert.Equal(0, td.Number);
        Assert.Null(td.String);
    }

    // GetValue() fills the props-gump edit box; an ambiguous string must come out quoted.
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

// [get emits @"null" for the literal string "null"; [set has to decode it to round trip.
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
