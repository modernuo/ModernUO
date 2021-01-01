using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpItemProperty : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("itemproperty");

        public GumpItemProperty(uint serial) => Serial = serial;

        public uint Serial { get; set; }

        public override string Compile(NetState ns) => $"{{ itemproperty {Serial} }}";
        public override string Compile(IndexList<string> strings) => $"{{ itemproperty {Serial} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(Serial);
        }

        public override void AppendTo(ref SpanWriter writer, IndexList<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(m_LayoutName);
            writer.WriteAscii(Serial.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
