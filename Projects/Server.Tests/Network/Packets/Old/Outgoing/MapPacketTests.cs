using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class MapPatchesTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestMapPatches()
    {
      Span<byte> data = new MapPatches().Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBF, // Packet ID
        0x00, 0x29, // Length
        0x00, 0x18, // Sub-packet
        0x00, 0x00, 0x00, 0x04, // 4 maps
        0x00, 0x00, 0x00, 0x00, // Felucca
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, // Trammel
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, // Ilshenar
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, // Malas
        0x00, 0x00, 0x00, 0x00
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestInvalidMapEnable()
    {
      Span<byte> data = new InvalidMapEnable().Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xC6 // Packet ID
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMapChange()
    {
      Span<byte> data = new MapChange(Map.Felucca).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBF, // Packet ID
        0x00, 0x06, // Length
        0x00, 0x08, // Sub-packet
        0x00, // Felucca
      };

      AssertThat.Equal(data, expectedData);
    }
  }
}
