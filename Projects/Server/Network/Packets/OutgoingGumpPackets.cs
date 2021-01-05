/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingGumpPackets.cs                                          *
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
using Server.Collections;
using Server.Gumps;

namespace Server.Network
{
    public static class OutgoingGumpPackets
    {
        public static void SendCloseGump(this NetState ns, int typeId, int buttonId)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[13]);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)13);

            writer.Write((short)0x04);
            writer.Write(typeId);
            writer.Write(buttonId);

            ns.Send(writer.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WritePacked(ref SpanWriter writer, ReadOnlySpan<byte> span)
        {
            var length = span.Length;

            if (length == 0)
            {
                writer.Write(0);
                return;
            }

            var wantLength = 1 + span.Length * 1024 / 1000;

            wantLength += 4095;
            wantLength &= ~4095;

            var packBuffer = ArrayPool<byte>.Shared.Rent(wantLength);
            var packLength = wantLength;

            Zlib.Pack(packBuffer, ref packLength, span, length, ZlibQuality.Default);

            writer.Write(4 + packLength);
            writer.Write(length);
            writer.Write(packBuffer.AsSpan(0, packLength));

            ArrayPool<byte>.Shared.Return(packBuffer);
        }

        public static void SendDisplayGump(this NetState ns, Gump gump, out int switches, out int entries)
        {
            switches = 0;
            entries = 0;

            if (ns == null)
            {
                return;
            }

            var packed = ns.Unpack;

            var writer = new SpanWriter(512, true);
            writer.Write((byte)(packed ? 0xDD : 0xB0)); // Packet ID
            writer.Seek(2, SeekOrigin.Current);

            writer.Write(gump.Serial);
            writer.Write(gump.TypeID);
            writer.Write(gump.X);
            writer.Write(gump.Y);

            var spanWriter = new SpanWriter(512, true);

            if (!gump.Draggable)
            {
                spanWriter.Write(Gump.NoMove);
            }

            if (!gump.Closable)
            {
                spanWriter.Write(Gump.NoClose);
            }

            if (!gump.Disposable)
            {
                spanWriter.Write(Gump.NoDispose);
            }

            if (!gump.Resizable)
            {
                spanWriter.Write(Gump.NoResize);
            }

            var stringsList = new OrderedHashSet<string>(11);

            foreach (var entry in gump.Entries)
            {
                entry.AppendTo(ref spanWriter, stringsList, ref entries, ref switches);
            }

            if (packed)
            {
                spanWriter.Write((byte)0); // Layout text terminator
                WritePacked(ref writer, spanWriter.Span);
            }
            else
            {
                writer.Write((ushort)spanWriter.BytesWritten);
                writer.Write(spanWriter.Span);
            }

            if (packed)
            {
                writer.Write(stringsList.Count);
            }
            else
            {
                writer.Write((ushort)stringsList.Count);
            }

            spanWriter.Seek(0, SeekOrigin.Begin);

            foreach (var str in stringsList)
            {
                var s = str ?? "";
                spanWriter.Write((ushort)s.Length);
                spanWriter.WriteBigUni(s);
            }

            if (packed)
            {
                WritePacked(ref writer, spanWriter.Span);
            }
            else
            {
                writer.Write(spanWriter.Span);
            }

            writer.WritePacketLength();

            ns.Send(writer.Span);
            spanWriter.Dispose(); // Can't use using and refs, so we dispose manually
        }

        public static void SendDisplaySignGump(this NetState ns, Serial serial, int gumpId, string unknown, string caption)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            unknown ??= "";
            caption ??= "";

            var length = 15 + unknown.Length + caption.Length;
            var writer = new SpanWriter(stackalloc byte[length]);
            writer.Write((byte)0x8B); // Packet ID
            writer.Write((ushort)length);

            writer.Write(serial);
            writer.Write((short)gumpId);
            writer.Write((short)(unknown.Length + 1));
            writer.WriteAsciiNull(unknown);
            writer.Write((short)(caption.Length + 1));
            writer.WriteAsciiNull(caption);

            ns.Send(writer.Span);
        }
    }
}
