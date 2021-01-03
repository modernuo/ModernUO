using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpPage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("page");

        public GumpPage(int page) => Page = page;

        public int Page { get; set; }

        public override string Compile(NetState ns) => $"{{ page {Page} }}";
        public override string Compile(OrderedHashSet<string> strings) => $"{{ page {Page} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Page);
        }

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(m_LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Page.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
