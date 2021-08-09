/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GumpHtml.cs                                                     *
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
    public class GumpHtml : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("htmlgump");

        public GumpHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Text = text;
            Background = background;
            Scrollbar = scrollbar;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Text { get; set; }

        public bool Background { get; set; }

        public bool Scrollbar { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ htmlgump {X} {Y} {Width} {Height} {strings.GetOrAdd(Text ?? "")} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.WriteAscii(' ');
            writer.WriteAscii(X.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Y.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Width.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Height.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(strings.GetOrAdd(Text ?? "").ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Background ? '1' : '0');
            writer.WriteAscii(' ');
            writer.WriteAscii(Scrollbar ? '1' : '0');
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
