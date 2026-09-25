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

    // The child's serial must never be written at all: if it were, and the child were still
    // alive (not merely deleted, as above) when load runs, the serial would resolve to that
    // live-but-unrelated item and land it in this container's list with a mismatched Parent.
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

    // An item saved in the post-freeze window can carry frozen bytes naming a skipping container as
    // parent even though it is no longer in that pack's list. On load the parent still resolves (the
    // pack is alive), so this must be treated like a missing parent instead of becoming an unreachable
    // orphan. DelayCall's actual firing isn't observed here (see report); Parent == null is what the
    // fix guarantees synchronously, and it's also the precondition the missing-parent delete checks.
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
