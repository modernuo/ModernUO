/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistSnapshotTests.cs                                       *
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

public class BlocklistSnapshotTests
{
    private static BlocklistSnapshot Build(params string[] lines) =>
        BlocklistSnapshot.Build(System.Text.Encoding.ASCII.GetBytes(string.Join('\n', lines)), out _, out _);

    [Fact]
    public void Single_ip_is_matched()
    {
        var s = Build("1.2.3.4");
        Assert.True(s.IsBanned(IPAddress.Parse("1.2.3.4")));
        Assert.False(s.IsBanned(IPAddress.Parse("1.2.3.5")));
    }

    [Fact]
    public void Cidr_contains_and_excludes_boundaries()
    {
        var s = Build("10.0.0.0/24");
        Assert.True(s.IsBanned(IPAddress.Parse("10.0.0.0")));
        Assert.True(s.IsBanned(IPAddress.Parse("10.0.0.255")));
        Assert.False(s.IsBanned(IPAddress.Parse("10.0.1.0")));
        Assert.False(s.IsBanned(IPAddress.Parse("9.255.255.255")));
    }

    [Fact]
    public void Comments_blanks_and_garbage_are_skipped_not_thrown()
    {
        var s = BlocklistSnapshot.Build(
            System.Text.Encoding.ASCII.GetBytes(
                string.Join('\n', "# header generated=x", "", "not-an-ip", "1.2.3.4", "::1", "5.6.7.0/24")),
            out var parsed, out var skipped);
        Assert.Equal(3, parsed);      // 1.2.3.4 + ::1 (valid loopback) + 5.6.7.0/24
        Assert.True(skipped >= 1);    // "not-an-ip"; blank/comment lines are silently skipped, not counted
        Assert.True(s.IsBanned(IPAddress.Parse("5.6.7.200")));
    }

    [Fact]
    public void Empty_snapshot_matches_nothing()
    {
        Assert.False(BlocklistSnapshot.Empty.IsBanned(IPAddress.Parse("1.2.3.4")));
    }

    [Fact]
    public void Ipv6_single_and_cidr_are_matched()
    {
        var s = Build("2001:db8::1", "2001:db8:1::/48");
        Assert.True(s.IsBanned(IPAddress.Parse("2001:db8::1")));
        Assert.True(s.IsBanned(IPAddress.Parse("2001:db8:1::abcd")));
        Assert.False(s.IsBanned(IPAddress.Parse("2001:db8:2::1")));
    }

    [Fact]
    public void Ipv4_mapped_ipv6_is_normalized_to_v4()
    {
        var s = Build("1.2.3.4");
        Assert.True(s.IsBanned(IPAddress.Parse("::ffff:1.2.3.4"))); // must not bypass the v4 set
    }

    [Fact]
    public void Nested_cidr_intervals_are_coalesced()
    {
        // A /32 nested inside a /24: InRange's binary search only inspects the
        // rightmost interval starting <= ip, so without coalescing an IP inside
        // the /24 but outside the /32 would land on the /32 and wrongly pass.
        var s = Build("10.0.0.0/24", "10.0.0.5/32");
        Assert.True(s.IsBanned(IPAddress.Parse("10.0.0.100"))); // inside /24, outside /32
        Assert.True(s.IsBanned(IPAddress.Parse("10.0.0.5")));   // the nested /32 itself
        Assert.False(s.IsBanned(IPAddress.Parse("10.0.1.0")));  // genuinely outside both
    }

    [Fact]
    public void Overlapping_cidr_intervals_are_coalesced()
    {
        var s = Build("10.0.0.0/25", "10.0.0.64/25");
        Assert.True(s.IsBanned(IPAddress.Parse("10.0.0.100")));  // covered by the second /25
        Assert.False(s.IsBanned(IPAddress.Parse("10.0.0.200"))); // outside both
    }
}
