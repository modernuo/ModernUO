using Server.Buffers;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendDamage(NetState ns, Serial m, int amount)
    {
      if (ns == null)
        return;

      if (ns.DamagePacket)
        SendDamageNew(ns, m, amount);
      else
        SendDamageOld(ns, m, amount);
    }
    public static void SendDamageOld(NetState ns, Serial m, int amount)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[11]);
      w.Write((byte)0xBF); // Extended Packet ID
      w.Write((ushort)11); // Length

      w.Write((short)0x22);
      w.Write((byte)1);
      w.Write(m);

      if (amount > 255)
        amount = 255;
      else if (amount < 0)
        amount = 0;

      w.Write((byte)amount);

      ns.Send(w.RawSpan);
    }

    public static void SendDamageNew(NetState ns, Serial m, int amount)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[7]);
      w.Write((byte)0xBF); // Extended Packet ID
      w.Write((ushort)7); // Length

      w.Write(m);

      if (amount > 0xFFFF)
        amount = 0xFFFF;
      else if (amount < 0)
        amount = 0;

      w.Write((ushort)amount);

      ns.Send(w.RawSpan);
    }
  }
}
