using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class DamageOldPacketTests : IClassFixture<ServerFixture>
  {
    private Mobile mobile;

    public DamageOldPacketTests(ServerFixture fixture) => mobile = fixture.mobile;

    [Theory]
    [InlineData(10, 10)]
    [InlineData(-5, 0)]
    [InlineData(1024, 255)]
    public void TestDamagePacketOld(int inputAmount, byte expectedAmount)
    {
      DamagePacketOld packet = new DamagePacketOld(mobile, inputAmount);

      Span<byte> data = packet.Compile(false, out int length).AsSpan(0, length);

      byte[] expectedData = {
        0xBF, // Packet
        0x00, 0x0B, // Length
        0x00, 0x22, // Sub-packet
        0x01, // Command
        0x00, 0x00, 0x00, 0x01, // Serial
        expectedAmount // Amount
      };

      Assert.Equal(data.ToArray(), expectedData);
    }
  }
}
