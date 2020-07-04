using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class LightPacketTests
  {
    [Fact]
    public void TestGlobalLightLevel()
    {
      byte lightLevel = 5;
      Span<byte> data = new GlobalLightLevel(lightLevel).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x4F, // Packet ID
        lightLevel
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestPersonalLightLevel()
    {
      Serial serial = 0x1;
      byte lightLevel = 5;
      Span<byte> data = new PersonalLightLevel(serial, lightLevel).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x4E, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        lightLevel
      };

      serial.CopyTo(expectedData.Slice(1, 4));

      AssertThat.Equal(data, expectedData);
    }
  }
}
