using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class ContainerBulkRemovalTests
{
    [Fact]
    public void DeletingLargeContainer_DeletesAllChildren_AndTotalsStayConsistent()
    {
        var outer = new Container(0xE75);
        var pack = new Container(0xE75);
        outer.DropItem(pack);

        var children = new Item[100_000];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = new Item(0x1234);
            pack.DropItem(children[i]);
        }

        Assert.Equal(children.Length + 1, outer.TotalItems);

        pack.Delete();

        for (var i = 0; i < children.Length; i++)
        {
            Assert.True(children[i].Deleted);
        }

        Assert.Empty(outer.Items);
        Assert.Equal(0, outer.TotalItems);
        outer.Delete();
    }

    // The tail fast path must not change which entry is removed when the item is elsewhere.
    [Fact]
    public void RemovingFromFrontMiddleAndEnd_RemovesExactlyThatItem()
    {
        var pack = new Container(0xE75);
        var items = new Item[5];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new Item(0x1234);
            pack.DropItem(items[i]);
        }

        items[0].Delete();
        items[2].Delete();
        items[4].Delete();

        Assert.Equal(2, pack.Items.Count);
        Assert.Same(items[1], pack.Items[0]);
        Assert.Same(items[3], pack.Items[1]);
        pack.Delete();
    }
}
