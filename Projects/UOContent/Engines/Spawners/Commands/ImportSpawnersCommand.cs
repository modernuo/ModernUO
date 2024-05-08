using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Server.Collections;
using Server.Json;
using Server.Logging;
using Server.Network;

namespace Server.Engines.Spawners;

public static class ImportSpawnersCommand
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ImportSpawnersCommand));

    public static void Configure()
    {
        CommandSystem.Register("ImportSpawners", AccessLevel.Developer, GenerateSpawners_OnCommand);
    }

    [Usage("ImportSpawners <relative search pattern to distribution>")]
    [Aliases("ImportSpawner", "GenerateSpawners", "GenSpawners")]
    [Description("Imports JSON, Nerun Premium, and RunUO spawner files. Supports basic globbing.")]
    private static void GenerateSpawners_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [GenerateSpawners <relative search pattern to distribution>");
            return;
        }

        var di = new DirectoryInfo(Core.BaseDirectory);

        var patternMatches = new Matcher()
            .AddInclude(e.Arguments[0])
            .Execute(new DirectoryInfoWrapper(di))
            .Files;

        List<FileInfo> files = [];
        foreach (var match in patternMatches)
        {
            files.Add(new FileInfo(match.Path));
        }

        if (files.Count == 0)
        {
            from.SendMessage("GenerateSpawners: No files found matching the pattern.");
            return;
        }

        var watch = Stopwatch.StartNew();

        var allSpawners = new Dictionary<Guid, ISpawner>();
        foreach (var item in World.Items.Values)
        {
            if (item is ISpawner spawner)
            {
                allSpawners[spawner.Guid] = spawner;
            }
        }

        var totalGenerated = 0;
        var totalFailures = 0;

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            from.SendMessage($"GenerateSpawners: Generating spawners from {file.Name}...");
            logger.Information("{User} is generating spawners from {File}", from, file.FullName);

            NetState.FlushAll();

            if (file.Extension == ".json")
            {
                ImportJsonSpawners(from, file, allSpawners, ref totalGenerated, ref totalFailures);
            }
            else if (file.Extension == ".xml")
            {
                ImportXmlSpawners(from, file, allSpawners, ref totalGenerated, ref totalFailures);
            }
            else if (file.Extension == ".map")
            {
                ImportPremiumSpawners(from, file, allSpawners, ref totalGenerated, ref totalFailures);
            }
            else
            {
                from.SendMessage($"GenerateSpawners: Unsupported file type {file.Extension}.");
            }
        }

        watch.Stop();

        logger.Information(
            "Generated {Count} spawners ({Duration:F2} seconds, {Failures} failures)",
            totalGenerated,
            watch.Elapsed.TotalSeconds,
            totalFailures
        );

        from.SendMessage(
            $"GenerateSpawners: Generated {totalGenerated} spawners ({watch.Elapsed.TotalSeconds:F2} seconds, {totalFailures} failures)"
        );
    }

    private static void ImportXmlSpawners(
        Mobile from,
        FileInfo file,
        Dictionary<Guid, ISpawner> allSpawners,
        ref int totalGenerated,
        ref int totalFailures
    )
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(file.FullName);

        XmlElement root = doc["spawners"];

        if (root == null)
        {
            return;
        }

        var index = 0;
        foreach (XmlElement node in root.GetElementsByTagName("spawner"))
        {
            try
            {
                int count = int.Parse(Utility.GetText(node["count"], "1"));
                int homeRange = int.Parse(Utility.GetText(node["homerange"], "4"));

                int walkingRange = int.Parse(Utility.GetText(node["walkingrange"], "-1"));

                int team = int.Parse(Utility.GetText(node["team"], "0"));

                TimeSpan maxDelay = TimeSpan.Parse(Utility.GetText(node["maxdelay"], "10:00"));
                TimeSpan minDelay = TimeSpan.Parse(Utility.GetText(node["mindelay"], "05:00"));
                var creaturesNameNode = node["creaturesname"];
                List<string> creatureNames = [];
                if (creaturesNameNode != null)
                {
                    foreach (XmlElement ele in creaturesNameNode.GetElementsByTagName("creaturename"))
                    {
                        if (ele != null)
                        {
                            creatureNames.Add(ele.InnerText);
                        }
                    }
                }

                string name = Utility.GetText(node["name"], "Spawner");
                Point3D location = Point3D.Parse(Utility.GetText(node["location"], "Error"));
                Map map = Map.Parse(Utility.GetText(node["map"], "Error"));

                Spawner spawner = new Spawner(count, minDelay, maxDelay, team, homeRange, creatureNames.ToArray());
                if (walkingRange >= 0)
                {
                    spawner.WalkingRange = walkingRange;
                }

                spawner.Name = name;
                spawner.MoveToWorld(location, map);
                if (spawner.Map == Map.Internal)
                {
                    spawner.Delete();
                    totalFailures++;
                    throw new Exception("Spawner created on Internal map.");
                }

                spawner.Respawn();
                allSpawners.Add(spawner.Guid, spawner);
                totalGenerated++;
            }
            catch (Exception ex)
            {
                TraceException(ex, $"Failed to generate spawner {index} in {file.FullName}.");
                from.SendMessage(
                    $"GenerateSpawners: Exception parsing {file.FullName}, file may not be in the correct format."
                );
            }

            index++;
        }
    }

    private static void ImportJsonSpawners(
        Mobile from,
        FileInfo file,
        Dictionary<Guid, ISpawner> allSpawners,
        ref int totalGenerated,
        ref int totalFailures
    )
    {
        var options = JsonConfig.GetOptions();
        try
        {
            var spawners = JsonConfig.Deserialize<List<DynamicJson>>(file.FullName);
            using var queue = PooledRefQueue<Item>.Create();
            for (var i = 0; i < spawners.Count; i++)
            {
                var json = spawners[i];
                var type = AssemblyHandler.FindTypeByName(json.Type);

                if (type == null || !typeof(BaseSpawner).IsAssignableFrom(type))
                {
                    logger.Error($"Invalid spawner type {json.Type ?? "(-null-)"} ({i}).");
                    totalFailures++;
                    continue;
                }

                json.GetProperty("location", options, out Point3D location);
                json.GetProperty("map", options, out Map map);

                // Delete all spawners at this location.
                // Probably shouldn't do this outside of migrations? Is there a better way to find/fix spawners?
                foreach (var spawner in map.GetItemsAt<BaseSpawner>(location))
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
                    totalGenerated++;
                }
                catch (Exception ex)
                {
                    json.GetProperty("guid", options, out Guid guid);
                    TraceException(ex, $"Failed to generate spawner {guid}.");

                    totalFailures++;
                }
            }
        }
        catch (JsonException)
        {
            from.SendMessage(
                $"GenerateSpawners: Exception parsing {file.FullName}, file may not be in the correct format."
            );
        }
    }

    private static void TraceException(Exception ex, string message = "")
    {
        try
        {
            using var op = new StreamWriter("spawner-errors.log", true);
            op.WriteLine("# {0}", Core.Now);

            if (!string.IsNullOrEmpty(message))
            {
                op.WriteLine(message);
            }

            op.WriteLine(ex);

            op.WriteLine();
            op.WriteLine();
        }
        catch
        {
            // ignored
        }

#if DEBUG
        logger.Error(ex, message);
#endif
    }

    private static void ImportPremiumSpawners(
        Mobile from,
        FileInfo file,
        Dictionary<Guid, ISpawner> allSpawners,
        ref int totalGenerated,
        ref int totalFailures
    )
    {
        var lines = File.ReadAllLines(file.FullName);

        TimeSpan minTimeOverride = TimeSpan.MinValue;
        TimeSpan maxTimeOverride = TimeSpan.MinValue;
        int mapIdOverride = -1;
        string spawnerIdOverride = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWithOrdinal("#"))
            {
                continue;
            }

            try
            {

                if (line.StartsWith('*'))
                {
                    var parts = line.Split('|');
                    var mapId = mapIdOverride != -1 ? mapIdOverride : int.Parse(parts[10]);
                    Map[] maps = mapId == 0 ? [Map.Felucca, Map.Trammel] : [Map.Maps[mapId - 1]];

                    foreach (var map in maps)
                    {
                        try
                        {
                            List<(string, int)> spawnerEntries = [];

                            var totalCount = 0;
                            for (var j = 0; j < 6; j++)
                            {
                                var count = int.Parse(parts[j + 16]);
                                totalCount += count;
                                spawnerEntries.AddRange(CreateSpawnerEntries(parts[j + 1], count));
                            }

                            if (spawnerEntries.Count == 0)
                            {
                                continue;
                            }

                            var minDelay = GetTimeSpan(minTimeOverride, parts[11]);
                            var maxDelay = GetTimeSpan(maxTimeOverride, parts[12]);
                            var homeRange = int.Parse(parts[14]);

                            var spawner = new Spawner(totalCount, minDelay, maxDelay, 0, homeRange)
                            {
                                WalkingRange = int.Parse(parts[13]),
                                Name = $"Spawner ({spawnerIdOverride ?? parts[15]})"
                            };

                            foreach (var (type, amount) in spawnerEntries)
                            {
                                spawner.AddEntry(type, amount: amount);
                            }

                            spawner.MoveToWorld(
                                new Point3D(int.Parse(parts[7]), int.Parse(parts[8]), int.Parse(parts[9])),
                                map
                            );

                            spawner.Respawn();
                            allSpawners.Add(spawner.Guid, spawner);
                            totalGenerated++;
                        }
                        catch (Exception ex)
                        {
                            TraceException(ex, $"Failed to generate spawner on line {i} in {file.FullName}.");
                            from.SendMessage(
                                $"GenerateSpawners: Exception parsing {file.FullName}, file may not be in the correct format."
                            );
                            totalFailures++;
                        }
                    }

                    continue;
                }

                var over = line.Split(' ');
                switch (over[0].ToLowerInvariant())
                {
                    case "overrideid":
                        {
                            spawnerIdOverride = over[1];
                            break;
                        }
                    case "overridemap":
                        {
                            mapIdOverride = int.Parse(over[1]);
                            break;
                        }
                    case "overridemintime":
                        {
                            minTimeOverride = GetTimeSpan(over[1]);
                            break;
                        }
                    case "overridemaxtime":
                        {
                            maxTimeOverride = GetTimeSpan(over[1]);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                TraceException(ex, $"Failed to generate spawner on line {i} in {file.FullName}.");
                from.SendMessage(
                    $"GenerateSpawners: Exception parsing {file.FullName}, file may not be in the correct format."
                );
                return;
            }
        }
    }

    private static List<(string, int)> CreateSpawnerEntries(string typeList, int maxCount)
    {
        var list = new List<(string, int)>();
        if (string.IsNullOrWhiteSpace(typeList))
        {
            return list;
        }

        foreach (var spawnType in typeList.Split(':'))
        {
            var actualType = AssemblyHandler.FindTypeByName(ConvertType(spawnType))?.Name ?? spawnType;
            list.Add((actualType, maxCount));
        }

        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TimeSpan GetTimeSpan(TimeSpan timeOverride, string time) =>
        timeOverride != TimeSpan.MinValue ? timeOverride : GetTimeSpan(time);

    private static TimeSpan GetTimeSpan(string time)
    {
        if (string.IsNullOrEmpty(time))
        {
            return TimeSpan.MinValue;
        }

        return char.ToLowerInvariant(time[^1]) switch
        {
            'h' => TimeSpan.FromHours(double.Parse(time.AsSpan(0, time.Length - 1))),
            'm' => TimeSpan.FromMinutes(double.Parse(time.AsSpan(0, time.Length - 1))),
            's' => TimeSpan.FromSeconds(double.Parse(time.AsSpan(0, time.Length - 1))),
            _   => TimeSpan.FromMinutes(double.Parse(time))
        };
    }

    private static string ConvertType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "treasurelevel1" => "treasurechestlevel1",
            "treasurelevel2" => "treasurechestlevel2",
            "treasurelevel3" => "treasurechestlevel3",
            "treasurelevel4" => "treasurechestlevel4",
            "treasurelevel5" => "treasurechestlevel5",
            _                => type
        };
    }
}
