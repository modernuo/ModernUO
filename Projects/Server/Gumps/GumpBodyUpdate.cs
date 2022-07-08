using Server.Collections;
using System.Buffers;

namespace Server.Gumps;

public class GumpBodyUpdate : GumpEntry
{
    public GumpBodyUpdate(string responseButtonId, string body)
    {
        Body = body;
        ResponseButtonId = responseButtonId;
    }
    public string Body { get; }
    public string ResponseButtonId { get; }
    public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
    {
        writer.WriteAscii($"{{ bodyUpdate {ResponseButtonId} {Body} }}");
    }
}
