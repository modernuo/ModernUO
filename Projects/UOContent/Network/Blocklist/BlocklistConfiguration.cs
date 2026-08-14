/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistConfiguration.cs                                       *
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
/// Loads the <see cref="BlocklistSettings"/> from <c>Configuration/blocklist.json</c>. Loaded once; a
/// missing file writes a template so operators have something to edit.
/// </summary>
public static class BlocklistConfiguration
{
    private const string _path = "Configuration/blocklist.json";

    public static BlocklistSettings Settings { get; private set; }

    public static void Load()
    {
        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<BlocklistSettings>(path);
        }
        else
        {
            Settings = new BlocklistSettings();
            Save();
        }
    }

    private static void Save()
    {
        JsonConfig.Serialize(Path.Join(Core.BaseDirectory, _path), Settings);
    }
}

/// <summary>
/// Bound configuration for <see cref="BlocklistFilter"/>. The filter is inert unless <see cref="Enabled"/>
/// is set and <see cref="File"/> points at a list that exists, so a shard that never runs the generator
/// pays nothing for the defaults.
/// </summary>
public record BlocklistSettings
{
    /// <summary>
    /// Whether the accept-path gate runs at all. Off by default: the reload poll runs for the whole
    /// uptime, which no shard should pay before an operator has chosen to run a blocklist.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Path to the blocklist. A relative path resolves against <see cref="Core.BaseDirectory"/>; an
    /// absolute path is used as-is (handy when several shards share one generated list). Set to
    /// <c>""</c> to disable the gate entirely. Produce the file with <c>tools/Export-IpBlocklist.ps1</c>.
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; } = "Configuration/ip-blocklist.txt";

    /// <summary>How often the file is checked for changes. Reloads only happen when it actually changed.</summary>
    [JsonPropertyName("reloadInterval")]
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Whether blocklist hits are contributed to the ban channel (the demand-paging promotion).</summary>
    [JsonPropertyName("reportHits")]
    public bool ReportHits { get; set; } = true;

    /// <summary>Duration reported for a blocklist-matched ban.</summary>
    [JsonPropertyName("banDuration")]
    public TimeSpan BanDuration { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// How long the accept-path guard suppresses re-reporting a promoted address. This only needs to
    /// bridge the gap until the OS bouncer picks up the promotion (seconds); after that the kernel drops
    /// repeat traffic. Decoupled from <see cref="BanDuration"/> so the guard doesn't have to remember
    /// hours' worth of distinct addresses.
    /// </summary>
    [JsonPropertyName("promoteSuppression")]
    public TimeSpan PromoteSuppression { get; set; } = TimeSpan.FromSeconds(60);
}
