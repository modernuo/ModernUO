/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MapLoader.cs                                                    *
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
using System.Text.Json.Serialization;
using Server.Json;
using Server.Logging;

namespace Server
{
    public static class MapLoader
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(MapLoader));

        /* Here we configure all maps. Some notes:
         *
         * 1) The first 32 maps are reserved for core use.
         * 2) Map 127 is reserved for core use.
         * 3) Map 255 is reserved for core use.
         * 4) Changing or removing any predefined maps may cause server instability.
         *
         * Map definitions are modified in Data/Map Definitions/<expansion>.json:
         *  - <index> : An unreserved unique index for this map
         *  - <id> : An identification number used in client communications. For any visible maps, this value must be from 0-5
         *  - <fileIndex> : A file identification number. For any visible maps, this value must be from 0-5
         *  - <width>, <height> : Size of the map (in tiles)
         *  - <season> : Season of the map. 0 = Spring, 1 = Summer, 2 = Fall, 3 = Winter, 4 = Desolation
         *  - <name> : Reference name for the map, used in props gump, get/set commands, region loading, etc
         *  - <rules> : Rules and restrictions associated with the map. See documentation for details
         */
        [CallPriority(2)]
        public static void Configure()
        {
            var failures = new List<string>();
            var count = 0;

            var path = Path.Combine(Core.BaseDirectory, "Data/map-definitions.json");

            logger.Information("Loading Map Definitions");

            var stopwatch = Stopwatch.StartNew();
            var maps = JsonConfig.Deserialize<List<MapDefinition>>(path);
            if (maps == null)
            {
                throw new JsonException($"Failed to deserialize {path}.");
            }

            foreach (var def in maps)
            {
                try
                {
                    RegisterMap(def);
                    count++;
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, "Failed to load map definition {MapDefName} ({MapDefId})", def.Name, def.Id);
                    failures.Add($"\tInvalid map definition {def.Name} ({def.Id})");
                }
            }

            stopwatch.Stop();

            if (failures.Count > 0)
            {
                logger.Warning(
                    "Map Definitions loaded with failures ({Count} maps, {FailureCount} failures) ({Duration:F2} seconds)",
                    count,
                    failures.Count,
                    stopwatch.Elapsed.TotalSeconds
                );

                logger.Warning("Map load failures: {Failure}", failures);
            }
            else
            {
                logger.Information(
                    "Map Definitions loaded successfully ({Count} maps, {FailureCount} failures) ({Duration:F2} seconds)",
                    count,
                    failures.Count,
                    stopwatch.Elapsed.TotalSeconds
                );
            }
        }

        private static void RegisterMap(MapDefinition mapDefinition)
        {
            var newMap = new Map(
                mapDefinition.Id,
                mapDefinition.Index,
                mapDefinition.FileIndex,
                Math.Max(mapDefinition.Width, Map.SectorSize),
                Math.Max(mapDefinition.Height, Map.SectorSize),
                mapDefinition.Season,
                mapDefinition.Name,
                mapDefinition.Rules
            );

            Map.Maps[mapDefinition.Index] = newMap;
            Map.AllMaps.Add(newMap);
        }

        internal class MapDefinition
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("fileIndex")]
            public int FileIndex { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("width")]
            public int Width { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }

            [JsonPropertyName("season")]
            public int Season { get; set; }

            [JsonPropertyName("rules")]
            public MapRules Rules { get; set; }
        }
    }
}
