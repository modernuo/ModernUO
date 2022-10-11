/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ConvertPremiumSpawners.cs                                       *
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
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Server.Json;
using Server.Logging;
using Server.Network;

namespace Server.Engines.Spawners
{
    public static class ConvertPremiumSpawners
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ConvertPremiumSpawners));

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

            var inputDi = new DirectoryInfo(Core.BaseDirectory);

            var patternMatches = new Matcher()
                .AddInclude(args.Arguments[0])
                .Execute(new DirectoryInfoWrapper(inputDi))
                .Files;

            List<(FileInfo, string)> files = new List<(FileInfo, string)>();
            foreach (var match in patternMatches)
            {
                files.Add((new FileInfo(match.Path), match.Stem));
            }

            if (files.Count == 0)
            {
                from.SendMessage("ConvertPremiumSpawners: No files found.");
                return;
            }

            var outputDir = Path.Combine(Core.BaseDirectory, args.Arguments[1]);
            var options = JsonConfig.GetOptions(new TextDefinitionConverterFactory());

            for (var i = 0; i < files.Count; i++)
            {
                var (file, stem) = files[i];
                from.SendMessage("ConvertPremiumSpawners: Converting spawners for {0}...", stem);

                NetState.FlushAll();

                try
                {
                    var stemDir = stem[..^file.Name.Length];
                    var fullOutputDir = Path.Combine(outputDir, stemDir);
                    PathUtility.EnsureDirectory(fullOutputDir);

                    var outputFileName = $"{file.Name[..^file.Extension.Length]}.json";
                    ParsePremiumSpawnerFile(file.FullName, Path.Combine(fullOutputDir, outputFileName), options);
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                    from.SendMessage($"ConvertPremiumSpawners: Exception parsing {stem}, file may not be in the correct format.");
                }
            }
        }

        private static void ParsePremiumSpawnerFile(string inputFile, string outputFile, JsonSerializerOptions options)
        {
            var lines = File.ReadAllLines(inputFile);

            TimeSpan minTime = TimeSpan.MinValue;
            TimeSpan maxTime = TimeSpan.MinValue;
            int mapId = -1;
            string spawnerId = null;

            var json = new List<DynamicJson>();

            foreach (var line in lines)
            {
                if (line.StartsWithOrdinal("#"))
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

                var totalCount = 0;

                for (var i = 0; i < 6; i++)
                {
                    var count = int.Parse(parts[i + 16]);
                    totalCount += count;
                    spawner.Entries.AddRange(CreateSpawnerEntries(parts[i + 1], count));
                }

                if (spawner.Entries.Count == 0)
                {
                    spawner.Delete();
                    continue;
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
