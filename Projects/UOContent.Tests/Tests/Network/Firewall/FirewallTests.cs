/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FirewallTests.cs                                                *
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
using System.Net;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Firewall;

[Collection("Sequential UOContent Tests")]
public class FirewallTests : IDisposable
{
    // The test host never configures the firewall, so empty is the state every other test expects.
    public void Dispose() => Server.Network.Firewall.ResetForTesting();

    private static IPAddress Ip(string s) => IPAddress.Parse(s);

    [Fact]
    public void Add_ThenIsBlocked_SingleIp()
    {
        Server.Network.Firewall.ResetForTesting();
        Assert.True(Server.Network.Firewall.Add(new SingleIpFirewallEntry(Ip("1.2.3.4"))));
        Assert.True(Server.Network.Firewall.IsBlocked(Ip("1.2.3.4")));
        Assert.False(Server.Network.Firewall.IsBlocked(Ip("1.2.3.5")));
    }

    [Fact]
    public void Add_Range_BlocksInside_NotOutside()
    {
        Server.Network.Firewall.ResetForTesting();
        Server.Network.Firewall.Add(new CidrFirewallEntry(Ip("10.0.0.0"), Ip("10.0.0.255")));
        Assert.True(Server.Network.Firewall.IsBlocked(Ip("10.0.0.7")));
        Assert.False(Server.Network.Firewall.IsBlocked(Ip("10.0.1.0")));
    }

    [Fact]
    public void Remove_Unblocks()
    {
        Server.Network.Firewall.ResetForTesting();
        var entry = new SingleIpFirewallEntry(Ip("1.2.3.4"));
        Server.Network.Firewall.Add(entry);
        Assert.True(Server.Network.Firewall.Remove(entry));
        Assert.False(Server.Network.Firewall.IsBlocked(Ip("1.2.3.4")));
    }

    [Fact]
    public void ExpireEntries_RemovesExpired_KeepsPermanent()
    {
        Server.Network.Firewall.ResetForTesting();
        var permanent = new SingleIpFirewallEntry(Ip("1.1.1.1"));
        var temporary = new SingleIpFirewallEntry(Ip("2.2.2.2"));
        Server.Network.Firewall.Add(permanent);                                   // no ttl
        Server.Network.Firewall.Add(temporary, TimeSpan.FromMilliseconds(50));    // ttl

        Server.Network.Firewall.ExpireEntries(Core.TickCount + 100);              // past the ttl

        Assert.True(Server.Network.Firewall.IsBlocked(Ip("1.1.1.1")));
        Assert.False(Server.Network.Firewall.IsBlocked(Ip("2.2.2.2")));
    }

    [Fact]
    public void ToFirewallEntry_ParsesForms()
    {
        Assert.IsType<SingleIpFirewallEntry>(Server.Network.Firewall.ToFirewallEntry("1.2.3.4"));
        Assert.IsType<CidrFirewallEntry>(Server.Network.Firewall.ToFirewallEntry("10.0.0.0/24"));
        Assert.IsType<CidrFirewallEntry>(Server.Network.Firewall.ToFirewallEntry("10.0.0.0-10.0.0.255"));
        Assert.Null(Server.Network.Firewall.ToFirewallEntry("not-an-ip"));
    }

    [Fact]
    public void CreateSubscriberEntry_IPv6_CoversSlash64()
    {
        var entry = Server.Network.Firewall.CreateSubscriberEntry(Ip("2001:db8:1:2:aaaa:bbbb:cccc:dddd"));
        Assert.True(entry.MinIpAddress <= Ip("2001:db8:1:2::").ToUInt128());
        Assert.True(entry.MaxIpAddress >= Ip("2001:db8:1:2:ffff:ffff:ffff:ffff").ToUInt128());
        Assert.True(entry.MinIpAddress > Ip("2001:db8:1:1:ffff:ffff:ffff:ffff").ToUInt128());
        Assert.True(entry.MaxIpAddress < Ip("2001:db8:1:3::").ToUInt128());
    }

    [Fact]
    public void CreateSubscriberEntry_IPv4_Single()
    {
        var entry = Server.Network.Firewall.CreateSubscriberEntry(Ip("1.2.3.4"));
        Assert.IsType<SingleIpFirewallEntry>(entry);
        Assert.Equal(entry.MinIpAddress, entry.MaxIpAddress);
    }

    [Fact]
    public void CreateSubscriberEntry_MappedIPv4_Single()
    {
        var entry = Server.Network.Firewall.CreateSubscriberEntry(Ip("::ffff:1.2.3.4"));
        Assert.IsType<SingleIpFirewallEntry>(entry);
        Assert.Equal(Ip("1.2.3.4").ToUInt128(), entry.MinIpAddress);
    }

    [Fact]
    public void ReportableNetwork_IPv4Single_True()
    {
        Assert.True(Server.Network.Firewall.TryGetReportableNetwork(new SingleIpFirewallEntry(Ip("1.2.3.4")), out var network));
        Assert.Equal(32, network.PrefixLength);
        Assert.Equal(Ip("1.2.3.4"), network.BaseAddress);
    }

    [Fact]
    public void ReportableNetwork_IPv4Range_False()
    {
        Assert.False(Server.Network.Firewall.TryGetReportableNetwork(new CidrFirewallEntry("10.0.0.0/24"), out _));
    }

    [Fact]
    public void ReportableNetwork_UsesEntryRange()
    {
        var entry = new CidrFirewallEntry("2001:db8:1:200::/56");
        Assert.True(Server.Network.Firewall.TryGetReportableNetwork(entry, out var network));
        Assert.Equal(56, network.PrefixLength);
        Assert.Equal(Ip("2001:db8:1:200::"), network.BaseAddress);
    }

    [Fact]
    public void ReportableNetwork_IPv6Slash32_False()
    {
        Assert.False(Server.Network.Firewall.TryGetReportableNetwork(new CidrFirewallEntry("2001:db8::/32"), out _));
    }

    [Fact]
    public void ReportableNetwork_IPv6NonCidrRange_False()
    {
        var entry = new CidrFirewallEntry(Ip("2001:db8:1:2::1"), Ip("2001:db8:1:2::3"));
        Assert.False(Server.Network.Firewall.TryGetReportableNetwork(entry, out _));
    }
}
