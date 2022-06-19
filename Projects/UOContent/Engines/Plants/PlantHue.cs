using System;
using System.Collections.Generic;

namespace Server.Engines.Plants
{
    [Flags]
    public enum PlantHue
    {
        Plain = 0x1 | Crossable | Reproduces,

        Red = 0x2 | Crossable | Reproduces,
        Blue = 0x4 | Crossable | Reproduces,
        Yellow = 0x8 | Crossable | Reproduces,

        BrightRed = Red | Bright,
        BrightBlue = Blue | Bright,
        BrightYellow = Yellow | Bright,

        Purple = Red | Blue,
        Green = Blue | Yellow,
        Orange = Red | Yellow,

        BrightPurple = Purple | Bright,
        BrightGreen = Green | Bright,
        BrightOrange = Orange | Bright,

        Black = 0x10,
        White = 0x20,
        Pink = 0x40,
        Magenta = 0x80,
        Aqua = 0x100,
        FireRed = 0x200,

        None = 0,
        Reproduces = 0x2000000,
        Crossable = 0x4000000,
        Bright = 0x8000000
    }

    public class PlantHueInfo
    {
        private static readonly Dictionary<PlantHue, PlantHueInfo> m_Table;

        static PlantHueInfo() =>
            m_Table = new Dictionary<PlantHue, PlantHueInfo>
            {
                [PlantHue.Plain] = new(0, 1060813, PlantHue.Plain, 0x835),
                [PlantHue.Red] = new(0x66D, 1060814, PlantHue.Red, 0x24),
                [PlantHue.Blue] = new(0x53D, 1060815, PlantHue.Blue, 0x6),
                [PlantHue.Yellow] = new(0x8A5, 1060818, PlantHue.Yellow, 0x38),
                [PlantHue.BrightRed] = new(0x21, 1060814, PlantHue.BrightRed, 0x21),
                [PlantHue.BrightBlue] = new(0x5, 1060815, PlantHue.BrightBlue, 0x6),
                [PlantHue.BrightYellow] = new(0x38, 1060818, PlantHue.BrightYellow, 0x35),
                [PlantHue.Purple] = new(0xD, 1060816, PlantHue.Purple, 0x10),
                [PlantHue.Green] = new(0x59B, 1060819, PlantHue.Green, 0x42),
                [PlantHue.Orange] = new(0x46F, 1060817, PlantHue.Orange, 0x2E),
                [PlantHue.BrightPurple] = new(0x10, 1060816, PlantHue.BrightPurple, 0xD),
                [PlantHue.BrightGreen] = new(0x42, 1060819, PlantHue.BrightGreen, 0x3F),
                [PlantHue.BrightOrange] = new(0x2B, 1060817, PlantHue.BrightOrange, 0x2B),
                [PlantHue.Black] = new(0x455, 1060820, PlantHue.Black, 0),
                [PlantHue.White] = new(0x481, 1060821, PlantHue.White, 0x481),
                [PlantHue.Pink] = new(0x48E, 1061854, PlantHue.Pink),
                [PlantHue.Magenta] = new(0x486, 1061852, PlantHue.Magenta),
                [PlantHue.Aqua] = new(0x495, 1061853, PlantHue.Aqua),
                [PlantHue.FireRed] = new(0x489, 1061855, PlantHue.FireRed)
            };

        private PlantHueInfo(int hue, int name, PlantHue plantHue) : this(hue, name, plantHue, hue)
        {
        }

        private PlantHueInfo(int hue, int name, PlantHue plantHue, int gumpHue)
        {
            Hue = hue;
            Name = name;
            PlantHue = plantHue;
            GumpHue = gumpHue;
        }

        public int Hue { get; }

        public int Name { get; }

        public PlantHue PlantHue { get; }

        public int GumpHue { get; }

        public static PlantHueInfo GetInfo(PlantHue plantHue) =>
            m_Table.TryGetValue(plantHue, out var info) ? info : m_Table[PlantHue.Plain];

        public static PlantHue RandomFirstGeneration()
        {
            return Utility.Random(4) switch
            {
                0 => PlantHue.Plain,
                1 => PlantHue.Red,
                2 => PlantHue.Blue,
                _ => PlantHue.Yellow
            };
        }

        public static bool CanReproduce(PlantHue plantHue) => (plantHue & PlantHue.Reproduces) != PlantHue.None;

        public static bool IsCrossable(PlantHue plantHue) => (plantHue & PlantHue.Crossable) != PlantHue.None;

        public static bool IsBright(PlantHue plantHue) => (plantHue & PlantHue.Bright) != PlantHue.None;

        public static PlantHue GetNotBright(PlantHue plantHue) => plantHue & ~PlantHue.Bright;

        public static bool IsPrimary(PlantHue plantHue) =>
            plantHue is PlantHue.Red or PlantHue.Blue or PlantHue.Yellow;

        public static PlantHue Cross(PlantHue first, PlantHue second)
        {
            if (!IsCrossable(first) || !IsCrossable(second))
            {
                return PlantHue.None;
            }

            if (Utility.RandomDouble() < 0.01)
            {
                return Utility.RandomBool() ? PlantHue.Black : PlantHue.White;
            }

            if (first == PlantHue.Plain || second == PlantHue.Plain)
            {
                return PlantHue.Plain;
            }

            var notBrightFirst = GetNotBright(first);
            var notBrightSecond = GetNotBright(second);

            if (notBrightFirst == notBrightSecond)
            {
                return first | PlantHue.Bright;
            }

            var firstPrimary = IsPrimary(notBrightFirst);
            var secondPrimary = IsPrimary(notBrightSecond);

            if (firstPrimary && secondPrimary)
            {
                return notBrightFirst | notBrightSecond;
            }

            if (firstPrimary)
            {
                return notBrightFirst;
            }

            if (secondPrimary)
            {
                return notBrightSecond;
            }

            return notBrightFirst & notBrightSecond;
        }

        public bool IsCrossable() => IsCrossable(PlantHue);

        public bool IsBright() => IsBright(PlantHue);

        public PlantHue GetNotBright() => GetNotBright(PlantHue);

        public bool IsPrimary() => IsPrimary(PlantHue);
    }
}
