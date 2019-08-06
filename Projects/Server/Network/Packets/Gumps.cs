using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<int, int> CloseGump(out int length)
    {
      length = 13;
      static void write(Memory<byte> mem, int typeId, int buttonId)
      {
        SpanWriter w = new SpanWriter(mem.Span, 13);
        w.Write((byte)0xBF); // Packet ID
        w.Write((short)8); // Length

        w.Write((short)0x04); // Command
        w.Write(typeId);
        w.Write(buttonId);
      }

      return write;
    }
  }
}
