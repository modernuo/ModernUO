using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpHtml : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("htmlgump");

        public GumpHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Text = text;
            Background = background;
            Scrollbar = scrollbar;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Text { get; set; }

        public bool Background { get; set; }

        public bool Scrollbar { get; set; }

        public override string Compile(NetState ns) =>
            $"{{ htmlgump {X} {Y} {Width} {Height} {Parent.Intern(Text)} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}";

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ htmlgump {X} {Y} {Width} {Height} {strings.GetOrAdd(Text)} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
            disp.AppendLayout(Parent.Intern(Text));
            disp.AppendLayout(Background);
            disp.AppendLayout(Scrollbar);
        }

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(m_LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Width.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Height.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(strings.GetOrAdd(Text).ToString());
            writer.Write((byte)0x20); // ' '
            writer.Write((byte)(Background ? 0x31 : 0x30)); // 1 or 0
            writer.Write((byte)0x20); // ' '
            writer.Write((byte)(Scrollbar ? 0x31 : 0x30)); // 1 or 0
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
