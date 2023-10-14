/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

    private static ServerSettings _settings;
    private static bool m_Mocked;

    public static List<string> AssemblyDirectories => _settings.AssemblyDirectories;

    public static HashSet<string> DataDirectories => _settings.DataDirectories;

    public static List<IPEndPoint> Listeners => _settings.Listeners;

    public static string ConfigurationFilePath => _relPath;

    public static ClientVersion GetSetting(string key, ClientVersion defaultValue) =>
        _settings.Settings.TryGetValue(key, out var value) ? new ClientVersion(value) : defaultValue;

    public static string GetSetting(string key, string defaultValue) =>
        _settings.Settings.TryGetValue(key, out var value) ? value : defaultValue;

    public static int GetSetting(string key, int defaultValue)
    {
        _settings.Settings.TryGetValue(key, out var strValue);
        return int.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static long GetSetting(string key, long defaultValue)
    {
        _settings.Settings.TryGetValue(key, out var strValue);
        return long.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static bool GetSetting(string key, bool defaultValue)
    {
        _settings.Settings.TryGetValue(key, out var strValue);
        return bool.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static TimeSpan GetSetting(string key, TimeSpan defaultValue)
    {
        _settings.Settings.TryGetValue(key, out var strValue);
        return TimeSpan.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static T GetSetting<T>(string key, T defaultValue) where T : struct, Enum
    {
        _settings.Settings.TryGetValue(key, out var strValue);
        return Enum.TryParse(strValue, out T value) ? value : defaultValue;
    }

    public static double GetSetting(string key, double defaultValue)
    {
        _settings.Settings.TryGetValue(key, out var strValue);
        return double.TryParse(strValue, out var value) ? value : defaultValue;
    }

    public static T? GetSetting<T>(string key) where T : struct, Enum
    {
        if (!_settings.Settings.TryGetValue(key, out var strValue))
        {
            return null;
        }

        return Enum.TryParse(strValue, out T value) ? value : null;
    }

    public static string GetOrUpdateSetting(string key, string defaultValue)
    {
        if (_settings.Settings.TryGetValue(key, out var value))
        {
            return value;
        }

        SetSetting(key, value = defaultValue);
        return value;
    }

    public static int GetOrUpdateSetting(string key, int defaultValue)
    {
        int value;

        if (_settings.Settings.TryGetValue(key, out var strValue))
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

        if (_settings.Settings.TryGetValue(key, out var strValue))
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

        if (_settings.Settings.TryGetValue(key, out var strValue))
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

        if (_settings.Settings.TryGetValue(key, out var strValue))
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

        if (_settings.Settings.TryGetValue(key, out var strValue))
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

        if (_settings.Settings.TryGetValue(key, out var strValue))
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
        _settings.Settings[key] = value;
        Save();
    }

    // If mock is enabled we skip the console readline.
    public static void Load(bool mocked = false)
    {
        m_Mocked = mocked;
        var updated = false;

        if (File.Exists(m_FilePath))
        {
            logger.Information("Reading server configuration from {Path}...", _relPath);
            _settings = JsonConfig.Deserialize<ServerSettings>(m_FilePath);

            if (_settings == null)
            {
                logger.Error("Reading server configuration failed");
                throw new FileNotFoundException($"Failed to deserialize {m_FilePath}.");
            }

            logger.Information("Reading server configuration {Status}", "done");
        }
        else
        {
            updated = true;
            _settings = new ServerSettings();
        }

        if (mocked)
        {
            return;
        }

        if (_settings.DataDirectories.Count == 0)
        {
            updated = true;
            foreach (var directory in ServerConfigurationPrompts.GetDataDirectories())
            {
                _settings.DataDirectories.Add(directory);
            }
        }

        UOClient.Load();
        var cuoClientFiles = UOClient.CuoSettings?.UltimaOnlineDirectory;

        if (cuoClientFiles != null)
        {
            DataDirectories.Add(cuoClientFiles);
        }

        if (_settings.Listeners.Count == 0)
        {
            updated = true;
            _settings.Listeners.AddRange(ServerConfigurationPrompts.GetListeners());
        }

        // We have a known, current expansion, so we can deserialize it from Configuration
        if (!ExpansionInfo.LoadConfiguration(out var currentExpansion))
        {
            currentExpansion = (_settings.Data.Remove("expansion", out var el)
                ? el.ToObject<Expansion?>(JsonConfig.DefaultOptions)
                : null) ?? ExpansionConfigurationPrompts.GetExpansion();

            // We've updated the selected expansion, so choose the maps we want from it, then store and save our selection
            var selectedMaps = ExpansionConfigurationPrompts.GetSelectedMaps(currentExpansion);
            ExpansionInfo.StoreMapSelection(selectedMaps, currentExpansion);

            updated = true;
        }

        Core.Expansion = currentExpansion;

        if (updated)
        {
            Save();
            Console.Write("Server configuration saved to ");
            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine($"{_relPath}.");
            Utility.PopColor();

            // Either the expansion.json file has never existed, or it's been deleted, so create it now
            ExpansionInfo.SaveConfiguration();

            Console.Write("Expansion configuration saved to ");
            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine($"{ExpansionInfo.ExpansionConfigurationPath}.");
            Utility.PopColor();
        }
    }

    public static void Save()
    {
        if (m_Mocked)
        {
            return;
        }

        JsonConfig.Serialize(m_FilePath, _settings);
    }
}
