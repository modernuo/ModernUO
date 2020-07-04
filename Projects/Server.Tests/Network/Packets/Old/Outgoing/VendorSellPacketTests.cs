using System;
using System.Collections.Generic;
using System.Linq;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class VendorSellPacketTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestVendorSellList()
    {
      var vendor = new Mobile(0x1);
      vendor.DefaultMobileInit();

      var item1 = new Item(Serial.LastItem + 1);
      var item2 = new Item(Serial.LastItem + 2) { Name = "Second Item" };
      var item3 = new Item(Serial.LastItem + 3);

      var sellStates = new List<SellItemState>
      {
        new SellItemState(item1, 100, "Item 1"),
        new SellItemState(item2, 100000, "Item 2"),
        new SellItemState(item3, 1, "Item 3")
      };

      Span<byte> data = new VendorSellList(vendor, sellStates).Compile();

      int length = 9 + 14 * 3 + sellStates.Sum(state =>
        (string.IsNullOrWhiteSpace(state.Item.Name) ? state.Name ?? "" : state.Item.Name.Trim()).Length
      );

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;
      ((byte)0x9E).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)length).CopyTo(ref pos, expectedData);
      vendor.Serial.CopyTo(ref pos, expectedData);
      ((ushort)sellStates.Count).CopyTo(ref pos, expectedData);

      for (int i = 0; i < sellStates.Count; i++)
      {
        SellItemState state = sellStates[i];
        state.Item.Serial.CopyTo(ref pos, expectedData);
        ((ushort)state.Item.ItemID).CopyTo(ref pos, expectedData);
        ((ushort)state.Item.Hue).CopyTo(ref pos, expectedData);
        ((ushort)state.Item.Amount).CopyTo(ref pos, expectedData);
        ((ushort)state.Price).CopyTo(ref pos, expectedData);
        string name = string.IsNullOrWhiteSpace(state.Item.Name) ? state.Name ?? "" : state.Item.Name.Trim();
        name.CopyASCIITo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestEndVendorSell()
    {
      var vendor = new Mobile(0x1);
      vendor.DefaultMobileInit();

      Span<byte> data = new EndVendorBuy(vendor).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x3B, // Packet ID
        0x00, 0x08, // Length
        0x00, 0x00, 0x00, 0x00, // Vendor Serial
        0x00
      };

      vendor.Serial.CopyTo(expectedData.Slice(3, 4));

      AssertThat.Equal(data, expectedData);
    }
  }
}
