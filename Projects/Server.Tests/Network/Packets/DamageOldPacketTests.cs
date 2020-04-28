using System;
using System.Buffers.Binary;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class DamageOldPacketTests : IClassFixture<ServerFixture>
  {
    private Mobile mobile;

    public DamageOldPacketTests(ServerFixture fixture) => mobile = fixture.mobile;

    [Fact]
    public void TestDamagePacketOld()
    {
      DamagePacketOld packet = new DamagePacketOld(mobile, 10);

      Span<byte> data = packet.Compile(false, out int length).AsSpan(0, length);

      byte[] expected = {
        0xBF, // Packet
        0x00, 0x0B, // Length
        0x00, 0x22, // Sub-packet
        0x01, // Command
        0x00, 0x00, 0x00, 0x01, // Serial
        0x0A, // Amount
      };

      Assert.Equal(data.ToArray(), expected);
    }

    [Fact]
    public void TestDamagePacketOldBelowZero()
    {
      DamagePacketOld packet = new DamagePacketOld(mobile, -1);

      Span<byte> data = packet.Compile(false, out int length).AsSpan(0, length);

      byte[] expected = {
        0xBF, // Packet
        0x00, 0x0B, // Length
        0x00, 0x22, // Sub-packet
        0x01, // Command
        0x00, 0x00, 0x00, 0x01, // Serial
        0x00, // Amount
      };

      Assert.Equal(data.ToArray(), expected);
    }

    [Fact]
    public void TestDamagePacketOldAboveOneByte()
    {
      DamagePacketOld packet = new DamagePacketOld(mobile, 1024);

      Span<byte> data = packet.Compile(false, out int length).AsSpan(0, length);

      byte[] expected = {
        0xBF, // Packet
        0x00, 0x0B, // Length
        0x00, 0x22, // Sub-packet
        0x01, // Command
        0x00, 0x00, 0x00, 0x01, // Serial
        0xFF // Amount
      };

      Assert.Equal(data.ToArray(), expected);
    }
  }
}
