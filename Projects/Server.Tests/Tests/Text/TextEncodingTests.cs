using System;
using System.Buffers;
using System.Text;
using Server.Text;
using Xunit;

namespace Server.Tests.Text;

public class TextEncodingTests
{
    private static readonly Encoding Windows1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252);

    private static bool IsInvalidCp1252Byte(byte b) =>
        b < 0x20 || b is 0x7F or 0x81 or 0x8D or 0x8F or 0x90 or 0x9D;

    private static bool IsInvalidUnicodeChar(char c) => c < 0x20 || c is >= '\x7F' and <= '\x9F' || c >= '￾';

    [Fact]
    public void GetStringCp1252_MatchesWindows1252ForEveryByte()
    {
        for (var b = 0; b < 256; b++)
        {
            byte[] bytes = [(byte)b];
            Assert.Equal(Windows1252.GetString(bytes), TextEncoding.GetStringCp1252(bytes));
        }

        // Every byte in one span, and repeated past the stack buffer size
        var all = new byte[256 * 3];
        for (var i = 0; i < all.Length; i++)
        {
            all[i] = (byte)i;
        }

        Assert.Equal(Windows1252.GetString(all), TextEncoding.GetStringCp1252(all));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(300)]
    public void GetStringCp1252Safe_DropsControlsAndUnassignedKeepsTypographic(int padding)
    {
        var bytes = new byte[256 + padding];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        var expected = new StringBuilder();
        foreach (var b in bytes)
        {
            if (!IsInvalidCp1252Byte(b))
            {
                expected.Append(Windows1252.GetString([b]));
            }
        }

        var result = TextEncoding.GetStringCp1252(bytes, true);

        Assert.Equal(expected.ToString(), result);
        Assert.Contains('…', result);
        Assert.DoesNotContain('\u0081', result);
    }

    [Fact]
    public void GetStringCp1252Safe_TypographicWithoutInvalidBytes()
    {
        // "Isn't it..." with 0x92 (right single quote) and 0x85 (ellipsis)
        byte[] bytes = [(byte)'I', (byte)'s', (byte)'n', 0x92, (byte)'t', (byte)' ', (byte)'i', (byte)'t', 0x85];

        Assert.Equal("Isn’t it…", TextEncoding.GetStringCp1252(bytes, true));
    }

    [Fact]
    public void GetBytesCp1252_RoundTripsEveryAssignedChar()
    {
        Span<byte> dest = stackalloc byte[1];

        for (var b = 0; b < 256; b++)
        {
            if (b is 0x81 or 0x8D or 0x8F or 0x90 or 0x9D)
            {
                continue;
            }

            var str = Windows1252.GetString([(byte)b]);

            Assert.Equal(1, str.GetBytesCp1252(dest));
            Assert.Equal((byte)b, dest[0]);
        }
    }

    [Theory]
    [InlineData("hello", new byte[] { (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o' })]
    [InlineData("café™", new byte[] { (byte)'c', (byte)'a', (byte)'f', 0xE9, 0x99 })]
    [InlineData("a\u0085b", new byte[] { (byte)'a', (byte)'?', (byte)'b' })]
    [InlineData("中", new byte[] { (byte)'?' })]
    [InlineData("x\U0001F600y", new byte[] { (byte)'x', (byte)'?', (byte)'y' })]
    public void GetBytesCp1252_EncodesOneBytePerCodePoint(string value, byte[] expected)
    {
        var dest = new byte[value.Length];
        var written = value.GetBytesCp1252(dest);

        Assert.Equal(expected, dest.AsSpan(0, written).ToArray());
    }

    [Fact]
    public void GetBytesCp1252_LoneSurrogateIsOneQuestionMark()
    {
        // Built at runtime: a lone surrogate does not survive attribute metadata
        var value = new string([(char)0xD83D, 'z']);
        var dest = new byte[value.Length];

        Assert.Equal(2, value.GetBytesCp1252(dest));
        Assert.Equal(new[] { (byte)'?', (byte)'z' }, dest);
    }

    [Fact]
    public void WriteCp1252_FixedLengthTruncatesAndZeroFills()
    {
        var writer = new SpanWriter(stackalloc byte[16]);

        writer.WriteCp1252("ab…", 5);
        writer.WriteCp1252("abcdefgh", 3);
        writer.WriteCp1252("x\U0001F600", 4);

        byte[] expected =
        [
            (byte)'a', (byte)'b', 0x85, 0, 0,
            (byte)'a', (byte)'b', (byte)'c',
            (byte)'x', (byte)'?', 0, 0
        ];

        Assert.Equal(expected, writer.Span.ToArray());
    }

    [Fact]
    public void WriteAndReadCp1252_RoundTrip()
    {
        var writer = new SpanWriter(stackalloc byte[32]);
        writer.WriteCp1252Null("“Hi” €5");

        var reader = new SpanReader(writer.Span);

        Assert.Equal("“Hi” €5", reader.ReadCp1252Safe());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetStringAsciiSafe_FiltersControls(bool longText)
    {
        var text = longText ? new string('a', 300) : "abc";
        var bytes = Encoding.ASCII.GetBytes($"\u0001{text}\u007F{text}\u001F");

        Assert.Equal(text + text, TextEncoding.GetStringAscii(bytes, true));
    }

    // Covers every vector width, the unrolled pair, and the overlapping tail block
    [Fact]
    public void SafeUnicodeDecode_FindsInvalidCharAtEveryPosition()
    {
        char[] invalidChars = ['\0', '\x1F', '\x7F', '\x80', '\x9F', '￾', '￿'];
        char[] validChars = ['a', '~', ' ', '\xA0', 'é', '中', '�', '…'];

        for (var length = 0; length <= 80; length++)
        {
            for (var pos = -1; pos < length; pos++)
            {
                var chars = new char[length];
                for (var i = 0; i < length; i++)
                {
                    chars[i] = validChars[(i + length) % validChars.Length];
                }

                if (pos >= 0)
                {
                    chars[pos] = invalidChars[(pos + length) % invalidChars.Length];
                    // A second invalid char later must not be the one reported
                    if (pos + 3 < length)
                    {
                        chars[pos + 3] = '\x01';
                    }
                }

                var expected = new StringBuilder();
                foreach (var c in chars)
                {
                    if (!IsInvalidUnicodeChar(c))
                    {
                        expected.Append(c);
                    }
                }

                var le = Encoding.Unicode.GetBytes(chars);
                Assert.Equal(expected.ToString(), TextEncoding.GetStringLittleUni(le, true));

                var be = Encoding.BigEndianUnicode.GetBytes(chars);
                Assert.Equal(expected.ToString(), TextEncoding.GetStringBigUni(be, true));
            }
        }
    }

    [Fact]
    public void GetStringUtf8Safe_FiltersControlsAndNonChars()
    {
        var bytes = Encoding.UTF8.GetBytes("a\u0001b\u0085c￿d中e\u007F");

        Assert.Equal("abcd中e", TextEncoding.GetStringUtf8(bytes, true));
        Assert.Equal("café", TextEncoding.GetStringUtf8(Encoding.UTF8.GetBytes("café"), true));
    }

    [Theory]
    [InlineData("utf-8", 1)]
    [InlineData("utf-16", 2)]
    [InlineData("utf-16BE", 2)]
    [InlineData("utf-32", 4)]
    [InlineData("us-ascii", 1)]
    public void GetByteLengthForEncoding_MatchesCodeUnitSize(string name, int expected)
    {
        Assert.Equal(expected, Encoding.GetEncoding(name).GetByteLengthForEncoding());
    }
}
