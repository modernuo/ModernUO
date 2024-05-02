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
using System.Threading;
using Server.Logging;

namespace Server;

public static class ItemBounds
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ItemBounds));
    private static readonly string _pathToBounds = Path.Combine(Core.BaseDirectory, "Data", "Binary", "Bounds.bin");
    private static bool _isGenerating;

    public static void Configure()
    {
        CommandSystem.Register("GenBounds", AccessLevel.Developer, GenBounds_OnCommand);
    }

    [Usage("GenBounds")]
    [Description("Asynchronously generates the bounds.bin file from art.mul/artidx.mul or artLegacyMUL.uop to determine container boundaries.")]
    private static void GenBounds_OnCommand(CommandEventArgs e)
    {
        GenerateBoundsFileAsync(e.Mobile);
    }

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

    public static Rectangle2D[] Table { get; private set; }

    private static void GenerateBoundsFileAsync(Mobile m)
    {
        if (_isGenerating)
        {
            m?.SendMessage("Bounds file is already being generated.");
            return;
        }

        _isGenerating = true;
        ThreadPool.QueueUserWorkItem(
            state =>
            {
                var from = state as Mobile;
                logger.Information("Generating {BoundsFilePath}...", "Bounds.bin");
                if (from != null)
                {
                    Core.LoopContext.Post(() => from?.SendMessage("Generating bounds file..."));
                }

                try
                {
                    var table = GenerateBoundsFile();
                    Core.LoopContext.Post(() => Table = table);
                }
                catch (Exception ex)
                {
                    if (from != null)
                    {
                        Core.LoopContext.Post(() =>
                            {
                                from?.SendMessage("Failed to generate bounds file:");
                                from?.SendMessage(ex.Message);
                            }
                        );
                    }

                    logger.Error(ex, "Failed to generate {BoundsFilePath}", "Bounds.bin");
                    return;
                }

                if (from != null)
                {
                    Core.LoopContext.Post(
                        () => from?.SendMessage(
                            $"Bounds file saved to {Path.GetRelativePath(Core.BaseDirectory, _pathToBounds)}."
                        )
                    );
                }

                logger.Information("Generated {BoundsFilePath} successfully.", "Bounds.bin");
                _isGenerating = false;
            },
            m
        );
    }

    private static Rectangle2D[] GenerateBoundsFile()
    {
        var table = new Rectangle2D[TileData.ItemTable.Length];
        using var artData = new ArtData();
        if (!artData.IsInitialized)
        {
            throw new FileNotFoundException("Unable to load art.mul/artidx.mul or artLegacyMUL.uop");
        }

        using var fs = new FileStream(_pathToBounds, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        for (var i = 0; i < table.Length; i++)
        {
            var bounds = artData.GetStaticBounds(i);

            bw.Write((short)bounds.X);
            bw.Write((short)bounds.Y);
            bw.Write((short)(bounds.X + bounds.Width + 1));
            bw.Write((short)(bounds.Y + bounds.Height + 1));

            table[i] = bounds;
        }

        return table;
    }

    private static void GenerateTable()
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
