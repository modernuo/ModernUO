/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
    private static readonly string _pathToBounds = Path.Combine(Core.BaseDirectory, "Data", "Binary", "Bounds.bin");

    static ItemBounds()
    {
        Table = new Rectangle2D[TileData.ItemTable.Length];

        if (!File.Exists(_pathToBounds))
        {
            logger.Information("Generating {BoundsFilePath}...", "Bounds.bin");
            try
            {
                GenerateBoundsFile();
                logger.Information("Generated {BoundsFilePath} successfully.", "Bounds.bin");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to generate {BoundsFilePath}", "Bounds.bin");
            }

            return;
        }

        GenerateTable();
    }

    public static Rectangle2D[] Table { get; }

    public static void GenerateBoundsFile()
    {
        using var artData = new ArtData();
        if (!artData.IsInitialized)
        {
            throw new FileNotFoundException("Unable to load art.mul/artidx.mul or artLegacyMUL.uop");
        }

        using var fs = new FileStream(_pathToBounds, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        for (var i = 0; i < Table.Length; i++)
        {
            var bounds = artData.GetStaticBounds(i);

            bw.Write((short)bounds.X);
            bw.Write((short)bounds.Y);
            bw.Write((short)(bounds.X + bounds.Width + 1));
            bw.Write((short)(bounds.Y + bounds.Height + 1));

            Table[i] = bounds;
        }
    }

    public static void GenerateTable()
    {
        using var fs = new FileStream(_pathToBounds, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var bin = new BinaryReader(fs);

        var count = Math.Min(Table.Length, (int)(fs.Length / 8));

        for (var i = 0; i < count; ++i)
        {
            int xMin = bin.ReadInt16();
            int yMin = bin.ReadInt16();
            int xMax = bin.ReadInt16();
            int yMax = bin.ReadInt16();

            Table[i].Set(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
