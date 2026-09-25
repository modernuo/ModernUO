using System;
using Server.Accounting;
using Server.Items;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class RestrictedChildRemovalTests
{
    private class RestrictedContainer : Container
    {
        public RestrictedContainer() : base(0xE75)
        {
        }

        public override bool RestrictsChildRemoval => true;
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

    private static void Cleanup(NetState ns, Mobile client, Mobile owner)
    {
        ns.Mobile = null;
        ns.Dispose();
        client.Delete();
        owner.Delete();
    }

    [Fact]
    public void RestrictedChildDelete_IsNotBroadcastToNearbyClients()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var owner = CreateOwnerWith(new RestrictedContainer());
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
    public void RestrictedChildDelete_ReachesOpeners()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var pack = new RestrictedContainer();
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

    // Invisible containers are used for gump tooltips on some shards; they must keep broadcasting.
    [Fact]
    public void UnrestrictedInvisibleChildDelete_IsStillBroadcast()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var owner = CreateOwnerWith(new Container(0xE75));
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
    public void DeletingOwner_SendsNoRemoveForRestrictedChildren()
    {
        var (ns, client) = CreateClient(new Point3D(_ownerLoc.X + 1, _ownerLoc.Y, 0));
        var pack = new RestrictedContainer();
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
}
