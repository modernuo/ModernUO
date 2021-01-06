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

namespace Server.Gumps
{
    public class GumpTooltip : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("tooltip");

        public GumpTooltip(int number, string args)
        {
            Number = number;
            Args = args;
        }

        public int Number { get; set; }

        public string Args { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            string.IsNullOrEmpty(Args) ? $"{{ tooltip {Number} }}" : $"{{ tooltip {Number} @{Args}@ }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.WriteAscii(' ');
            writer.WriteAscii(Number.ToString());

            if (!string.IsNullOrEmpty(Args))
            {
                writer.WriteAscii(' ');
                writer.WriteAscii('@');
                writer.WriteAscii(Args);
                writer.WriteAscii('@');
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
