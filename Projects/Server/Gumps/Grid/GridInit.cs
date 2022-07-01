using System.Collections.Generic;

namespace Server.Gumps;

public enum GridHues
{
    White = 2394,
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

public class StackItems
{
    private int _coord;
    private int _delta;

    public StackItems(int coord, int delta)
    {
        _coord = coord;
        _delta = delta;
    }

    public int Calc(int index) => _coord + _delta * index;
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
