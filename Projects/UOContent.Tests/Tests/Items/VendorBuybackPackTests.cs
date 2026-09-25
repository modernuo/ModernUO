using Server.Items;
using Xunit;

namespace Server.Tests.Items;

[Collection("Sequential UOContent Tests")]
public class VendorBuybackPackTests
{
    [Fact]
    public void AddBuyback_EvictsOldestFirst_WhenFull()
    {
        var pack = new VendorBuybackPack();
        var items = new Item[5];

        try
        {
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = new Item(0x1234);
                pack.AddBuyback(items[i], 3);
            }

            Assert.Equal(3, pack.Items.Count);
            Assert.True(items[0].Deleted);
            Assert.True(items[1].Deleted);
            Assert.Same(items[2], pack.Items[0]);
            Assert.Same(items[3], pack.Items[1]);
            Assert.Same(items[4], pack.Items[2]);
        }
        finally
        {
            pack.Delete();
        }
    }

    [Fact]
    public void AddBuyback_WithNoCapacity_ConsumesTheItem()
    {
        var pack = new VendorBuybackPack();
        var item = new Item(0x1234);

        try
        {
            pack.AddBuyback(item, 0);

            Assert.True(item.Deleted);
            Assert.Empty(pack.Items);
        }
        finally
        {
            pack.Delete();
        }
    }

    [Fact]
    public void Purge_DeletesEveryChild()
    {
        var pack = new VendorBuybackPack();
        var items = new Item[10];

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new Item(0x1234);
            pack.AddBuyback(items[i], 250);
        }

        try
        {
            pack.Purge();

            Assert.Empty(pack.Items);
            for (var i = 0; i < items.Length; i++)
            {
                Assert.True(items[i].Deleted);
            }
        }
        finally
        {
            pack.Delete();
        }
    }
}
