/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StaticGump.cs                                                   *
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
using System.IO;
using System.IO.Compression;

namespace Server.Gumps;

// StaticLayoutGump is a self referencing generic class, allowing the static variables to propagate for each concrete type.
public abstract class StaticLayoutGump<TSelf> : BaseGump where TSelf : StaticLayoutGump<TSelf>
{
    private static LayoutEntry _layout;
    private static DynamicStringsEntry _strings;

    protected abstract int X { get; }
    protected abstract int Y { get; }
    protected virtual GumpFlags Flags => GumpFlags.None;
    public override int Switches => _layout.Switches;
    public override int TextEntries => _layout.TextEntries;

    public override void SendTo(NetState ns)
    {
        if (_layout.IsEmpty)
        {
            GumpBuilder<DynamicStringsHandler> builder = new(Flags);

            try
            {
                Build(ref builder);
                builder.CompileCompressed(out _layout, out _strings);
            }
            finally
            {
                builder.Dispose();
            }
        }

        DynamicStringsFiller filler = _strings.CreateFiller();
        FillStrings(ref filler);

        if (!filler.Finalize())
        {
            throw new Exception();
        }

        ns.AddGump(this);
        Send(ns, in _layout, in filler);
    }

    protected abstract void Build(ref GumpBuilder<DynamicStringsHandler> builder);

    protected abstract void FillStrings(ref DynamicStringsFiller strings);

    private void Send(NetState ns, in LayoutEntry layout, in DynamicStringsFiller strings)
    {
        int worstLayoutLength = Zlib.MaxPackSize(layout.UncompressedLength);
        int worstStringsLength = Zlib.MaxPackSize(strings.UncompressedLength);

        int maxLength = 40 + worstLayoutLength + worstStringsLength;

        SpanWriter writer = new(maxLength);
        writer.Write((byte)0xDD); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(Serial);
        writer.Write(TypeID);
        writer.Write(X);
        writer.Write(Y);

        writer.Write(layout.Data);

        writer.Write(strings.Count);
        OutgoingGumpPackets.WritePacked(strings.Data, ref writer);

        writer.WritePacketLength();

        ns.Send(writer.Span);

        writer.Dispose();
    }
}
