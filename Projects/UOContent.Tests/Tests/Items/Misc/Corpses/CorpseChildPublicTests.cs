using System;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CorpseChildPublicTests
{
    // Weapons take their layer from tiledata, which CI doesn't load; without an explicit layer the
    // equip is silently refused and the corpse's worn-gear list stays empty.
    private static VikingSword EquipSword(Mobile owner)
    {
        var sword = new VikingSword { Layer = Layer.OneHanded };
        var equipped = owner.EquipItem(sword);
        if (!equipped)
        {
            sword.Delete();
        }

        Assert.True(equipped);
        return sword;
    }

    private static Mobile NewOwner(int body)
    {
        var owner = new Mobile(World.NewMobile);
        owner.DefaultMobileInit();
        owner.Body = body;
        return owner;
    }

    [Fact]
    public void IsChildPublic_HumanCorpseWithWornGear_ReturnsTrue()
    {
        var owner = NewOwner(0x190);
        Corpse corpse = null;

        try
        {
            var wornItem = EquipSword(owner);
            corpse = new Corpse(owner, owner.Items);

            Assert.Equal(0x2006, corpse.ItemID);
            Assert.True(((Body)corpse.Amount).IsHuman);
            Assert.True(corpse.IsChildPublic(wornItem));
        }
        finally
        {
            corpse?.Delete();
            owner.Delete();
        }
    }

    [Fact]
    public void IsChildPublic_HumanCorpseWithLootItem_ReturnsFalse()
    {
        var owner = NewOwner(0x190);
        Corpse corpse = null;

        try
        {
            EquipSword(owner);
            corpse = new Corpse(owner, owner.Items);

            var lootItem = new Gold(100);
            corpse.DropItem(lootItem);

            Assert.Equal(0x2006, corpse.ItemID);
            Assert.True(((Body)corpse.Amount).IsHuman);
            Assert.False(corpse.IsChildPublic(lootItem));
        }
        finally
        {
            corpse?.Delete();
            owner.Delete();
        }
    }

    [Fact]
    public void IsChildPublic_BoneCorpse_ReturnsFalse()
    {
        var owner = NewOwner(0x190);
        Corpse corpse = null;

        try
        {
            var wornItem = EquipSword(owner);
            corpse = new Corpse(owner, owner.Items);

            corpse.ItemID = 0xECA;

            Assert.True(((Body)corpse.Amount).IsHuman);
            Assert.NotEqual(0x2006, corpse.ItemID);
            Assert.False(corpse.IsChildPublic(wornItem));
        }
        finally
        {
            corpse?.Delete();
            owner.Delete();
        }
    }

    [Fact]
    public void IsChildPublic_NonHumanCorpse_ReturnsFalse()
    {
        var owner = NewOwner(0xC9);
        Corpse corpse = null;

        try
        {
            var wornItem = EquipSword(owner);
            corpse = new Corpse(owner, owner.Items);

            Assert.Equal(0x2006, corpse.ItemID);
            Assert.False(((Body)corpse.Amount).IsHuman);
            Assert.False(corpse.IsChildPublic(wornItem));
        }
        finally
        {
            corpse?.Delete();
            owner.Delete();
        }
    }

    private static bool ReceivedRemove(NetState ns, Serial serial)
    {
        Span<byte> expected = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength];
        OutgoingEntityPackets.CreateRemoveEntity(expected, serial);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected) >= 0;
    }

    private static Mobile NewBystander(NetState ns, Point3D location)
    {
        ns.Account = new MockAccount();
        var bystander = new Mobile(World.NewMobile);
        bystander.DefaultMobileInit();
        ns.Mobile = bystander;
        bystander.NetState = ns;
        bystander.MoveToWorld(location, Map.Felucca);
        return bystander;
    }

    private static void DeleteBystander(NetState ns, Mobile bystander)
    {
        ns.Mobile = null;
        ns.Dispose();
        bystander?.Delete();
    }

    [Fact]
    public void HumanCorpse_WornGearRemovalBroadcasts_LootRemovalDoesNot()
    {
        var corpseLoc = new Point3D(1500, 1500, 0);

        var bystanderNs = PacketTestUtilities.CreateTestNetState();
        Mobile bystander = null;
        var owner = NewOwner(0x190);
        Corpse corpse = null;
        Container pouch = null;

        try
        {
            bystander = NewBystander(bystanderNs, new Point3D(corpseLoc.X + 1, corpseLoc.Y, corpseLoc.Z));

            var sword = new VikingSword();
            corpse = new Corpse(owner, [sword]);
            corpse.AddItem(sword);

            var lootItem = new Gold(100);
            corpse.DropItem(lootItem);

            corpse.MoveToWorld(corpseLoc, Map.Felucca);

            // A container not in the world is a valid destination; only the removal from the corpse matters here.
            pouch = new Container(0xE75);
            pouch.AddItem(sword);

            Assert.True(ReceivedRemove(bystanderNs, sword.Serial));

            lootItem.Delete();
            Assert.False(ReceivedRemove(bystanderNs, lootItem.Serial));
        }
        finally
        {
            pouch?.Delete();
            corpse?.Delete();
            owner.Delete();
            DeleteBystander(bystanderNs, bystander);
        }
    }

    [Fact]
    public void HumanCorpse_LiftingWornGear_BroadcastsRemoval()
    {
        var corpseLoc = new Point3D(1500, 1500, 0);

        var bystanderNs = PacketTestUtilities.CreateTestNetState();
        Mobile bystander = null;
        var owner = NewOwner(0x190);
        var sword = new VikingSword { Layer = Layer.OneHanded };
        Corpse corpse = null;

        try
        {
            bystander = NewBystander(bystanderNs, new Point3D(corpseLoc.X + 1, corpseLoc.Y, corpseLoc.Z));
            owner.MoveToWorld(new Point3D(corpseLoc.X - 1, corpseLoc.Y, corpseLoc.Z), Map.Felucca);

            corpse = new Corpse(owner, [sword]);
            corpse.AddItem(sword);
            corpse.MoveToWorld(corpseLoc, Map.Felucca);

            // Lifting takes the piece off the worn list before the remove is sent; the remove must
            // still reach everyone who was shown it worn.
            owner.Lift(sword, sword.Amount, out var rejected, out _);

            Assert.False(rejected);
            Assert.DoesNotContain(sword, corpse.EquipItems);
            Assert.True(ReceivedRemove(bystanderNs, sword.Serial));
        }
        finally
        {
            owner.Holding = null;
            sword.Delete();
            corpse?.Delete();
            owner.Delete();
            DeleteBystander(bystanderNs, bystander);
        }
    }
}
