/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Server.Json;
using Server.Utilities;

namespace Server
{
    public static class RegionLoader
    {
        public static void LoadRegions()
        {
            var path = Path.Join(Core.BaseDirectory, "Data/regions.json");

            var failures = new List<string>();
            var count = 0;

            Console.Write("Regions: Loading...");

            var stopwatch = Stopwatch.StartNew();
            var regions = JsonConfig.Deserialize<List<DynamicJson>>(path);

            foreach (var json in regions)
            {
                var type = AssemblyHandler.FindFirstTypeForName(json.Type);

                if (type == null || !typeof(Region).IsAssignableFrom(type))
                {
                    failures.Add($"\tInvalid region type {json.Type}");
                    continue;
                }

                var region = ActivatorUtil.CreateInstance(type, json, JsonConfig.DefaultOptions) as Region;
                region?.Register();
                count++;
            }

            stopwatch.Stop();

            Console.ForegroundColor = failures.Count > 0 ? ConsoleColor.Yellow : ConsoleColor.Green;
            Console.Write("done{0}. ", failures.Count > 0 ? " with failures" : "");
            Console.ResetColor();
            Console.WriteLine(
                "({0} regions, {1} failures) ({2:F2} seconds)",
                count,
                failures.Count,
                stopwatch.Elapsed.TotalSeconds
            );
            if (failures.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Join("\n", failures));
                Console.ResetColor();
            }
        }
    }
}
