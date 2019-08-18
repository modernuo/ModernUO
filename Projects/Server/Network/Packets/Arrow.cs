using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendSetArrow(NetState ns, short x, short y)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[6]);
      w.Write((byte)0xBA); // Packet ID

      w.Write((byte)1);
      w.Write(x);
      w.Write(y);

      ns.SendCompressed(w.Span);
    }

    private static byte[] m_CancelArrowPacket;

    public static void SendCancelArrow(NetState ns)
    {
      if (m_CancelArrowPacket == null)
      {
        ReadOnlySpan<byte> input = stackalloc byte[]
        {
          0xBA, // Packet ID
          0x00,
          0xFF,
          0xFF,
          0xFF,
          0xFF
        };

        m_CancelArrowPacket = CreateStaticPacket(input, true);
      }

      ns.Send(m_CancelArrowPacket);
    }

    public static void SendSetArrowHS(NetState ns, Serial s, short x, short y)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[10]);
      w.Write((byte)0xBA); // Packet ID

      w.Write((byte)1);
      w.Write(x);
      w.Write(y);
      w.Write(s);

      ns.SendCompressed(w.Span);
    }

    public static void SendCancelArrowHS(NetState ns, Serial s, short x, short y)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[10]);
      w.Write((byte)0xBA); // Packet ID

      w.Position++;
      w.Write(x);
      w.Write(y);
      w.Write(s);

      ns.SendCompressed(w.Span);
    }
  }
}
