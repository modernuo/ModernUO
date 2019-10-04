using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server
{
  public class Configuration
  {
    public List<String> DataDirectories { get; set; } = new List<String>();

    internal static string FilePath { get {
      return Path.Join(Core.BaseDirectory, "modernuo.json");
    } }

    internal static Configuration CreateConfiguration()
    {
      Configuration config = new Configuration();

      Console.WriteLine();
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"modernuo.json not found at {Configuration.FilePath}");
      Console.ResetColor();
      Console.WriteLine();
      Console.WriteLine("Please follow the prompts to generate a configuration file");
      Console.WriteLine();
      Console.WriteLine("Please enter the Ultima Online directory:");

      string directory = "";
      do
      {
        Console.Write("> ");
        directory = Console.ReadLine();
      } while (!Directory.Exists(directory));

      config.DataDirectories.Add(directory);

      Configuration.WriteConfiguration(config);

      return config;
    }

    internal static Configuration ReadConfiguration()
    {
      if (File.Exists(Configuration.FilePath))
      {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Reading configuration from {Configuration.FilePath}...");
        Console.ResetColor();
        Console.WriteLine();

        Configuration config;
        using (FileStream fs = new FileStream(Configuration.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
          byte[] configBytes = new byte[fs.Length];
          fs.Read(configBytes, 0, (int)fs.Length);
          config = JsonSerializer.Deserialize<Configuration>(new UTF8Encoding(true).GetString(configBytes));
        }

        if (config.DataDirectories.Count == 0)
        {
          Console.WriteLine();
          Console.WriteLine("DataDirectories not defined, please enter the Ultima Online directory:");

          string directory = "";
          do
          {
            Console.Write("> ");
            directory = Console.ReadLine();
          } while (!Directory.Exists(directory));

          config.DataDirectories.Add(directory);

          Console.WriteLine();
          Console.WriteLine("Save configuration? [y/n]");

          ConsoleKey response;
          do
          {
            response = Console.ReadKey(true).Key;
            if (response != ConsoleKey.Enter)
              Console.WriteLine();

          } while (response != ConsoleKey.Y && response != ConsoleKey.N);

          if (response == ConsoleKey.Y)
          {
            Configuration.WriteConfiguration(config);
          }
        }

        return config;
      }
      else
      {
        return Configuration.CreateConfiguration();
      }
    }

    internal static string WriteConfiguration(Configuration config)
    {
      using (FileStream fs = new FileStream(Configuration.FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
      {
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
        jsonOptions.WriteIndented = true;
        string configJson = JsonSerializer.Serialize<Configuration>(config, jsonOptions);
        byte[] data = new UTF8Encoding(true).GetBytes(configJson);
        fs.Write(data, 0, data.Length);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Configuration saved to {Configuration.FilePath}");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine(configJson);
        Console.WriteLine();
        return configJson;
      }
    }
  }
}
