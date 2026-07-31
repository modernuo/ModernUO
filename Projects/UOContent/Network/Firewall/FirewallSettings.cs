/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FirewallSettings.cs                                             *
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
using System.Text.Json.Serialization;

namespace Server.Network;

/// <summary>
/// Persisted local firewall entries (manual admin bans). Stored at <c>Configuration/firewall.json</c>.
/// Auto-detected rate-limit trips are never persisted — they are contributed to CrowdSec, not stored here.
/// </summary>
public record FirewallSettings
{
    [JsonPropertyName("entries")]
    public FirewallEntryRecord[] Entries { get; set; } = [];
}

/// <summary>
/// One persisted entry. <see cref="Value"/> is a single IP, a <c>min-max</c> range, or CIDR.
/// <see cref="Expires"/> is UTC wall-clock; null means permanent.
/// </summary>
public record FirewallEntryRecord
{
    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("expires")]
    public DateTime? Expires { get; set; }
}
