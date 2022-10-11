/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ServerSettings.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace Server;

public class ServerSettings
{
    [JsonPropertyName("assemblyDirectories")]
    public List<string> AssemblyDirectories { get; set; } = new();

    [JsonPropertyName("dataDirectories")]
    public HashSet<string> DataDirectories { get; set; } = new();

    [JsonPropertyName("listeners")]
    public List<IPEndPoint> Listeners { get; set; } = new();

    [JsonPropertyName("expansion")]
    public Expansion? Expansion { get; set; }

    [JsonPropertyName("settings")]
    public SortedDictionary<string, string> Settings { get; set; } = new();
}
