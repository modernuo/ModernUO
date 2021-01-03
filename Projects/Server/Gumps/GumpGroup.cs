using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpGroup : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("group");

        public GumpGroup(int group) => Group = group;

        public int Group { get; set; }
        public override string Compile(OrderedHashSet<string> strings) => $"{{ group {Group} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Group.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
