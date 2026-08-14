/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AutoDenylistConfiguration.cs                                    *
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
/// Loads the <see cref="AutoDenylistSettings"/> from <c>Configuration/auto-denylist.json</c>. Loaded once;
/// a missing file writes a template so operators have something to edit.
/// </summary>
public static class AutoDenylistConfiguration
{
    private const string _path = "Configuration/auto-denylist.json";

    public static AutoDenylistSettings Settings { get; private set; }

    public static void Load()
    {
        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<AutoDenylistSettings>(path);
        }
        else
        {
            Settings = new AutoDenylistSettings();
            Save();
        }
    }

    private static void Save()
    {
        JsonConfig.Serialize(Path.Join(Core.BaseDirectory, _path), Settings);
    }
}

/// <summary>Bound configuration for <see cref="AutoDenylist"/>.</summary>
public record AutoDenylistSettings
{
    /// <summary>Whether behavioural detections are held locally. Disabled makes the filter inert.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How long an address is denied at accept after the shard catches it misbehaving. Deliberately
    /// independent of the duration reported to external bouncers: this is a local holding pen, not a ban.
    /// </summary>
    /// <remarks>
    /// Short on purpose: it covers the gap before an OS bouncer reacts, and blunts a flood on shards running
    /// none. An address still attacking is simply re-detected and re-added, so the list sustains itself while
    /// a mistake clears on its own.
    /// </remarks>
    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Hard cap on tracked addresses: a distinct-source flood is the case this exists for, so the cap is what
    /// stops it becoming the exhaustion it prevents. At the cap new addresses are not tracked, but are still
    /// disconnected by whichever gate detected them.
    /// </summary>
    /// <remarks>
    /// A <c>HashSet</c> capacity, not a round number. Grown from empty it steps 36,353 → 75,431 → 156,437
    /// → 324,449, so this fills one exactly instead of stranding slots: 65,536 sat just past a resize and
    /// left 9,895 of them unusable. Sized to cover the 50k–250k distinct-source floods seen in practice,
    /// for ~19 MB — 36 bytes a set slot plus 24 for the ring record. Raising it is bounded by memory
    /// rather than by a scan, since holds are retired from the ring in expiry order; a flood past it wants
    /// upstream scrubbing rather than a larger cap.
    /// </remarks>
    [JsonPropertyName("maxEntries")]
    public int MaxEntries { get; set; } = 324_449;
}
