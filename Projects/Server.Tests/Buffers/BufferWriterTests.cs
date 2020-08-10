using System;
using Xunit;

namespace Server.Tests.Buffers
{
  public class BufferWriterTests
  {
    [Fact]
    public void TestWriteAsciiFixed()
    {
      Span<byte> first = stackalloc byte[100];
      Span<byte> second = stackalloc byte[100];
    }
  }
}
