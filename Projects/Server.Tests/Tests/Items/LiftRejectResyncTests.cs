using System;
using System.Buffers;
using Server.Accounting;
using Server.Items;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class LiftRejectResyncTests
{
    private static readonly Point3D _baseLoc = new(2200, 2200, 0);

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

    private static bool ReceivedContentUpdate(NetState ns, Serial serial)
    {
        Span<byte> expected = stackalloc byte[5];
        var writer = new SpanWriter(expected);
        writer.Write((byte)0x25); // ContainerContentUpdate packet ID
        writer.Write(serial);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected) >= 0;
    }

    private static bool ReceivedWorldItem(NetState ns, Item item)
    {
        Span<byte> expected = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength];
        var length = OutgoingItemPackets.CreateWorldItem(expected, item);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected[..length]) >= 0;
    }

    [Fact]
    public void PrivateNestedItem_RejectedLift_DoesNotResyncBystander()
    {
        var (ownerNs, owner) = CreateClient(_baseLoc);
        var (bystanderNs, bystander) = CreateClient(new Point3D(_baseLoc.X + 1, _baseLoc.Y, 0));

        var backpack = new Container(0xE75) { Layer = Layer.Backpack };
        owner.AddItem(backpack);
        var pouch = new Container(0xE75);
        backpack.DropItem(pouch);
        var item = new Item(0x1234);
        pouch.DropItem(item);

        try
        {
            bystander.Lift(item, item.Amount, out var rejected, out _);

            // Whichever check trips first (CheckNonlocalLift, accessibility, ...), the bystander was
            // never sent this private child, so the rejection must not resynchronize it to them.
            Assert.True(rejected);
            Assert.False(ReceivedContentUpdate(bystanderNs, item.Serial));
        }
        finally
        {
            DisposeClient(ownerNs, owner);
            DisposeClient(bystanderNs, bystander);
        }
    }

    [Fact]
    public void OwnerAlreadyHolding_RejectedLift_ResyncsPrivateChildToOwner()
    {
        var (ownerNs, owner) = CreateClient(_baseLoc);

        var backpack = new Container(0xE75) { Layer = Layer.Backpack };
        owner.AddItem(backpack);
        var item = new Item(0x1234);
        backpack.DropItem(item);

        owner.Holding = new Item(0x1);

        try
        {
            owner.Lift(item, item.Amount, out var rejected, out var reject);

            Assert.True(rejected);
            Assert.Equal(LRReason.AreHolding, reject);
            Assert.True(ReceivedContentUpdate(ownerNs, item.Serial));
        }
        finally
        {
            owner.Holding?.Delete();
            DisposeClient(ownerNs, owner);
        }
    }

    [Fact]
    public void GroundItemOutOfRange_RejectedLift_DoesNotResyncRequester()
    {
        var (requesterNs, requester) = CreateClient(_baseLoc);

        var item = new Item(0x1234);
        item.MoveToWorld(new Point3D(_baseLoc.X, _baseLoc.Y + 100, 0), Map.Felucca);

        try
        {
            requester.Lift(item, item.Amount, out var rejected, out var reject);

            Assert.True(rejected);
            Assert.Equal(LRReason.OutOfRange, reject);
            Assert.False(ReceivedWorldItem(requesterNs, item));
        }
        finally
        {
            DisposeClient(requesterNs, requester);
            item.Delete();
        }
    }

    [Fact]
    public void GroundContainerOpener_ForcedRejection_ResyncsPrivateChildToOpener()
    {
        var (requesterNs, requester) = CreateClient(_baseLoc);

        var container = new Container(0xE75);
        container.MoveToWorld(new Point3D(_baseLoc.X + 1, _baseLoc.Y, 0), Map.Felucca);
        var item = new Item(0x1234);
        container.DropItem(item);
        container.Openers = [requester];

        requester.Holding = new Item(0x1);

        try
        {
            requester.Lift(item, item.Amount, out var rejected, out var reject);

            Assert.True(rejected);
            Assert.Equal(LRReason.AreHolding, reject);
            Assert.True(ReceivedContentUpdate(requesterNs, item.Serial));
        }
        finally
        {
            requester.Holding?.Delete();
            DisposeClient(requesterNs, requester);
            container.Delete();
        }
    }

    [Fact]
    public void TradePartner_RejectedLift_ResyncsNestedItemToTradePartner()
    {
        var (aNs, a) = CreateClient(_baseLoc);
        var (bNs, b) = CreateClient(new Point3D(_baseLoc.X + 1, _baseLoc.Y, 0));

        var trade = new SecureTrade(a, b);
        var pouch = new Container(0xE75);
        trade.From.Container.DropItem(pouch);
        var item = new Item(0x1234);
        pouch.DropItem(item);

        b.Holding = new Item(0x1);

        try
        {
            // b is a's trade partner: not the item's root and never an Opener of the pouch, but a
            // sanctioned recipient of this private child through the trade relationship.
            b.Lift(item, item.Amount, out var rejected, out var reject);

            Assert.True(rejected);
            Assert.Equal(LRReason.AreHolding, reject);
            Assert.True(ReceivedContentUpdate(bNs, item.Serial));
        }
        finally
        {
            b.Holding?.Delete();
            trade.From.Container.Delete();
            trade.To.Container.Delete();
            DisposeClient(aNs, a);
            DisposeClient(bNs, b);
        }
    }

    [Fact]
    public void InvisibleItem_RejectedLift_DoesNotResyncToNonStaffOwner()
    {
        var (ownerNs, owner) = CreateClient(_baseLoc);

        var backpack = new Container(0xE75) { Layer = Layer.Backpack };
        owner.AddItem(backpack);
        var item = new Item(0x1234) { Visible = false };
        backpack.DropItem(item);

        owner.Holding = new Item(0x1);

        try
        {
            owner.Lift(item, item.Amount, out var rejected, out var reject);

            // ProcessDelta never sends an invisible item to a non-staff owner (CanSee gates every
            // private recipient); the rejection resync must honor the same gate.
            Assert.True(rejected);
            Assert.Equal(LRReason.AreHolding, reject);
            Assert.False(ReceivedContentUpdate(ownerNs, item.Serial));
        }
        finally
        {
            owner.Holding?.Delete();
            DisposeClient(ownerNs, owner);
        }
    }

    [Fact]
    public void InvisibleItem_RejectedLift_ResyncsToStaffOwner()
    {
        var (ownerNs, owner) = CreateClient(_baseLoc);
        owner.AccessLevel = AccessLevel.GameMaster;

        var backpack = new Container(0xE75) { Layer = Layer.Backpack };
        owner.AddItem(backpack);
        var item = new Item(0x1234) { Visible = false };
        backpack.DropItem(item);

        owner.Holding = new Item(0x1);

        try
        {
            owner.Lift(item, item.Amount, out var rejected, out var reject);

            Assert.True(rejected);
            Assert.Equal(LRReason.AreHolding, reject);
            Assert.True(ReceivedContentUpdate(ownerNs, item.Serial));
        }
        finally
        {
            owner.Holding?.Delete();
            DisposeClient(ownerNs, owner);
        }
    }
}
