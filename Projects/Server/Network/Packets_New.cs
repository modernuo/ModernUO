using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static readonly int MaxPacketSize = 0x10000;

    public static WriteDynamicPacketMethod<Serial, string> ObjectHelpResponse(out int length, Serial e, string text)
    {
      length = 9 + text.Length * 2;

      static int write(Memory<byte> mem, int length, Serial e, string text)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
        w.Write((byte)0xB7); // Extended Packet ID
        w.Write((ushort)length); // Length

        w.Write(e);
        w.WriteBigUniNull(text);

        return length;
      }

      return write;
    }
  }
}