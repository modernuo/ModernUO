using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Network.Packets
{
  public class VendorBuyPacketTests : IClassFixture<ServerFixture>
  {
    private readonly Mobile vendor;
    private readonly Container cont;

    private readonly List<BuyItemState> buyStates;
    private ITestOutputHelper help;

    public VendorBuyPacketTests(ServerFixture fixture, ITestOutputHelper helper)
    {
      vendor = fixture.fromMobile;
      cont = fixture.fromCont;

      buyStates = new List<BuyItemState>
      {
        new BuyItemState("First Item", cont.Serial, Serial.NewItem, 10, 1, 0x01, 0),
        new BuyItemState("Second Item", cont.Serial, Serial.NewItem, 20, 2, 0x0A, 0),
        new BuyItemState("Third Item", cont.Serial, Serial.NewItem, 30, 10, 0x0F, 0)
      };

      help = helper;
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
        expectedData[pos++] = 0; // ItemID Offset
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
        expectedData[pos++] = 0; // ItemID Offset
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
  }
}
