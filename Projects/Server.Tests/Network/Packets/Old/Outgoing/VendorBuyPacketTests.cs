using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class VendorBuyPacketTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestVendorBuyContent()
    {
      var cont = new Container(Serial.LastItem + 1);

      var buyStates = new List<BuyItemState>
      {
        new BuyItemState("First Item", cont.Serial, Serial.NewItem, 10, 1, 0x01, 0),
        new BuyItemState("Second Item", cont.Serial, Serial.NewItem, 20, 2, 0x0A, 0),
        new BuyItemState("Third Item", cont.Serial, Serial.NewItem, 30, 10, 0x0F, 0)
      };

      Span<byte> data = new VendorBuyContent(buyStates).Compile();

      Span<byte> expectedData = stackalloc byte[5 + buyStates.Count * 19];

      int pos = 0;

      ((byte)0x3C).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length
      ((ushort)buyStates.Count).CopyTo(ref pos, expectedData); // Count

      for (int i = buyStates.Count - 1; i >= 0; i--)
      {
        BuyItemState buyState = buyStates[i];

        buyState.MySerial.CopyTo(ref pos, expectedData);
        ((ushort)buyState.ItemID).CopyTo(ref pos, expectedData);
        pos++; // ItemID Offset
        ((ushort)buyState.Amount).CopyTo(ref pos, expectedData);
        ((ushort)(i + 1)).CopyTo(ref pos, expectedData); // X
        ((byte)0).CopyTo(ref pos, expectedData);
        ((byte)1).CopyTo(ref pos, expectedData); // Y
        buyState.ContainerSerial.CopyTo(ref pos, expectedData);
        ((ushort)buyState.Hue).CopyTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestVendorBuyContent6017()
    {
      var cont = new Container(Serial.LastItem + 1);

      var buyStates = new List<BuyItemState>
      {
        new BuyItemState("First Item", cont.Serial, Serial.NewItem, 10, 1, 0x01, 0),
        new BuyItemState("Second Item", cont.Serial, Serial.NewItem, 20, 2, 0x0A, 0),
        new BuyItemState("Third Item", cont.Serial, Serial.NewItem, 30, 10, 0x0F, 0)
      };

      Span<byte> data = new VendorBuyContent6017(buyStates).Compile();

      Span<byte> expectedData = stackalloc byte[5 + buyStates.Count * 20];

      int pos = 0;

      ((byte)0x3C).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length
      ((ushort)buyStates.Count).CopyTo(ref pos, expectedData); // Count

      for (int i = buyStates.Count - 1; i >= 0; i--)
      {
        BuyItemState buyState = buyStates[i];

        buyState.MySerial.CopyTo(ref pos, expectedData);
        ((ushort)buyState.ItemID).CopyTo(ref pos, expectedData);
        pos++; // ItemID Offset
        ((ushort)buyState.Amount).CopyTo(ref pos, expectedData);
        ((ushort)(i + 1)).CopyTo(ref pos, expectedData); // X
        ((byte)0).CopyTo(ref pos, expectedData);
        ((byte)1).CopyTo(ref pos, expectedData); // Y
        ((byte)0).CopyTo(ref pos, expectedData); // Grid Location
        buyState.ContainerSerial.CopyTo(ref pos, expectedData);
        ((ushort)buyState.Hue).CopyTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplayBuyList()
    {
      var vendor = new Mobile(0x1);
      vendor.DefaultMobileInit();

      Span<byte> data = new DisplayBuyList(vendor).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x24, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Vendor Serial
        0x00, 0x30 // Buy Window Gump Id
      };

      vendor.Serial.CopyTo(expectedData.Slice(1, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplayBuyListHS()
    {
      var vendor = new Mobile(0x1);
      vendor.DefaultMobileInit();

      Span<byte> data = new DisplayBuyListHS(vendor).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x24, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Vendor Serial
        0x00, 0x30, // Buy Window Gump Id
        0x00, 0x00
      };

      vendor.Serial.CopyTo(expectedData.Slice(1, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestVendorBuyList()
    {
      var vendor = new Mobile(0x1);
      vendor.DefaultMobileInit();

      var cont = new Container(Serial.LastItem + 1);

      var buyStates = new List<BuyItemState>
      {
        new BuyItemState("First Item", cont.Serial, Serial.NewItem, 10, 1, 0x01, 0),
        new BuyItemState("Second Item", cont.Serial, Serial.NewItem, 20, 2, 0x0A, 0),
        new BuyItemState("Third Item", cont.Serial, Serial.NewItem, 30, 10, 0x0F, 0)
      };

      Span<byte> data = new VendorBuyList(vendor, buyStates).Compile();

      int length = 8 + buyStates.Sum(state => 6 + state.Description.Length);

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;

      ((byte)0x74).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length
      Serial.MinusOne.CopyTo(ref pos, expectedData); // Vendor Buy Pack Serial or -1
      ((byte)buyStates.Count).CopyTo(ref pos, expectedData);

      for (int i = 0; i < buyStates.Count; i++)
      {
        BuyItemState state = buyStates[i];
        state.Price.CopyTo(ref pos, expectedData);
        (state.Description ?? "").CopySmallASCIINullTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestEndVendorBuy()
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
