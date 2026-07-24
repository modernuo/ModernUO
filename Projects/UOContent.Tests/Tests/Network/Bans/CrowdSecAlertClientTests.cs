/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecAlertClientTests.cs                                     *
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
using Server.Network.Bans.CrowdSec;
using Xunit;

namespace Server.Tests.Network.Bans;

public class CrowdSecAlertClientTests
{
    [Fact]
    public void BuildDeleteQuery_EscapesOrigin()
    {
        // origin is operator-controlled config (crowdsec.json), so it must be treated as untrusted
        // input going into the URL, same as any other interpolated value.
        var query = CrowdSecAlertClient.BuildDeleteQuery("modern uo/test&x=1", IPAddress.Parse("1.2.3.4"));

        Assert.Equal("/v1/decisions?origin=modern%20uo%2Ftest%26x%3D1&ip=1.2.3.4", query);
    }

    [Fact]
    public void BuildDeleteQuery_PlainOrigin_Ipv4()
    {
        var query = CrowdSecAlertClient.BuildDeleteQuery("modernuo", IPAddress.Parse("192.168.1.1"));

        Assert.Equal("/v1/decisions?origin=modernuo&ip=192.168.1.1", query);
    }

    [Fact]
    public void BuildDeleteQuery_Ipv6_IsEscaped()
    {
        // IPv6 textual form contains ':', which Uri.EscapeDataString percent-encodes like any other
        // reserved character — confirms the escaping is applied uniformly, not just for IPv4.
        var query = CrowdSecAlertClient.BuildDeleteQuery("modernuo", IPAddress.Parse("2001:db8::1"));

        Assert.Equal("/v1/decisions?origin=modernuo&ip=2001%3Adb8%3A%3A1", query);
    }
}
