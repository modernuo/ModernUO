using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpImageTiled : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("gumppictiled");

        public GumpImageTiled(int x, int y, int width, int height, int gumpID)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            GumpID = gumpID;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int GumpID { get; set; }
        public override string Compile(OrderedHashSet<string> strings) => $"{{ gumppictiled {X} {Y} {Width} {Height} {GumpID} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Width.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Height.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(GumpID.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
