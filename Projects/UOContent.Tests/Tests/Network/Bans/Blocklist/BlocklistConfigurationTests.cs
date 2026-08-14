/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistConfigurationTests.cs                                  *
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
using System.Text.Json;
using Server.Json;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.Bans.Blocklist;

public class BlocklistConfigurationTests
{
    // Locks the JsonConfig casing contract: JsonConfig's options are case-SENSITIVE, so every settings
    // member must carry an explicit [JsonPropertyName("camelCase")] or it silently binds nothing.
    [Fact]
    public void BlocklistSettings_RoundTripsThroughJsonConfig()
    {
        var original = new BlocklistSettings
        {
            Enabled = true,
            File = "D:/shared/ip-blocklist.txt",
            ReloadInterval = TimeSpan.FromMinutes(5),
            ReportHits = false,
            BanDuration = TimeSpan.FromHours(2),
            PromoteSuppression = TimeSpan.FromSeconds(30)
        };

        var json = JsonConfig.Serialize(original);

        Assert.Contains("\"enabled\"", json);
        Assert.Contains("\"file\"", json);
        Assert.Contains("\"reloadInterval\"", json);
        Assert.Contains("\"reportHits\"", json);
        Assert.Contains("\"banDuration\"", json);
        Assert.Contains("\"promoteSuppression\"", json);

        var restored = JsonSerializer.Deserialize<BlocklistSettings>(json, JsonConfig.DefaultOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.Enabled, restored.Enabled);
        Assert.Equal(original.File, restored.File);
        Assert.Equal(original.ReloadInterval, restored.ReloadInterval);
        Assert.Equal(original.ReportHits, restored.ReportHits);
        Assert.Equal(original.BanDuration, restored.BanDuration);
        Assert.Equal(original.PromoteSuppression, restored.PromoteSuppression);
    }

    // The point of the flag: a shard that never opts in must not start the reload poll.
    [Fact]
    public void Blocklist_is_off_by_default()
    {
        Assert.False(new BlocklistSettings().Enabled);
    }

    // The generator (tools/Export-IpBlocklist.ps1) writes to this path by default; if one side moves
    // without the other, a shard silently enforces nothing.
    [Fact]
    public void Default_file_matches_the_generator_output_path()
    {
        Assert.Equal("Configuration/ip-blocklist.txt", new BlocklistSettings().File);
    }
}
