/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AutoDenylistTests.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Net;
using Server.Network;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.AutoDenylists;

// Static store, so every test resets it first. Addresses come from TEST-NET-2 (198.51.100.0/24) to stay clear
// of the other ban tests if xUnit runs these classes concurrently.
public class AutoDenylistTests
{
    private const long DurationMs = 900_000; // 15 minutes, the shipped default
    private const long Now = 1_000_000;

    private static void Reset(bool enabled = true, int maxEntries = 1024) =>
        AutoDenylist.LoadForTesting(enabled, DurationMs, maxEntries);

    [Fact]
    public void Behavioral_detection_is_held_then_lapses()
    {
        Reset();
        var ip = IPAddress.Parse("198.51.100.10");

        Assert.True(AutoDenylist.Hold(ip, BanReasons.InvalidSeed, Now));

        Assert.True(AutoDenylist.IsDenied(ip, Now));
        Assert.True(AutoDenylist.IsDenied(ip, Now + DurationMs - 1));

        // Expiry is decided on read, so the hold lapses without waiting for a sweep.
        Assert.False(AutoDenylist.IsDenied(ip, Now + DurationMs));
    }

    [Fact]
    public void Manual_and_list_verdicts_are_not_held()
    {
        Reset();
        var manual = IPAddress.Parse("198.51.100.11");
        var listed = IPAddress.Parse("198.51.100.12");

        // A manual ban belongs in the firewall; a blocklist match is enforced by the blocklist's own filter.
        Assert.False(AutoDenylist.Hold(manual, BanReasons.Manual, Now));
        Assert.False(AutoDenylist.Hold(listed, BanReasons.Blocklist, Now));

        Assert.False(AutoDenylist.IsDenied(manual, Now));
        Assert.False(AutoDenylist.IsDenied(listed, Now));
        Assert.Equal(0, AutoDenylist.Count);
    }

    [Fact]
    public void Repeat_detection_extends_the_hold()
    {
        Reset();
        var ip = IPAddress.Parse("198.51.100.13");

        AutoDenylist.Hold(ip, BanReasons.SilentConnect, Now);
        AutoDenylist.Hold(ip, BanReasons.SilentConnect, Now + DurationMs - 1);

        Assert.True(AutoDenylist.IsDenied(ip, Now + DurationMs + 1)); // would have lapsed without the second
        Assert.Equal(1, AutoDenylist.Count);                          // and did not add a duplicate
    }

    [Fact]
    public void Release_drops_the_hold_immediately()
    {
        Reset();
        var ip = IPAddress.Parse("198.51.100.14");

        AutoDenylist.Hold(ip, BanReasons.RateLimit, Now);
        AutoDenylist.Release(ip);

        Assert.False(AutoDenylist.IsDenied(ip, Now));
    }

    [Fact]
    public void Cap_is_enforced_so_a_distinct_source_flood_cannot_grow_it()
    {
        Reset(maxEntries: 4);

        for (var i = 0; i < 10; i++)
        {
            AutoDenylist.Hold(IPAddress.Parse($"198.51.100.{100 + i}"), BanReasons.InvalidSeed, Now);
        }

        Assert.Equal(4, AutoDenylist.Count);

        // The first four are still held; the rest were disconnected by their gate but not tracked.
        Assert.True(AutoDenylist.IsDenied(IPAddress.Parse("198.51.100.100"), Now));
        Assert.False(AutoDenylist.IsDenied(IPAddress.Parse("198.51.100.109"), Now));
    }

    [Fact]
    public void Reaching_the_cap_reclaims_lapsed_entries_first()
    {
        Reset(maxEntries: 2);

        AutoDenylist.Hold(IPAddress.Parse("198.51.100.20"), BanReasons.InvalidSeed, Now);
        AutoDenylist.Hold(IPAddress.Parse("198.51.100.21"), BanReasons.InvalidSeed, Now);

        // Once those two have lapsed, a new detection sweeps them out rather than being refused.
        var later = Now + DurationMs + 1;
        Assert.True(AutoDenylist.Hold(IPAddress.Parse("198.51.100.22"), BanReasons.InvalidSeed, later));
        Assert.True(AutoDenylist.IsDenied(IPAddress.Parse("198.51.100.22"), later));
    }

    [Fact]
    public void Disabled_store_denies_nobody()
    {
        Reset(enabled: false);
        var ip = IPAddress.Parse("198.51.100.30");

        Assert.False(AutoDenylist.Hold(ip, BanReasons.InvalidSeed, Now));
        Assert.False(AutoDenylist.IsDenied(ip, Now));
    }

    [Fact]
    public void Null_address_is_handled()
    {
        Reset();

        Assert.False(AutoDenylist.Hold(null, BanReasons.InvalidSeed, Now));
        Assert.False(AutoDenylist.IsDenied(null, Now));
    }
}
