using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public enum GumpHtmlLocalizedType
    {
        Plain,
        Color,
        Args
    }

    public class GumpHtmlLocalized : GumpEntry
    {
        public static readonly byte[] LayoutNamePlain = Gump.StringToBuffer("xmfhtmlgump");
        public static readonly byte[] LayoutNameColor = Gump.StringToBuffer("xmfhtmlgumpcolor");
        public static readonly byte[] LayoutNameArgs = Gump.StringToBuffer("xmfhtmltok");

        public GumpHtmlLocalized(
            int x, int y, int width, int height, int number,
            bool background = false, bool scrollbar = false
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Number = number;
            Background = background;
            Scrollbar = scrollbar;

            Type = GumpHtmlLocalizedType.Plain;
        }

        public GumpHtmlLocalized(
            int x, int y, int width, int height, int number, int color,
            bool background = false, bool scrollbar = false
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Number = number;
            Color = color;
            Background = background;
            Scrollbar = scrollbar;

            Type = GumpHtmlLocalizedType.Color;
        }

        public GumpHtmlLocalized(
            int x, int y, int width, int height, int number, string args, int color,
            bool background = false, bool scrollbar = false
        )
        {
            // Are multiple arguments unsupported? And what about non ASCII arguments?

            X = x;
            Y = y;
            Width = width;
            Height = height;
            Number = number;
            Args = args;
            Color = color;
            Background = background;
            Scrollbar = scrollbar;

            Type = GumpHtmlLocalizedType.Args;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Number { get; set; }

        public string Args { get; set; }

        public int Color { get; set; }

        public bool Background { get; set; }

        public bool Scrollbar { get; set; }

        public GumpHtmlLocalizedType Type { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            Type switch
            {
                GumpHtmlLocalizedType.Plain =>
                    $"{{ xmfhtmlgump {X} {Y} {Width} {Height} {Number} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}",
                GumpHtmlLocalizedType.Color =>
                    $"{{ xmfhtmlgumpcolor {X} {Y} {Width} {Height} {Number} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} {Color} }}",
                _ =>
                    $"{{ xmfhtmltok {X} {Y} {Width} {Height} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} {Color} {Number} @{Args}@ }}"
            };

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "

            switch (Type)
            {
                case GumpHtmlLocalizedType.Plain:
                    {
                        writer.Write(LayoutNamePlain);
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(X.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Y.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Width.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Height.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Number.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.Write((byte)(Background ? 0x31 : 0x30)); // 1 or 0
                        writer.Write((byte)0x20); // ' '
                        writer.Write((byte)(Scrollbar ? 0x31 : 0x30)); // 1 or 0

                        break;
                    }
                case GumpHtmlLocalizedType.Color:
                    {
                        writer.Write(LayoutNameColor);
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(X.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Y.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Width.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Height.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Number.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.Write((byte)(Background ? 0x31 : 0x30)); // 1 or 0
                        writer.Write((byte)0x20); // ' '
                        writer.Write((byte)(Scrollbar ? 0x31 : 0x30)); // 1 or 0
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Color.ToString());

                        break;
                    }
                case GumpHtmlLocalizedType.Args:
                    {
                        writer.Write(LayoutNameArgs);
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(X.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Y.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Width.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Height.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.Write((byte)(Background ? 0x31 : 0x30)); // 1 or 0
                        writer.Write((byte)0x20); // ' '
                        writer.Write((byte)(Scrollbar ? 0x31 : 0x30)); // 1 or 0
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Color.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.WriteAscii(Number.ToString());
                        writer.Write((byte)0x20); // ' '
                        writer.Write((byte)0x40); // '@'
                        writer.WriteAscii(Args ?? "");
                        writer.Write((byte)0x40); // '@'

                        break;
                    }
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
