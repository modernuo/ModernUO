using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpImage : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("gumppic");
        public static readonly byte[] HueEquals = Gump.StringToBuffer(" hue=");
        public static readonly byte[] ClassEquals = Gump.StringToBuffer(" class=");

        public GumpImage(int x, int y, int gumpID, int hue = 0, string cls = null)
        {
            X = x;
            Y = y;
            GumpID = gumpID;
            Hue = hue;
            Class = cls;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int GumpID { get; set; }

        public int Hue { get; set; }

        public string Class { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ gumppic {X} {Y} {GumpID}{(Hue == 0 ? "" : $"hue={Hue}")}{(string.IsNullOrEmpty(Class) ? "" : $"class={Class}")} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(GumpID.ToString());

            if (Hue != 0)
            {
                writer.Write(HueEquals);
                writer.WriteAscii(Hue.ToString());
            }

            if (!string.IsNullOrWhiteSpace(Class))
            {
                writer.Write(ClassEquals);
                writer.WriteAscii(Class);
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
