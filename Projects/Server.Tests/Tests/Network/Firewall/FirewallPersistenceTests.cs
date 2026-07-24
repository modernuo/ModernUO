/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FirewallPersistenceTests.cs                                     *
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
public class FirewallPersistenceTests
{
    [Fact]
    public void ToSettings_RoundTrips_PermanentAndTtl()
    {
        Server.Network.Firewall.ResetForTesting();
        Server.Network.Firewall.Add(new SingleIpFirewallEntry(IPAddress.Parse("1.2.3.4")));
        Server.Network.Firewall.Add(new SingleIpFirewallEntry(IPAddress.Parse("2.2.2.2")), TimeSpan.FromHours(1));

        var settings = Server.Network.Firewall.ToSettings();

        Server.Network.Firewall.ResetForTesting();
        Server.Network.Firewall.LoadFrom(settings);

        Assert.True(Server.Network.Firewall.IsBlocked(IPAddress.Parse("1.2.3.4")));
        Assert.True(Server.Network.Firewall.IsBlocked(IPAddress.Parse("2.2.2.2")));
    }

    [Fact]
    public void LoadFrom_SkipsAlreadyExpired()
    {
        Server.Network.Firewall.ResetForTesting();
        var settings = new FirewallSettings
        {
            Entries =
            [
                new FirewallEntryRecord { Value = "9.9.9.9", Expires = DateTime.UtcNow.AddHours(-1) }
            ]
        };

        Server.Network.Firewall.LoadFrom(settings);

        Assert.False(Server.Network.Firewall.IsBlocked(IPAddress.Parse("9.9.9.9")));
    }

    [Fact]
    public void ToSettings_OmitsExpiryForPermanent()
    {
        Server.Network.Firewall.ResetForTesting();
        Server.Network.Firewall.Add(new SingleIpFirewallEntry(IPAddress.Parse("1.2.3.4")));

        var settings = Server.Network.Firewall.ToSettings();

        Assert.Single(settings.Entries);
        Assert.Null(settings.Entries[0].Expires);
        Assert.Equal("1.2.3.4", settings.Entries[0].Value);
    }
}
