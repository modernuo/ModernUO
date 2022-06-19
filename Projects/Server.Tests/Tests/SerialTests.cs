using System;
using Xunit;

namespace Server.Tests;

public class SerialTests
{
    [Fact]
    public void TestSerialTryFormatDefault()
    {
        var serial = (Serial)0xABCD1234u;
        const string serialStr = "0xABCD1234";
        Span<char> buffer = stackalloc char[serialStr.Length];
        var result = serial.TryFormat(buffer, out var charsWritten, null, null);
        Assert.True(result);
        Assert.Equal(serialStr.Length, charsWritten);
        Assert.Equal(serialStr, buffer.ToString());

        var interpolated = $"{serial}";
        Assert.Equal(serialStr, interpolated);
    }

    [Fact]
    public void TestSerialTryFormatCustom()
    {
        var serial = (Serial)0xABCD1234u;
        const string serialStr = "2882343476";
        Span<char> buffer = stackalloc char[serialStr.Length];
        var result = serial.TryFormat(buffer, out var charsWritten, "##", null);
        Assert.True(result);
        Assert.Equal(serialStr.Length, charsWritten);
        Assert.Equal(serialStr, buffer.ToString());

        var interpolated = $"{serial:##}";
        Assert.Equal(serialStr, interpolated);
    }
}
