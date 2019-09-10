using Server.Buffers;

namespace Server.Network.Packets
{
  public static partial class Packets
  {
    public static void SendSetArrow(NetState ns, short x, short y)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[6]);
      w.Write((byte)0xBA); // Packet ID

      w.Write((byte)1);
      w.Write(x);
      w.Write(y);

      ns.Send(w.RawSpan);
    }

    private static byte[] _cancelArrowPacket;

    public static void SendCancelArrow(NetState ns)
    {
      ns?.Send(_cancelArrowPacket ??= new byte[]
      {
        0xBA, // Packet ID
        0x00,
        0xFF,
        0xFF,
        0xFF,
        0xFF
      });
    }

    public static void SendSetArrowHS(NetState ns, Serial s, short x, short y)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[10]);
      w.Write((byte)0xBA); // Packet ID

      w.Write((byte)1);
      w.Write(x);
      w.Write(y);
      w.Write(s);

      ns.Send(w.RawSpan);
    }

    public static void SendCancelArrowHS(NetState ns, Serial s, short x, short y)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[10]);
      w.Write((byte)0xBA); // Packet ID

      w.Position++;
      w.Write(x);
      w.Write(y);
      w.Write(s);

      ns.Send(w.RawSpan);
    }
  }
}
