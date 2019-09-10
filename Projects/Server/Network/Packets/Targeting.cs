using Server.Buffers;
using Server.Targeting;

namespace Server.Network.Packets
{
  public static partial class Packets
  {
    public static void SendMultiTargetReqHS(NetState ns, MultiTarget t)
    {
      if (ns == null)
        return;

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

      ns.Send(w.RawSpan);
    }

    public static void SendMultiTargetReq(NetState ns, MultiTarget t)
    {
      if (ns == null)
        return;

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

      ns.Send(w.RawSpan);
    }

    private static byte[] _targetReqPacket;

    public static void SendCancelTarget(NetState ns)
    {
      if (ns == null)
        return;

      if (_targetReqPacket == null)
      {
        _targetReqPacket = new byte[19];
        _targetReqPacket[0] = 0x6C; // Packet ID
        _targetReqPacket[6] = 0x03; // ?
      }

      ns.Send(_targetReqPacket);
    }

    public static void SendTargetReq(NetState ns, Target t)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[19]);
      w.Write((byte)0x6C); // Packet ID

      w.Write(t.AllowGround);
      w.Write(t.TargetID);
      w.Write((byte)t.Flags);

      ns.Send(w.RawSpan);
    }
  }
}
