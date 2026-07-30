/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BanConfiguration.cs                                             *
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
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Network.Bans;

/// <summary>
/// Loads the <see cref="BanSettings"/> from <c>Configuration/bans.json</c> (matching the per-feature
/// JSON config pattern used by <c>AssistantConfiguration</c>). Loaded once; a missing file writes a
/// local-only, fail-open template so operators have something to edit.
/// </summary>
public static class BanConfiguration
{
    private const string _path = "Configuration/bans.json";

    private static bool _loaded;

    /// <summary>
    /// Never null: the accept and reap paths read this per connection, including before <see cref="Configure"/>
    /// has run (a harness driving <c>NetState.Slice</c> directly), so it starts at the record's defaults.
    /// </summary>
    public static BanSettings Settings { get; private set; } = new();

    public static void Configure()
    {
        // Idempotent; flagged rather than null-checked because Settings is non-null from the start.
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<BanSettings>(path);
        }
        else
        {
            Settings = new BanSettings
            {
                ReportRateLimitTrips = true,
                AutoBanDuration = TimeSpan.FromHours(4)
            };

            Save();
        }
    }

    private static void Save()
    {
        JsonConfig.Serialize(Path.Join(Core.BaseDirectory, _path), Settings);
    }
}

/// <summary>Ban-channel policy: which reporters receive contributions, and how auto-detections are handled.</summary>
public record BanSettings
{
    /// <summary>Whether IP rate-limiter trips are contributed to reporters. They never enter the local firewall set.</summary>
    [JsonPropertyName("reportRateLimitTrips")]
    public bool ReportRateLimitTrips { get; set; } = true;

    /// <summary>Duration reported for an auto-detected (rate-limit) ban.</summary>
    [JsonPropertyName("autoBanDuration")]
    public TimeSpan AutoBanDuration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Whether behavioural detections are contributed to reporters. Those connections are disconnected either
    /// way; this only controls escalation.
    /// </summary>
    /// <remarks>
    /// Keyed on bytes-received, never elapsed time: a connection that sent something and ran out of time is
    /// far more likely a slow link than an attack. See <c>dev-docs/ip-bans-and-allowlists.md</c>.
    /// </remarks>
    [JsonPropertyName("reportBadConnects")]
    public bool ReportBadConnects { get; set; } = true;

    [JsonPropertyName("badConnectDuration")]
    public TimeSpan BadConnectDuration { get; set; } = TimeSpan.FromHours(4);
}
