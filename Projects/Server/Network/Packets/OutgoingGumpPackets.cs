/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using Server.Buffers;
using Server.Collections;
using Server.Gumps;
using Server.Logging;

namespace Server.Network;

public static class OutgoingGumpPackets
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(OutgoingGumpPackets));

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

    private static readonly byte[] _layoutBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
    private static readonly byte[] _stringsBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
    private static readonly byte[] _packBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
    private static readonly OrderedHashSet<string> _stringsList = new(32);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePacked(ReadOnlySpan<byte> span, ref SpanWriter writer)
    {
        var length = span.Length;

        if (length == 0)
        {
            writer.Write(0);
            return;
        }

        var wantLength = Zlib.MaxPackSize(length);
        var packBuffer = _packBuffer;
        byte[] rentedBuffer = null;

        if (wantLength > packBuffer.Length)
        {
            packBuffer = rentedBuffer = STArrayPool<byte>.Shared.Rent(wantLength);
        }

        var packLength = wantLength;

        var error = Zlib.Pack(packBuffer, ref packLength, span, length, ZlibQuality.Default);

        if (error != ZlibError.Okay)
        {
            logger.Warning("Gump compression failed: {Error}", error);

            writer.Write(4);
            writer.Write(0);
            return;
        }

        writer.Write(4 + packLength);
        writer.Write(length);
        writer.Write(packBuffer.AsSpan(0, packLength));

        if (rentedBuffer != null)
        {
            STArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    public static void SendDisplayGump(this NetState ns, Gump gump, out int switches, out int entries)
    {
        switches = 0;
        entries = 0;

        if (ns.CannotSendPackets())
        {
            return;
        }

        var packed = ns.Unpack;

        var layoutWriter = new SpanWriter(_layoutBuffer);

        if (!gump.Draggable)
        {
            layoutWriter.Write(Gump.NoMove);
        }

        if (!gump.Closable)
        {
            layoutWriter.Write(Gump.NoClose);
        }

        if (!gump.Disposable)
        {
            layoutWriter.Write(Gump.NoDispose);
        }

        if (!gump.Resizable)
        {
            layoutWriter.Write(Gump.NoResize);
        }

        foreach (var entry in gump.Entries)
        {
            entry.AppendTo(ref layoutWriter, _stringsList, ref entries, ref switches);
        }

        var stringsWriter = new SpanWriter(_stringsBuffer);

        foreach (var str in _stringsList)
        {
            var s = str ?? "";
            stringsWriter.Write((ushort)s.Length);
            stringsWriter.WriteBigUni(s);
        }

        int maxLength;
        if (packed)
        {
            var worstLayoutLength = Zlib.MaxPackSize(layoutWriter.BytesWritten);
            var worstStringsLength = Zlib.MaxPackSize(stringsWriter.BytesWritten);
            maxLength = 40 + worstLayoutLength + worstStringsLength;
        }
        else
        {
            maxLength = 23 + layoutWriter.BytesWritten + stringsWriter.BytesWritten;
        }

        var writer = new SpanWriter(maxLength);
        writer.Write((byte)(packed ? 0xDD : 0xB0)); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(gump.Serial);
        writer.Write(gump.TypeID);
        writer.Write(gump.X);
        writer.Write(gump.Y);

        if (packed)
        {
            layoutWriter.Write((byte)0); // Layout text terminator
            WritePacked(layoutWriter.Span, ref writer);

            writer.Write(_stringsList.Count);
            WritePacked(stringsWriter.Span, ref writer);
        }
        else
        {
            writer.Write((ushort)layoutWriter.BytesWritten);
            writer.Write(layoutWriter.Span);

            writer.Write((ushort)_stringsList.Count);
            writer.Write(stringsWriter.Span);
        }

        writer.WritePacketLength();

        ns.Send(writer.Span);

        layoutWriter.Dispose();  // Just in case
        stringsWriter.Dispose(); // Just in case

        if (_stringsList.Count > 0)
        {
            _stringsList.Clear();
        }
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
        writer.WriteAsciiNull(unknown);
        writer.Write((short)(caption.Length + 1));
        writer.WriteAsciiNull(caption);

        ns.Send(writer.Span);
    }
}
