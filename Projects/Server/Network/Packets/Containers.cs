using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<Serial, int> DisplayContainer(out int length)
    {
      length = 7;

      static void write(Memory<byte> mem, Serial s, int gumpid)
      {
        SpanWriter w = new SpanWriter(mem.Span, 7);
        w.Write((byte)0x24); // Packet ID


        w.Write(s);
        w.Write((short)gumpid);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, int> DisplayContainerHS(out int length)
    {
      length = 9;

      static void write(Memory<byte> mem, Serial s, int gumpid)
      {
        SpanWriter w = new SpanWriter(mem.Span, 9);
        w.Write((byte)0x24); // Packet ID


        w.Write(s);
        w.Write((short)gumpid);
        w.Write((short)0x7D);
      }

      return write;
    }
  }
}
