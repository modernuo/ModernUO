/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ManualAllowlistConfigurationTests.cs                              *
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

namespace Server.Tests.Network.ManualAllowlists;

public class ManualAllowlistConfigurationTests
{
    // Locks the JsonConfig casing contract: JsonConfig's options are case-SENSITIVE, so every settings
    // member must carry an explicit [JsonPropertyName("camelCase")] or it silently binds nothing.
    [Fact]
    public void ManualAllowlistSettings_RoundTripsThroughJsonConfig()
    {
        var original = new ManualAllowlistSettings
        {
            Enabled = true,
            Files = ["D:/shared/ip-allowlist*.txt"],
            ReloadInterval = TimeSpan.FromMinutes(5)
        };

        var json = JsonConfig.Serialize(original);

        Assert.Contains("\"enabled\"", json);
        Assert.Contains("\"files\"", json);
        Assert.Contains("\"reloadInterval\"", json);

        var restored = JsonSerializer.Deserialize<ManualAllowlistSettings>(json, JsonConfig.DefaultOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.Enabled, restored.Enabled);
        Assert.Equal(original.Files, restored.Files);
        Assert.Equal(original.ReloadInterval, restored.ReloadInterval);
    }

    // The point of the flag: a shard that never opts in must not start the reload poll.
    [Fact]
    public void Manual_allowlist_is_off_by_default()
    {
        Assert.False(new ManualAllowlistSettings().Enabled);
    }

    // The generator creates ip-allowlist.txt beside the blocklist; the wildcard is what picks up a
    // carve-out file (-RefreshCarveouts writes ip-allowlist-starlink.txt) with no config edit.
    [Fact]
    public void Default_pattern_matches_the_generator_output_path()
    {
        Assert.Equal(["Configuration/ip-allowlist*.txt"], new ManualAllowlistSettings().Files);
    }
}
