using System.Collections.Generic;
using Server.Mobiles;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Pins the looting-rights rules that the inline damage entry list has to keep producing: the
/// returned stores are sorted by damage descending, the first (least recent) damager takes the
/// 1.25x bonus, the hitsMax band decides who clears the threshold, and a pet's damage is credited
/// to its damage master rather than to the pet.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class LootingRightsTests
{
    private class TestMobile : Mobile
    {
    }

    private class PetMobile : Mobile
    {
        public Mobile Master { get; set; }

        public override Mobile GetDamageMaster(Mobile damagee) => Master;
    }

    // GetLootingRights only ever credits mobiles flagged as players.
    private static TestMobile NewPlayer() => new() { Player = true };

    private static DamageStore FindStore(List<DamageStore> rights, Mobile m)
    {
        for (var i = 0; i < rights.Count; i++)
        {
            if (rights[i].m_Mobile == m)
            {
                return rights[i];
            }
        }

        return null;
    }

    [Fact]
    public void TwoPlayerDamagers_SortDescending_AndTheFirstDamagerTakesTheBonus()
    {
        var victim = new TestMobile();
        var first = NewPlayer();
        var second = NewPlayer();

        try
        {
            victim.RegisterDamage(100, first);
            victim.RegisterDamage(40, second); // second is the most recent, first is the "first damager"

            // hitsMax < 200 puts the bar at topDamage / 2.
            var rights = BaseCreature.GetLootingRights(victim.DamageEntries, 100);

            Assert.Equal(2, rights.Count);

            // Sorted by damage descending.
            Assert.True(rights[0].m_Damage >= rights[1].m_Damage);
            Assert.Same(first, rights[0].m_Mobile);
            Assert.Same(second, rights[1].m_Mobile);

            // The first damager - the least recent entry - gets the 1.25x bonus; nobody else does.
            Assert.Equal(125, rights[0].m_Damage);
            Assert.Equal(40, rights[1].m_Damage);

            // topDamage 125 / 2 = 62, so 40 is below the bar.
            Assert.True(rights[0].m_HasRight);
            Assert.False(rights[1].m_HasRight);
        }
        finally
        {
            victim.Delete();
            first.Delete();
            second.Delete();
        }
    }

    [Fact]
    public void HitsMaxBand_MovesTheRightsThreshold()
    {
        var victim = new TestMobile();
        var first = NewPlayer();
        var second = NewPlayer();

        try
        {
            victim.RegisterDamage(100, first);
            victim.RegisterDamage(40, second);

            // hitsMax >= 200 drops the bar to topDamage / 4 = 31, which 40 clears.
            var rights = BaseCreature.GetLootingRights(victim.DamageEntries, 200);

            Assert.Equal(2, rights.Count);
            Assert.True(rights[0].m_HasRight);
            Assert.True(rights[1].m_HasRight);
            Assert.Same(second, rights[1].m_Mobile);
        }
        finally
        {
            victim.Delete();
            first.Delete();
            second.Delete();
        }
    }

    [Fact]
    public void PetDamage_CreditsTheMaster_NotThePet()
    {
        var victim = new TestMobile();
        var master = NewPlayer();
        var pet = new PetMobile { Master = master };
        var wild = new TestMobile(); // no damage master, and not a player

        try
        {
            victim.RegisterDamage(50, pet);
            victim.RegisterDamage(20, wild);

            var rights = BaseCreature.GetLootingRights(victim.DamageEntries, 100);

            // The master is credited through the entry's Responsible sub-entry, and is the only one.
            Assert.Single(rights);

            var masterStore = FindStore(rights, master);
            Assert.NotNull(masterStore);
            Assert.Equal(62, masterStore.m_Damage); // 50, then the first-damager 1.25x bonus
            Assert.True(masterStore.m_HasRight);

            // The pet's own damage was fully handed to the master, so it earns no store.
            Assert.Null(FindStore(rights, pet));

            // A non-player damager earns nothing even when its damage was never reassigned.
            Assert.Null(FindStore(rights, wild));
        }
        finally
        {
            victim.Delete();
            master.Delete();
            pet.Delete();
            wild.Delete();
        }
    }
}
