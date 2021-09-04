/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenerateSpawnersCommand.cs                                      *
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
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Server.Collections;
using Server.Json;
using Server.Network;
using Server.Utilities;

namespace Server.Engines.Spawners
{
    public static class GenerateSpawnersCommand
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
                from.SendMessage("Usage: [GenerateSpawners <relative search pattern to distribution>");
                return;
            }

            var di = new DirectoryInfo(Core.BaseDirectory);

            FileInfo[] files;

            try
            {
                files = di.GetFiles(e.Arguments[0], SearchOption.AllDirectories);
            }
            catch
            {
                from.SendMessage("GenerateSpawners: Bad path. Path must be relative to the distribution folder.");
                return;
            }


            if (files.Length == 0)
            {
                from.SendMessage("GenerateSpawners: No files found matching the pattern");
                return;
            }

            var allSpawners = new Dictionary<Guid, ISpawner>();
            foreach (var item in World.Items.Values)
            {
                if (item is ISpawner spawner)
                {
                    allSpawners[spawner.Guid] = spawner;
                }
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
                    ParseSpawnerList(from, spawners, options, allSpawners);
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

        private static void ParseSpawnerList(
            Mobile from,
            List<DynamicJson> spawners,
            JsonSerializerOptions options,
            Dictionary<Guid, ISpawner> allSpawners
        )
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

                // Delete all spawners at this location.
                // Probably shouldn't do this outside of migrations? Is there a better way to find/fix spawners?
                var eable = map.GetItemsInRange<BaseSpawner>(location, 0);
                using var queue = PooledRefQueue<Item>.Create();
                foreach (var spawner in eable)
                {
                    if (spawner.GetType() == type)
                    {
                        queue.Enqueue(spawner);
                        allSpawners.Remove(spawner.Guid);
                    }
                }

                while (queue.Count > 0)
                {
                    queue.Dequeue().Delete();
                }

                eable.Free();

                try
                {
                    var spawner = type.CreateInstance<ISpawner>(json, options);

                    spawner!.MoveToWorld(location, map);
                    spawner!.Respawn();

                    if (allSpawners.Remove(spawner.Guid, out var oldSpawner))
                    {
                        oldSpawner.Delete();
                    }

                    allSpawners.Add(spawner.Guid, spawner);
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
