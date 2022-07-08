/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ItemBounds.cs                                                   *
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
using System.IO;
using Server.Logging;

namespace Server;

public static class ItemBounds
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ItemBounds));

    static ItemBounds()
    {
        Table = new Rectangle2D[TileData.ItemTable.Length];

        if (!File.Exists("Data/Binary/Bounds.bin"))
        {
            logger.Error("Data/Binary/Bounds.bin does not exist");
            return;
        }

        using var fs = new FileStream(
            "Data/Binary/Bounds.bin",
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );
        var bin = new BinaryReader(fs);

        var count = Math.Min(Table.Length, (int)(fs.Length / 8));

        for (var i = 0; i < count; ++i)
        {
            int xMin = bin.ReadInt16();
            int yMin = bin.ReadInt16();
            int xMax = bin.ReadInt16();
            int yMax = bin.ReadInt16();

            Table[i].Set(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        bin.Close();
    }

    public static Rectangle2D[] Table { get; }
}
