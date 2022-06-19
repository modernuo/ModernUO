/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: HousePackets.cs                                                 *
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
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Server.Logging;
using Server.Network;

namespace Server.Multis
{
    public static class HousePackets
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(HousePackets));

        public static void SendBeginHouseCustomization(this NetState ns, Serial house)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[17]);
            writer.Write((byte)0xBF); // Packet Id
            writer.Write((ushort)17);
            writer.Write((short)0x20); // Sub-packet
            writer.Write(house);
            writer.Write((byte)0x04); // command
            writer.Write((ushort)0x0000);
            writer.Write((ushort)0xFFFF);
            writer.Write((ushort)0xFFFF);
            writer.Write((byte)0xFF);

            ns.Send(writer.Span);
        }

        public static void SendEndHouseCustomization(this NetState ns, Serial house)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[17]);
            writer.Write((byte)0xBF); // Packet Id
            writer.Write((ushort)17);
            writer.Write((short)0x20); // Sub-packet
            writer.Write(house);
            writer.Write((byte)0x05); // command
            writer.Write((ushort)0x0000);
            writer.Write((ushort)0xFFFF);
            writer.Write((ushort)0xFFFF);
            writer.Write((byte)0xFF);

            ns.Send(writer.Span);
        }

        public static void SendDesignStateGeneral(this NetState ns, Serial house, int revision)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[13]);
            writer.Write((byte)0xBF); // Packet Id
            writer.Write((ushort)13);
            writer.Write((short)0x1D); // Sub-packet
            writer.Write(house);
            writer.Write(revision);

            ns.Send(writer.Span);
        }

        private const int planeCount = 9;
        private const int maxPlaneLength = 0x400;
        private const int maxPerPlaneOffsetBuffer = 750;
        private static readonly int maxPackedPlaneOffsetBuffer = Zlib.MaxPackSize(maxPerPlaneOffsetBuffer * 5);

        public static byte[] CreateHouseDesignStateDetailed(Serial serial, int revision, MultiComponentList components)
        {
            var xMin = components.Min.X;
            var yMin = components.Min.Y;
            var xMax = components.Max.X;
            var yMax = components.Max.Y;
            var width = xMax - xMin + 1;
            var height = yMax - yMin + 1;
            var tiles = components.List;

            var planeLength = width * height * 2;
            using var planeOffsetWriter = new SpanWriter(0x500, true);
            using var planesWriter = new SpanWriter(planeLength * planeCount);

            Span<bool> planesUsed = stackalloc bool[9];
            int index;
            var totalPlaneOffsets = 0;
            var totalPlanes = 0;

            for (var i = 0; i < tiles.Length; ++i)
            {
                var mte = tiles[i];
                var x = mte.OffsetX - xMin;
                var y = mte.OffsetY - yMin;
                int z = mte.OffsetZ;
                var floor = TileData.ItemTable[mte.ItemId & TileData.MaxItemValue].Height <= 0;

                int plane = z switch
                {
                    0  => 0,
                    7  => 1,
                    27 => 2,
                    47 => 3,
                    67 => 4,
                    _  => -1
                };

                if (plane > -1)
                {
                    int size;
                    if (plane == 0)
                    {
                        size = height;
                    }
                    else if (floor)
                    {
                        size = height - 2;
                        x -= 1;
                        y -= 1;
                    }
                    else
                    {
                        size = height - 1;
                        plane += 4;
                    }

                    index = (x * size + y) * 2;

                    if (x >= 0 && y >= 0 && y < size && index + 1 < maxPlaneLength)
                    {
                        var planeUsed = planesUsed[plane];
                        var planeWriterIndex = planeLength * plane;
                        if (!planeUsed)
                        {
                            planesUsed[plane] = true;
                            totalPlanes++;
                            planesWriter.Seek(planeWriterIndex, SeekOrigin.Begin);
                            planesWriter.Clear(planeLength);
                        }

                        planesWriter.Seek(planeWriterIndex + index, SeekOrigin.Begin);
                        planesWriter.Write(mte.ItemId);
                        continue;
                    }
                }

                planeOffsetWriter.Write(mte.ItemId);
                planeOffsetWriter.Write((byte)mte.OffsetX);
                planeOffsetWriter.Write((byte)mte.OffsetY);
                planeOffsetWriter.Write((byte)mte.OffsetZ);
                totalPlaneOffsets++;
            }

            var maxPlanesLength = (Zlib.MaxPackSize(planeLength) + 4) * totalPlanes;
            var maxPlaneOffsetLength = totalPlaneOffsets == 0 ? 0
                : (maxPackedPlaneOffsetBuffer + 4) * (totalPlaneOffsets / maxPerPlaneOffsetBuffer + 1);

            var buffer = GC.AllocateUninitializedArray<byte>(18 + maxPlanesLength + maxPlaneOffsetLength);
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xD8); // Packet ID
            writer.Seek(2, SeekOrigin.Current); // Length

            writer.Write((byte)0x03); // Compression Type
            writer.Write((byte)0x00); // Unknown
            writer.Write(serial);
            writer.Write(revision);
            writer.Write((short)tiles.Length);
            writer.Seek(3, SeekOrigin.Current); // Buffer Length, Plane Count

            var totalLength = 1; // includes plane count

            for (var i = 0; i < planeCount; i++)
            {
                if (!planesUsed[i])
                {
                    continue;
                }

                int size = i switch
                {
                    0   => planeLength,
                    < 5 => (width - 1) * (height - 2) * 2,
                    _   => width * (height - 1) * 2
                };

                var planeWriterIndex = planeLength * i;

                var source = planesWriter.RawBuffer.Slice(planeWriterIndex, size);

                writer.Write((byte)(0x20 | i));
                WritePacked(source, ref writer, out int destLength);

                totalLength += 4 + destLength;
            }

            index = 0;
            while (totalPlaneOffsets > 0)
            {
                var count = Math.Min(maxPerPlaneOffsetBuffer, totalPlaneOffsets);
                totalPlaneOffsets -= count;

                var source = planeOffsetWriter.RawBuffer.Slice(index * 5, count * 5);

                writer.Write((byte)(9 + index++));
                WritePacked(source, ref writer, out int destLength);
                totalLength += 4 + destLength;
                totalPlanes++;
            }

            writer.Seek(15, SeekOrigin.Begin);
            writer.Write((short)totalLength);
            writer.Write((byte)totalPlanes);
            writer.WritePacketLength();

            // TODO: Avoid this somehow.
            Array.Resize(ref buffer, writer.BytesWritten);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WritePacked(ReadOnlySpan<byte> source, ref SpanWriter writer, out int length)
        {
            var size = source.Length;
            var dest = writer.RawBuffer[(writer.Position + 3)..];
            length = dest.Length;

            var ce = Zlib.Pack(dest, ref length, source, ZlibQuality.Default);

            if (ce != ZlibError.Okay)
            {
                logger.Warning("ZLib error: {Error} (#{ErrorCode})", ce, (int)ce);
                length = 0;
                size = 0;
            }

            writer.Write((byte)size);
            writer.Write((byte)length);
            writer.Write((byte)(((size >> 4) & 0xF0) | ((length >> 8) & 0xF)));
            writer.Seek(length, SeekOrigin.Current);
        }
    }
}
