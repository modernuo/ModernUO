using Server.Network;

namespace Server.Gumps
{
    public class GumpItemProperty : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("itemproperty");

        public GumpItemProperty(uint serial) => Serial = serial;

        public uint Serial { get; set; }

        public override string Compile(NetState ns) => $"{{ itemproperty {Serial} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Serial);
        }
    }
}
