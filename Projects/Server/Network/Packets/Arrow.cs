using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendSetArrow(NetState ns, short x, short y)
    {
      Span<byte> span = stackalloc byte[6];
      SpanWriter w = new SpanWriter(span);
      w.Write((byte)0xBA); // Packet ID

      w.Write((byte)1);
      w.Write(x);
      w.Write(y);

      ns.SendCompressed(span, 6);
    }

    public static void SendCancelArrow(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(6);

      span[0] = 0xBA; // Packet ID
      span[2] = 0xFF;
      span[3] = 0xFF;
      span[4] = 0xFF;
      span[5] = 0xFF;

      _ = ns.Flush(6);
    }

    public static void SendSetArrowHS(NetState ns, Serial s, short x, short y)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(10));
      w.Write((byte)0xBA); // Packet ID

      w.Write((byte)1);
      w.Write(x);
      w.Write(y);
      w.Write(s);

      _ = ns.Flush(10);
    }

    public static void SendCancelArrowHS(NetState ns, Serial s, short x, short y)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(10));
      w.Write((byte)0xBA); // Packet ID

      w.Position++;
      w.Write(x);
      w.Write(y);
      w.Write(s);

      _ = ns.Flush(10);
    }
  }
}
