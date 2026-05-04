using System;
using Server.Buffers;
using Xunit;

namespace Server.Tests.Buffers;

public class RawInterpolatedStringHandlerTests
{
    [Fact]
    public void TestLowercaseFormatString()
    {
        var name = "Hello WORLD";
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted(name, format: "L");
        Assert.Equal("hello world", handler.Text.ToString());
        handler.Clear();
    }

    [Fact]
    public void TestLowercaseFormatEnum()
    {
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted(DayOfWeek.Wednesday, format: "L");
        Assert.Equal("wednesday", handler.Text.ToString());
        handler.Clear();
    }

    [Fact]
    public void TestLowercaseFormatInt()
    {
        // Numerics have no uppercase chars; :L should be a no-op
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted(42, format: "L");
        Assert.Equal("42", handler.Text.ToString());
        handler.Clear();
    }

    [Fact]
    public void TestLowercaseFormatSpan()
    {
        var span = "MIXED Case TEXT".AsSpan();
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted(span, alignment: 0, format: "L");
        Assert.Equal("mixed case text", handler.Text.ToString());
        handler.Clear();
    }

    [Fact]
    public void TestLowercaseFormatWithAlignment()
    {
        // Right-aligned: "Gold" in width 8 with :L -> "    gold"
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted("Gold", alignment: 8, format: "L");
        Assert.Equal("    gold", handler.Text.ToString());
        handler.Clear();
    }

    [Fact]
    public void TestNoFormatPreservesCase()
    {
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted("Hello WORLD");
        Assert.Equal("Hello WORLD", handler.Text.ToString());
        handler.Clear();
    }

    [Fact]
    public void TestUnicodeLowercase()
    {
        // ToLowerInvariant on Greek letter
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted("ΑΒΓ", format: "L");
        Assert.Equal("αβγ", handler.Text.ToString());
        handler.Clear();
    }

    [Fact]
    public void TestLowercaseFormatSurrogatePairAtChunkBoundary()
    {
        // The chunked lowercase path uses a 256-char stackalloc temp buffer. Place a
        // supplementary-plane code point (U+10400 DESERET CAPITAL LONG I, encoded as
        // surrogate pair "𐐀") so its high half lands at offset 255 and its
        // low half at offset 256 — straddling the chunk boundary. Without the
        // surrogate-aware boundary trim, ToLowerInvariant would see two lone surrogates
        // and pass them through unchanged, leaving the capital code point intact.
        // With the trim, the chunk shrinks to 255 chars and the pair stays together
        // in the next chunk, lowercasing correctly to U+10428 ("𐐨").
        var input = new string('a', 255) + "𐐀" + new string('b', 10);
        var handler = new RawInterpolatedStringHandler(0, 1);
        handler.AppendFormatted(input, format: "L");
        var expected = new string('a', 255) + "𐐨" + new string('b', 10);
        Assert.Equal(expected, handler.Text.ToString());
        handler.Clear();
    }
}
