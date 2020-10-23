using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace Server.Tests.Buffers
{
    public class CircularBufferWriterTests
    {
        [Theory]
        // First only, beginning
        [InlineData("Test String", "ASCII", -1, 1024, 1024, 0)]
        [InlineData("Test String", "UTF8", -1, 1024, 1024, 0)]
        [InlineData("Test String", "Unicode", -1, 1024, 1024, 0)]
        [InlineData("Test String", "UnicodeLE", -1, 1024, 1024, 0)]

        // Second only
        [InlineData("Test String", "ASCII", -1, 1024, 1024, 1030)]
        [InlineData("Test String", "UTF8", -1, 1024, 1024, 1030)]
        [InlineData("Test String", "Unicode", -1, 1024, 1024, 1030)]
        [InlineData("Test String", "UnicodeLE", -1, 1024, 1024, 1030)]

        // Split
        [InlineData("Test String", "ASCII", -1, 1024, 1024, 1020)]
        [InlineData("Test String", "UTF8", -1, 1024, 1024, 1020)]
        [InlineData("Test String", "Unicode", -1, 1024, 1024, 1020)]
        [InlineData("Test String", "UnicodeLE", -1, 1024, 1024, 1020)]

        // First only, beginning, fixed length smaller
        [InlineData("Test String", "ASCII", 8, 1024, 1024, 0)]
        [InlineData("Test String", "UTF8", 8, 1024, 1024, 0)]
        [InlineData("Test String", "Unicode", 8, 1024, 1024, 0)]
        [InlineData("Test String", "UnicodeLE", 8, 1024, 1024, 0)]

        // Second only, fixed length smaller
        [InlineData("Test String", "ASCII", 8, 1024, 1024, 1030)]
        [InlineData("Test String", "UTF8", 8, 1024, 1024, 1030)]
        [InlineData("Test String", "Unicode", 8, 1024, 1024, 1030)]
        [InlineData("Test String", "UnicodeLE", 8, 1024, 1024, 1030)]

        // Split, fixed length smaller
        [InlineData("Test String", "ASCII", 8, 1024, 1024, 1020)]
        [InlineData("Test String", "UTF8", 8, 1024, 1024, 1020)]
        [InlineData("Test String", "Unicode", 8, 1024, 1024, 1020)]
        [InlineData("Test String", "UnicodeLE", 8, 1024, 1024, 1020)]

        // First only, beginning, fixed length bigger
        [InlineData("Test String", "ASCII", 20, 1024, 1024, 0)]
        [InlineData("Test String", "UTF8", 20, 1024, 1024, 0)]
        [InlineData("Test String", "Unicode", 20, 1024, 1024, 0)]
        [InlineData("Test String", "UnicodeLE", 20, 1024, 1024, 0)]

        // Second only, fixed length bigger
        [InlineData("Test String", "ASCII", 20, 1024, 1024, 1030)]
        [InlineData("Test String", "UTF8", 20, 1024, 1024, 1030)]
        [InlineData("Test String", "Unicode", 20, 1024, 1024, 1030)]
        [InlineData("Test String", "UnicodeLE", 20, 1024, 1024, 1030)]

        // Split, fixed length bigger
        [InlineData("Test String", "ASCII", 20, 1024, 1024, 1020)]
        [InlineData("Test String", "UTF8", 20, 1024, 1024, 1020)]
        [InlineData("Test String", "Unicode", 20, 1024, 1024, 1020)]
        [InlineData("Test String", "UnicodeLE", 20, 1024, 1024, 1020)]
        public void TestWriteString(
            string value,
            string encodingStr,
            int fixedLength,
            int firstSize,
            int secondSize,
            int offset
        )
        {
            Span<byte> buffer = stackalloc byte[firstSize + secondSize];
            buffer.Clear();

            var (encoding, byteType) = EncodingHelpers.GetEncoding(encodingStr);

            var writer = new CircularBufferWriter(buffer.Slice(0, firstSize), buffer.Slice(firstSize));
            writer.Seek(offset, SeekOrigin.Begin);

            if (byteType == typeof(char))
            {
                writer.WriteString<char>(value, encoding, fixedLength);
            }
            else
            {
                writer.WriteString<byte>(value, encoding, fixedLength);
            }

            if (offset > 0)
            {
                Span<byte> testEmpty = stackalloc byte[offset];
                testEmpty.Clear();
                AssertThat.Equal(buffer.Slice(0, offset), testEmpty);
            }

            var strLength = fixedLength > -1 ? Math.Min(value.Length, fixedLength) : value.Length;
            var chars = value.AsSpan(0, strLength);

            Span<byte> expectedStr = stackalloc byte[encoding.GetByteCount(chars)];
            encoding.GetBytes(chars, expectedStr.Slice(0));

            AssertThat.Equal(buffer.Slice(offset, expectedStr.Length), expectedStr);
            offset += expectedStr.Length;

            if (offset < buffer.Length)
            {
                Span<byte> testEmpty = stackalloc byte[buffer.Length - offset];
                testEmpty.Clear();
                AssertThat.Equal(buffer.Slice(offset), testEmpty);
            }
        }
    }
}
