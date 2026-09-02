using System;
using System.Collections.Generic;
using Server.Collections;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class DamageEntryTests
{
    private class TestMobile : Mobile
    {
    }

    private class PetMobile : Mobile
    {
        public Mobile Master { get; set; }

        public override Mobile GetDamageMaster(Mobile damagee) => Master;
    }

    private static List<Mobile> Damagers(Mobile victim)
    {
        var result = new List<Mobile>();
        foreach (var de in victim.DamageEntries)
        {
            result.Add(de.Damager);
        }

        return result;
    }

    [Fact]
    public void FreshMobile_HasNoEntries()
    {
        var m = new TestMobile();

        try
        {
            Assert.Equal(0, m.DamageEntries.Count);
            Assert.Null(m.FindMostRecentDamageEntry(true));
            Assert.Null(m.FindLeastRecentDamageEntry(true));
            Assert.Null(m.FindMostTotalDamageEntry(true));
            Assert.Null(m.FindLeastTotalDamageEntry(true));
            Assert.Null(m.FindDamageEntryFor(m));
        }
        finally
        {
            m.Delete();
        }
    }

    [Fact]
    public void RegisterDamage_OrdersLeastRecentToMostRecent()
    {
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();

        try
        {
            victim.RegisterDamage(10, a);
            victim.RegisterDamage(20, b);
            victim.RegisterDamage(5, a); // a becomes most recent again

            Assert.Equal(2, victim.DamageEntries.Count);
            Assert.Equal(new[] { b, a }, Damagers(victim));
            Assert.Equal(15, victim.FindDamageEntryFor(a).DamageGiven);
            Assert.Same(a, victim.FindMostRecentDamager(true));
            Assert.Same(b, victim.FindLeastRecentDamager(true));
        }
        finally
        {
            victim.Delete();
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void FindRecent_HonorsAllowSelf()
    {
        var victim = new TestMobile();
        var a = new TestMobile();

        try
        {
            victim.RegisterDamage(10, a);
            victim.RegisterDamage(10, victim); // self is most recent

            Assert.Same(victim, victim.FindMostRecentDamager(true));
            Assert.Same(a, victim.FindMostRecentDamager(false));
            Assert.Same(a, victim.FindLeastRecentDamager(false));
        }
        finally
        {
            victim.Delete();
            a.Delete();
        }
    }

    [Fact]
    public void FindLeastRecent_HonorsAllowSelf()
    {
        var victim = new TestMobile();
        var a = new TestMobile();

        try
        {
            victim.RegisterDamage(10, victim); // self is least recent, so the head is the one to skip
            victim.RegisterDamage(10, a);

            Assert.Same(victim, victim.FindLeastRecentDamager(true));
            Assert.Same(a, victim.FindLeastRecentDamager(false));
        }
        finally
        {
            victim.Delete();
            a.Delete();
        }
    }

    [Fact]
    public void FindTotal_PicksByDamage_MostRecentWinsTies()
    {
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();
        var c = new TestMobile();

        try
        {
            victim.RegisterDamage(30, a);
            victim.RegisterDamage(30, b); // ties a; b is more recent
            victim.RegisterDamage(1, c);

            Assert.Same(b, victim.FindMostTotalDamager(true));
            Assert.Same(c, victim.FindLeastTotalDamager(true));
        }
        finally
        {
            victim.Delete();
            a.Delete();
            b.Delete();
            c.Delete();
        }
    }

    [Fact]
    public void FindLeastTotal_MostRecentWinsTies()
    {
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();
        var c = new TestMobile();

        try
        {
            victim.RegisterDamage(30, a);
            victim.RegisterDamage(5, b);
            victim.RegisterDamage(5, c); // ties b for the minimum; c is more recent

            Assert.Same(a, victim.FindMostTotalDamager(true));
            Assert.Same(c, victim.FindLeastTotalDamager(true));
        }
        finally
        {
            victim.Delete();
            a.Delete();
            b.Delete();
            c.Delete();
        }
    }

    [Fact]
    public void Prune_RemovesExpiredPrefix_KeepsOrder()
    {
        var start = Core._now;
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();

        try
        {
            victim.RegisterDamage(10, a);

            Core._now = start + DamageEntry.ExpireDelay + TimeSpan.FromSeconds(1);
            victim.RegisterDamage(10, b); // a is now expired, b is live

            Assert.Equal(new[] { b }, Damagers(victim));
            Assert.Null(victim.FindDamageEntryFor(a));
        }
        finally
        {
            Core._now = start;
            victim.Delete();
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void Prune_AllExpired_EmptiesList()
    {
        var start = Core._now;
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();

        try
        {
            victim.RegisterDamage(10, a);
            victim.RegisterDamage(10, b);

            Core._now = start + DamageEntry.ExpireDelay + TimeSpan.FromSeconds(1);

            Assert.Equal(0, victim.DamageEntries.Count);
            Assert.Null(victim.FindMostRecentDamageEntry(true));
        }
        finally
        {
            Core._now = start;
            victim.Delete();
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void ClearDamageEntries_UnlinksEveryNode()
    {
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();

        try
        {
            var ea = victim.RegisterDamage(10, a);
            var eb = victim.RegisterDamage(10, b);

            victim.ClearDamageEntries();

            Assert.Equal(0, victim.DamageEntries.Count);
            Assert.False(ea.OnLinkList);
            Assert.False(eb.OnLinkList);
            Assert.Null(ea.Next);
            Assert.Null(ea.Previous);
            Assert.Null(eb.Next);
            Assert.Null(eb.Previous);
        }
        finally
        {
            victim.Delete();
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void FullHitPoints_ClearsEntries()
    {
        var victim = new TestMobile();
        var a = new TestMobile();

        try
        {
            victim.RawStr = 50; // HitsMax follows Str for a base Mobile
            victim.Hits = 10;
            victim.RegisterDamage(10, a);
            Assert.Equal(1, victim.DamageEntries.Count);

            // Also stops the HitsTimer the Hits = 10 write started, so the test leaves no timer behind.
            victim.Hits = victim.HitsMax;

            Assert.Equal(0, victim.DamageEntries.Count);
        }
        finally
        {
            victim.Delete();
            a.Delete();
        }
    }

    [Fact]
    public void RegisterDamage_AccumulatesResponsibleMaster()
    {
        var victim = new TestMobile();
        var master = new TestMobile();
        var pet = new PetMobile { Master = master };

        try
        {
            victim.RegisterDamage(10, pet);
            var entry = victim.RegisterDamage(5, pet);

            Assert.Same(pet, entry.Damager);
            Assert.Equal(15, entry.DamageGiven);
            Assert.NotNull(entry.Responsible);
            Assert.Single(entry.Responsible);
            Assert.Same(master, entry.Responsible[0].Damager);
            Assert.Equal(15, entry.Responsible[0].DamageGiven);
            Assert.False(entry.Responsible[0].OnLinkList); // sub-entries never join the main list
        }
        finally
        {
            victim.Delete();
            master.Delete();
            pet.Delete();
        }
    }
}
