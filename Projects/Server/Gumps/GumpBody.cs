using Server.Collections;
using System.Buffers;

namespace Server.Gumps;

public class GumpBody : GumpEntry
{
    public GumpBody(string body) => Body = body;
    public string Body { get; }
    public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
    {
        writer.WriteAscii($"{{ body {Body} }}");
    }
}
