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
    private readonly Mobile vendor;
    private readonly Container cont;

    private readonly List<BuyItemState> buyStates;

    public VendorBuyPacketTests(ServerFixture fixture)
    {
      vendor = fixture.fromMobile;
      cont = fixture.fromCont;

      buyStates = new List<BuyItemState>
      {
        new BuyItemState("First Item", cont.Serial, Serial.NewItem, 10, 1, 0x01, 0),
        new BuyItemState("Second Item", cont.Serial, Serial.NewItem, 20, 2, 0x0A, 0),
        new BuyItemState("Third Item", cont.Serial, Serial.NewItem, 30, 10, 0x0F, 0)
      };
    }

    [Fact]
    public void TestVendorBuyContent()
    {
      Span<byte> data = new VendorBuyContent(buyStates).Compile();

      Span<byte> expectedData = stackalloc byte[5 + buyStates.Count * 19];

      int pos = 0;

      expectedData[pos++] = 0x3C; // Packet ID
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
        expectedData[pos++] = 0;
        expectedData[pos++] = 1; // Y
        buyState.ContainerSerial.CopyTo(ref pos, expectedData);
        ((ushort)buyState.Hue).CopyTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestVendorBuyContent6017()
    {
      Span<byte> data = new VendorBuyContent6017(buyStates).Compile();

      Span<byte> expectedData = stackalloc byte[5 + buyStates.Count * 20];

      int pos = 0;

      expectedData[pos++] = 0x3C; // Packet ID
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
        expectedData[pos++] = 0;
        expectedData[pos++] = 1; // Y
        expectedData[pos++] = 0; // Grid Location
        buyState.ContainerSerial.CopyTo(ref pos, expectedData);
        ((ushort)buyState.Hue).CopyTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplayBuyList()
    {
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
      Span<byte> data = new VendorBuyList(vendor, buyStates).Compile();

      int length = 8 + 5 * 3 + buyStates.Sum(state => state.Description.Length + 1);

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;

      expectedData[pos++] = 0x74; // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length
      Serial.MinusOne.CopyTo(ref pos, expectedData); // Vendor Buy Pack Serial or -1
      expectedData[pos++] = (byte)buyStates.Count;

      for (int i = 0; i < buyStates.Count; i++)
      {
        BuyItemState state = buyStates[i];
        state.Price.CopyTo(ref pos, expectedData);
        string desc = state.Description ?? "";
        expectedData[pos++] = (byte)(desc.Length + 1);
        desc.CopyASCIITo(ref pos, expectedData);
        pos++;
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestEndVendorBuy()
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
