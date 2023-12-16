/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicGridGump.cs                                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps.Components;
using Server.Gumps.Enums;
using Server.Network;
using System;
using System.Buffers;
using System.IO.Compression;
using System.IO;

namespace Server.Gumps;

public abstract class DynamicGridGump : BaseGump
{
    protected const int ArrowLeftID1 = 0x15E3;
    protected const int ArrowLeftID2 = 0x15E7;
    protected const int ArrowLeftWidth = 16;
    protected const int ArrowLeftHeight = 16;
    protected const int ArrowRightID1 = 0x15E1;
    protected const int ArrowRightID2 = 0x15E5;
    protected const int ArrowRightWidth = 16;
    protected const int ArrowRightHeight = 16;

    private readonly int _x;
    private readonly int _y;
    private readonly GumpFlags _flags;
    private int _switches;
    private int _textEntries;

    protected virtual ushort BorderSize => 10;
    protected virtual ushort OffsetSize => 1;
    protected virtual ushort EntryHeight => 20;
    protected virtual ushort OffsetGumpId => 0x0A40;
    protected virtual ushort HeaderGumpId => 0x0E14;
    protected virtual ushort EntryGumpId => 0x0BBC;
    protected virtual ushort BackGumpId => 0x13BE;
    protected virtual ushort TextHue => 0;
    protected virtual ushort TextOffsetX => 2;
    public override int Switches => _switches;
    public override int TextEntries => _textEntries;

    protected DynamicGridGump(int x, int y, GumpFlags flags = GumpFlags.None)
    {
        _x = x;
        _y = y;
        _flags = flags;
    }

    protected static int GetButtonID(int typeCount, int type, int index)
    {
        return 1 + index * typeCount + type;
    }

    protected static bool SplitButtonID(int buttonID, int typeCount, out int type, out int index)
    {
        if (buttonID < 1)
        {
            type = 0;
            index = 0;
            return false;
        }

        buttonID -= 1;
        index = Math.DivRem(buttonID, typeCount, out type);

        return true;
    }

    public override void SendTo(NetState ns)
    {
        GridGumpBuilder<StaticStringsHandler> builder = new(_flags, BorderSize, OffsetSize, EntryHeight,
            OffsetGumpId, HeaderGumpId, EntryGumpId, BackGumpId, TextHue, TextOffsetX);

        try
        {
            Build(ref builder);

            _switches = builder.Switches;
            _textEntries = builder.TextEntries;

            ns.AddGump(this);
            Send(ns, ref builder);
        }
        finally
        {
            builder.Dispose();
        }
    }

    protected abstract void Build(ref GridGumpBuilder<StaticStringsHandler> builder);

    private void Send(NetState ns, ref GridGumpBuilder<StaticStringsHandler> builder)
    {
        ref readonly StaticStringsHandler stringsWriter = ref builder.StringsWriter;

        int worstLayoutLength = Zlib.MaxPackSize(builder.LayoutSize);
        int worstStringsLength = Zlib.MaxPackSize(stringsWriter.BytesWritten);

        int maxLength = 40 + worstLayoutLength + worstStringsLength;

        SpanWriter writer = new(maxLength);
        writer.Write((byte)0xDD); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(Serial);
        writer.Write(TypeID);
        writer.Write(_x);
        writer.Write(_y);

        builder.FinalizeLayout();
        OutgoingGumpPackets.WritePacked(builder.Layout, ref writer);

        writer.Write(stringsWriter.Count);
        OutgoingGumpPackets.WritePacked(stringsWriter.Span, ref writer);

        writer.WritePacketLength();

        ns.Send(writer.Span);

        writer.Dispose();
    }
}
