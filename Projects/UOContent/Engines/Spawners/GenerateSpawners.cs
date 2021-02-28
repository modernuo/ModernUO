using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Server.Json;
using Server.Network;
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
            var from = e.Mobile;

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

            var options = JsonConfig.GetOptions(new TextDefinitionConverterFactory());

            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                from.SendMessage("GenerateSpawners: Generating spawners for {0}...", file.Name);

                NetState.FlushAll();

                try
                {
                    var spawners = JsonConfig.Deserialize<List<DynamicJson>>(file.FullName);
                    ParseSpawnerList(from, spawners, options);
                }
                catch (JsonException)
                {
                    from.SendMessage(
                        "GenerateSpawners: Exception parsing {0}, file may not be in the correct format.",
                        file.FullName
                    );
                }
            }
        }

        private static void ParseSpawnerList(Mobile from, List<DynamicJson> spawners, JsonSerializerOptions options)
        {
            var watch = Stopwatch.StartNew();
            var failures = new List<string>();
            var count = 0;

            for (var i = 0; i < spawners.Count; i++)
            {
                var json = spawners[i];
                var type = AssemblyHandler.FindTypeByName(json.Type);

                if (type == null || !typeof(BaseSpawner).IsAssignableFrom(type))
                {
                    var failure = $"GenerateSpawners: Invalid spawner type {json.Type ?? "(-null-)"} ({i})";
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

                try
                {
                    var spawner = type.CreateInstance<ISpawner>(json, options);

                    spawner!.MoveToWorld(location, map);
                    spawner!.Respawn();
                }
                catch (Exception)
                {
                    var failure = $"GenerateSpawners: Spawner {type} failed to construct";
                    if (!failures.Contains(failure))
                    {
                        failures.Add(failure);
                        from.SendMessage(failure);
                    }

                    continue;
                }

                count++;
            }

            watch.Stop();
            from.SendMessage(
                "GenerateSpawners: Generated {0} spawners ({1:F2} seconds, {2} failures)",
                count,
                watch.Elapsed.TotalSeconds,
                failures.Count
            );
        }
    }
}
