/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AssistantConfiguration.cs                                       *
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
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Assistants;

public static class AssistantConfiguration
{
    private const string _path = "Configuration/assistants.json";

    public static bool Enabled { get; private set; }

    public static AssistantSettings Settings { get; private set; }

    private const string _defaultWarningMessage = "The server was unable to negotiate features with your assistant. "
                                                  + "You must download and run an updated version of <A HREF=\"https://uosteam.com\">UOSteam</A>"
                                                  + " or <A HREF=\"https://github.com/markdwags/Razor/releases/latest\">Razor</A>."
                                                  + "<BR><BR>Make sure you've checked the option <B>Negotiate features with server</B>, "
                                                  + "once you have this box checked you may log in and play normally."
                                                  + "<BR><BR>You will be disconnected shortly.";

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("assistants.enableRazorNegotiation", false);

        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<AssistantSettings>(path);
        }
        else
        {
            Settings = new AssistantSettings
            {
                WarnOnFailure = true,
                KickOnFailure = true,
                DisallowedFeatures = AssistantFeatures.None,
                DisconnectDelay = TimeSpan.FromSeconds(15.0),
                WarningMessage = _defaultWarningMessage
            };

            Save(path);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DisallowFeature(AssistantFeatures feature) => SetDisallowed(feature, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AllowFeature(AssistantFeatures feature) => SetDisallowed(feature, false);

    public static void SetDisallowed(AssistantFeatures feature, bool value)
    {
        if (value)
        {
            Settings.DisallowedFeatures |= feature;
        }
        else
        {
            Settings.DisallowedFeatures &= ~feature;
        }

        Save();
    }

    private static void Save(string path = null)
    {
        path ??= Path.Join(Core.BaseDirectory, _path);
        JsonConfig.Serialize(path, Settings);
    }
}

public record AssistantSettings
{
    [JsonPropertyName("warnOnFailure")]
    public bool WarnOnFailure { get; set; }

    [JsonPropertyName("kickOnFailure")]
    public bool KickOnFailure { get; set; }

    [JsonPropertyName("features")]
    public AssistantFeatures DisallowedFeatures { get; set; }

    [JsonPropertyName("disconnectDelay")]
    public TimeSpan DisconnectDelay { get; set; }

    [JsonPropertyName("warningMessage")]
    public string WarningMessage { get; set; }
}
