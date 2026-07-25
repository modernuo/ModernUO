/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistFileTests.cs                                           *
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
using System.IO;
using System.Net;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.Bans.Blocklist;

public class BlocklistFileTests
{
    private static string WriteTemp(string content)
    {
        var p = Path.Combine(Path.GetTempPath(), "bl-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(p, content);
        return p;
    }

    [Fact]
    public void Reads_header_generated_and_count()
    {
        var p = WriteTemp("# modernuo-blocklist v1 generated=2026-07-21T09:24:23Z count=2\n1.2.3.4\n5.6.7.0/24\n");
        Assert.True(BlocklistFile.TryReadHeader(p, out var h));
        Assert.True(h.Present);
        Assert.Equal("2026-07-21T09:24:23Z", h.Generated);
        Assert.Equal(2, h.Count);
        File.Delete(p);
    }

    [Fact]
    public void Missing_file_reports_absent_and_loads_empty()
    {
        var p = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N"));
        Assert.False(BlocklistFile.TryReadHeader(p, out var h));
        Assert.False(h.Present);
        var snap = BlocklistFile.Load(p, out _, out _);
        Assert.False(snap.IsBanned(IPAddress.Parse("1.2.3.4")));
    }

    // Pins the exact line tools/Export-IpBlocklist.ps1 emits: the producer adds informational tokens the
    // reader doesn't know about, and the reload detector breaks silently if generated= stops being read.
    [Fact]
    public void Reads_generator_header_with_extra_tokens()
    {
        var p = WriteTemp(
            "# modernuo-blocklist generated=2026-07-25T18:03:11Z count=12442 ipv4=7868 cidr=4574 feeds=2\n" +
            "2.181.183.77\n223.169.0.0/16\n"
        );

        Assert.True(BlocklistFile.TryReadHeader(p, out var h));
        Assert.Equal("2026-07-25T18:03:11Z", h.Generated);
        Assert.Equal(12442, h.Count);

        var snap = BlocklistFile.Load(p, out var parsed, out var skipped);
        Assert.Equal(2, parsed);
        Assert.Equal(0, skipped);
        Assert.True(snap.IsBanned(IPAddress.Parse("2.181.183.77")));
        Assert.True(snap.IsBanned(IPAddress.Parse("223.169.4.9")));
        File.Delete(p);
    }

    [Fact]
    public void Load_parses_body()
    {
        var p = WriteTemp("# generated=x count=1\n8.8.8.0/24\n");
        var snap = BlocklistFile.Load(p, out var parsed, out _);
        Assert.Equal(1, parsed);
        Assert.True(snap.IsBanned(IPAddress.Parse("8.8.8.8")));
        File.Delete(p);
    }
}
