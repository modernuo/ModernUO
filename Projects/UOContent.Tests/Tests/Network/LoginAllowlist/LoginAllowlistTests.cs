/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LoginAllowlistTests.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Server.Network;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.LoginAllowlists;

// The list is static and the login path and BlocklistFilter both read it, so every test resets it first,
// the class runs in the sequential collection, and Dispose puts the original state back. Addresses come
// from TEST-NET-3 (203.0.113.0/24).
[Collection("Sequential UOContent Tests")]
public class LoginAllowlistTests : IDisposable
{
    private readonly List<(FieldInfo Field, object Value, DictionaryEntry[] Entries)> _saved = [];

    public LoginAllowlistTests()
    {
        var fields = typeof(LoginAllowlist).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (field.IsLiteral)
            {
                continue;
            }

            var value = field.GetValue(null);

            // Readonly tables cannot be reassigned, so their contents are copied and refilled instead.
            DictionaryEntry[] entries = null;
            if (field.IsInitOnly && value is IDictionary table)
            {
                entries = new DictionaryEntry[table.Count];
                table.CopyTo(entries, 0);
            }
            else if (field.IsInitOnly)
            {
                continue;
            }

            _saved.Add((field, value, entries));
        }
    }

    public void Dispose()
    {
        for (var i = 0; i < _saved.Count; i++)
        {
            var (field, value, entries) = _saved[i];
            if (entries == null)
            {
                field.SetValue(null, value);
                continue;
            }

            var table = (IDictionary)value;
            table.Clear();
            for (var j = 0; j < entries.Length; j++)
            {
                table.Add(entries[j].Key, entries[j].Value);
            }
        }
    }

    private const long Ttl = 90 * 24 * 60 * 60; // 90 days, the shipped default
    private const long Window = 3600;
    private const long Now = 1_800_000_000;

    private static void Reset(bool enabled = true, int strikes = 0) =>
        LoginAllowlist.LoadForTesting(enabled, Ttl, strikes, Window);

    [Fact]
    public void Recent_login_allows_its_address()
    {
        Reset();
        var ip = IPAddress.Parse("203.0.113.10");

        LoginAllowlist.RecordLogin(ip, Now);

        Assert.True(LoginAllowlist.IsAllowed(ip, Now));
        Assert.True(LoginAllowlist.IsAllowed(ip, Now + Ttl / 2));
    }

    [Fact]
    public void Entry_lapses_once_past_the_ttl()
    {
        Reset();
        var ip = IPAddress.Parse("203.0.113.11");

        LoginAllowlist.RecordLogin(ip, Now);

        Assert.True(LoginAllowlist.IsAllowed(ip, Now + Ttl));      // the boundary still counts
        Assert.False(LoginAllowlist.IsAllowed(ip, Now + Ttl + 1)); // a second later it does not
    }

    [Fact]
    public void Logging_in_again_renews_the_window()
    {
        Reset();
        var ip = IPAddress.Parse("203.0.113.12");

        LoginAllowlist.RecordLogin(ip, Now);
        LoginAllowlist.RecordLogin(ip, Now + Ttl); // still allowed here, so the entry is refreshed

        Assert.True(LoginAllowlist.IsAllowed(ip, Now + Ttl + Ttl));
    }

    [Fact]
    public void Unknown_address_is_not_allowed()
    {
        Reset();
        LoginAllowlist.RecordLogin(IPAddress.Parse("203.0.113.13"), Now);

        Assert.False(LoginAllowlist.IsAllowed(IPAddress.Parse("203.0.113.14"), Now));
    }

    [Fact]
    public void Private_addresses_are_never_recorded()
    {
        Reset();

        // A LAN or loopback login says nothing about the public internet.
        LoginAllowlist.RecordLogin(IPAddress.Parse("10.0.0.5"), Now);
        LoginAllowlist.RecordLogin(IPAddress.Loopback, Now);

        Assert.False(LoginAllowlist.IsAllowed(IPAddress.Parse("10.0.0.5"), Now));
        Assert.False(LoginAllowlist.IsAllowed(IPAddress.Loopback, Now));
        Assert.Equal(0, LoginAllowlist.Count);
    }

    [Fact]
    public void Disabled_list_allows_nobody()
    {
        Reset(enabled: false);
        var ip = IPAddress.Parse("203.0.113.15");

        LoginAllowlist.RecordLogin(ip, Now);

        Assert.False(LoginAllowlist.IsAllowed(ip, Now));
        Assert.Equal(0, LoginAllowlist.Count);
    }

    [Fact]
    public void Null_address_is_handled()
    {
        Reset();

        LoginAllowlist.RecordLogin(null, Now);

        Assert.False(LoginAllowlist.IsAllowed(null, Now));
    }

    // ----- escalation -------------------------------------------------------------------------------

    [Fact]
    public void Manual_bans_are_never_exempt()
    {
        Reset();
        var ip = IPAddress.Parse("203.0.113.20");
        LoginAllowlist.RecordLogin(ip, Now);

        // An explicit decision must reach the reporters even for an allowlisted address.
        Assert.False(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.Manual, Now));
    }

    [Fact]
    public void Unopted_reasons_are_never_exempt()
    {
        Reset();
        var ip = IPAddress.Parse("203.0.113.21");
        LoginAllowlist.RecordLogin(ip, Now);

        // A reason nobody opted into IsBehavioral escalates normally rather than inheriting an exemption.
        Assert.False(LoginAllowlist.IsExemptFromEscalation(ip, "some-future-reason", Now));
        Assert.False(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.Blocklist, Now));
    }

    [Fact]
    public void Behavioral_reasons_are_exempt_while_allowed()
    {
        Reset();
        var ip = IPAddress.Parse("203.0.113.22");
        LoginAllowlist.RecordLogin(ip, Now);

        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.SilentConnect, Now));
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.InvalidSeed, Now));
    }

    [Fact]
    public void Entry_is_revoked_once_the_strikes_run_out()
    {
        Reset(strikes: 3);
        var ip = IPAddress.Parse("203.0.113.23");
        LoginAllowlist.RecordLogin(ip, Now);

        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));

        // The third strike is the one that escalates, and it takes the entry with it.
        Assert.False(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        Assert.False(LoginAllowlist.IsAllowed(ip, Now));

        // Still revoked afterwards, so everything from here escalates too.
        Assert.False(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
    }

    [Fact]
    public void Quiet_window_clears_the_tally()
    {
        Reset(strikes: 3);
        var ip = IPAddress.Parse("203.0.113.24");
        LoginAllowlist.RecordLogin(ip, Now);

        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));

        // Past the window the count restarts, so an occasional tripper never accumulates to revocation.
        var later = Now + Window + 1;
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, later));
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, later));
        Assert.True(LoginAllowlist.IsAllowed(ip, later));
    }

    [Fact]
    public void Logging_in_again_forgives_accumulated_strikes()
    {
        Reset(strikes: 3);
        var ip = IPAddress.Parse("203.0.113.25");
        LoginAllowlist.RecordLogin(ip, Now);

        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));

        LoginAllowlist.RecordLogin(ip, Now); // someone proved they hold an account again

        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        Assert.True(LoginAllowlist.IsAllowed(ip, Now));
    }

    [Fact]
    public void Zero_threshold_disables_revocation()
    {
        Reset(strikes: 0);
        var ip = IPAddress.Parse("203.0.113.26");
        LoginAllowlist.RecordLogin(ip, Now);

        for (var i = 0; i < 50; i++)
        {
            Assert.True(LoginAllowlist.IsExemptFromEscalation(ip, BanReasons.RateLimit, Now));
        }

        Assert.True(LoginAllowlist.IsAllowed(ip, Now));
    }
}
