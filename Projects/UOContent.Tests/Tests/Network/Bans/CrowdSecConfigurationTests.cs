/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecConfigurationTests.cs                                   *
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
using Server.Network.Bans.CrowdSec;
using Xunit;

namespace Server.Tests.Network.Bans;

public class CrowdSecConfigurationTests
{
    // Locks the JsonConfig casing/converter contract: JsonConfig's options are case-SENSITIVE, so
    // every settings member must carry an explicit [JsonPropertyName("camelCase")] or it silently
    // binds nothing. These tests round-trip through the exact options the loader uses.

    [Fact]
    public void CrowdSecSettings_RoundTripsThroughJsonConfig()
    {
        var original = new CrowdSecSettings
        {
            LapiUrl = "http://10.0.0.5:9090",
            MachineId = "shard",
            Password = "secret",
            Origin = "modernuo",
            ManualBanDuration = TimeSpan.FromHours(168),
            FlushInterval = TimeSpan.FromSeconds(1),
            MaxQueue = 10000
        };

        var json = JsonConfig.Serialize(original);

        // camelCase property names must be present (not PascalCase) or the case-sensitive reader binds nothing.
        Assert.Contains("\"lapiUrl\"", json);
        Assert.Contains("\"machineId\"", json);
        Assert.Contains("\"password\"", json);
        Assert.Contains("\"origin\"", json);
        Assert.Contains("\"manualBanDuration\"", json);
        Assert.Contains("\"flushInterval\"", json);
        Assert.Contains("\"maxQueue\"", json);

        var restored = JsonSerializer.Deserialize<CrowdSecSettings>(json, JsonConfig.DefaultOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.LapiUrl, restored.LapiUrl);
        Assert.Equal(original.MachineId, restored.MachineId);
        Assert.Equal(original.Password, restored.Password);
        Assert.Equal(original.Origin, restored.Origin);
        Assert.Equal(original.ManualBanDuration, restored.ManualBanDuration); // TimeSpan survives
        Assert.Equal(original.FlushInterval, restored.FlushInterval);        // TimeSpan survives
        Assert.Equal(original.MaxQueue, restored.MaxQueue);
        Assert.True(restored.ReportingEnabled);
    }

    [Fact]
    public void CrowdSecSettings_Defaults_AreReportingDisabledLocalLoopback()
    {
        var settings = new CrowdSecSettings();

        Assert.Equal("http://127.0.0.1:8080", settings.LapiUrl);
        Assert.Equal("", settings.MachineId);
        Assert.Equal("", settings.Password);
        Assert.Equal("modernuo", settings.Origin);
        Assert.Equal(TimeSpan.FromHours(168), settings.ManualBanDuration);
        Assert.Equal(TimeSpan.FromSeconds(1), settings.FlushInterval);
        Assert.Equal(10000, settings.MaxQueue);
        Assert.False(settings.ReportingEnabled); // empty machineId/password => inert
    }
}
