using Server.Collections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Gumps
{
    public class GumpBodyUpdate : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("bodyUpdate");

        public override string Compile(OrderedHashSet<string> strings) => $"{{ bodyUpdate {_responseButtonId} {_body} }}";
        public GumpBodyUpdate(string responseButtonId, string body)
        {
            _body = body;
            _responseButtonId = responseButtonId;
        }
        public string _body { get; set; }
        public string _responseButtonId { get; set; }
        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.WriteAscii(' ');
            writer.WriteAscii(_responseButtonId);
            writer.WriteAscii(' ');
            writer.WriteAscii(_body);
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
