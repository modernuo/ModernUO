/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GumpRadio.cs                                                    *
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
    public class GumpRadio : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("radio");

        public GumpRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
        {
            X = x;
            Y = y;
            InactiveID = inactiveID;
            ActiveID = activeID;
            InitialState = initialState;
            SwitchID = switchID;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int InactiveID { get; set; }

        public int ActiveID { get; set; }

        public bool InitialState { get; set; }

        public int SwitchID { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ radio {X} {Y} {InactiveID} {ActiveID} {(InitialState ? 1 : 0)} {SwitchID} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.WriteAscii(' ');
            writer.WriteAscii(X.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Y.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(InactiveID.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(ActiveID.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(InitialState ? '1' : '0');
            writer.WriteAscii(' ');
            writer.WriteAscii(SwitchID.ToString());
            writer.Write((ushort)0x207D); // " }"

            switches++;
        }
    }
}
