using System;
using System.Buffers;
using Server.Items;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class PrivateChildPropertiesTests
{
    private class PublicContainer : Container
    {
        public PublicContainer() : base(0xE75)
        {
        }

        public override bool IsPublicContainer => true;
    }

    private static readonly Point3D _ownerLoc = new(1600, 1600, 0);

    private static (NetState, Mobile) CreateClient(Point3D location)
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        ns.Account = new MockAccount();

        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        ns.Mobile = mobile;
        mobile.NetState = ns;
        mobile.MoveToWorld(location, Map.Felucca);
        return (ns, mobile);
    }

    private static void DisposeClient(NetState ns, Mobile mobile)
    {
        ns.Mobile = null;
        ns.Dispose();
        mobile.Delete();
    }

    private static bool ReceivedOplInfo(NetState ns, Serial serial)
    {
        Span<byte> expected = stackalloc byte[5];
        var writer = new SpanWriter(expected);
        writer.Write((byte)0xDC); // OPL info packet ID
        writer.Write(serial);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected) >= 0;
    }

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
        var wasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;

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
            ObjectPropertyList.Enabled = wasEnabled;
        }
    }

    [Fact]
    public void PrivateGroundContainer_PropertiesOnlyUpdate_ReachesOpenerNotNonOpener()
    {
        var wasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;

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
            ObjectPropertyList.Enabled = wasEnabled;
        }
    }

    [Fact]
    public void PublicContainerChild_PropertiesOnlyUpdate_StillReachesBystander()
    {
        var wasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;

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
            ObjectPropertyList.Enabled = wasEnabled;
        }
    }

    [Fact]
    public void GroundItem_PropertiesOnlyUpdate_StillReachesBystander()
    {
        var wasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;

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
            ObjectPropertyList.Enabled = wasEnabled;
        }
    }
}
