/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingGumpPackets.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Compression;
using Server.Logging;
using Server.Network;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Server.Gumps;

public static class OutgoingGumpPackets
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(OutgoingGumpPackets));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePacked(ReadOnlySpan<byte> span, ref SpanWriter writer)
    {
        var length = span.Length;

        if (length == 0)
        {
            writer.Write(0);
            return;
        }

        var dest = writer.RawBuffer[(writer.Position + 8)..];

        var bytesPacked = Deflate.Standard.Pack(dest, span);
        if (bytesPacked == 0)
        {
            _logger.Warning("Gump compression failed");

            writer.Write(4);
            writer.Write(0);
            return;
        }

        writer.Write(4 + bytesPacked);
        writer.Write(length);
        writer.Seek(bytesPacked, SeekOrigin.Current);
    }

    public static void SendDisplaySignGump(this NetState ns, Serial serial, int gumpId, string unknown, string caption)
    {
        if (ns.CannotSendPackets())
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
        writer.WriteLatin1Null(unknown);
        writer.Write((short)(caption.Length + 1));
        writer.WriteLatin1Null(caption);

        ns.Send(writer.Span);
    }

    public static void SendCloseGump(this NetState ns, int typeId, int buttonId)
    {
        if (ns.CannotSendPackets())
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
}
