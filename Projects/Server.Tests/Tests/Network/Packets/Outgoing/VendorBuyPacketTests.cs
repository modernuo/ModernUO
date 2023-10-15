using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    [Collection("Sequential Tests")]
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

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = protocolChanges;

            var expected = new VendorBuyContent(buyStates, ns.ContainerGridLines).Compile();

            ns.SendVendorBuyContent(buyStates);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.None)]
        [InlineData(ProtocolChanges.HighSeas)]
        public void TestDisplayBuyList(ProtocolChanges protocolChanges)
        {
            var vendor = new Mobile((Serial)0x1);
            vendor.DefaultMobileInit();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = protocolChanges;

            var expected = new DisplayBuyList(vendor.Serial, ns.HighSeas).Compile();

            ns.SendDisplayBuyList(vendor.Serial);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestVendorBuyList()
        {
            var vendor = new Mobile((Serial)0x1);
            vendor.DefaultMobileInit();

            var cont = new Container(World.NewItem);

            var buyStates = new List<BuyItemState>
            {
                new("First Item", cont.Serial, World.NewItem, 10, 1, 0x01, 0),
                new("Second Item", cont.Serial, World.NewItem, 20, 2, 0x0A, 0),
                new("Third Item", cont.Serial, World.NewItem, 30, 10, 0x0F, 0)
            };

            var expected = new VendorBuyList(vendor, buyStates).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendVendorBuyList(vendor, buyStates);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestEndVendorBuy()
        {
            var vendor = new Mobile((Serial)0x1);
            vendor.DefaultMobileInit();

            var expected = new EndVendorBuy(vendor.Serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendEndVendorBuy(vendor.Serial);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
