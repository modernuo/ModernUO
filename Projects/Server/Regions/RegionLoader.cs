using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Server.Json;
using Server.Utilities;

namespace Server
{
  public static class RegionLoader
  {
    public static void LoadRegions()
    {
      var path = Path.Join(Core.BaseDirectory, "Data/regions.json");

      // Json Deserialization options for custom objects
      JsonSerializerOptions options = new JsonSerializerOptions();
      options.Converters.Add(new MapConverterFactory());
      options.Converters.Add(new Point3DConverterFactory());
      options.Converters.Add(new Rectangle3DConverterFactory());

      List<string> failures = new List<string>();
      int count = 0;

      Console.Write("Regions: Loading...");

      var stopwatch = Stopwatch.StartNew();
      List<DynamicJson> regions = JsonConfig.Deserialize<List<DynamicJson>>(path);

      foreach (var json in regions)
      {
        Type type = AssemblyHandler.FindFirstTypeForName(json.Type);

        if (type == null || !typeof(Region).IsAssignableFrom(type))
        {
          failures.Add($"\tInvalid region type {json.Type}");
          continue;
        }

        var region = ActivatorUtil.CreateInstance(type, json, options) as Region;
        region?.Register();
        count++;
      }

      stopwatch.Stop();

      Console.ForegroundColor = failures.Count > 0 ? ConsoleColor.Yellow : ConsoleColor.Green;
      Console.Write("done{0}. ", failures.Count > 0 ? " with failures" : "");
      Console.ResetColor();
      Console.WriteLine("({0} regions, {1} failures) ({2:F2} seconds)", count, failures.Count, stopwatch.Elapsed.TotalSeconds);
      if (failures.Count > 0)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(string.Join("\n", failures));
        Console.ResetColor();
      }
    }
  }
}
