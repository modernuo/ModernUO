/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GumpImage.cs                                                    *
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
    public class GumpImage : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("gumppic");
        public static readonly byte[] HueEquals = Gump.StringToBuffer(" hue=");
        public static readonly byte[] ClassEquals = Gump.StringToBuffer(" class=");

        public GumpImage(int x, int y, int gumpID, int hue = 0, string cls = null)
        {
            X = x;
            Y = y;
            GumpID = gumpID;
            Hue = hue;
            Class = cls;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int GumpID { get; set; }

        public int Hue { get; set; }

        public string Class { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ gumppic {X} {Y} {GumpID}{(Hue == 0 ? "" : $"hue={Hue}")}{(string.IsNullOrEmpty(Class) ? "" : $"class={Class}")} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.WriteAscii(' ');
            writer.WriteAscii(X.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Y.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(GumpID.ToString());

            if (Hue != 0)
            {
                writer.Write(HueEquals);
                writer.WriteAscii(Hue.ToString());
            }

            if (!string.IsNullOrWhiteSpace(Class))
            {
                writer.Write(ClassEquals);
                writer.WriteAscii(Class);
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
