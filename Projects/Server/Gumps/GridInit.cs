using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Gumps
{
    public enum Color
    {
        White = 2394,
        Black = 0,
    }
    public static class ColorCode
    {
        public static string Gold = "#ffc959";
        public static string White = "#ffffff";
        public static string Blue = "#1D63DC";
        public static string LightBlue = "#7799EE";
        public static string Green = "#80E732";
        public static string Orange = "#F28B00";
        public static string Purple = "#F3ACF6";
        public static string Red = "#B52B2D";
        public static string LightGreen = "#E6FFC0";
        public static string Yellow = "#FFFFBB";
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

    public class StackItems
    {
        int _coord = 0;
        int _delta = 0;
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
        public List<Col> Columns { get; set; } = new List<Col>();
        public List<Row> Rows { get; set; } = new List<Row>();
    }
    public class Swap
    {
        public int Index { get; set; }
        public int Size { get; set; }
    }
}
