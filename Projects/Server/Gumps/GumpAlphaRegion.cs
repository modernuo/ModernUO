using System.Buffers;
using System.Collections.Generic;
using Server.Network;

namespace Server.Gumps
{
    public class GumpAlphaRegion : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("checkertrans");

        public GumpAlphaRegion(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public override string Compile(NetState ns) => $"{{ checkertrans {X} {Y} {Width} {Height} }}";

        public override string Compile(SortedSet<string> strings) => $"{{ checkertrans {X} {Y} {Width} {Height} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
        }

        public override void AppendTo(ref SpanWriter writer, SortedSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write(m_LayoutName);
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Width.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Height.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
