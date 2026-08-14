/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ManualAllowlistConfiguration.cs                                   *
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
/// Loads the <see cref="ManualAllowlistSettings"/> from <c>Configuration/ip-allowlist.json</c>. Loaded once;
/// a missing file writes a template so operators have something to edit.
/// </summary>
public static class ManualAllowlistConfiguration
{
    private const string _path = "Configuration/ip-allowlist.json";

    public static ManualAllowlistSettings Settings { get; private set; }

    public static void Load()
    {
        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<ManualAllowlistSettings>(path);
        }
        else
        {
            Settings = new ManualAllowlistSettings();
            Save();
        }
    }

    private static void Save()
    {
        JsonConfig.Serialize(Path.Join(Core.BaseDirectory, _path), Settings);
    }
}

/// <summary>
/// Bound configuration for <see cref="ManualAllowlist"/>. Its own file rather than a corner of
/// <c>blocklist.json</c>: the blocklist is only one of two consumers, and the other
/// (<see cref="BanExemptions"/>) works on a shard that runs no blocklist at all.
/// </summary>
public record ManualAllowlistSettings
{
    /// <summary>
    /// Whether the shard reads <see cref="Files"/> at all. Off by default: reading them costs a poll for
    /// the whole uptime, which no shard should pay before an operator has written a carve-out.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Addresses that must never be blocked and never escalated, in the blocklist's own format. The same
    /// files <c>tools/Export-IpBlocklist.ps1</c> subtracts at generation time; the shard reads them so an
    /// entry also suppresses ban contributions, which the generator alone cannot do.
    /// </summary>
    /// <remarks>
    /// The filename may contain wildcards, which is how the default picks up a carve-out an admin adds
    /// without anyone editing this file.
    /// </remarks>
    [JsonPropertyName("files")]
    public string[] Files { get; set; } = ["Configuration/ip-allowlist*.txt"];

    /// <summary>How often the files are checked for changes. Reloads only happen when one actually changed.</summary>
    [JsonPropertyName("reloadInterval")]
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromSeconds(60);
}
