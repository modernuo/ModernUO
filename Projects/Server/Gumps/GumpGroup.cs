using Server.Network;

namespace Server.Gumps
{
    public class GumpGroup : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("group");

        public GumpGroup(int group) => Group = group;

        public int Group { get; set; }

        public override string Compile(NetState ns) => $"{{ group {Group} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Group);
        }
    }
}
