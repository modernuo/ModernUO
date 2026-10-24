using System;
using System.Collections.Generic;
using Server.Collections;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class AggressorListTests
{
    private class TestMobile : Mobile
    {
    }

    private static List<Mobile> Attackers(Mobile victim)
    {
        var result = new List<Mobile>();
        foreach (var info in victim.Aggressors)
        {
            result.Add(info.Attacker);
        }

        return result;
    }

    private static List<Mobile> Defenders(Mobile attacker)
    {
        var result = new List<Mobile>();
        foreach (var info in attacker.Aggressed)
        {
            result.Add(info.Defender);
        }

        return result;
    }

    [Fact]
    public void FreshMobile_HasEmptyListsAndNoAggression()
    {
        var m = new TestMobile();

        try
        {
            Assert.Equal(0, m.Aggressors.Count);
            Assert.Equal(0, m.Aggressed.Count);
            Assert.False(m.HasAggressors);
            Assert.False(m.HasAggressed);
        }
        finally
        {
            m.Delete();
        }
    }

    [Fact]
    public void AggressiveAction_PopulatesBothSides()
    {
        var victim = new TestMobile();
        var attacker = new TestMobile();

        try
        {
            victim.AggressiveAction(attacker, criminal: false);

            Assert.True(victim.HasAggressors);
            Assert.Equal(1, victim.Aggressors.Count);
            Assert.Same(attacker, Attackers(victim)[0]);

            Assert.True(attacker.HasAggressed);
            Assert.Equal(1, attacker.Aggressed.Count);
            foreach (var info in attacker.Aggressed)
            {
                Assert.Same(victim, info.Defender);
            }

            Assert.False(victim.HasAggressed);
            Assert.False(attacker.HasAggressors);
        }
        finally
        {
            victim.Delete();
            attacker.Delete();
        }
    }

    [Fact]
    public void RepeatAggression_RefreshesAndMovesToTail()
    {
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();

        try
        {
            victim.AggressiveAction(a, criminal: false);
            victim.AggressiveAction(b, criminal: false);
            victim.AggressiveAction(a, criminal: false); // a refreshed, now most recent

            Assert.Equal(2, victim.Aggressors.Count);
            Assert.Equal(new[] { b, a }, Attackers(victim));
        }
        finally
        {
            victim.Delete();
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void RepeatAggression_RelinksMiddleEntryToTail()
    {
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();
        var c = new TestMobile();

        try
        {
            victim.AggressiveAction(a, criminal: false);
            victim.AggressiveAction(b, criminal: false);
            victim.AggressiveAction(c, criminal: false);
            victim.AggressiveAction(b, criminal: false); // b was in the middle

            Assert.Equal(new[] { a, c, b }, Attackers(victim));
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
    public void RepeatAggression_RelinksAggressedSideToTail()
    {
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();
        var c = new TestMobile();

        try
        {
            a.AggressiveAction(victim, criminal: false);
            b.AggressiveAction(victim, criminal: false);
            c.AggressiveAction(victim, criminal: false);
            b.AggressiveAction(victim, criminal: false); // b was in the middle of the aggressed list

            Assert.Equal(new[] { a, c, b }, Defenders(victim));
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
    public void MutualCombat_KeepsOnePairedRecord_AndDeleteClearsPeer()
    {
        var victim = new TestMobile();
        var attacker = new TestMobile();

        try
        {
            victim.AggressiveAction(attacker, criminal: false);
            attacker.AggressiveAction(victim, criminal: false); // fighting back does not add a second pair

            Assert.Equal(1, victim.Aggressors.Count);
            Assert.Equal(1, attacker.Aggressed.Count);
            Assert.Equal(0, attacker.Aggressors.Count);
            Assert.Equal(0, victim.Aggressed.Count);

            attacker.Delete();

            Assert.Equal(0, victim.Aggressors.Count);
            Assert.False(victim.HasAggressors);
        }
        finally
        {
            victim.Delete();
            attacker.Delete(); // deleting twice is a no-op
        }
    }

    [Fact]
    public void RemoveAggression_UnlinksBothHalves()
    {
        var victim = new TestMobile();
        var attacker = new TestMobile();

        try
        {
            victim.AggressiveAction(attacker, criminal: false);
            AggressorInfo entry = null;
            foreach (var info in victim.Aggressors)
            {
                entry = info;
            }

            victim.RemoveAggression(attacker);

            Assert.Equal(0, victim.Aggressors.Count);
            Assert.False(victim.HasAggressors);
            Assert.Equal(0, attacker.Aggressed.Count); // both halves of the pair are gone

            // The entry has been returned to the pool; Next/Previous/OnLinkList are the only
            // members safe to read after Free().
            Assert.False(entry.OnLinkList);
            Assert.Null(entry.Next);
            Assert.Null(entry.Previous);
        }
        finally
        {
            victim.Delete();
            attacker.Delete();
        }
    }

    [Fact]
    public void RemoveOnEmptyLists_IsNoOp()
    {
        var m = new TestMobile();
        var other = new TestMobile();

        try
        {
            m.RemoveAggression(other);

            Assert.Equal(0, m.Aggressors.Count);
            Assert.Equal(0, m.Aggressed.Count);
        }
        finally
        {
            m.Delete();
            other.Delete();
        }
    }

    [Fact]
    public void Expiry_RemovesExpiredEntries_OnBothSides()
    {
        var start = Core._now;
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();
        var c = new TestMobile();
        var d = new TestMobile();

        try
        {
            victim.AggressiveAction(a, criminal: false); // a attacks victim
            c.AggressiveAction(victim, criminal: false); // victim attacks c

            Core._now = start + TimeSpan.FromMinutes(1);
            victim.AggressiveAction(b, criminal: false); // b attacks victim
            d.AggressiveAction(victim, criminal: false); // victim attacks d

            // a and c are 2.5 min old (expired at 2 min); b and d are 1.5 min old (live)
            Core._now = start + TimeSpan.FromMinutes(2.5);
            victim.CheckAggrExpire();

            Assert.Equal(new[] { b }, Attackers(victim));
            Assert.Equal(new[] { d }, Defenders(victim));

            Assert.Equal(0, a.Aggressed.Count);    // peer entries removed too
            Assert.Equal(0, c.Aggressors.Count);
            Assert.Equal(1, b.Aggressed.Count);
            Assert.Equal(1, d.Aggressors.Count);
        }
        finally
        {
            Core._now = start;
            victim.Delete();
            a.Delete();
            b.Delete();
            c.Delete();
            d.Delete();
        }
    }

    [Fact]
    public void DeletingAPeer_UnlinksImmediately()
    {
        var victim = new TestMobile();
        var attacker = new TestMobile();

        try
        {
            victim.AggressiveAction(attacker, criminal: false);
            Assert.Equal(1, victim.Aggressors.Count);

            attacker.Delete();

            Assert.Equal(0, victim.Aggressors.Count);
            Assert.False(victim.HasAggressors);
        }
        finally
        {
            victim.Delete();
            attacker.Delete();
        }
    }
}
