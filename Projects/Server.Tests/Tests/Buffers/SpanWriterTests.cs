using System;
using System.Buffers;
using Xunit;

namespace Server.Tests
{
    public class SpanWriterTests
    {
        [Fact]
        public unsafe void TestSpanWriterResizes()
        {
            Span<byte> smallStack = stackalloc byte[8];
            using var writer = new SpanWriter(smallStack, true);
            writer.Write(0x1024L);
            writer.Write(0x1024L);

            var span = writer.RawBuffer;
            fixed (byte* spanPtr = span)
            {
                fixed (byte* stackPtr = smallStack)
                {
                    Assert.True(spanPtr != stackPtr);
                }
            }

            Assert.True(span.Length > smallStack.Length);
        }

        [Fact]
        public unsafe void TestSpanWriterOnlyStackAlloc()
        {
            Span<byte> smallStack = stackalloc byte[8];
            using var writer = new SpanWriter(smallStack, true);
            writer.Write(0x1024L);

            var span = writer.RawBuffer;
            fixed (byte* spanPtr = span)
            {
                fixed (byte* stackPtr = smallStack)
                {
                    Assert.True(spanPtr == stackPtr);
                }
            }

            Assert.True(span.Length == smallStack.Length);
            AssertThat.Equal(smallStack, stackalloc byte[] { 0, 0, 0, 0, 0, 0, 0x10, 0x24 });
        }

        [Fact]
        public void TestSpanWriterNoResizeThrows()
        {
            Assert.Throws<OutOfMemoryException>(
                () =>
                {
                    Span<byte> smallStack = stackalloc byte[8];
                    using var writer = new SpanWriter(smallStack);
                    writer.Write(0x1024L);
                    writer.Write(0x1024L);
                }
            );
        }
    }
}
