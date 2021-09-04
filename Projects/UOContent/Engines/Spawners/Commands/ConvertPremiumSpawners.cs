using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Json;
using Server.Network;

namespace Server.Engines.Spawners
{
    public static class ConvertPremiumSpawners
    {
        public static void Initialize()
        {
            CommandSystem.Register("ConvertPremiumSpawners", AccessLevel.Developer, ConvertPremiumSpawners_OnCommand);
        }

        private static void ConvertPremiumSpawners_OnCommand(CommandEventArgs args)
        {
            var from = args.Mobile;

            if (args.Arguments.Length != 2)
            {
                from.SendMessage("Usage: [ConvertPremiumSpawners <relative search pattern to distribution> <output directory relative to distribution>");
                return;
            }

            var inputDir = Path.Combine(Core.BaseDirectory, args.Arguments[0]);
            var inputDi = new DirectoryInfo(inputDir);

            if (!inputDi.Exists)
            {
                from.SendMessage("ConvertPremiumSpawners: Input path doesn't exist.");
                return;
            }

            FileInfo[] files;

            try
            {
                files = inputDi.GetFiles("*.map", SearchOption.AllDirectories);
            }
            catch
            {
                from.SendMessage("ConvertPremiumSpawners: Bad input path. Path must be relative to the distribution folder.");
                return;
            }

            if (files.Length == 0)
            {
                from.SendMessage("ConvertPremiumSpawners: No files found.");
                return;
            }

            var outputDir = Path.Combine(Core.BaseDirectory, args.Arguments[1]);

            var options = JsonConfig.GetOptions(new TextDefinitionConverterFactory());

            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                from.SendMessage("ConvertPremiumSpawners: Converting spawners for {0}...", file.Name);

                NetState.FlushAll();

                try
                {
                    var relativePath = Path.GetRelativePath(inputDir, file.DirectoryName!);
                    var fullOutputDir = Path.Combine(outputDir, relativePath);
                    AssemblyHandler.EnsureDirectory(fullOutputDir);
                    ParsePremiumSpawnerFile(file, fullOutputDir, options);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    from.SendMessage(
                        "ConvertPremiumSpawners: Exception parsing {0}, file may not be in the correct format.",
                        file.FullName
                    );
                }
            }
        }

        private static void ParsePremiumSpawnerFile(FileInfo file, string outputDirectory, JsonSerializerOptions options)
        {
            var lines = File.ReadAllLines(file.FullName);

            TimeSpan minTime = TimeSpan.MinValue;
            TimeSpan maxTime = TimeSpan.MinValue;
            int mapId = -1;
            string spawnerId = null;

            var json = new List<DynamicJson>();

            foreach (var line in lines)
            {
                if (line.StartsWithOrdinal("##"))
                {
                    continue;
                }

                if (line.StartsWith('*'))
                {
                    var spawners = ParsePremiumSpawner(line.Split('|'), spawnerId, mapId, minTime, maxTime);
                    foreach (var spawner in spawners)
                    {
                        var dynamicJson = new DynamicJson
                        {
                            Type = "Spawner",
                            Data = new Dictionary<string, JsonElement>()
                        };

                        spawner.ToJson(dynamicJson, options);
                        json.Add(dynamicJson);
                        spawner.Delete();
                    }

                    spawners.Clear();
                    continue;
                }

                var over = line.Split(' ');
                switch (over[0].ToLowerInvariant())
                {
                    case "overrideid":
                        {
                            spawnerId = over[1];
                            break;
                        }
                    case "overridemap":
                        {
                            mapId = int.Parse(over[1]);
                            break;
                        }
                    case "overridemintime":
                        {
                            minTime = GetTimeSpan(over[1]);
                            break;
                        }
                    case "overridemaxtime":
                        {
                            maxTime = GetTimeSpan(over[1]);
                            break;
                        }
                }
            }

            var outputFile = Path.Combine(outputDirectory, $"{file.Name[..^file.Extension.Length]}.json");
            Console.WriteLine("Writing to: {0}", outputFile);
            JsonConfig.Serialize(outputFile, json, options);
        }

        private static List<Spawner> ParsePremiumSpawner(
            string[] parts,
            string spawnerIdOverride,
            int mapIdOverride,
            TimeSpan minTimeOverride,
            TimeSpan maxTimeOverride
        )
        {
            var spawnersList = new List<Spawner>();
            var mapId = mapIdOverride != -1 ? mapIdOverride : int.Parse(parts[10]);
            Map[] maps = mapId == 0 ? new[] { Map.Felucca, Map.Trammel } : new[] { Map.Maps[mapId - 1] };

            foreach (var map in maps)
            {
                var spawner = new Spawner
                {
                    X = int.Parse(parts[7]),
                    Y = int.Parse(parts[8]),
                    Z = int.Parse(parts[9]),
                    Map = map,
                    MinDelay = minTimeOverride != TimeSpan.MinValue ? minTimeOverride : GetTimeSpan(parts[11]),
                    MaxDelay = maxTimeOverride != TimeSpan.MinValue ? maxTimeOverride : GetTimeSpan(parts[12]),
                    WalkingRange = int.Parse(parts[13]),
                    HomeRange = int.Parse(parts[14]),
                    Name = $"Spawner ({spawnerIdOverride ?? parts[15]})",
                    Guid = Guid.NewGuid()
                };

                for (var i = 0; i < 6; i++)
                {
                    spawner.Entries.AddRange(CreateSpawnerEntries(parts[i + 1], int.Parse(parts[i + 15])));
                }

                if (spawner.Entries.Count == 0)
                {
                    spawner.Delete();
                    continue;
                }

                var totalCount = 0;
                foreach (var entry in spawner.Entries)
                {
                    totalCount += entry.SpawnedMaxCount;
                }
                spawner.Count = totalCount;

                spawnersList.Add(spawner);
            }

            return spawnersList;
        }

        private static List<SpawnerEntry> CreateSpawnerEntries(string typeList, int maxCount)
        {
            var list = new List<SpawnerEntry>();
            if (string.IsNullOrWhiteSpace(typeList))
            {
                return list;
            }

            foreach (var spawnType in typeList.Split(':'))
            {
                var actualType = AssemblyHandler.FindTypeByName(ConvertType(spawnType))?.Name ?? spawnType;
                list.Add(new SpawnerEntry(actualType, 100, maxCount));
            }

            return list;
        }

        private static TimeSpan GetTimeSpan(string time)
        {
            if (string.IsNullOrEmpty(time))
            {
                return TimeSpan.MinValue;
            }

            time = time.ToLowerInvariant();

            return time[^1] switch
            {
                'h' => TimeSpan.FromHours(double.Parse(time[..^1])),
                'm' => TimeSpan.FromMinutes(double.Parse(time[..^1])),
                's' => TimeSpan.FromSeconds(double.Parse(time[..^1])),
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
}
