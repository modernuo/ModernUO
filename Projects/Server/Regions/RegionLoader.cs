using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Json;

namespace Server
{
  public static class RegionLoader
  {
    public static void LoadRegions()
    {
      Console.Write("Regions: Loading...");

      var path = Path.Join(Core.BaseDirectory, "Data\regions.json");

      // Json Deserialization options for custom objects
      JsonSerializerOptions options = new JsonSerializerOptions();
      options.Converters.Add(new MapConverterFactory());
      options.Converters.Add(new Point3DConverterFactory());
      options.Converters.Add(new Rectangle3DConverterFactory());

      List<DynamicJson> regions = JsonConfig.Deserialize<List<DynamicJson>>(path);
      foreach (var json in regions)
      {
        var region = new Region(json, options);
        region.Register();
      }

      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("done");
      Console.ResetColor();
    }
  }
}
