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
    public static readonly List<string> DataDirectories = new List<string>();

    public static void ReadServerConfiguration()
    {
      const string relPath = "Configuration/modernuo.json";
      string filePath = Path.Join(Core.BaseDirectory, relPath);

      Settings settings;
      bool updated = false;

      if (File.Exists(filePath))
      {
        Console.Write($"Core: Reading configuration from {relPath}...");
        settings = JsonConfig.Deserialize<Settings>(filePath);

        if (settings == null)
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
        Console.WriteLine($"Core: Creating server configuration at {relPath}.");
        updated = true;
        settings = new Settings();
      }

      if (settings.dataDirectories.Count == 0)
      {
        updated = true;
        Console.WriteLine("Core: Server configuration is missing data directories.");
        settings.dataDirectories.Add(GetDataDirectory());
      }

      if (updated)
      {
        JsonConfig.Serialize(filePath, settings);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Core: Configuration saved to {relPath}");
        Console.ResetColor();
      }

      DataDirectories.AddRange(settings.dataDirectories);
    }

    internal class Settings
    {
      [JsonPropertyName("dataDirectories")]
      public List<string> dataDirectories { get; set; } = new List<string>();
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
  }
}
