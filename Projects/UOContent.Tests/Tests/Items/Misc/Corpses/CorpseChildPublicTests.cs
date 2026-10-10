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
        Assert.True(owner.EquipItem(sword));
        return sword;
    }

    [Fact]
    public void IsChildPublic_HumanCorpseWithWornGear_ReturnsTrue()
    {
        var owner = new Mobile((Serial)0x1);
        owner.DefaultMobileInit();
        owner.Body = 0x190;

        var wornItem = EquipSword(owner);

        var corpse = new Corpse(owner, owner.Items);

        try
        {
            Assert.Equal(0x2006, corpse.ItemID);
            Assert.True(((Body)corpse.Amount).IsHuman);
            Assert.True(corpse.IsChildPublic(wornItem));
        }
        finally
        {
            corpse.Delete();
            owner.Delete();
        }
    }

    [Fact]
    public void IsChildPublic_HumanCorpseWithLootItem_ReturnsFalse()
    {
        var owner = new Mobile((Serial)0x1);
        owner.DefaultMobileInit();
        owner.Body = 0x190;

        var wornItem = EquipSword(owner);

        var corpse = new Corpse(owner, owner.Items);

        var lootItem = new Gold(100);
        corpse.DropItem(lootItem);

        try
        {
            Assert.Equal(0x2006, corpse.ItemID);
            Assert.True(((Body)corpse.Amount).IsHuman);
            Assert.False(corpse.IsChildPublic(lootItem));
        }
        finally
        {
            corpse.Delete();
            owner.Delete();
        }
    }

    [Fact]
    public void IsChildPublic_BoneCorpse_ReturnsFalse()
    {
        var owner = new Mobile((Serial)0x1);
        owner.DefaultMobileInit();
        owner.Body = 0x190;

        var wornItem = EquipSword(owner);

        var corpse = new Corpse(owner, owner.Items);

        try
        {
            corpse.ItemID = 0xECA;

            Assert.True(((Body)corpse.Amount).IsHuman);
            Assert.NotEqual(0x2006, corpse.ItemID);
            Assert.False(corpse.IsChildPublic(wornItem));
        }
        finally
        {
            corpse.Delete();
            owner.Delete();
        }
    }

    [Fact]
    public void IsChildPublic_NonHumanCorpse_ReturnsFalse()
    {
        var owner = new Mobile((Serial)0x1);
        owner.DefaultMobileInit();
        owner.Body = 0xC9;

        var wornItem = EquipSword(owner);

        var corpse = new Corpse(owner, owner.Items);

        try
        {
            Assert.Equal(0x2006, corpse.ItemID);
            Assert.False(((Body)corpse.Amount).IsHuman);
            Assert.False(corpse.IsChildPublic(wornItem));
        }
        finally
        {
            corpse.Delete();
            owner.Delete();
        }
    }

    private static bool ReceivedRemove(NetState ns, Serial serial)
    {
        Span<byte> expected = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength];
        OutgoingEntityPackets.CreateRemoveEntity(expected, serial);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected) >= 0;
    }

    [Fact]
    public void HumanCorpse_WornGearRemovalBroadcasts_LootRemovalDoesNot()
    {
        var corpseLoc = new Point3D(1500, 1500, 0);

        var bystanderNs = PacketTestUtilities.CreateTestNetState();
        bystanderNs.Account = new MockAccount();
        var bystander = new Mobile(World.NewMobile);
        bystander.DefaultMobileInit();
        bystanderNs.Mobile = bystander;
        bystander.NetState = bystanderNs;
        bystander.MoveToWorld(new Point3D(corpseLoc.X + 1, corpseLoc.Y, corpseLoc.Z), Map.Felucca);

        var owner = new Mobile((Serial)0x1);
        owner.DefaultMobileInit();
        owner.Body = 0x190;

        var sword = new VikingSword();
        var corpse = new Corpse(owner, [sword]);
        corpse.AddItem(sword);

        var lootItem = new Gold(100);
        corpse.DropItem(lootItem);

        corpse.MoveToWorld(corpseLoc, Map.Felucca);

        Container pouch = null;

        try
        {
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
            corpse.Delete();
            owner.Delete();
            bystanderNs.Mobile = null;
            bystanderNs.Dispose();
            bystander.Delete();
        }
    }

    [Fact]
    public void HumanCorpse_LiftingWornGear_BroadcastsRemoval()
    {
        var corpseLoc = new Point3D(1500, 1500, 0);

        var bystanderNs = PacketTestUtilities.CreateTestNetState();
        bystanderNs.Account = new MockAccount();
        var bystander = new Mobile(World.NewMobile);
        bystander.DefaultMobileInit();
        bystanderNs.Mobile = bystander;
        bystander.NetState = bystanderNs;
        bystander.MoveToWorld(new Point3D(corpseLoc.X + 1, corpseLoc.Y, corpseLoc.Z), Map.Felucca);

        var owner = new Mobile(World.NewMobile);
        owner.DefaultMobileInit();
        owner.Body = 0x190;
        owner.MoveToWorld(new Point3D(corpseLoc.X - 1, corpseLoc.Y, corpseLoc.Z), Map.Felucca);

        var sword = new VikingSword { Layer = Layer.OneHanded };
        var corpse = new Corpse(owner, [sword]);
        corpse.AddItem(sword);
        corpse.MoveToWorld(corpseLoc, Map.Felucca);

        try
        {
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
            corpse.Delete();
            owner.Delete();
            bystanderNs.Mobile = null;
            bystanderNs.Dispose();
            bystander.Delete();
        }
    }
}
