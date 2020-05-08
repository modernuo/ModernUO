using System;
using System.Collections.Generic;
using System.Linq;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class VendorSellPacketTests : IClassFixture<ServerFixture>
  {
    private readonly Mobile vendor;

    private readonly List<SellItemState> sellStates;

    public VendorSellPacketTests(ServerFixture fixture)
    {
      vendor = fixture.fromMobile;

      sellStates = new List<SellItemState>
      {
        new SellItemState(fixture.item1, 100, "Item 1"),
        new SellItemState(fixture.item2, 100000, "Item 2"),
        new SellItemState(fixture.item3, 1, "Item 3")
      };
    }

    [Fact]
    public void TestVendorSellList()
    {
      Span<byte> data = new VendorSellList(vendor, sellStates).Compile();

      int length = 9 + 14 * 3 + sellStates.Sum(state =>
        (string.IsNullOrWhiteSpace(state.Item.Name) ? state.Name ?? "" : state.Item.Name.Trim()).Length
      );

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;
      expectedData[pos++] = 0x9E; // Packet ID
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
        ((ushort)name.Length).CopyTo(ref pos, expectedData);
        name.CopyASCIITo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestEndVendorSell()
    {
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
