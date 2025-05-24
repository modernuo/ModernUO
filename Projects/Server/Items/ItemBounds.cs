/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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
using Size = System.ValueTuple<ushort, ushort>;

namespace Server;

public static class ItemBounds
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ItemBounds));
    private const string _boundsFileName = "ItemBounds.bin";
    private static readonly string _boundsFolder = Path.Combine(Core.BaseDirectory, "Data", "Items");
    private static bool _isGenerating;

    public static void Configure()
    {
        CommandSystem.Register("GenBounds", AccessLevel.Developer, GenBounds_OnCommand);
    }

    [Usage("GenBounds")]
    [Description("Asynchronously generates the ItemBounds.bin file from art.mul/artidx.mul or artLegacyMUL.uop to determine graphic sizes and boundaries.")]
    private static void GenBounds_OnCommand(CommandEventArgs e)
    {
        GenerateBoundsFileAsync(e.Mobile);
    }

    static ItemBounds()
    {
        if (!File.Exists(Path.Combine(_boundsFolder, _boundsFileName)))
        {
            logger.Information("Generating {BoundsFilePath}...", _boundsFileName);
            try
            {
                GenerateBoundsFile(out var sizes, out var bounds);
                Sizes = sizes;
                Bounds = bounds;

                logger.Information("Generated {BoundsFilePath} successfully.", _boundsFileName);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to generate {BoundsFilePath}", _boundsFileName);
            }

            return;
        }

        GenerateTable();
    }

    public static Size[] Sizes { get; private set; }

    public static Rectangle2D[] Bounds { get; private set; }

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
                logger.Information("Generating {BoundsFilePath}...", _boundsFileName);
                if (from != null)
                {
                    Core.LoopContext.Post(() => from.SendMessage("Generating bounds file..."));
                }

                try
                {
                    GenerateBoundsFile(out var sizes, out var bounds);
                    Core.LoopContext.Post(() =>
                        {
                            Sizes = sizes;
                            Bounds = bounds;
                        }
                    );
                }
                catch (Exception ex)
                {
                    if (from != null)
                    {
                        Core.LoopContext.Post(() =>
                            {
                                from.SendMessage("Failed to generate bounds file:");
                                from.SendMessage(ex.Message);
                            }
                        );
                    }

                    logger.Error(ex, "Failed to generate {BoundsFilePath}", _boundsFileName);
                    return;
                }

                if (from != null)
                {
                    Core.LoopContext.Post(
                        () => from.SendMessage(
                            $"Bounds file saved to {Path.GetRelativePath(Core.BaseDirectory, Path.Combine(_boundsFolder, _boundsFileName))}."
                        )
                    );
                }

                logger.Information("Generated {BoundsFilePath} successfully.", "Bounds.bin");
                _isGenerating = false;
            },
            m
        );
    }

    private static void GenerateBoundsFile(out Size[] sizes, out Rectangle2D[] bounds)
    {
        bounds = new Rectangle2D[TileData.ItemTable.Length];
        sizes = new Size[TileData.ItemTable.Length];

        using var artData = new ArtData();
        if (!artData.IsInitialized)
        {
            throw new FileNotFoundException("Unable to load art.mul/artidx.mul or artLegacyMUL.uop");
        }

        PathUtility.EnsureDirectory(_boundsFolder);
        using var fs = new FileStream(Path.Combine(_boundsFolder, _boundsFileName), FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        for (var i = 0; i < bounds.Length; i++)
        {
            var (w, h, b) = artData.GetStaticBounds(i);

            bw.Write(w);
            bw.Write(h);
            bw.Write((short)b.X);
            bw.Write((short)b.Y);
            bw.Write((short)(b.X + b.Width + 1));
            bw.Write((short)(b.Y + b.Height + 1));

            bounds[i].Set(b.X, b.Y, b.Width, b.Height);
            sizes[i] = (w, h);
        }
    }

    private static void GenerateTable()
    {
        Bounds = new Rectangle2D[TileData.ItemTable.Length];
        Sizes = new Size[TileData.ItemTable.Length];

        using var fs = new FileStream(Path.Combine(_boundsFolder, _boundsFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
        using var bin = new BinaryReader(fs);

        var count = Math.Min(Bounds.Length, (int)(fs.Length / 8));

        for (var i = 0; i < count; ++i)
        {
            Sizes[i] = (bin.ReadUInt16(), bin.ReadUInt16());

            int xMin = bin.ReadInt16();
            int yMin = bin.ReadInt16();
            int xMax = bin.ReadInt16();
            int yMax = bin.ReadInt16();

            Bounds[i].Set(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
