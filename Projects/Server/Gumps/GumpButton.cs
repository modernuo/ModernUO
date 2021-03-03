/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GumpButton.cs                                                   *
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
    public enum GumpButtonType
    {
        Page = 0,
        Reply = 1
    }

    public class GumpButton : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("button");

        public GumpButton(
            int x, int y, int normalID, int pressedID, int buttonID,
            GumpButtonType type = GumpButtonType.Reply, int param = 0
        )
        {
            X = x;
            Y = y;
            NormalID = normalID;
            PressedID = pressedID;
            ButtonID = buttonID;
            Type = type;
            Param = param;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int NormalID { get; set; }

        public int PressedID { get; set; }

        public int ButtonID { get; set; }

        public GumpButtonType Type { get; set; }

        public int Param { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ button {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.WriteAscii(' ');
            writer.WriteAscii(X.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Y.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(NormalID.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(PressedID.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(((int)Type).ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Param.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(ButtonID.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
