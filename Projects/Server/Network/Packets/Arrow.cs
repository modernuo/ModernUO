using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<short, short> SetArrow(out int length)
    {
      length = 6;

      static void write(Memory<byte> mem, short x, short y)
      {
        SpanWriter w = new SpanWriter(mem.Span, 6);
        w.Write((byte)0xBA); // Packet ID

        w.Write((byte)1);
        w.Write(x);
        w.Write(y);
      }

      return write;
    }

    public static WriteFixedPacketMethod CancelArrow(out int length)
    {
      length = 6;

      static void write(Memory<byte> mem)
      {
        Span<byte> span = mem.Span;

        span[0] = 0xBA; // Packet ID
        span[2] = 0xFF;
        span[3] = 0xFF;
        span[4] = 0xFF;
        span[5] = 0xFF;
      }

      return write;
    }

    public static WriteFixedPacketMethod<short, short, Serial> SetArrowHS(out int length)
    {
      length = 10;

      static void write(Memory<byte> mem, short x, short y, Serial s)
      {
        SpanWriter w = new SpanWriter(mem.Span, 10);
        w.Write((byte)0xBA); // Packet ID

        w.Write((byte)1);
        w.Write(x);
        w.Write(y);
        w.Write(s);
      }

      return write;
    }

    public static WriteFixedPacketMethod<short, short, Serial> CancelArrowHS(out int length)
    {
      length = 10;

      static void write(Memory<byte> mem, short x, short y, Serial s)
      {
        SpanWriter w = new SpanWriter(mem.Span, 10);
        w.Write((byte)0xBA); // Packet ID

        w.Position++; // Write((byte)0)
        w.Write(x);
        w.Write(y);
        w.Write(s);
      }

      return write;
    }
  }
}
