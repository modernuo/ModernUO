using System;
using System.Buffers;
using System.IO;
using Server.Network;
using Xunit;

namespace Server.Tests.Buffers
{
    public class CircularBufferWriterTests
    {
        [Theory]
        [InlineData("Test String", "us-ascii", -1, 1024, 1024, 0)]
        [InlineData("Test String", "utf-8", -1, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16BE", -1, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16", -1, 1024, 1024, 0)]
        [InlineData("Test String", "us-ascii", -1, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-8", -1, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16BE", -1, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16", -1, 1024, 1024, 1030)]
        [InlineData("Test String", "us-ascii", -1, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-8", -1, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16BE", -1, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16", -1, 1024, 1024, 1020)]
        [InlineData("Test String", "us-ascii", 8, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16BE", 8, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16", 8, 1024, 1024, 0)]
        [InlineData("Test String", "us-ascii", 8, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16BE", 8, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16", 8, 1024, 1024, 1030)]
        [InlineData("Test String", "us-ascii", 8, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16BE", 8, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16", 8, 1024, 1024, 1020)]
        [InlineData("Test String", "us-ascii", 20, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16BE", 20, 1024, 1024, 0)]
        [InlineData("Test String", "utf-16", 20, 1024, 1024, 0)]
        [InlineData("Test String", "us-ascii", 20, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16BE", 20, 1024, 1024, 1030)]
        [InlineData("Test String", "utf-16", 20, 1024, 1024, 1030)]
        [InlineData("Test String", "us-ascii", 20, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16BE", 20, 1024, 1024, 1020)]
        [InlineData("Test String", "utf-16", 20, 1024, 1024, 1020)]
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

            var encoding = EncodingHelpers.GetEncoding(encodingStr);
            var strLength = fixedLength > -1 ? Math.Min(value.Length, fixedLength) : value.Length;
            var chars = value.AsSpan(0, strLength);

            var writer = new CircularBufferWriter(buffer[..firstSize], buffer[firstSize..]);
            writer.Seek(offset, SeekOrigin.Begin);
            writer.WriteString(chars, encoding);

            if (offset > 0)
            {
                Span<byte> testEmpty = stackalloc byte[offset];
                testEmpty.Clear();
                AssertThat.Equal(buffer[..offset], testEmpty);
            }

            Span<byte> expectedStr = stackalloc byte[encoding.GetByteCount(chars)];
            encoding.GetBytes(chars, expectedStr[..]);

            AssertThat.Equal(buffer.Slice(offset, expectedStr.Length), expectedStr);
            offset += expectedStr.Length;

            if (offset < buffer.Length)
            {
                Span<byte> testEmpty = stackalloc byte[buffer.Length - offset];
                testEmpty.Clear();
                AssertThat.Equal(buffer[offset..], testEmpty);
            }
        }
    }
}
