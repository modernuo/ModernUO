/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GumpItem.cs                                                     *
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
    public class GumpItem : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("tilepic");
        public static readonly byte[] LayoutNameHue = Gump.StringToBuffer("tilepichue");

        public GumpItem(int x, int y, int itemID, int hue = 0)
        {
            X = x;
            Y = y;
            ItemID = itemID;
            Hue = hue;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int ItemID { get; set; }

        public int Hue { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            Hue == 0 ? $"{{ tilepic {X} {Y} {ItemID} }}" : $"{{ tilepichue {X} {Y} {ItemID} {Hue} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(Hue == 0 ? LayoutName : LayoutNameHue);
            writer.WriteAscii(' ');
            writer.WriteAscii(X.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Y.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(ItemID.ToString());

            if (Hue != 0)
            {
                writer.WriteAscii(' ');
                writer.WriteAscii(Hue.ToString());
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
