/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecConfiguration.cs                                        *
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

namespace Server.Network.Bans.CrowdSec;

/// <summary>
/// Loads the <see cref="CrowdSecSettings"/> from <c>Configuration/crowdsec.json</c> (matching the
/// per-feature JSON config pattern used by <c>AssistantConfiguration</c>). Loaded once; a missing file
/// writes a disabled-by-default template so operators have something to edit.
/// </summary>
public static class CrowdSecConfiguration
{
    private const string _path = "Configuration/crowdsec.json";

    public static CrowdSecSettings Settings { get; private set; }

    public static void Configure()
    {
        // Idempotent: this Configure() is auto-discovered and also called explicitly by the reporter,
        // so guard against a redundant second load.
        if (Settings != null)
        {
            return;
        }

        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<CrowdSecSettings>(path);
        }
        else
        {
            Settings = new CrowdSecSettings
            {
                LapiUrl = "http://127.0.0.1:8080",
                MachineId = "",
                Password = "",
                Origin = "modernuo",
                ManualBanDuration = TimeSpan.FromHours(168),
                FlushInterval = TimeSpan.FromSeconds(1),
                MaxQueue = 10000
            };

            Save();
        }
    }

    private static void Save()
    {
        JsonConfig.Serialize(Path.Join(Core.BaseDirectory, _path), Settings);
    }
}

/// <summary>
/// Bound configuration for <see cref="CrowdSecReporter"/>. Read once at <c>Configure()</c>.
/// The reporter is inert unless <see cref="ReportingEnabled"/> is true.
/// </summary>
public record CrowdSecSettings
{
    /// <summary>LAPI endpoint. Default <c>http://127.0.0.1:8080</c>.</summary>
    [JsonPropertyName("lapiUrl")]
    public string LapiUrl { get; set; } = "http://127.0.0.1:8080";

    /// <summary>Watcher machine id from <c>cscli machines add</c>. Empty disables reporting.</summary>
    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = "";

    /// <summary>Watcher password paired with <see cref="MachineId"/>.</summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    /// <summary>Decision <c>origin</c> stamped on our contributions. Default <c>modernuo</c>.</summary>
    [JsonPropertyName("origin")]
    public string Origin { get; set; } = "modernuo";

    /// <summary>Duration for manual admin bans pushed to CrowdSec. Default 168h (renewable). Finite so a missed retract self-heals.</summary>
    [JsonPropertyName("manualBanDuration")]
    public TimeSpan ManualBanDuration { get; set; } = TimeSpan.FromHours(168);

    /// <summary>Max time the drain coalesces before flushing a batch. Default 1s.</summary>
    [JsonPropertyName("flushInterval")]
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Bounded contribution queue capacity; overflow is dropped (counted). Default 10000.</summary>
    [JsonPropertyName("maxQueue")]
    public int MaxQueue { get; set; } = 10000;

    [JsonIgnore]
    public bool ReportingEnabled => !string.IsNullOrWhiteSpace(MachineId) && !string.IsNullOrWhiteSpace(Password);
}
