using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class DamagePacketTests : IClassFixture<ServerFixture>
  {
    private readonly Mobile mobile;

    public DamagePacketTests(ServerFixture fixture) => mobile = fixture.fromMobile;

    [Theory]
    [InlineData(10, 10)]
    [InlineData(-5, 0)]
    [InlineData(1024, 0xFF)]
    public void TestDamagePacketOld(int inputAmount, byte expectedAmount)
    {
      Span<byte> data = new DamagePacketOld(mobile, inputAmount).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBF, // Packet
        0x00, 0x0B, // Length
        0x00, 0x22, // Sub-packet
        0x01, // Command
        0x00, 0x00, 0x00, 0x00, // Mobile Serial
        expectedAmount // Amount
      };

      mobile.Serial.CopyTo(expectedData.Slice(6, 4));
      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(-5, 0)]
    [InlineData(1024, 1024)]
    [InlineData(100000, 0xFFFF)]
    public void TestDamagePacket(int inputAmount, ushort expectedAmount)
    {
      Span<byte> data = new DamagePacket(mobile, inputAmount).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x0B, // Packet
        0x00, 0x00, 0x00, 0x00, // Mobile Serial
        0x00, 0x00 // Amount
      };

      mobile.Serial.CopyTo(expectedData.Slice(1, 4));
      expectedAmount.CopyTo(expectedData.Slice(5, 2));

      AssertThat.Equal(data, expectedData);
    }
  }
}
