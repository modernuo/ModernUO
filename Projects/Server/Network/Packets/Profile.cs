using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteDynamicPacketMethod<Serial, string, string, string> DisplayProfile(out int length, Serial m, string header, string body, string footer)
    {
      length = 12 + header.Length + footer.Length * 2 + body.Length * 2;

      static int write(Memory<byte> mem, int length, Serial m, string header, string body, string footer)
      {
        SpanWriter w = new SpanWriter(mem.Span, 8);
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

        return length;
      }

      return write;
    }
  }
}
