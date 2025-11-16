using System;
using System.Linq;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Tests for the Html.EscapeHtml extension methods.
/// These tests verify correct HTML entity escaping and edge cases.
/// </summary>
public class HtmlEscapeTests
{
    [Theory(DisplayName = "No escaping needed")]
    [InlineData("")]
    [InlineData("Hello World")]
    [InlineData("Plain text without special characters")]
    [InlineData("123456789")]
    [InlineData("!@#$%^*()_+-=[]{}|;:,.?")]
    public void EscapeHtml_NoSpecialCharacters_ReturnsUnchanged(string input)
    {
        var result = input.EscapeHtml();
        Assert.Equal(input, result);
    }

    [Theory(DisplayName = "Single character escaping")]
    [InlineData("<", "&lt;")]
    [InlineData(">", "&gt;")]
    [InlineData("&", "&amp;")]
    [InlineData("\"", "&quot;")]
    [InlineData("'", "&#39;")]
    public void EscapeHtml_SingleSpecialCharacter_EscapesCorrectly(string input, string expected)
    {
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Multiple instances of same character")]
    [InlineData("<><>", "&lt;&gt;&lt;&gt;")]
    [InlineData("&&&&", "&amp;&amp;&amp;&amp;")]
    [InlineData("\"\"\"", "&quot;&quot;&quot;")]
    [InlineData("'''", "&#39;&#39;&#39;")]
    [InlineData(">>>", "&gt;&gt;&gt;")]
    public void EscapeHtml_MultipleSpecialCharacters_EscapesAll(string input, string expected)
    {
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Mixed content")]
    [InlineData("Hello <world>", "Hello &lt;world&gt;")]
    [InlineData("<div>Hello</div>", "&lt;div&gt;Hello&lt;/div&gt;")]
    [InlineData("Tom & Jerry", "Tom &amp; Jerry")]
    [InlineData("He said \"hello\"", "He said &quot;hello&quot;")]
    [InlineData("It's a test", "It&#39;s a test")]
    public void EscapeHtml_MixedContent_EscapesMixedSpecialCharacters(string input, string expected)
    {
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Starting with special character")]
    [InlineData("<Hello", "&lt;Hello")]
    [InlineData(">World", "&gt;World")]
    [InlineData("&Start", "&amp;Start")]
    [InlineData("\"Quote", "&quot;Quote")]
    [InlineData("'Apostrophe", "&#39;Apostrophe")]
    public void EscapeHtml_StartsWithSpecialCharacter_EscapesStart(string input, string expected)
    {
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Ending with special character")]
    [InlineData("Hello<", "Hello&lt;")]
    [InlineData("World>", "World&gt;")]
    [InlineData("End&", "End&amp;")]
    [InlineData("Quote\"", "Quote&quot;")]
    [InlineData("Test'", "Test&#39;")]
    public void EscapeHtml_EndsWithSpecialCharacter_EscapesEnd(string input, string expected)
    {
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Complex mixed scenarios")]
    [InlineData("<p>Hello & goodbye</p>", "&lt;p&gt;Hello &amp; goodbye&lt;/p&gt;")]
    [InlineData("&lt;already&gt;", "&amp;lt;already&amp;gt;")]
    [InlineData("<tag attr=\"value\" data='test'>", "&lt;tag attr=&quot;value&quot; data=&#39;test&#39;&gt;")]
    [InlineData("a<b>c&d\"e'f", "a&lt;b&gt;c&amp;d&quot;e&#39;f")]
    [InlineData("&nbsp;", "&amp;nbsp;")]
    public void EscapeHtml_ComplexScenarios_EscapesAllSpecialCharacters(string input, string expected)
    {
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Null string input")]
    public void EscapeHtml_NullString_ReturnsEmpty()
    {
        string? input = null;
        var result = input.EscapeHtml();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "Empty string input")]
    public void EscapeHtml_EmptyString_ReturnsEmpty()
    {
        var result = "".EscapeHtml();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "Only special characters")]
    public void EscapeHtml_OnlySpecialCharacters_EscapesAll()
    {
        const string input = "<>&\"'";
        const string expected = "&lt;&gt;&amp;&quot;&#39;";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Ampersand must be escaped first")]
    public void EscapeHtml_AmpersandFirst_PreventDoubleEscaping()
    {
        // This is critical: & must be escaped to &amp;
        // If we're not careful, we could double-escape already-escaped content
        const string input = "&lt;";
        const string expected = "&amp;lt;";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "ReadOnlySpan overload - no special characters")]
    [InlineData("Hello World")]
    [InlineData("Plain text")]
    public void EscapeHtml_ReadOnlySpan_NoSpecialCharacters_ReturnsUnchanged(string input)
    {
        var result = input.AsSpan().EscapeHtml();
        Assert.Equal(input, result);
    }

    [Theory(DisplayName = "ReadOnlySpan overload - with special characters")]
    [InlineData("<div>", "&lt;div&gt;")]
    [InlineData("Tom & Jerry", "Tom &amp; Jerry")]
    public void EscapeHtml_ReadOnlySpan_WithSpecialCharacters_EscapesCorrectly(string input, string expected)
    {
        var result = input.AsSpan().EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "ReadOnlySpan overload - empty input")]
    [InlineData("")]
    public void EscapeHtml_ReadOnlySpan_Empty_ReturnsEmpty(string input)
    {
        var result = input.AsSpan().EscapeHtml();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "Consecutive special characters")]
    public void EscapeHtml_ConsecutiveSpecialCharacters_EscapesAll()
    {
        const string input = "<<>>&&\"\"''";
        const string expected = "&lt;&lt;&gt;&gt;&amp;&amp;&quot;&quot;&#39;&#39;";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Special characters with single normal character between")]
    public void EscapeHtml_SpecialCharactersWithGaps_EscapesAll()
    {
        const string input = "<a>b&c\"d'e";
        const string expected = "&lt;a&gt;b&amp;c&quot;d&#39;e";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "HTML tags")]
    public void EscapeHtml_HtmlTags_EscapesTagBrackets()
    {
        var input = "<html><body>Hello</body></html>";
        var expected = "&lt;html&gt;&lt;body&gt;Hello&lt;/body&gt;&lt;/html&gt;";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "HTML attributes with mixed quotes")]
    public void EscapeHtml_HtmlAttributesWithQuotes_EscapesCorrectly()
    {
        var input = "<a href=\"test\" data='value'>";
        var expected = "&lt;a href=&quot;test&quot; data=&#39;value&#39;&gt;";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Whitespace handling")]
    [InlineData("  spaces  ", "  spaces  ")]
    [InlineData("\ttabs\t", "\ttabs\t")]
    [InlineData("\nnewlines\n", "\nnewlines\n")]
    public void EscapeHtml_Whitespace_PreservedAsIs(string input, string expected)
    {
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Performance: long string without special characters")]
    public void EscapeHtml_LongStringNoSpecialCharacters_ReturnsQuickly()
    {
        var input = new string('a', 10000);
        var result = input.EscapeHtml();
        Assert.Equal(input, result);
    }

    [Fact(DisplayName = "Performance: long string with special characters")]
    public void EscapeHtml_LongStringWithSpecialCharacters_HandlesCorrectly()
    {
        var input = $"Start{new string('<', 100)}End{new string('&', 100)}Final";
        var expected =
            $"Start{string.Join("", Enumerable.Repeat("&lt;", 100))}End{string.Join("", Enumerable.Repeat("&amp;", 100))}Final";

        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Unicode characters")]
    public void EscapeHtml_UnicodeCharacters_PreservedWithSpecialCharsEscaped()
    {
        const string input = "Hello 世界 <test> & 🎉";
        const string expected = "Hello 世界 &lt;test&gt; &amp; 🎉";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "String overload matches ReadOnlySpan overload")]
    public void EscapeHtml_StringVsReadOnlySpan_ProduceSameResult()
    {
        const string input = "<div>Tom & Jerry 'in' \"quotes\"</div>";

        var resultString = input.EscapeHtml();
        var resultSpan = input.AsSpan().EscapeHtml();

        Assert.Equal(resultString, resultSpan);
    }
}
