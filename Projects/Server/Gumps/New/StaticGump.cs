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
using Server.Network;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace Server.Gumps
{
    public abstract class StaticGump : BaseGump
    {
        protected abstract int X { get; }
        protected abstract int Y { get; }

        public override void SendTo(NetState ns)
        {
            GetGumpData(out LayoutEntry layout, out StringsEntry strings);
            ns.AddGump(this);

            Send(ns, in layout, in strings);
        }

        protected abstract void GetGumpData(out LayoutEntry layout, out StringsEntry strings);

        private void Send(NetState ns, in LayoutEntry layout, in StringsEntry strings)
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

            if (!layout.Compressed)
            {
                OutgoingGumpPackets.WritePacked(layout.Data, ref writer);
            }
            else
            {
                writer.Write(layout.Data);
            }

            writer.Write(strings.Count);

            if (!strings.Compressed)
            {
                OutgoingGumpPackets.WritePacked(strings.Data, ref writer);
            }
            else
            {
                writer.Write(strings.Data);
            }

            writer.WritePacketLength();

            ns.Send(writer.Span);

            writer.Dispose();
        }
    }
}
