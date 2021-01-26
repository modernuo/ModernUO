/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: TileMatrixLoader.cs                                             *
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
using System.Diagnostics;

namespace Server
{
    internal static class TileMatrixLoader
    {
        internal static void LoadTileMatrix()
        {
            Console.Write("Maps: Loading...");

            var stopwatch = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                foreach (var m in Map.AllMaps)
                {
                    m.Tiles.Force(); // Forces the map file stream references to load
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            stopwatch.Stop();

            Utility.PushColor(exception != null ? ConsoleColor.Yellow : ConsoleColor.Green);
            Console.Write(exception != null ? "failed" : "done");
            Utility.PopColor();
            Console.WriteLine(" ({0:F2} seconds)", stopwatch.Elapsed.TotalSeconds);

            if (exception != null)
            {
                Console.WriteLine(exception);
                throw exception;
            }
        }
    }
}
