/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LoginAllowlistConfiguration.cs                                  *
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

namespace Server.Network;

/// <summary>
/// Loads the <see cref="LoginAllowlistSettings"/> from <c>Configuration/login-allowlist.json</c>. Loaded
/// once; a missing file writes a template so operators have something to edit.
/// </summary>
public static class LoginAllowlistConfiguration
{
    private const string _path = "Configuration/login-allowlist.json";

    public static LoginAllowlistSettings Settings { get; private set; }

    public static void Load()
    {
        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<LoginAllowlistSettings>(path);
        }
        else
        {
            Settings = new LoginAllowlistSettings();
            Save();
        }
    }

    private static void Save()
    {
        JsonConfig.Serialize(Path.Join(Core.BaseDirectory, _path), Settings);
    }
}

/// <summary>
/// Bound configuration for <see cref="LoginAllowlist"/>: which addresses have recently proven they carry a
/// real player, how long that proof counts, and how much misbehaviour revokes it.
/// </summary>
/// <remarks>
/// The recency window is the point. Consumer addresses are reassigned constantly, and on CGNAT the same
/// address fronts a different subscriber week to week, so a list without a TTL becomes a list of strangers.
/// </remarks>
public record LoginAllowlistSettings
{
    /// <summary>Whether successful logins are recorded and consulted at all. Disabled makes the list inert.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Where the list is persisted. A relative path resolves against <see cref="Core.BaseDirectory"/>.
    /// Plain text, one <c>address unix-seconds</c> pair per line, so it can be read and edited by hand.
    /// Set to <c>""</c> to disable.
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; } = "Configuration/login-allowlist.txt";

    /// <summary>
    /// How long a successful login allowlists its address; dropped on the next flush after that. 90 days
    /// covers a player who takes a season off without carrying a reassigned address indefinitely.
    /// </summary>
    [JsonPropertyName("ttl")]
    public TimeSpan Ttl { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// How often a changed list is written out. A clean shutdown always writes, so this only bounds what a
    /// crash loses — and an entry is re-earned by the next login. Hourly against a 90-day TTL, because each
    /// flush walks the whole list on the game loop.
    /// </summary>
    [JsonPropertyName("flushInterval")]
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How many suppressed contributions inside <see cref="StrikeWindow"/> revoke an address's entry. Past
    /// this it escalates like anything else until it earns a new entry by logging in again.
    /// </summary>
    /// <remarks>
    /// Generous on purpose: local defenses never stop applying, so a high threshold only delays the external
    /// ban. A bad line might trip a gate a few times an hour; a host being used to flood burns through this
    /// in seconds. Set to 0 to never revoke.
    /// </remarks>
    [JsonPropertyName("escalateAfterStrikes")]
    public int EscalateAfterStrikes { get; set; } = 10;

    /// <summary>Rolling window the strike count is measured over. A quiet hour clears the tally.</summary>
    [JsonPropertyName("strikeWindow")]
    public TimeSpan StrikeWindow { get; set; } = TimeSpan.FromHours(1);
}
