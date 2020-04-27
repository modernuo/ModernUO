using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class DamageOldPacketTests
  {
    [Fact]
    public void TestPacket()
    {
      const uint s = 100;
      const byte amount = 10;

      DamagePacketOld packet = new DamagePacketOld(s, amount);

      Span<byte> data = packet.Compile(false, out int length).AsSpan(0, length);

      Assert.Equal(data.ToArray(), new byte[]
      {
        0xBF,
        0x00,
        0x0B,
        0x00,
        0x22,
        0x01,
        0x00,
        0x00,
        0x00,
        (byte)s,
        amount,
      });
    }
  }
}
