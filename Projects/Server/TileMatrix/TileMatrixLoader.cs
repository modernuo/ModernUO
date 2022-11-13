/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using Server.Logging;

namespace Server;

internal static class TileMatrixLoader
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TileMatrixLoader));

    internal static void LoadTileMatrix()
    {
        logger.Information("Loading maps");

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

        if (exception == null)
        {
            logger.Information("Loading maps {Status} ({Duration:F2} seconds)", "done", stopwatch.Elapsed.TotalSeconds);
        }
        else
        {
            logger.Error(
                exception,
                "Loading maps {Status} ({Duration:F2} seconds)",
                "failed",
                stopwatch.Elapsed.TotalSeconds
            );
            throw exception;
        }
    }
}
