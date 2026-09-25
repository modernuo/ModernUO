using System;
using Server.Accounting;
using Server.Items;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class ContainerChildRemovalTests
{
    private class PublicContainer : Container
    {
        public PublicContainer() : base(0xE75)
        {
        }

        public override bool IsPublicContainer => true;
    }

    private class OneChildPublicContainer : Container
    {
        public OneChildPublicContainer() : base(0xE75)
        {
        }

        public Item PublicChild { get; set; }

        public override bool IsChildPublic(Item child) => child == PublicChild;
    }

    // An override that narrows below IsPublicContainer instead of only widening it, exercising the guard
    // that keeps a public container's removals broadcast regardless of what IsChildPublic returns.
    private class PublicContainerWithNarrowingOverride : Container
    {
        public PublicContainerWithNarrowingOverride() : base(0xE75)
        {
        }

        public override bool IsPublicContainer => true;

        public override bool IsChildPublic(Item child) => false;
    }

    private static readonly Point3D _ownerLoc = new(1500, 1500, 0);

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

    private static Mobile CreateOwnerWith(Container pack)
    {
        var owner = new Mobile(World.NewMobile);
        owner.DefaultMobileInit();
        owner.MoveToWorld(_ownerLoc, Map.Felucca);
        pack.Layer = Layer.ShopBuy;
        pack.Visible = false;
        owner.AddItem(pack);
        return owner;
    }

    private static bool ReceivedRemove(NetState ns, Serial serial)
    {
        Span<byte> expected = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength];
        OutgoingEntityPackets.CreateRemoveEntity(expected, serial);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected) >= 0;
    }

    private static int CountRemoves(NetState ns, Serial serial)
    {
        Span<byte> expected = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength];
        OutgoingEntityPackets.CreateRemoveEntity(expected, serial);

        var span = ns.SendBuffer.GetReadSpan();
        var count = 0;

        while (true)
        {
            var index = span.IndexOf(expected);

            if (index < 0)
            {
                break;
            }

            count++;
            span = span[(index + expected.Length)..];
        }

        return count;
    }

    private static void DisposeClient(NetState ns, Mobile mobile)
    {
        ns.Mobile = null;
        ns.Dispose();
        mobile.Delete();
    }

    private static void Cleanup(NetState ns, Mobile client, Mobile owner)
    {
        DisposeClient(ns, client);
        owner.Delete();
    }

    [Fact]
    public void PrivateChildDelete_IsNotBroadcastToNearbyClients()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var owner = CreateOwnerWith(new Container(0xE75));
        var child = new Item(0x1234);
        ((Container)owner.FindItemOnLayer(Layer.ShopBuy)).DropItem(child);

        try
        {
            child.Delete();
            Assert.False(ReceivedRemove(ns, child.Serial));
        }
        finally
        {
            Cleanup(ns, client, owner);
        }
    }

    [Fact]
    public void PrivateChildDelete_ReachesOpeners()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var pack = new Container(0xE75);
        var owner = CreateOwnerWith(pack);
        var child = new Item(0x1234);
        pack.DropItem(child);
        pack.Openers = [client];

        try
        {
            child.Delete();
            Assert.True(ReceivedRemove(ns, child.Serial));
        }
        finally
        {
            Cleanup(ns, client, owner);
        }
    }

    [Fact]
    public void PrivateChildDelete_ReachesTheRootOwnersClient()
    {
        var (ownerNs, owner) = CreateClient(_ownerLoc);
        var (bystanderNs, bystander) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));

        var pack = new Container(0xE75)
        {
            Layer = Layer.Backpack,
            Visible = false
        };
        owner.AddItem(pack);

        var child = new Item(0x1234);
        pack.DropItem(child);

        try
        {
            child.Delete();
            Assert.True(ReceivedRemove(ownerNs, child.Serial));
            Assert.False(ReceivedRemove(bystanderNs, child.Serial));
        }
        finally
        {
            DisposeClient(ownerNs, owner);
            DisposeClient(bystanderNs, bystander);
        }
    }

    [Fact]
    public void PublicContainerChildDelete_IsBroadcast()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var owner = CreateOwnerWith(new PublicContainer());
        var child = new Item(0x1234);
        ((Container)owner.FindItemOnLayer(Layer.ShopBuy)).DropItem(child);

        try
        {
            child.Delete();
            Assert.True(ReceivedRemove(ns, child.Serial));
        }
        finally
        {
            Cleanup(ns, client, owner);
        }
    }

    [Fact]
    public void PublicContainer_NarrowingChildOverride_StillBroadcastsRemoval()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var owner = CreateOwnerWith(new PublicContainerWithNarrowingOverride());
        var child = new Item(0x1234);
        ((Container)owner.FindItemOnLayer(Layer.ShopBuy)).DropItem(child);

        try
        {
            child.Delete();
            Assert.True(ReceivedRemove(ns, child.Serial));
        }
        finally
        {
            Cleanup(ns, client, owner);
        }
    }

    [Fact]
    public void IsChildPublic_BroadcastsOnlyThatChild()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var pack = new OneChildPublicContainer();
        var owner = CreateOwnerWith(pack);
        var publicChild = new Item(0x1234);
        var privateChild = new Item(0x1235);
        pack.DropItem(publicChild);
        pack.DropItem(privateChild);
        pack.PublicChild = publicChild;

        try
        {
            privateChild.Delete();
            Assert.False(ReceivedRemove(ns, privateChild.Serial));

            publicChild.Delete();
            Assert.True(ReceivedRemove(ns, publicChild.Serial));
        }
        finally
        {
            Cleanup(ns, client, owner);
        }
    }

    [Fact]
    public void DeletingOwner_SendsNoRemoveForPrivateChildren()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var pack = new Container(0xE75);
        var owner = CreateOwnerWith(pack);
        var children = new Item[50];
        for (var i = 0; i < children.Length; i++)
        {
            children[i] = new Item(0x1234);
            pack.DropItem(children[i]);
        }

        try
        {
            owner.Delete();

            for (var i = 0; i < children.Length; i++)
            {
                Assert.True(children[i].Deleted);
                Assert.False(ReceivedRemove(ns, children[i].Serial));
            }
        }
        finally
        {
            Cleanup(ns, client, owner);
        }
    }

    [Fact]
    public void TradePartner_ReceivesRemoveForNestedItem()
    {
        var (nsA, a) = CreateClient(_ownerLoc);
        var (nsB, b) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));

        var trade = new SecureTrade(a, b);
        var pouch = new Container(0xE75);
        trade.From.Container.DropItem(pouch);
        var item = new Item(0x1234);
        pouch.DropItem(item);

        try
        {
            item.Delete();
            Assert.True(ReceivedRemove(nsB, item.Serial));
        }
        finally
        {
            trade.From.Container.Delete();
            trade.To.Container.Delete();
            DisposeClient(nsA, a);
            DisposeClient(nsB, b);
        }
    }

    [Fact]
    public void FacetChange_DoesNotRemoveNestedContainerChildren()
    {
        var (ownerNs, owner) = CreateClient(_ownerLoc);
        var (bystanderNs, bystander) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));

        var backpack = new Container(0xE75) { Layer = Layer.Backpack };
        owner.AddItem(backpack);

        var pouch = new Container(0xE75);
        backpack.DropItem(pouch);

        var reagent = new Item(0xF7A);
        pouch.DropItem(reagent);

        try
        {
            owner.MoveToWorld(_ownerLoc, Map.Trammel);

            Assert.False(ReceivedRemove(ownerNs, pouch.Serial));
            Assert.False(ReceivedRemove(ownerNs, reagent.Serial));
            Assert.False(ReceivedRemove(bystanderNs, pouch.Serial));
            Assert.False(ReceivedRemove(bystanderNs, reagent.Serial));
        }
        finally
        {
            DisposeClient(ownerNs, owner);
            DisposeClient(bystanderNs, bystander);
        }
    }

    [Fact]
    public void NoDuplicateRemove_WhenRootIsAlsoAnOpener()
    {
        var (ownerNs, owner) = CreateClient(_ownerLoc);

        var pack = new Container(0xE75)
        {
            Layer = Layer.Backpack,
            Visible = false
        };
        owner.AddItem(pack);
        pack.Openers = [owner];

        var child = new Item(0x1234);
        pack.DropItem(child);

        try
        {
            child.Delete();
            Assert.Equal(1, CountRemoves(ownerNs, child.Serial));
        }
        finally
        {
            DisposeClient(ownerNs, owner);
        }
    }
}
