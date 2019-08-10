using System;
using Server.Targeting;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendMultiTargetReqHS(NetState ns, MultiTarget t)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(30));
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

      _ = ns.Flush(30);
    }

    public static void SendMultiTargetReq(NetState ns, MultiTarget t)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(26));
      w.Write((byte)0x99); // Packet ID

      w.Write(t.AllowGround);
      w.Write(t.TargetID);
      w.Write((byte)t.Flags);
      w.Position += 14;
      w.Write((short)t.MultiID);
      w.Write((short)t.Offset.X);
      w.Write((short)t.Offset.Y);
      w.Write((short)t.Offset.Z);

      _ = ns.Flush(26);
    }

    public static void SendTargetReq(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(19);
      span[0] = 0x6C; // Packet ID
      span[6] = 0x03;

      _ = ns.Flush(19);
    }

    public static void SendCancelTarget(NetState ns, Target t)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(19));
      w.Write((byte)0x6C); // Packet ID

      w.Write(t.AllowGround);
      w.Write(t.TargetID);
      w.Write((byte)t.Flags);

      _ = ns.Flush(19);
    }
  }
}
