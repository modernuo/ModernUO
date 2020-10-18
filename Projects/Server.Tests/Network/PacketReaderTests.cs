using System;
using System.IO;
using System.Text;
using Server.Network;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Network
{
    public class PacketReaderTests
    {
        private (Encoding, Type) GetEncoding(string value) =>
            value.ToUpper() switch
            {
                "UTF8"      => (Utility.UTF8, typeof(byte)),
                "UNICODELE" => (Utility.UnicodeLE, typeof(char)),
                "UNICODE"   => (Utility.Unicode, typeof(char)),
                _           => (Encoding.ASCII, typeof(byte))
            };

        private ITestOutputHelper _outputHelper;

        public PacketReaderTests(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;

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
            var (encoding, byteType) = GetEncoding(encodingStr);
            var bytes = encoding.GetBytes(value);
            Span<byte> expectedBytes = bytes.AsSpan(0, fixedLength > -1 ? Math.Min(fixedLength, bytes.Length) : bytes.Length);

            if (offset + expectedBytes.Length > firstSize + secondSize)
            {
                throw new ArgumentException("Test failed due to bad assumptions. Out of memory exception would be thrown");
            }

            Span<byte> first = stackalloc byte[firstSize];
            first.Clear(); // Just in case

            int bytesWrittenFirst = 0;

            if (offset < first.Length)
            {
                bytesWrittenFirst = Math.Min(first.Length, expectedBytes.Length);
                expectedBytes.Slice(offset, bytesWrittenFirst).CopyTo(first);
            }

            Span<byte> second = stackalloc byte[secondSize];
            second.Clear(); // Just in case

            if (offset > first.Length || expectedBytes.Length > bytesWrittenFirst)
            {
                var secondOffset = Math.Max(offset - first.Length, 0);
                var secondSlice = second.Slice(secondOffset, second.Length - secondOffset);
                expectedBytes.Slice(bytesWrittenFirst, expectedBytes.Length - bytesWrittenFirst).CopyTo(secondSlice);
            }

            // _outputHelper.WriteLine(HexStringConverter.GetString(first));
            // _outputHelper.WriteLine(HexStringConverter.GetString(second));

            var reader = new PacketReader(first, second);
            reader.Seek(offset, SeekOrigin.Begin);

            _outputHelper.WriteLine(reader.Position.ToString());

            var actual = byteType switch
            {
                var b when b == typeof(char) => reader.ReadString<char>(encoding, isSafe, fixedLength),
                _                                  => reader.ReadString<byte>(encoding, isSafe, fixedLength)
            };

            _outputHelper.WriteLine(actual);

            if (fixedLength > 0 && fixedLength < value.Length)
            {
                value = value.Substring(0, fixedLength);
            }

            Assert.Equal(value, actual);
        }
    }
}
