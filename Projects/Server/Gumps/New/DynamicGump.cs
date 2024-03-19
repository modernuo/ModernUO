/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicGump.cs                                                  *
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
using System.Buffers;
using System.IO.Compression;
using System.IO;

namespace Server.Gumps;

public abstract class DynamicGump : BaseGump
{
    private readonly int _x;
    private readonly int _y;
    private readonly GumpFlags _flags;
    private int _switches;
    private int _textEntries;

    public override int Switches => _switches;
    public override int TextEntries => _textEntries;

    protected DynamicGump(int x, int y, GumpFlags flags = GumpFlags.None)
    {
        _x = x;
        _y = y;
        _flags = flags;
    }

    public override void SendTo(NetState ns)
    {
        GumpBuilder<StaticStringsHandler> builder = new(_flags);

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

    protected abstract void Build(ref GumpBuilder<StaticStringsHandler> builder);

    private void Send(NetState ns, ref GumpBuilder<StaticStringsHandler> builder)
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
