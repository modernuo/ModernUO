/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Grid.cs                                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;

namespace Server.Gumps;

public enum GridHues
{
    White = 2049, // Change to 0x480 if you have old hues.mul
    Black = 0,
}

public static class GridColors
{
    public const string Gold = "#ffc959";
    public const string White = "#ffffff";
    public const string Blue = "#1D63DC";
    public const string LightBlue = "#7799EE";
    public const string Green = "#80E732";
    public const string Orange = "#F28B00";
    public const string Purple = "#F3ACF6";
    public const string Red = "#B52B2D";
    public const string LightGreen = "#E6FFC0";
    public const string Yellow = "#FFFFBB";
}

public class ListItem
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Index { get; set; }
    public Col[] Cols { get; set; }
}

public class Col
{
    public int X { get; set; }
    public int Width { get; set; }
    public int HCenter => X + Width / 2;
    public int HEnd => X + Width;
}

public class Row
{
    public int Y { get; set; }
    public int Height { get; set; }
    public int VCenter => Y + Height / 2;
    public int VEnd => Y + Height;
}

public class Grid
{
    public string Name { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public List<Col> Columns { get; set; } = new();
    public List<Row> Rows { get; set; } = new();
}

public class Swap
{
    public int Index { get; set; }
    public int Size { get; set; }
}
