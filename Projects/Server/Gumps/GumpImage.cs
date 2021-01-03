using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpImage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("gumppic");
        private static readonly byte[] m_HueEquals = Gump.StringToBuffer(" hue=");
        private static readonly byte[] m_ClassEquals = Gump.StringToBuffer(" class=");

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

        public override string Compile(NetState ns) =>
            $"{{ gumppic {X} {Y} {GumpID}{(Hue == 0 ? "" : $"hue={Hue}")}{(string.IsNullOrEmpty(Class) ? "" : $"class={Class}")} }}";

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ gumppic {X} {Y} {GumpID}{(Hue == 0 ? "" : $"hue={Hue}")}{(string.IsNullOrEmpty(Class) ? "" : $"class={Class}")} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(GumpID);

            if (Hue != 0)
            {
                disp.AppendLayout(m_HueEquals);
                disp.AppendLayoutNS(Hue);
            }

            if (!string.IsNullOrEmpty(Class))
            {
                disp.AppendLayout(m_ClassEquals);
                disp.AppendLayoutNS(Class);
            }
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
            writer.WriteAscii(GumpID.ToString());
            writer.Write((byte)0x20); // ' '

            if (Hue != 0)
            {
                writer.Write(m_HueEquals);
                writer.WriteAscii(Hue.ToString());
                writer.Write((byte)0x20); // ' '
            }

            if (!string.IsNullOrWhiteSpace(Class))
            {
                writer.Write(m_ClassEquals);
                writer.WriteAscii(Class);
                writer.Write((byte)0x20); // ' '
            }

            writer.Write((byte)0x7D); // '}'
        }
    }
}
