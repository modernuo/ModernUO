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

[Collection("Sequential Server Tests")]
public class FirewallTests
{
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
    public void ReadFirewallSet_SurfacesAddedEntries()
    {
        Server.Network.Firewall.ResetForTesting();
        var entry = new SingleIpFirewallEntry(Ip("1.2.3.4"));
        Server.Network.Firewall.Add(entry);

        Server.Network.Firewall.ReadFirewallSet(set => Assert.Contains(entry, set));
    }
}
