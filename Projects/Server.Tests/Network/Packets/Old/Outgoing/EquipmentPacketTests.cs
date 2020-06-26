using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class EquipmentPacketTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestDisplayEquipmentInfo()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      var item = new Item(Serial.LastItem + 1);

      var info = new EquipmentInfo(
        500000,
        m,
        false,
        new []
        {
          new EquipInfoAttribute(500001, 1),
          new EquipInfoAttribute(500002, 2),
          new EquipInfoAttribute(500002, 3)
        }
      );

      Span<byte> data = new DisplayEquipmentInfo(item, info).Compile();

      var attrs = info.Attributes;

      int length = 17 + (info.Unidentified ? 4 : 0) + attrs.Length * 6;
      if (info.Crafter != null) length += 6 + (info.Crafter.Name?.Length ?? 0);

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;

      ((byte)0xBF).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)length).CopyTo(ref pos, expectedData); // Length
      ((ushort)0x10).CopyTo(ref pos, expectedData); // Subcommand
      item.Serial.CopyTo(ref pos, expectedData);
      info.Number.CopyTo(ref pos, expectedData);
      if (info.Crafter != null)
      {
        var name = info.Crafter.Name ?? "";
        (-3).CopyTo(ref pos, expectedData);
        name.CopyASCIITo(ref pos, expectedData);
      }

      if (info.Unidentified) (-4).CopyTo(ref pos, expectedData);

      for (var i = 0; i < attrs.Length; i++)
      {
        var attr = attrs[i];
        attr.Number.CopyTo(ref pos, expectedData);
        ((ushort)attr.Charges).CopyTo(ref pos, expectedData);
      }

      (-1).CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestEquipUpdate()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      var item = new Item(Serial.LastItem + 1) { Parent = m };

      Span<byte> data = new EquipUpdate(item).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x2E, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Item ID
        0x00, (byte)item.Layer,
        0x00, 0x00, 0x00, 0x00, // Parent Serial
        0x00, 0x00, // Hue
      };

      item.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)item.ItemID).CopyTo(expectedData.Slice(5, 2));
      item.Parent.Serial.CopyTo(expectedData.Slice(9, 4));
      ((ushort)item.Hue).CopyTo(expectedData.Slice(13, 2));

      AssertThat.Equal(data, expectedData);
    }
  }
}
