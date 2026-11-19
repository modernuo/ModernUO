using System;
using System.Linq;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Tests for the Html.EscapeHtml extension methods.
/// These tests verify correct HTML entity escaping and edge cases.
/// </summary>
[Collection("Sequential Server Tests")]
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

    [Fact(DisplayName = "Consecutive special characters")]
    public void EscapeHtml_ConsecutiveSpecialCharacters_EscapesAll()
    {
        const string input = "<<>>&&\"\"''";
        const string expected = "&lt;&lt;&gt;&gt;&amp;&amp;&quot;&quot;&#39;&#39;";
        var result = input.EscapeHtml();
        Assert.Equal(expected, result);
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
}
