using System.Buffers;
using System.Collections.Generic;
using Server.Network;

namespace Server.Gumps
{
    public class GumpGroup : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("group");

        public GumpGroup(int group) => Group = group;

        public int Group { get; set; }

        public override string Compile(NetState ns) => $"{{ group {Group} }}";
        public override string Compile(SortedSet<string> strings) => $"{{ group {Group} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Group);
        }

        public override void AppendTo(ref SpanWriter writer, SortedSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write(m_LayoutName);
            writer.WriteAscii(Group.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
