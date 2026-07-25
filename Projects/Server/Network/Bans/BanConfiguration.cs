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

    public static BanSettings Settings { get; private set; }

    public static void Configure()
    {
        // Reached by the Configure sweep, which is the only caller. Idempotent anyway, so a second call
        // cannot re-deserialize or re-write the template over an operator's edits.
        if (Settings != null)
        {
            return;
        }

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
}
