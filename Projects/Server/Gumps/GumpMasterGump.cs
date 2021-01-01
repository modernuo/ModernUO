/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpMasterGump.cs                                               *
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
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpMasterGump : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("mastergump");

        public GumpMasterGump(int gumpID) => GumpID = gumpID;

        public int GumpID { get; set; }

        public override string Compile(NetState ns) => $"{{ mastergump {GumpID} }}";
        public override string Compile(IndexList<string> strings) => $"{{ mastergump {GumpID} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(GumpID);
        }

        public override void AppendTo(ref SpanWriter writer, IndexList<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(m_LayoutName);
            writer.WriteAscii(GumpID.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
