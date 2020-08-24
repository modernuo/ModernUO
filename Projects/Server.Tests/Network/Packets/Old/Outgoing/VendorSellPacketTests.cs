using System;
using System.Buffers;
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

            expectedData.Write(ref pos, (byte)0x9E); // Packet ID
            expectedData.Write(ref pos, (ushort)length);
            expectedData.Write(ref pos, vendor.Serial);
            expectedData.Write(ref pos, (ushort)sellStates.Count);

            for (int i = 0; i < sellStates.Count; i++)
            {
                SellItemState state = sellStates[i];
                expectedData.Write(ref pos, state.Item.Serial);
                expectedData.Write(ref pos, (ushort)state.Item.ItemID);
                expectedData.Write(ref pos, (ushort)state.Item.Hue);
                expectedData.Write(ref pos, (ushort)state.Item.Amount);
                expectedData.Write(ref pos, (ushort)state.Price);
                string name = string.IsNullOrWhiteSpace(state.Item.Name) ? state.Name ?? "" : state.Item.Name.Trim();
                expectedData.Write(ref pos, (ushort)name.Length);
                expectedData.WriteAscii(ref pos, name);
            }

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestEndVendorSell()
        {
            var vendor = new Mobile(0x1);
            vendor.DefaultMobileInit();

            Span<byte> data = new EndVendorBuy(vendor).Compile();

            Span<byte> expectedData = stackalloc byte[8];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x3B); // Packet ID
            expectedData.Write(ref pos, (ushort)0x08); // Length
            expectedData.Write(ref pos, vendor.Serial);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#endif

            AssertThat.Equal(data, expectedData);
        }
    }
}
