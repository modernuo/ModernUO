using System;
using System.Buffers.Binary;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class ArrowPacketTests
  {
    [Fact]
    public void TestCancelArrow()
    {
      Span<byte> data = new CancelArrow().Compile();

      Span<byte> expectedData = stackalloc byte[] {
        0xBA, // Packet
        0x00, // Command
        0xFF, 0xFF, // X
        0xFF, 0xFF // Y
      };

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 10)]
    [InlineData(100000, 100000)]
    public void TestSetArrow(int x, int y)
    {
      Span<byte> data = new SetArrow(x, y).Compile();

      Span<byte> expectedXBytes = stackalloc byte[2];
      BinaryPrimitives.WriteUInt16BigEndian(expectedXBytes, (ushort)x);

      Span<byte> expectedYBytes = stackalloc byte[2];
      BinaryPrimitives.WriteUInt16BigEndian(expectedYBytes, (ushort)y);

      Span<byte> expectedData = stackalloc byte[] {
        0xBA, // Packet
        0x01, // Command
        expectedXBytes[0], expectedXBytes[1], // X
        expectedYBytes[0], expectedYBytes[1] // Y
      };

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 10)]
    [InlineData(100000, 100000)]
    public void TestCancelArrowHS(int x, int y)
    {
      Span<byte> data = new CancelArrowHS(x, y, 0x1).Compile();

      Span<byte> expectedXBytes = stackalloc byte[2];
      BinaryPrimitives.WriteUInt16BigEndian(expectedXBytes, (ushort)x);

      Span<byte> expectedYBytes = stackalloc byte[2];
      BinaryPrimitives.WriteUInt16BigEndian(expectedYBytes, (ushort)y);

      Span<byte> expectedData = stackalloc byte[] {
        0xBA, // Packet
        0x00, // Command
        expectedXBytes[0], expectedXBytes[1], // X
        expectedYBytes[0], expectedYBytes[1], // Y
        0x00, 0x00, 0x00, 0x01 // Serial
      };

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 10)]
    [InlineData(100000, 100000)]
    public void TestSetArrowHS(int x, int y)
    {
      Span<byte> data = new SetArrowHS(x, y, 0x1).Compile();

      Span<byte> expectedXBytes = stackalloc byte[2];
      BinaryPrimitives.WriteUInt16BigEndian(expectedXBytes, (ushort)x);

      Span<byte> expectedYBytes = stackalloc byte[2];
      BinaryPrimitives.WriteUInt16BigEndian(expectedYBytes, (ushort)y);

      Span<byte> expectedData = stackalloc byte[] {
        0xBA, // Packet
        0x01, // Command
        expectedXBytes[0], expectedXBytes[1], // X
        expectedYBytes[0], expectedYBytes[1], // Y
        0x00, 0x00, 0x00, 0x01 // Serial
      };

      AssertThat.Equal(data, expectedData);
    }
  }
}
