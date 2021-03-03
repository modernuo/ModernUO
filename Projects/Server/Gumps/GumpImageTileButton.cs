/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GumpImageTileButton.cs                                          *
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
    public class GumpImageTileButton : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("buttontileart");
        public static readonly byte[] LayoutTooltip = Gump.StringToBuffer(" }{ tooltip");

        // Note, on OSI, the tooltip supports ONLY clilocs as far as I can figure out,
        // and the tooltip ONLY works after the buttonTileArt (as far as I can tell from testing)

        public GumpImageTileButton(
            int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param,
            int itemID, int hue, int width, int height, int localizedTooltip = -1
        )
        {
            X = x;
            Y = y;
            NormalID = normalID;
            PressedID = pressedID;
            ButtonID = buttonID;
            Type = type;
            Param = param;

            ItemID = itemID;
            Hue = hue;
            Width = width;
            Height = height;

            LocalizedTooltip = localizedTooltip;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int NormalID { get; set; }

        public int PressedID { get; set; }

        public int ButtonID { get; set; }

        public GumpButtonType Type { get; set; }

        public int Param { get; set; }

        public int ItemID { get; set; }

        public int Hue { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int LocalizedTooltip { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            LocalizedTooltip > 0 ?
                $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}{{ tooltip {LocalizedTooltip} }}" :
                $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}";

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
            writer.WriteAscii(' ');
            writer.WriteAscii(ItemID.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Hue.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Width.ToString());
            writer.WriteAscii(' ');
            writer.WriteAscii(Height.ToString());

            if (LocalizedTooltip > 0)
            {
                writer.Write(LayoutTooltip);
                writer.WriteAscii(LocalizedTooltip.ToString());
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
