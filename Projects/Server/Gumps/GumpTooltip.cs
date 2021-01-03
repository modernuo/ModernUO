/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpTooltip.cs                                                  *
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
    public class GumpTooltip : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("tooltip");

        public GumpTooltip(int number, string args)
        {
            Number = number;
            Args = args;
        }

        public int Number { get; set; }

        public string Args { get; set; }

        public override string Compile(NetState ns) =>
            string.IsNullOrEmpty(Args) ? $"{{ tooltip {Number} }}" : $"{{ tooltip {Number} @{Args}@ }}";
        public override string Compile(OrderedHashSet<string> strings) =>
            string.IsNullOrEmpty(Args) ? $"{{ tooltip {Number} }}" : $"{{ tooltip {Number} @{Args}@ }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Number);

            if (!string.IsNullOrEmpty(Args))
            {
                disp.AppendLayout(Args);
            }
        }

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(m_LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Number.ToString());

            if (!string.IsNullOrEmpty(Args))
            {
                writer.Write((byte)0x20); // ' '
                writer.Write((byte)0x40); // '@'
                writer.WriteAscii(Args);
                writer.Write((byte)0x40); // '@'
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
