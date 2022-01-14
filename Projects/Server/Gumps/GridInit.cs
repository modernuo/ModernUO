using System.Collections.Generic;

namespace Server.Gumps;

public static class ColorCode
{
    public const string Gold = "#ffc959";
    public const string White = "#ffffff";
    public const string Blue = "#1D63DC";
    public const string Green = "#80E732";
    public const string Orange = "#F28B00";
    public const string Purple = "#F3ACF6";
    public const string Red = "#B52B2D";
    public const string LightGreen = "#E6FFC0";
    public const string Yellow = "#FFFFBB";

    public static string GetByStatus(int dipStatus) => dipStatus switch
    {
        0 => White,
        1 => Green,
        2 => Red,
        _ => Yellow,
    };

}

public class ListItem
{
    public int X;
    public int Y;
    public int Index;
    public Col[] Cols;
}

public class Col
{
    public int X { get; set; }
    public int Width { get; set; }
    public int H_Center
    {
        get => X + Width / 2;
    }
    public int H_End
    {
        get => X + Width;
    }
}

public class Row
{
    public int Y { get; set; }
    public int Height { get; set; }
    public int V_Center
    {
        get => Y + Height / 2;
    }
    public int V_End
    {
        get => Y + Height;
    }
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
