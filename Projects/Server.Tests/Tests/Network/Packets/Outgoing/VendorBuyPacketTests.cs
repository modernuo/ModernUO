using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class VendorBuyPacketTests
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

        PacketAssert.Sends(
            ns => new VendorBuyContent(buyStates, ns.ContainerGridLines),
            ns => ns.SendVendorBuyContent(buyStates),
            protocolChanges
        );
    }

    [Theory]
    [InlineData(ProtocolChanges.None)]
    [InlineData(ProtocolChanges.HighSeas)]
    public void TestDisplayBuyList(ProtocolChanges protocolChanges)
    {
        var vendor = new Mobile((Serial)0x1);
        vendor.DefaultMobileInit();

        PacketAssert.Sends(
            ns => new DisplayBuyList(vendor.Serial, ns.HighSeas),
            ns => ns.SendDisplayBuyList(vendor.Serial),
            protocolChanges
        );
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

        PacketAssert.Sends(new VendorBuyList(vendor, buyStates), ns => ns.SendVendorBuyList(vendor, buyStates));
    }

    [Fact]
    public void TestEndVendorBuy()
    {
        var vendor = new Mobile((Serial)0x1);
        vendor.DefaultMobileInit();

        PacketAssert.Sends(new EndVendorBuy(vendor.Serial), ns => ns.SendEndVendorBuy(vendor.Serial));
    }
}
