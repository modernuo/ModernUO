using Server.Buffers;
using Server.Targeting;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendMultiTargetReqHS(NetState ns, MultiTarget t)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[30]);
      w.Write((byte)0x99); // Packet ID

      w.Write(t.AllowGround);
      w.Write(t.TargetID);
      w.Write((byte)t.Flags);
      w.Position += 14;
      w.Write((short)t.MultiID);
      w.Write((short)t.Offset.X);
      w.Write((short)t.Offset.Y);
      w.Write((short)t.Offset.Z);
      // 4 bytes unknown?

      ns.Send(w.Span);
    }

    public static void SendMultiTargetReq(NetState ns, MultiTarget t)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[26]);
      w.Write((byte)0x99); // Packet ID

      w.Write(t.AllowGround);
      w.Write(t.TargetID);
      w.Write((byte)t.Flags);
      w.Position += 14;
      w.Write((short)t.MultiID);
      w.Write((short)t.Offset.X);
      w.Write((short)t.Offset.Y);
      w.Write((short)t.Offset.Z);

      ns.Send(w.Span);
    }

    private static byte[] m_TargetReqPacket;

    public static void SendTargetReq(NetState ns)
    {
      if (m_TargetReqPacket == null)
      {
        m_TargetReqPacket = new byte[19];
        m_TargetReqPacket[0] = 0x6C; // Packet ID
        m_TargetReqPacket[6] = 0x03; // ?
      }

      ns.Send(m_TargetReqPacket);
    }

    public static void SendCancelTarget(NetState ns, Target t)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[19]);
      w.Write((byte)0x6C); // Packet ID

      w.Write(t.AllowGround);
      w.Write(t.TargetID);
      w.Write((byte)t.Flags);

      ns.Send(w.Span);
    }
  }
}
