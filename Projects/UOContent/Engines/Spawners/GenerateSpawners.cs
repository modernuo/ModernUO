using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Server.Json;
using Server.Utilities;

namespace Server.Engines.Spawners
{
  public static class GenerateSpawners
  {
    public static void Initialize()
    {
      CommandSystem.Register("GenerateSpawners", AccessLevel.Developer, GenerateSpawners_OnCommand);
    }

    private static void GenerateSpawners_OnCommand(CommandEventArgs e)
    {
      Mobile from = e.Mobile;

      if (e.Arguments.Length == 0)
      {
        from.SendMessage("Usage: [GenerateSpawners <path|search pattern>");
        return;
      }

      var di = new DirectoryInfo(Core.BaseDirectory);

      var files = di.GetFiles(e.Arguments[0], SearchOption.AllDirectories);

      if (files.Length == 0)
      {
        from.SendMessage("GenerateSpawners: No files found matching the pattern");
        return;
      }

      JsonSerializerOptions options = new JsonSerializerOptions();
      options.Converters.Add(new MapConverterFactory());
      options.Converters.Add(new Point3DConverterFactory());
      options.Converters.Add(new TimeSpanConverterFactory());
      options.Converters.Add(new TextDefinitionConverterFactory());

      for (int i = 0; i < files.Length; i++)
      {
        var file = files[i];
        from.SendMessage("GenerateSpawners: Generating spawners for {0}...", file.Name);
        List<DynamicJson> spawners = JsonConfig.Deserialize<List<DynamicJson>>(file.FullName);
        ParseSpawnerList(from, spawners, options);
      }
    }

    private static void ParseSpawnerList(Mobile from, List<DynamicJson> spawners, JsonSerializerOptions options)
    {
      Stopwatch watch = Stopwatch.StartNew();
      List<string> failures = new List<string>();
      int count = 0;

      foreach (var json in spawners)
      {
        Type type = AssemblyHandler.FindFirstTypeForName(json.Type);

        if (json.Type == null || !typeof(BaseSpawner).IsAssignableFrom(type))
        {
          string failure = $"GenerateSpawners: Invalid region type {json.Type}";
          if (!failures.Contains(failure))
          {
            failures.Add(failure);
            from.SendMessage(failure);
          }
          continue;
        }

        json.GetProperty("location", options, out Point3D location);
        json.GetProperty("map", options, out Map map);

        var eable = map.GetItemsInRange<BaseSpawner>(location, 0);

        if (eable.Any(sp => sp.GetType() == type))
        {
          eable.Free();
          continue;
        }

        eable.Free();

        var spawner = ActivatorUtil.CreateInstance(type, json, options) as ISpawner;
        if (spawner == null)
        {
          string failure = $"GenerateSpawners: Spawner type {type} is not valid";
          if (!failures.Contains(failure))
          {
            failures.Add(failure);
            from.SendMessage(failure);
          }

          continue;
        }

        spawner.MoveToWorld(location, map);
        spawner.Respawn();

        count++;
      }

      watch.Stop();
      from.SendMessage("GenerateSpawners: Generated {0} spawners ({1:F2} seconds, {2})", count, watch.Elapsed.TotalSeconds, failures);
    }
  }
}
