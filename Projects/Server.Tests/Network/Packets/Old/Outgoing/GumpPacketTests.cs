using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class GumpPacketTests
  {
    [Fact]
    public void TestCloseGump()
    {
      var typeId = 100;
      var buttonId = 10;

      Span<byte> data = new CloseGump(typeId, buttonId).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBF, // Packet ID
        0x00, 0xD, // Length
        0x00, 0x04, // Close
        0x00, 0x00, 0x00, 0x00, // Type Id
        0x00, 0x00, 0x00, 0x00, // Button Id
      };

      typeId.CopyTo(expectedData.Slice(5, 4));
      buttonId.CopyTo(expectedData.Slice(9, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplaySignGump()
    {
      Serial gumpSerial = 0x1000;
      var gumpId = 100;
      var unknownString = "This is an unknown string";
      var caption = "This is a caption";

      Span<byte> data = new DisplaySignGump(gumpSerial, gumpId, unknownString, caption).Compile();

      Span<byte> expectedData = stackalloc byte[15 + unknownString.Length + caption.Length];

      int pos = 0;
      ((byte)0x8B).CopyTo(ref pos, expectedData);
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData);
      gumpSerial.CopyTo(ref pos, expectedData);
      ((ushort)gumpId).CopyTo(ref pos, expectedData);
      unknownString.CopyASCIINullTo(ref pos, expectedData);
      caption.CopyASCIINullTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }
  }
}
