using System;
using System.IO;
using System.Text;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class CircularBufferReaderTests
    {
        [Theory]
        [InlineData("Test String", "us-ascii", false, -1, 1024, 1024, 0)]
        [InlineData("Test String", "utf-8", false, -1, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16BE", false, -1, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16", false, -1, 1024, 1024, 0)]
        [InlineData("Test String", "us-ascii", false, -1, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-8", false, -1, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16BE", false, -1, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16", false, -1, 1024, 1024, 1030)]
        [InlineData("Test String", "us-ascii", false, -1, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-8", false, -1, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16BE", false, -1, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16", false, -1, 1024, 1024, 1020)]
        [InlineData("Test String", "us-ascii", false, 8, 1024, 1024, 0)]
        [InlineData("Test String", "utf-8", false, 8, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16BE", false, 8, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16", false, 8, 1024, 1024, 0)]
        [InlineData("Test String", "us-ascii", false, 8, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-8", false, 8, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16BE", false, 8, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16", false, 8, 1024, 1024, 1030)]
        [InlineData("Test String", "us-ascii", false, 8, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-8", false, 8, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16BE", false, 8, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16", false, 8, 1024, 1024, 1020)]
        [InlineData("Test String", "us-ascii", false, 20, 1024, 1024, 0)]
        [InlineData("Test String", "utf-8", false, 20, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16BE", false, 20, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16", false, 20, 1024, 1024, 0)]
        [InlineData("Test String", "us-ascii", false, 20, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-8", false, 20, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16BE", false, 20, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16", false, 20, 1024, 1024, 1030)]
        [InlineData("Test String", "us-ascii", false, 20, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-8", false, 20, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16BE", false, 20, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16", false, 20, 1024, 1024, 1020)]
        [InlineData("测试", "utf-8", true, -1, 1024, 1024, 0)]
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

            var encoding = EncodingHelpers.GetEncoding(encodingStr);

            var strLength = fixedLength > -1 ? Math.Min(value.Length, fixedLength) : value.Length;
            var chars = value.AsSpan(0, strLength);

            encoding.GetBytes(chars, buffer[offset..]);

            var reader = new CircularBufferReader(buffer[..firstSize], buffer[firstSize..]);
            reader.Seek(offset, SeekOrigin.Begin);

            var actual = reader.ReadString(encoding, isSafe, fixedLength);

            Assert.Equal(value[..strLength], actual);
        }

        [Fact]
        public void TestReadStringBetween()
        {
            Span<byte> expected = stackalloc byte[19];
            expected[0] = 0x1;
            expected[1] = 0x1;
            expected[2] = 0x1;
            expected[3] = 0x1;
            Encoding.ASCII.GetBytes("TestString", expected[4..14]);
            expected[14] = 0x0; // Null
            expected[15] = 0x2;
            expected[16] = 0x2;
            expected[17] = 0x2;
            expected[18] = 0x2;

            var reader = new CircularBufferReader(expected, stackalloc byte[0]);

            var num1 = reader.ReadInt32();
            var str = reader.ReadAscii();
            var num2 = reader.ReadInt32();

            Assert.Equal(0x01010101, num1);
            Assert.Equal("TestString", str);
            Assert.Equal(0x02020202, num2);
        }
    }
}
