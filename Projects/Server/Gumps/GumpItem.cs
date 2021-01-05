using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpItem : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("tilepic");
        public static readonly byte[] LayoutNameHue = Gump.StringToBuffer("tilepichue");

        public GumpItem(int x, int y, int itemID, int hue = 0)
        {
            X = x;
            Y = y;
            ItemID = itemID;
            Hue = hue;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int ItemID { get; set; }

        public int Hue { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            Hue == 0 ? $"{{ tilepic {X} {Y} {ItemID} }}" : $"{{ tilepichue {X} {Y} {ItemID} {Hue} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write(Hue == 0 ? LayoutName : LayoutNameHue);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(ItemID.ToString());

            if (Hue != 0)
            {
                writer.Write((byte)0x20); // ' '
                writer.WriteAscii(Hue.ToString());
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
