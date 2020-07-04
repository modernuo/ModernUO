using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class MobilePacketTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestDeathAnimation()
    {
      Serial killed = 0x1;
      Serial corpse = 0x1000;

      Span<byte> data = new DeathAnimation(killed, corpse).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xAF, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial of killed
        0x00, 0x00, 0x00, 0x00, // Serial of corpse
        0x00, 0x00, 0x00, 0x00
      };

      killed.CopyTo(expectedData.Slice(1, 4));
      corpse.CopyTo(expectedData.Slice(5, 4));

      AssertThat.Equal(data, expectedData);
    }
  }
}
