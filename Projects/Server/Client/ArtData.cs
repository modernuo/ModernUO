/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArtData.cs                                                      *
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
using System.Runtime.InteropServices;

namespace Server;

public class ArtData : IDisposable
{
    private readonly FileStream _dataStream;
    private readonly Dictionary<int, UOPEntry> _dataRanges;

    public bool IsInitialized => _dataRanges.Count > 0 && _dataStream != null;

    public ArtData()
    {
        var artPath = Core.FindDataFile("art.mul", false);
        if (artPath != null)
        {
            var idxPath = Core.FindDataFile("artidx.mul", false);
            if (idxPath != null)
            {
                _dataRanges = LoadMulRanges(idxPath);
                _dataStream = new FileStream(artPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return;
            }
        }

        artPath = Core.FindDataFile("artLegacyMUL.uop", false);
        if (artPath != null)
        {

            _dataStream = new FileStream(artPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _dataRanges = UOPFiles.ReadUOPIndexes(_dataStream, ".tga", 0x14000, 5);
        }
    }

    public (ushort Width, ushort Height, Rectangle2D Bounds) GetStaticBounds(int index)
    {
        if (index is < 0 or > 0x10000)
        {
            return (0, 0, Rectangle2D.Empty);
        }

        index += 16384;

        if (!_dataRanges.TryGetValue(index, out var entry))
        {
            return (0, 0, Rectangle2D.Empty);
        }

        Span<ushort> buffer = stackalloc ushort[entry.Size / 2];
        _dataStream.Seek(entry.Offset, SeekOrigin.Begin);
        _ = _dataStream.Read(MemoryMarshal.AsBytes(buffer));

        var width = buffer[2];
        var height = buffer[3];

        if (width == 0 || height == 0)
        {
            return (0, 0, Rectangle2D.Empty);
        }

        return (width, height, GetBoundsFromRGBA1555Bitmap(width, height, buffer[4..]));
    }

    private static Dictionary<int, UOPEntry> LoadMulRanges(string idxPath)
    {
        using var fs = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);

        var count = (int)(fs.Length / 12);

        var ranges = new Dictionary<int, UOPEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var offset = br.ReadInt32();
            var length = br.ReadInt32();
            br.ReadInt32(); // Skip the Data field since we don't use it

            if (length > 0)
            {
                ranges[i] = new UOPEntry(offset, length);
            }
        }

        ranges.TrimExcess();
        return ranges;
    }

    public static Rectangle2D GetBoundsFromRGBA1555Bitmap(int width, int height, ReadOnlySpan<ushort> data)
    {
        ReadOnlySpan<ushort> lookups = data[..height];
        data = data[height..];

        int xMin = width;
        int yMin = height;
        int xMax = -1;
        int yMax = -1;

        for (int y = 0; y < height; y++)
        {
            var i = lookups[y];
            var x = 0;

            while (true)
            {
                int startX = data[i++];
                var pixelCount = data[i++];

                if (startX + pixelCount == 0 || startX > width)
                {
                    break;
                }

                x += startX;
                for (var end = x + pixelCount; x < end; x++)
                {
                    // Get the pixel value from the bitmap data
                    ushort pixel = data[i++];

                    // Check if the pixel is non-black (0 in 555 RGB is completely black)
                    if (pixel == 0)
                    {
                        continue;
                    }

                    if (x < xMin)
                    {
                        xMin = x;
                    }

                    if (x > xMax)
                    {
                        xMax = x;
                    }

                    if (y < yMin)
                    {
                        yMin = y;
                    }

                    if (y > yMax)
                    {
                        yMax = y;
                    }
                }
            }
        }

        return xMax switch
        {
            // If no non-black pixels were found, return an empty rectangle
            -1 => Rectangle2D.Empty,
            _  => new Rectangle2D(xMin, yMin, xMax - xMin, yMax - yMin)
        };
    }

    public void Dispose()
    {
        _dataStream?.Dispose();
        GC.SuppressFinalize(this);
    }

    ~ArtData()
    {
        _dataStream?.Dispose();
    }
}
