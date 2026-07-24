/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistAcceptGateTests.cs                                     *
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
using Server.Network.Bans.Blocklist;
using Xunit;

namespace Server.Tests.Network.Bans.Blocklist;

[Collection("Sequential Server Tests")]
public class BlocklistAcceptGateTests
{
    [Fact]
    public void Blocklisted_ip_denied_and_reported_once()
    {
        FileBlocklist.LoadForTesting(BlocklistSnapshot.Build(System.Text.Encoding.ASCII.GetBytes("1.2.3.4"), out _, out _));
        var guard = new PromotedGuard();
        var ip = IPAddress.Parse("1.2.3.4");

        var deny1 = BlocklistGate.Evaluate(ip, false, guard, 1000, true, 5000, out var r1);
        var deny2 = BlocklistGate.Evaluate(ip, false, guard, 1500, true, 5000, out var r2);

        Assert.True(deny1);
        Assert.True(r1);
        Assert.True(deny2);
        Assert.False(r2); // denied both, reported once within TTL
    }

    [Fact]
    public void Whitelisted_and_clean_ips_pass()
    {
        FileBlocklist.LoadForTesting(BlocklistSnapshot.Build(System.Text.Encoding.ASCII.GetBytes("1.2.3.4"), out _, out _));
        var guard = new PromotedGuard();

        Assert.False(BlocklistGate.Evaluate(IPAddress.Parse("1.2.3.4"), true, guard, 1, true, 5000, out _));
        Assert.False(BlocklistGate.Evaluate(IPAddress.Parse("9.9.9.9"), false, guard, 1, true, 5000, out _));
    }
}
