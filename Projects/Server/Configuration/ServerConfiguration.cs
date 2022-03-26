/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ServerConfiguration.cs                                          *
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Json;
using Server.Logging;

namespace Server;

public static class ServerConfiguration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ServerConfiguration));

    private const string _relPath = "Configuration/modernuo.json";
    private static readonly string m_FilePath = Path.Join(Core.BaseDirectory, _relPath);
    private static ServerSettings m_Settings;
    private static bool m_Mocked;

    public static List<string> AssemblyDirectories => m_Settings.AssemblyDirectories;

    public static HashSet<string> DataDirectories => m_Settings.DataDirectories;

    public static List<IPEndPoint> Listeners => m_Settings.Listeners;

    public static ClientVersion GetSetting(string key, ClientVersion defaultValue) =>
        m_Settings.Settings.TryGetValue(key, out var value) ? new ClientVersion(value) : defaultValue;

    public static string GetSetting(string key, string defaultValue) =>
        m_Settings.Settings.TryGetValue(key, out var value) ? value : defaultValue;

    public static int GetSetting(string key, int defaultValue)
    {
        m_Settings.Settings.TryGetValue(key, out var strValue);
        return int.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static long GetSetting(string key, long defaultValue)
    {
        m_Settings.Settings.TryGetValue(key, out var strValue);
        return long.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static bool GetSetting(string key, bool defaultValue)
    {
        m_Settings.Settings.TryGetValue(key, out var strValue);
        return bool.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static T GetSetting<T>(string key, T defaultValue) where T : struct, Enum
    {
        m_Settings.Settings.TryGetValue(key, out var strValue);
        return Enum.TryParse(strValue, out T value) ? value : defaultValue;
    }

    public static double GetSetting(string key, double defaultValue)
    {
        m_Settings.Settings.TryGetValue(key, out var strValue);
        return double.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static T? GetSetting<T>(string key) where T : struct, Enum
    {
        if (!m_Settings.Settings.TryGetValue(key, out var strValue))
        {
            return null;
        }

        return Enum.TryParse(strValue, out T value) ? value : null;
    }

    public static string GetOrUpdateSetting(string key, string defaultValue)
    {
        if (m_Settings.Settings.TryGetValue(key, out var value))
        {
            return value;
        }

        SetSetting(key, value = defaultValue);
        return value;
    }

    public static int GetOrUpdateSetting(string key, int defaultValue)
    {
        int value;

        if (m_Settings.Settings.TryGetValue(key, out var strValue))
        {
            value = int.TryParse(strValue, out value) ? value : defaultValue;
        }
        else
        {
            SetSetting(key, (value = defaultValue).ToString());
        }

        return value;
    }

    public static long GetOrUpdateSetting(string key, long defaultValue)
    {
        long value;

        if (m_Settings.Settings.TryGetValue(key, out var strValue))
        {
            value = long.TryParse(strValue, out value) ? value : defaultValue;
        }
        else
        {
            SetSetting(key, (value = defaultValue).ToString());
        }

        return value;
    }

    public static bool GetOrUpdateSetting(string key, bool defaultValue)
    {
        bool value;

        if (m_Settings.Settings.TryGetValue(key, out var strValue))
        {
            value = bool.TryParse(strValue, out value) ? value : defaultValue;
        }
        else
        {
            SetSetting(key, (value = defaultValue).ToString());
        }

        return value;
    }

    public static TimeSpan GetOrUpdateSetting(string key, TimeSpan defaultValue)
    {
        TimeSpan value;

        if (m_Settings.Settings.TryGetValue(key, out var strValue))
        {
            value = TimeSpan.TryParse(strValue, out value) ? value : defaultValue;
        }
        else
        {
            SetSetting(key, (value = defaultValue).ToString());
        }

        return value;
    }

    public static T GetOrUpdateSetting<T>(string key, T defaultValue) where T : struct, Enum
    {
        T value;

        if (m_Settings.Settings.TryGetValue(key, out var strValue))
        {
            value = Enum.TryParse(strValue, out value) ? value : defaultValue;
        }
        else
        {
            SetSetting(key, (value = defaultValue).ToString());
        }

        return value;
    }

    public static double GetOrUpdateSetting(string key, double defaultValue)
    {
        double value;

        if (m_Settings.Settings.TryGetValue(key, out var strValue))
        {
            value = double.TryParse(strValue, out value) ? value : defaultValue;
        }
        else
        {
            SetSetting(key, (value = defaultValue).ToString());
        }

        return value;
    }

    public static void SetSetting(string key, double value) => SetSetting(key, value.ToString());

    public static void SetSetting(string key, TimeSpan value) => SetSetting(key, value.ToString());

    public static void SetSetting(string key, int value) => SetSetting(key, value.ToString());

    public static void SetSetting(string key, long value) => SetSetting(key, value.ToString());

    public static void SetSetting(string key, bool value) => SetSetting(key, value.ToString());

    public static void SetSetting<T>(string key, T value) where T : struct, Enum =>
        SetSetting(key, value.ToString());

    public static void SetSetting(string key, string value)
    {
        m_Settings.Settings[key] = value;
        Save();
    }

    // If mock is enabled we skip the console readline.
    public static void Load(bool mocked = false)
    {
        m_Mocked = mocked;
        var updated = false;

        if (File.Exists(m_FilePath))
        {
            logger.Information($"Reading server configuration from {_relPath}...");
            m_Settings = JsonConfig.Deserialize<ServerSettings>(m_FilePath);

            if (m_Settings == null)
            {
                logger.Error("Reading server configuration failed");
                throw new FileNotFoundException($"Failed to deserialize {m_FilePath}.");
            }

            logger.Information("Reading server configuration done");
        }
        else
        {
            updated = true;
            m_Settings = new ServerSettings();
        }

        if (mocked)
        {
            return;
        }

        if (m_Settings.DataDirectories.Count == 0)
        {
            updated = true;
            foreach (var directory in ServerConfigurationPrompts.GetDataDirectories())
            {
                m_Settings.DataDirectories.Add(directory);
            }
        }

        UOClient.Load();
        var cuoClientFiles = UOClient.CuoSettings?.UltimaOnlineDirectory;

        if (cuoClientFiles != null)
        {
            DataDirectories.Add(cuoClientFiles);
        }

        if (m_Settings.Listeners.Count == 0)
        {
            updated = true;
            m_Settings.Listeners.AddRange(ServerConfigurationPrompts.GetListeners());
        }

        bool? isPre60000 = null;

        if (m_Settings.Expansion == null)
        {
            var expansion = GetSetting<Expansion>("currentExpansion");
            var hasExpansion = expansion != null;

            expansion ??= ServerConfigurationPrompts.GetExpansion();

            if (expansion <= Expansion.ML && !hasExpansion)
            {
                isPre60000 = ServerConfigurationPrompts.GetIsClientPre6000();
                if (isPre60000 == true)
                {
                    SetSetting("maps.enablePre6000Trammel", true.ToString());
                }
            }

            updated = true;
            m_Settings.Expansion = expansion;
        }

        if (isPre60000 != true)
        {
            if (ServerConfigurationPrompts.GetIsClient7090())
            {
                updated = true;
                SetSetting("maps.enablePostHSMultiComponentFormat", true);
            }
        }

        Core.Expansion = m_Settings.Expansion.Value;

        if (updated)
        {
            Save();
            Console.Write("Server configuration saved to ");
            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine($"{_relPath}.");
            Utility.PopColor();
        }
    }

    public static void Save()
    {
        if (m_Mocked)
        {
            return;
        }

        JsonConfig.Serialize(m_FilePath, m_Settings);
    }
}
