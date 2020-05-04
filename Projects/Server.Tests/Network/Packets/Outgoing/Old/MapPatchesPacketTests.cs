using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class MapPatchesTests
  {
    [Fact]
    public void TestMapPatches()
    {
      Span<byte> data = new MapPatches().Compile();

      Span<byte> expectedData = stackalloc byte[] {
        0xBF, // Packet
        0x00, 0x39, // Length
        0x00, 0x18, // Sub-packet
        0x00, 0x00, 0x00, 0x00
      };

      AssertThat.Equal(data, expectedData);
    }
  }
}
