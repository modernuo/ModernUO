using System;
using System.Text.Json;
using Server.Json;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests;

public class BanConfigurationTests
{
    // Locks the JsonConfig casing/converter contract: JsonConfig's options are case-SENSITIVE, so
    // every settings member must carry an explicit [JsonPropertyName("camelCase")] or it silently
    // binds nothing. These tests round-trip through the exact options the loader uses.

    [Fact]
    public void BanSettings_RoundTripsThroughJsonConfig()
    {
        var original = new BanSettings
        {
            ReportRateLimitTrips = false,
            AutoBanDuration = TimeSpan.FromHours(2)
        };

        var json = JsonConfig.Serialize(original);

        Assert.Contains("\"reportRateLimitTrips\"", json);
        Assert.Contains("\"autoBanDuration\"", json);

        var restored = JsonSerializer.Deserialize<BanSettings>(json, JsonConfig.DefaultOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.ReportRateLimitTrips, restored.ReportRateLimitTrips);
        Assert.Equal(original.AutoBanDuration, restored.AutoBanDuration); // TimeSpan survives
    }

    [Fact]
    public void BanSettings_Defaults_AreReportRateLimitTripsFourHourAutoBan()
    {
        var settings = new BanSettings();

        Assert.True(settings.ReportRateLimitTrips);
        Assert.Equal(TimeSpan.FromHours(4), settings.AutoBanDuration);
    }
}
