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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server
{
  public class Configuration
  {
    private static Configuration m_Configuration;

    public static Configuration Instance => m_Configuration ??= ReadConfiguration();

    [JsonPropertyName("dataDirectories")]
    public List<string> DataDirectories { get; set; } = new List<string>();

    [JsonPropertyName("emailSettings")]
    public EmailSettings emailSettings { get; set; } = new EmailSettings();

    private static string FilePath => Path.Join(Core.BaseDirectory, "Data/modernuo.json");

    private static void PromptDataDirectories(Configuration config)
    {
      Console.WriteLine("Please enter the Ultima Online directory:");

      string directory;
      do
      {
        Console.Write("> ");
        directory = Console.ReadLine();
      } while (!Directory.Exists(directory));

      config.DataDirectories.Add(directory);
    }

    private static Configuration ReadConfiguration()
    {
      var relPath = new Uri($"{Core.BaseDirectory}/").MakeRelativeUri(new Uri(FilePath)).ToString();
      Console.Write($"Reading configuration from {relPath}...");
      Configuration config;

      if (File.Exists(FilePath))
      {
        using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> configBytes = stackalloc byte[(int)fs.Length];
        fs.Read(configBytes);
        config = JsonSerializer.Deserialize<Configuration>(Utility.UTF8WithEncoding.GetString(configBytes));
        Console.WriteLine("done");
      }
      else
      {
        Console.WriteLine("not found");
        config = new Configuration();
      }

      // TODO: Extend with a config read verification function that can be extended.
      if (config.DataDirectories.Count == 0)
      {
        PromptDataDirectories(config);
        config.Flush();
      }

      return config;
    }

    public void Flush()
    {
      using var fs = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
      var configJson = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
      Span<byte> data = stackalloc byte[Utility.UTF8WithEncoding.GetMaxByteCount(configJson.Length)];
      var bytesWritten = Utility.UTF8WithEncoding.GetBytes(configJson, data);
      fs.Write(data.Slice(0, bytesWritten));
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine($"Configuration saved to {FilePath}");
      Console.ResetColor();
    }
  }

  // TODO: Make configuration pluggable. Move this to scripts
  public class EmailSettings
  {
    [JsonPropertyName("fromAddress")]
    public string FromAddress { get; set; }

    [JsonPropertyName("fromName")]
    public string FromName { get; set; }

    [JsonPropertyName("crashAddress")]
    public string crashAddress { get; set; }

    [JsonPropertyName("crashName")]
    public string crashName { get; set; }

    [JsonPropertyName("speechLogPageAddress")]
    public string speechLogPageAddress { get; set; }

    [JsonPropertyName("speechLogPageName")]
    public string speechLogPageName { get; set; }

    [JsonPropertyName("emailServer")]
    public string emailServer { get; set; }

    [JsonPropertyName("emailPort")]
    public int emailPort { get; set; }

    [JsonPropertyName("emailUsername")]
    public string emailUsername { get; set; }

    [JsonPropertyName("emailPassword")]
    public string emailPassword { get; set; }
  }
}
