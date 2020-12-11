using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class VendorBuyPacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(ProtocolChanges.None)]
        [InlineData(ProtocolChanges.ContainerGridLines)]
        public void TestVendorBuyContent(ProtocolChanges protocolChanges)
        {
            var cont = new Container(World.NewItem);

            var buyStates = new List<BuyItemState>
            {
                new("First Item", cont.Serial, World.NewItem, 10, 1, 0x01, 0),
                new("Second Item", cont.Serial, World.NewItem, 20, 2, 0x0A, 0),
                new("Third Item", cont.Serial, World.NewItem, 30, 10, 0x0F, 0)
            };

            var expected = new VendorBuyContent(buyStates).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = protocolChanges;
            ns.SendVendorBuyContent(buyStates);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.None)]
        [InlineData(ProtocolChanges.HighSeas)]
        public void TestDisplayBuyList(ProtocolChanges protocolChanges)
        {
            var vendor = new Mobile(0x1);
            vendor.DefaultMobileInit();

            var expected = new DisplayBuyList(vendor.Serial).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = protocolChanges;
            ns.SendDisplayBuyList(vendor.Serial);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestVendorBuyList()
        {
            var vendor = new Mobile(0x1);
            vendor.DefaultMobileInit();

            var cont = new Container(World.NewItem);

            var buyStates = new List<BuyItemState>
            {
                new("First Item", cont.Serial, World.NewItem, 10, 1, 0x01, 0),
                new("Second Item", cont.Serial, World.NewItem, 20, 2, 0x0A, 0),
                new("Third Item", cont.Serial, World.NewItem, 30, 10, 0x0F, 0)
            };

            var expected = new VendorBuyList(vendor, buyStates).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendVendorBuyList(vendor, buyStates);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestEndVendorBuy()
        {
            var vendor = new Mobile(0x1);
            vendor.DefaultMobileInit();

            var expected = new EndVendorBuy(vendor.Serial).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendEndVendorBuy(vendor.Serial);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
