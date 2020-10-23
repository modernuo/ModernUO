using System;
using System.IO;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class CircularBufferReaderTests
    {
        [Theory]
        // First only, beginning
        [InlineData("Test String", "ASCII", false, -1, 1024, 1024, 0)]
        [InlineData("Test String", "UTF8", false, -1, 1024, 1024, 0)]
        [InlineData("Test String", "Unicode", false, -1, 1024, 1024, 0)]
        [InlineData("Test String", "UnicodeLE", false, -1, 1024, 1024, 0)]

        // Second only
        [InlineData("Test String", "ASCII", false, -1, 1024, 1024, 1030)]
        [InlineData("Test String", "UTF8", false, -1, 1024, 1024, 1030)]
        [InlineData("Test String", "Unicode", false, -1, 1024, 1024, 1030)]
        [InlineData("Test String", "UnicodeLE", false, -1, 1024, 1024, 1030)]

        // Split
        [InlineData("Test String", "ASCII", false, -1, 1024, 1024, 1020)]
        [InlineData("Test String", "UTF8", false, -1, 1024, 1024, 1020)]
        [InlineData("Test String", "Unicode", false, -1, 1024, 1024, 1020)]
        [InlineData("Test String", "UnicodeLE", false, -1, 1024, 1024, 1020)]

        // First only, beginning, fixed length smaller
        [InlineData("Test String", "ASCII", false, 8, 1024, 1024, 0)]
        [InlineData("Test String", "UTF8", false, 8, 1024, 1024, 0)]
        [InlineData("Test String", "Unicode", false, 8, 1024, 1024, 0)]
        [InlineData("Test String", "UnicodeLE", false, 8, 1024, 1024, 0)]

        // Second only, fixed length smaller
        [InlineData("Test String", "ASCII", false, 8, 1024, 1024, 1030)]
        [InlineData("Test String", "UTF8", false, 8, 1024, 1024, 1030)]
        [InlineData("Test String", "Unicode", false, 8, 1024, 1024, 1030)]
        [InlineData("Test String", "UnicodeLE", false, 8, 1024, 1024, 1030)]

        // Split, fixed length smaller
        [InlineData("Test String", "ASCII", false, 8, 1024, 1024, 1020)]
        [InlineData("Test String", "UTF8", false, 8, 1024, 1024, 1020)]
        [InlineData("Test String", "Unicode", false, 8, 1024, 1024, 1020)]
        [InlineData("Test String", "UnicodeLE", false, 8, 1024, 1024, 1020)]

        // First only, beginning, fixed length bigger
        [InlineData("Test String", "ASCII", false, 20, 1024, 1024, 0)]
        [InlineData("Test String", "UTF8", false, 20, 1024, 1024, 0)]
        [InlineData("Test String", "Unicode", false, 20, 1024, 1024, 0)]
        [InlineData("Test String", "UnicodeLE", false, 20, 1024, 1024, 0)]

        // Second only, fixed length bigger
        [InlineData("Test String", "ASCII", false, 20, 1024, 1024, 1030)]
        [InlineData("Test String", "UTF8", false, 20, 1024, 1024, 1030)]
        [InlineData("Test String", "Unicode", false, 20, 1024, 1024, 1030)]
        [InlineData("Test String", "UnicodeLE", false, 20, 1024, 1024, 1030)]

        // Split, fixed length bigger
        [InlineData("Test String", "ASCII", false, 20, 1024, 1024, 1020)]
        [InlineData("Test String", "UTF8", false, 20, 1024, 1024, 1020)]
        [InlineData("Test String", "Unicode", false, 20, 1024, 1024, 1020)]
        [InlineData("Test String", "UnicodeLE", false, 20, 1024, 1024, 1020)]
        public void TestReadString(
            string value,
            string encodingStr,
            bool isSafe,
            int fixedLength,
            int firstSize,
            int secondSize,
            int offset
        )
        {
            Span<byte> buffer = stackalloc byte[firstSize + secondSize];
            buffer.Clear();

            var (encoding, byteType) = EncodingHelpers.GetEncoding(encodingStr);

            var strLength = fixedLength > -1 ? Math.Min(value.Length, fixedLength) : value.Length;
            var chars = value.AsSpan(0, strLength);
;
            encoding.GetBytes(chars, buffer.Slice(offset));

            var reader = new CircularBufferReader(buffer.Slice(0, firstSize), buffer.Slice(firstSize));
            reader.Seek(offset, SeekOrigin.Begin);

            var actual = byteType switch
            {
                var b when b == typeof(char) => reader.ReadString<char>(encoding, isSafe, fixedLength),
                _                                  => reader.ReadString<byte>(encoding, isSafe, fixedLength)
            };

            Assert.Equal(value.Substring(0, strLength), actual);
        }
    }
}
