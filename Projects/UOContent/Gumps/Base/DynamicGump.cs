/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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

using System.Buffers;
using System.IO;
using Server.Network;

namespace Server.Gumps;

public abstract class DynamicGump : BaseGump
{
    private int _switches;
    private int _textEntries;

    public override int Switches => _switches;
    public override int TextEntries => _textEntries;

    public DynamicGump(int x, int y) : base(x, y)
    {
    }

    protected abstract void BuildLayout(ref DynamicGumpBuilder builder);

    public override void Compile(ref SpanWriter writer)
    {
        writer.Write((byte)0xDD); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(Serial);
        writer.Write(TypeID);
        writer.Write(X);
        writer.Write(Y);

        DynamicGumpBuilder gumpBuilder = new DynamicGumpBuilder();
        BuildLayout(ref gumpBuilder);
        gumpBuilder.FinalizeLayout();

        _switches = gumpBuilder.Switches;
        _textEntries = gumpBuilder.TextEntries;

        OutgoingGumpPackets.WritePacked(gumpBuilder.LayoutData, ref writer);

        writer.Write(gumpBuilder._stringsCount);
        OutgoingGumpPackets.WritePacked(gumpBuilder.StringsData, ref writer);

        gumpBuilder.Dispose();

        writer.WritePacketLength();
    }
}
