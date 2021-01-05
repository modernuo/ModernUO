using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpItemProperty : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("itemproperty");

        public GumpItemProperty(uint serial) => Serial = serial;

        public uint Serial { get; set; }

        public override string Compile(OrderedHashSet<string> strings) => $"{{ itemproperty {Serial} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Serial.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
