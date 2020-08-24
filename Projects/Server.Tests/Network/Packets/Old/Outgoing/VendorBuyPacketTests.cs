using System;
using System.Buffers;
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

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length
            expectedData.Write(ref pos, (ushort)buyStates.Count); // Count

            for (int i = buyStates.Count - 1; i >= 0; i--)
            {
                BuyItemState buyState = buyStates[i];

                expectedData.Write(ref pos, buyState.MySerial);
                expectedData.Write(ref pos, (ushort)buyState.ItemID);
                pos++; // ItemID Offset
                expectedData.Write(ref pos, (ushort)buyState.Amount);
                expectedData.Write(ref pos, (ushort)(i + 1)); // X
                expectedData.Write(ref pos, (ushort)1); // Y
                expectedData.Write(ref pos, buyState.ContainerSerial);
                expectedData.Write(ref pos, (ushort)buyState.Hue);
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

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length
            expectedData.Write(ref pos, (ushort)buyStates.Count); // Count

            for (int i = buyStates.Count - 1; i >= 0; i--)
            {
                BuyItemState buyState = buyStates[i];

                expectedData.Write(ref pos, buyState.MySerial);
                expectedData.Write(ref pos, (ushort)buyState.ItemID);
                pos++; // ItemID Offset
                expectedData.Write(ref pos, (ushort)buyState.Amount);
                expectedData.Write(ref pos, (ushort)(i + 1)); // X
                expectedData.Write(ref pos, (ushort)1); // Y
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // Grid Location?
#else
                pos++;
#endif
                expectedData.Write(ref pos, buyState.ContainerSerial);
                expectedData.Write(ref pos, (ushort)buyState.Hue);
            }

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDisplayBuyList()
        {
            var vendor = new Mobile(0x1);
            vendor.DefaultMobileInit();

            Span<byte> data = new DisplayBuyList(vendor).Compile();

            Span<byte> expectedData = stackalloc byte[7];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, vendor.Serial);
            expectedData.Write(ref pos, (ushort)0x30); // Buy gump

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDisplayBuyListHS()
        {
            var vendor = new Mobile(0x1);
            vendor.DefaultMobileInit();

            Span<byte> data = new DisplayBuyListHS(vendor).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, vendor.Serial);
            expectedData.Write(ref pos, (ushort)0x30); // Buy gump

#if NO_LOCAL_INIT
    expectedData.Write(ref pos, (ushort)0);
#endif

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

            expectedData.Write(ref pos, (byte)0x74); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length
            expectedData.Write(ref pos, Serial.MinusOne); // Vendor Buy Pack Serial or -1
            expectedData.Write(ref pos, (byte)buyStates.Count);

            for (int i = 0; i < buyStates.Count; i++)
            {
                BuyItemState state = buyStates[i];
                expectedData.Write(ref pos, state.Price);
                var description = state.Description ?? "";
                expectedData.Write(ref pos, (byte)Math.Min(255, description.Length + 1));
                expectedData.WriteAsciiNull(ref pos, description, 255);
            }

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestEndVendorBuy()
        {
            var vendor = new Mobile(0x1);
            vendor.DefaultMobileInit();

            Span<byte> data = new EndVendorBuy(vendor).Compile();

            Span<byte> expectedData = stackalloc byte[8];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x3B); // Packet ID
            expectedData.Write(ref pos, (ushort)0x8); // Length
            expectedData.Write(ref pos, vendor.Serial);

#if NO_LOCAL_INIT
    expectedData.Write(ref pos, (byte)-);
#endif

            AssertThat.Equal(data, expectedData);
        }
    }
}
