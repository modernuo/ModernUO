using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpLabel : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("text");

        public GumpLabel(int x, int y, int hue, string text)
        {
            X = x;
            Y = y;
            Hue = hue;
            Text = text;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Hue { get; set; }

        public string Text { get; set; }
        public override string Compile(OrderedHashSet<string> strings) => $"{{ text {X} {Y} {Hue} {strings.GetOrAdd(Text)} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Hue.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(strings.GetOrAdd(Text).ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
