/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistFilterTests.cs                                         *
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
using System.Text;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.Bans.Blocklist;

// No [Collection] and no static reset hook: the filter is an instance, so each test owns its own
// snapshot and promote-guard. That is the point of it no longer being a static class.
public class BlocklistFilterTests
{
    private static BlocklistFilter WithList(string list, bool reportHits = true, long suppressionMs = 5000)
    {
        var filter = new BlocklistFilter();
        filter.LoadForTesting(
            BlocklistSnapshot.Build(Encoding.ASCII.GetBytes(list), out _, out _),
            reportHits,
            suppressionMs
        );

        return filter;
    }

    [Fact]
    public void Listed_address_is_denied_and_reported_once_per_window()
    {
        var filter = WithList("1.2.3.4");
        var ip = IPAddress.Parse("1.2.3.4");

        var deny1 = filter.Evaluate(ip, 1000, out var report1);
        var deny2 = filter.Evaluate(ip, 1500, out var report2);
        var deny3 = filter.Evaluate(ip, 1000 + 5001, out var report3);

        Assert.True(deny1);
        Assert.True(report1);

        Assert.True(deny2);
        Assert.False(report2); // denied again, but promotion suppressed inside the window

        Assert.True(deny3);
        Assert.True(report3); // window elapsed, promotion may be retried
    }

    [Fact]
    public void Unlisted_address_passes()
    {
        var filter = WithList("1.2.3.4");

        Assert.False(filter.Evaluate(IPAddress.Parse("9.9.9.9"), 1, out var report));
        Assert.False(report);
    }

    [Fact]
    public void Cidr_membership_is_honored()
    {
        var filter = WithList("10.20.30.0/24");

        Assert.True(filter.Evaluate(IPAddress.Parse("10.20.30.255"), 1, out _));
        Assert.False(filter.Evaluate(IPAddress.Parse("10.20.31.0"), 1, out _));
    }

    [Fact]
    public void ReportHits_disabled_still_denies_but_never_promotes()
    {
        var filter = WithList("1.2.3.4", reportHits: false);

        Assert.True(filter.Evaluate(IPAddress.Parse("1.2.3.4"), 1, out var report));
        Assert.False(report);
    }

    [Fact]
    public void Unconfigured_filter_denies_nothing()
    {
        var filter = new BlocklistFilter();

        Assert.Equal(0, filter.Count);
        Assert.False(filter.ShouldDeny(IPAddress.Parse("8.8.8.8")));
    }
}
