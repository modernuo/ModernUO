using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpItem : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("tilepic");
        private static readonly byte[] m_LayoutNameHue = Gump.StringToBuffer("tilepichue");

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

        public override string Compile(NetState ns) =>
            Hue == 0 ? $"{{ tilepic {X} {Y} {ItemID} }}" : $"{{ tilepichue {X} {Y} {ItemID} {Hue} }}";

        public override string Compile(IndexList<string> strings) =>
            Hue == 0 ? $"{{ tilepic {X} {Y} {ItemID} }}" : $"{{ tilepichue {X} {Y} {ItemID} {Hue} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(Hue == 0 ? m_LayoutName : m_LayoutNameHue);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(ItemID);

            if (Hue != 0)
            {
                disp.AppendLayout(Hue);
            }
        }

        public override void AppendTo(ref SpanWriter writer, IndexList<string> strings, ref int entries, ref int switches)
        {
            writer.Write(Hue == 0 ? m_LayoutName : m_LayoutNameHue);
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
