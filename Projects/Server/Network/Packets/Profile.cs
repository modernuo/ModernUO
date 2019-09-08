using System;
using Server.Buffers;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendDisplayProfile(NetState ns, Serial m, string header, string body, string footer)
    {
      int length = 12 + header.Length + footer.Length * 2 + body.Length * 2;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xB8); // Packet ID
      w.Write((short)length); // Length

      if (header == null)
        header = "";

      if (body == null)
        body = "";

      if (footer == null)
        footer = "";

      w.Write(m);
      w.WriteAsciiNull(header);
      w.WriteBigUniNull(footer);
      w.WriteBigUniNull(body);

      ns.Send(w.Span);
    }
  }
}
