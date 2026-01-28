using System.Collections.Generic;
using System.Linq;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class VendorSellPacketTests
{
    [Fact]
    public void TestVendorSellList()
    {
        var vendor = new Mobile((Serial)0x1024u);
        vendor.DefaultMobileInit();

        var item1 = new Item(World.NewItem);
        var item2 = new Item(World.NewItem) { Name = "Second Item" };
        var item3 = new Item(World.NewItem);

        var sellStates = new HashSet<SellItemState>
        {
            new(item1, 100, "Item 1"),
            new(item2, 100000, "Item 2"),
            new(item3, 1, "Item 3")
        };

        var expected = new VendorSellList(vendor, sellStates.ToList()).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendVendorSellList(vendor.Serial, sellStates);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestEndVendorSell()
    {
        var vendor = new Mobile((Serial)0x1024u);
        vendor.DefaultMobileInit();

        var expected = new EndVendorBuy(vendor.Serial).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendEndVendorSell(vendor.Serial);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
