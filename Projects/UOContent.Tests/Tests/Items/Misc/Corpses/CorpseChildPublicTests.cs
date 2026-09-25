using System;
using System.Collections.Generic;
using Server;
using Server.Items;
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
        owner.Body = 0x190; // Human body

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

        var corpse = new Corpse(owner, owner.Items);

        try
        {
            Assert.Equal(0x2006, corpse.ItemID);
            Assert.True(((Body)corpse.Amount).IsHuman);

            // Worn item should be public on human corpse
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
        owner.Body = 0x190; // Human body

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

        var corpse = new Corpse(owner, owner.Items);

        var lootItem = new Gold(100);
        corpse.DropItem(lootItem);

        try
        {
            Assert.Equal(0x2006, corpse.ItemID);
            Assert.True(((Body)corpse.Amount).IsHuman);

            // Loot item should not be public on human corpse
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
        owner.Body = 0x190; // Human body

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

        var corpse = new Corpse(owner, owner.Items);

        try
        {
            // Change to bone graphic
            corpse.ItemID = 0xECA;

            Assert.True(((Body)corpse.Amount).IsHuman); // Body is still human
            Assert.NotEqual(0x2006, corpse.ItemID); // But ItemID changed to bone graphic

            // Worn item should not be public on bone corpse
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
        owner.Body = 0xC9; // Non-human body

        var wornItem = new VikingSword();
        owner.EquipItem(wornItem);

        var corpse = new Corpse(owner, owner.Items);

        try
        {
            Assert.Equal(0x2006, corpse.ItemID);
            Assert.False(((Body)corpse.Amount).IsHuman);

            // Worn item should not be public on non-human corpse
            Assert.False(corpse.IsChildPublic(wornItem));
        }
        finally
        {
            corpse.Delete();
            owner.Delete();
        }
    }
}
