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

    // A skipped child is not saved, so the container must load without it.
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

    // A written child serial could resolve at load to an unrelated item that was handed the same serial.
    [Fact]
    public void SkippingContainer_DoesNotWriteAliveChildSerial()
    {
        var pack = new SkippingContainer();
        var item = new Item(0x1234);
        pack.DropItem(item);

        var writer = new BufferWriter(true);
        pack.Serialize(writer);
        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        // item stays alive and in pack's list past this point, so its serial is still resolvable
        // when copy deserializes below - deleting pack cascades to item, so both go together.
        try
        {
            var copy = new SkippingContainer(World.NewItem);
            copy.Deserialize(new BufferReader(buffer));

            Assert.Empty(copy.Items);
        }
        finally
        {
            pack.Delete();
        }
    }

    // An item can be saved naming a skipping container as parent while absent from its list; it must load
    // as parentless so the missing-parent path deletes it rather than leaving an unreachable orphan.
    [Fact]
    public void ItemLoadingWithSkippingContainerAsParent_ClearsParent()
    {
        var pack = new SkippingContainer();
        var item = new Item(0x1234);
        pack.DropItem(item);

        var writer = new BufferWriter(true);
        item.Serialize(writer);
        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        item.Delete();

        try
        {
            var copy = new Item(World.NewItem);
            copy.Deserialize(new BufferReader(buffer));

            Assert.Null(copy.Parent);
        }
        finally
        {
            pack.Delete();
        }
    }
}
