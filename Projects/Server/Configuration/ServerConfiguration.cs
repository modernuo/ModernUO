/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Configuration.cs - Created: 2019/10/04 - Updated: 2020/01/19    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server
{
  public static class ServerConfiguration
  {
    private const string m_RelPath = "Configuration/modernuo.json";
    private static readonly string m_FilePath = Path.Join(Core.BaseDirectory, m_RelPath);
    private static ServerSettings m_Settings;

    public static List<string> DataDirectories => m_Settings.dataDirectories;
    public static Dictionary<string, string> Settings => m_Settings.settings;
    public static Dictionary<string, object> Metadata => m_Settings.metadata;

    public static void LoadConfiguration()
    {
      bool updated = false;

      if (File.Exists(m_FilePath))
      {
        Console.Write($"Core: Reading configuration from {m_RelPath}...");
        m_Settings = JsonConfig.Deserialize<ServerSettings>(m_FilePath);

        if (m_Settings == null)
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine("failed");
          Console.ResetColor();
          throw new Exception("Core: Server configuration failed to deserialize.");
        }

        Console.WriteLine("done");
      }
      else
      {
        updated = true;
        m_Settings = new ServerSettings();
      }

      if (m_Settings.dataDirectories.Count == 0)
      {
        updated = true;
        Console.WriteLine("Core: Server configuration is missing data directories.");
        m_Settings.dataDirectories.Add(GetDataDirectory());
      }

      if (updated)
        SaveConfiguration();
    }

    internal class ServerSettings
    {
      [JsonPropertyName("dataDirectories")]
      public List<string> dataDirectories { get; set; } = new List<string>();

      [JsonPropertyName("settings")]
      public Dictionary<string, string> settings { get; set; } = new Dictionary<string, string>();

      [JsonExtensionData]
      public Dictionary<string, object> metadata { get; set; } = new Dictionary<string, object>();
    }

    private static string GetDataDirectory()
    {
      Console.WriteLine("Please enter the Ultima Online directory:");

      string directory;
      do
      {
        Console.Write("> ");
        directory = Console.ReadLine();
      } while (!Directory.Exists(directory));

      return directory;
    }

    public static void SaveConfiguration()
    {
      JsonConfig.Serialize(m_FilePath, m_Settings);
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine($"Core: Configuration saved to {m_RelPath}.");
      Console.ResetColor();
    }
  }
}
