using System;
using Xunit;

namespace Server.Tests;

public class TextDefinitionTests
{
    [Theory]
    [InlineData(100100, "#100100")]
    [InlineData("Hello", "Hello")]
    public void TestTextDefinitionToString(TextDefinition actual, string expected)
    {
        Assert.Equal(expected, actual.ToString());
    }

    [Theory]
    [InlineData(10345, "", 10345, "", true)]
    [InlineData(10345, "", 0, "Hello", false)]
    [InlineData(0, "Hello", 0, "Hello", true)]
    [InlineData(0, "Hello", 0, "hello", false)]
    [InlineData(10345, "Hello", 10345, "Goodbye", true)]
    [InlineData(10345, "Hello", 10510, "Hello", false)]
    public void TestTextDefinitionIsEqual(int a1, string a2, int b1, string b2, bool isEqual)
    {
        var a = TextDefinition.Of(a1, a2);
        var b = TextDefinition.Of(b1, b2);
        Assert.Equal(isEqual, a.Equals(b));
    }
}
