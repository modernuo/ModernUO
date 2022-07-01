/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RegionLoader.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Server.Json;
using Server.Logging;
using Server.Utilities;

namespace Server
{
    internal static class RegionLoader
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(RegionLoader));

        internal static void LoadRegions()
        {
            var path = Path.Join(Core.BaseDirectory, "Data/regions.json");

            var failures = new List<string>();
            var count = 0;

            logger.Information("Loading regions");

            var stopwatch = Stopwatch.StartNew();
            var regions = JsonConfig.Deserialize<List<DynamicJson>>(path);
            if (regions == null)
            {
                throw new JsonException($"Failed to deserialize {path}.");
            }

            foreach (var json in regions)
            {
                var type = AssemblyHandler.FindTypeByName(json.Type);

                if (type == null || !typeof(Region).IsAssignableFrom(type))
                {
                    failures.Add($"\tInvalid region type {json.Type}");
                    continue;
                }

                var region = type.CreateInstance<Region>(json, JsonConfig.DefaultOptions);
                region?.Register();
                count++;
            }

            stopwatch.Stop();

            if (failures.Count == 0)
            {
                logger.Information(
                    "Regions loaded ({Count} regions, {FailureCount} failures) ({Duration:F2} seconds)",
                    count,
                    failures.Count,
                    stopwatch.Elapsed.TotalSeconds
                );
            }
            else
            {
                logger.Warning(
                    "Failed loading regions ({Count} regions, {FailureCount} failures) ({Duration:F2} seconds)",
                    count,
                    failures.Count,
                    stopwatch.Elapsed.TotalSeconds
                );

                logger.Warning("{Failures}", failures);
            }
        }
    }
}
