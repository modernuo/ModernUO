/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecAlert.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text.Json.Serialization;

namespace Server.Network.Bans.CrowdSec;

/// <summary>One CrowdSec alert (<c>POST /v1/alerts</c> takes an array of these).</summary>
public sealed class CrowdSecAlert
{
    [JsonPropertyName("scenario")] public string Scenario { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; }
    [JsonPropertyName("events_count")] public int EventsCount { get; set; } = 1;
    [JsonPropertyName("start_at")] public string StartAt { get; set; }
    [JsonPropertyName("stop_at")] public string StopAt { get; set; }
    [JsonPropertyName("capacity")] public int Capacity { get; set; }
    [JsonPropertyName("leakspeed")] public string LeakSpeed { get; set; } = "0s";
    [JsonPropertyName("simulated")] public bool Simulated { get; set; }
    [JsonPropertyName("events")] public object[] Events { get; set; } = [];
    [JsonPropertyName("remediation")] public bool Remediation { get; set; } = true;
    [JsonPropertyName("source")] public CrowdSecSource Source { get; set; }
    [JsonPropertyName("decisions")] public CrowdSecDecisionDto[] Decisions { get; set; }
}

public sealed class CrowdSecSource
{
    [JsonPropertyName("scope")] public string Scope { get; set; } = "Ip";
    [JsonPropertyName("value")] public string Value { get; set; }
}

public sealed class CrowdSecDecisionDto
{
    [JsonPropertyName("origin")] public string Origin { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "ban";
    [JsonPropertyName("scope")] public string Scope { get; set; } = "Ip";
    [JsonPropertyName("value")] public string Value { get; set; }
    [JsonPropertyName("duration")] public string Duration { get; set; }
    [JsonPropertyName("scenario")] public string Scenario { get; set; }
}

/// <summary>Watcher login request/response for <c>POST /v1/watchers/login</c>.</summary>
public sealed class CrowdSecLoginRequest
{
    [JsonPropertyName("machine_id")] public string MachineId { get; set; }
    [JsonPropertyName("password")] public string Password { get; set; }
}

public sealed class CrowdSecLoginResponse
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("token")] public string Token { get; set; }
    [JsonPropertyName("expire")] public string Expire { get; set; }
}
