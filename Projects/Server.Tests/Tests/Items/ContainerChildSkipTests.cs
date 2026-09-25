using System;
using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class ContainerChildSkipTests
{
    private class SkippingContainer : Container
    {
        public SkippingContainer() : base(0xE75)
        {
        }

        public SkippingContainer(Serial serial) : base(serial)
        {
        }

        public override bool SkipsChildSerialization => true;
    }

    [Fact]
    public void ChildOfSkippingContainer_SkipsUntilMovedOut()
    {
        var pack = new SkippingContainer();
        var normal = new Container(0xE75);
        var item = new Item(0x1234);

        try
        {
            pack.DropItem(item);
            Assert.True(item.SkipSerialization);
            Assert.False(pack.SkipSerialization);

            normal.DropItem(item);
            Assert.False(item.SkipSerialization);
        }
        finally
        {
            item.Delete();
            pack.Delete();
            normal.Delete();
        }
    }

    [Fact]
    public void GrandchildOfSkippingContainer_IsNotSkipped()
    {
        var pack = new SkippingContainer();
        var inner = new Container(0xE75);
        var item = new Item(0x1234);

        try
        {
            pack.DropItem(inner);
            inner.DropItem(item);

            Assert.True(inner.SkipSerialization);
            Assert.False(item.SkipSerialization);
        }
        finally
        {
            pack.Delete();
        }
    }

    // The container still writes the skipped child's serial; load must drop the dangling reference.
    [Fact]
    public void SkippingContainer_LoadsWithoutItsSkippedChildren()
    {
        var pack = new SkippingContainer();
        var item = new Item(0x1234);
        pack.DropItem(item);

        var writer = new BufferWriter(true);
        pack.Serialize(writer);
        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        item.Delete();
        pack.Delete();

        var copy = new SkippingContainer(World.NewItem);
        copy.Deserialize(new BufferReader(buffer));

        Assert.Empty(copy.Items);
    }
}
