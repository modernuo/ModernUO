using System;
using Server.HuePickers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class DisplayHuePickerTests
  {
    [Fact]
    public void TestDisplayHuePicker()
    {
      const ushort itemID = 0xFF01;
      var huePicker = new HuePicker(itemID);

      Span<byte> data = new DisplayHuePicker(huePicker).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x95, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Hue Picker Serial
        0x00, 0x00, // Nothing
        0x00, 0x00 // Item ID
      };

      huePicker.Serial.CopyTo(expectedData.Slice(1, 4));
      itemID.CopyTo(expectedData.Slice(7, 2));

      AssertThat.Equal(data, expectedData);
    }
  }
}
