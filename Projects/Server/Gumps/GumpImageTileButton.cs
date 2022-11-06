/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2022 - ModernUO Development Team                   *
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

namespace Server.Gumps;

public class GumpImageTileButton : GumpEntry
{
    public GumpImageTileButton(
        int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param,
        int itemID, int hue, int width, int height
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

    public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, scoped ref int entries, scoped ref int switches)
    {
        writer.WriteAscii(
            $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}"
        );
    }
}
