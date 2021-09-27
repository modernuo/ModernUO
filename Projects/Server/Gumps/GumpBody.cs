using Server.Collections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Gumps
{
    public class GumpBody : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("body");
      
        public override string Compile(OrderedHashSet<string> strings) => $"{{ body {_body} }}";
        public GumpBody(string body) =>_body = body;
        public string _body { get; set; }
        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.WriteAscii(' ');
            writer.WriteAscii(_body.ToString());
            writer.Write((ushort)0x207D); // " }"
        }
    } 
}
