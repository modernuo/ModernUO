using System;
using Server.Items;
using Server.Network;
using Xunit;
using static Server.Tests.TestClientHarness;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class PrivateChildPropertiesTests : IDisposable
{
    private readonly bool _oplWasEnabled;

    public PrivateChildPropertiesTests()
    {
        _oplWasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;
    }

    public void Dispose() => ObjectPropertyList.Enabled = _oplWasEnabled;

    private class PublicContainer : Container
    {
        public PublicContainer() : base(0xE75)
        {
        }

        public override bool IsPublicContainer => true;
    }

    private static readonly Point3D _ownerLoc = new(1600, 1600, 0);

    // AddItem/MoveToWorld already queue their own Update (+Properties) delta as a side effect of the
    // Map setter. Draining it first isolates the properties-only delta the test actually exercises;
    // otherwise the leftover Update flag routes through the Update branch and the test would pass or
    // fail for the wrong reason.
    private static void DrainPendingDelta(Item item) => item.ProcessDelta();

    private static void InvalidateAndFlush(Item item)
    {
        // InvalidateProperties() only queues a delta when the OPL hash actually changes, which the
        // drain above already stabilized. Queue ItemDelta.Properties directly so the test deterministically
        // exercises the properties-only (no Update) branch under test.
        item.Delta(ItemDelta.Properties);
        item.ProcessDelta();
    }

    [Fact]
    public void PrivateChildInOwnerBackpack_PropertiesOnlyUpdate_ReachesOwnerNotBystander()
    {
        var (ownerNs, owner) = CreateClient(_ownerLoc);
        var (bystanderNs, bystander) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));

        var pack = new Container(0xE75) { Layer = Layer.Backpack };
        owner.AddItem(pack);

        var child = new Item(0x1234);
        pack.DropItem(child);

        try
        {
            DrainPendingDelta(child);
            InvalidateAndFlush(child);

            Assert.True(ReceivedOplInfo(ownerNs, child.Serial));
            Assert.False(ReceivedOplInfo(bystanderNs, child.Serial));
        }
        finally
        {
            child.Delete();
            DisposeClient(ownerNs, owner);
            DisposeClient(bystanderNs, bystander);
        }
    }

    [Fact]
    public void PrivateGroundContainer_PropertiesOnlyUpdate_ReachesOpenerNotNonOpener()
    {
        var (openerNs, opener) = CreateClient(_ownerLoc);
        var (bystanderNs, bystander) = CreateClient(new Point3D(_ownerLoc.X + 2, _ownerLoc.Y, 0));

        var container = new Container(0xE75);
        container.MoveToWorld(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0), Map.Felucca);
        container.Openers = [opener];

        var child = new Item(0x1234);
        container.DropItem(child);

        try
        {
            DrainPendingDelta(child);
            InvalidateAndFlush(child);

            Assert.True(ReceivedOplInfo(openerNs, child.Serial));
            Assert.False(ReceivedOplInfo(bystanderNs, child.Serial));
        }
        finally
        {
            container.Delete();
            DisposeClient(openerNs, opener);
            DisposeClient(bystanderNs, bystander);
        }
    }

    [Fact]
    public void PublicContainerChild_PropertiesOnlyUpdate_StillReachesBystander()
    {
        var (bystanderNs, bystander) = CreateClient(_ownerLoc);

        var pack = new PublicContainer();
        pack.MoveToWorld(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0), Map.Felucca);

        var child = new Item(0x1234);
        pack.DropItem(child);

        try
        {
            DrainPendingDelta(child);
            InvalidateAndFlush(child);

            Assert.True(ReceivedOplInfo(bystanderNs, child.Serial));
        }
        finally
        {
            pack.Delete();
            DisposeClient(bystanderNs, bystander);
        }
    }

    [Fact]
    public void GroundItem_PropertiesOnlyUpdate_StillReachesBystander()
    {
        var (bystanderNs, bystander) = CreateClient(_ownerLoc);

        var item = new Item(0x1234);
        item.MoveToWorld(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0), Map.Felucca);

        try
        {
            DrainPendingDelta(item);
            InvalidateAndFlush(item);

            Assert.True(ReceivedOplInfo(bystanderNs, item.Serial));
        }
        finally
        {
            item.Delete();
            DisposeClient(bystanderNs, bystander);
        }
    }
}
