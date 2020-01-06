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

    private static string FilePath => Path.Join(Core.BaseDirectory, "modernuo.json");

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
      string relPath = new Uri($"{Core.BaseDirectory}/").MakeRelativeUri(new Uri(FilePath)).ToString();
      Console.Write($"Reading configuration from {relPath}...");
      Configuration config;

      if (File.Exists(FilePath))
      {
        using FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
      using FileStream fs = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
      string configJson = JsonSerializer.Serialize(this, new JsonSerializerOptions {WriteIndented = true});
      Span<byte> data = stackalloc byte[Utility.UTF8WithEncoding.GetMaxByteCount(configJson.Length)];
      int bytesWritten = Utility.UTF8WithEncoding.GetBytes(configJson, data);
      fs.Write(data.Slice(0, bytesWritten));
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine($"Configuration saved to {FilePath}");
      Console.ResetColor();
    }
  }

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
