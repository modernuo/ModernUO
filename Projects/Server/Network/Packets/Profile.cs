using Server.Buffers;

namespace Server.Network.Packets
{
  public static partial class Packets
  {
    public static void SendDisplayProfile(NetState ns, Serial m, string header, string body, string footer)
    {
      if (ns == null)
        return;

      header ??= "";
      body ??= "";
      footer ??= "";

      int length = 12 + header.Length + footer.Length * 2 + body.Length * 2;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xB8); // Packet ID
      w.Write((short)length); // Length

      w.Write(m);
      w.WriteAsciiNull(header);
      w.WriteBigUniNull(footer);
      w.WriteBigUniNull(body);

      ns.Send(w.Span);
    }
  }
}
