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
    public void AddAggressor_AppendsWithoutDedupe()
    {
        var victim = new TestMobile();
        var attacker = new TestMobile();

        try
        {
            victim.AddAggressor(attacker, criminal: true);

            Assert.True(victim.HasAggressors);
            Assert.Equal(1, victim.Aggressors.Count);
            Assert.Same(attacker, Attackers(victim)[0]);

            foreach (var info in victim.Aggressors)
            {
                Assert.Same(victim, info.Defender);
                Assert.True(info.CriminalAggression);
                Assert.True(info.OnLinkList);
            }

            // Append-only, like the pet-defense path it replaces.
            victim.AddAggressor(attacker, criminal: false);
            Assert.Equal(2, victim.Aggressors.Count);
        }
        finally
        {
            victim.Delete();
            attacker.Delete();
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
    public void RemoveAggressor_UnlinksAndEmpties()
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

            victim.RemoveAggressor(attacker);

            Assert.Equal(0, victim.Aggressors.Count);
            Assert.False(victim.HasAggressors);
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
            m.RemoveAggressor(other);
            m.RemoveAggressed(other);

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
    public void Expiry_PopsOnlyTheExpiredHeadPrefix_OnBothSides()
    {
        var start = Core._now;
        var victim = new TestMobile();
        var a = new TestMobile();
        var b = new TestMobile();

        try
        {
            victim.AggressiveAction(a, criminal: false);

            Core._now = start + TimeSpan.FromMinutes(1);
            victim.AggressiveAction(b, criminal: false);

            // a is 2.5 min old (expired at 2 min); b is 1.5 min old (live)
            Core._now = start + TimeSpan.FromMinutes(2.5);
            victim.CheckAggrExpire();

            Assert.Equal(new[] { b }, Attackers(victim));
            Assert.Equal(0, a.Aggressed.Count);   // peer entry removed too
            Assert.Equal(1, b.Aggressed.Count);
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
