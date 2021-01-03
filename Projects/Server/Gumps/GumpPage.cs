using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpPage : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("page");

        public GumpPage(int page) => Page = page;

        public int Page { get; set; }
        public override string Compile(OrderedHashSet<string> strings) => $"{{ page {Page} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Page.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
