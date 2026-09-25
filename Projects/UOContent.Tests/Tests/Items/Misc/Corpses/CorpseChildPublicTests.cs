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
    [Fact]
    public void IsChildPublic_HumanCorpseWithWornGear_ReturnsTrue()
    {
        var owner = new Mobile((Serial)0x1);
        owner.DefaultMobileInit();
        owner.Body = 0x190;

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

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

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

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

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

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

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

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
}
