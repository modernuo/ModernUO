using Server.Network;

namespace Server.Gumps
{
    public class GumpPage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("page");

        public GumpPage(int page) => Page = page;

        public int Page { get; set; }

        public override string Compile(NetState ns) => $"{{ page {Page} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Page);
        }
    }
}
