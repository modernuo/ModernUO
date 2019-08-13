using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void DamageOld(NetState ns, Serial m, int amount)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(11));
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

      _ = ns.Flush(11);
    }

    public static void Damage(NetState ns, Serial m, int amount)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(7));
      w.Write((byte)0xBF); // Extended Packet ID
      w.Write((ushort)7); // Length

      w.Write(m);

      if (amount > 0xFFFF)
        amount = 0xFFFF;
      else if (amount < 0)
        amount = 0;

      w.Write((ushort)amount);

      _ = ns.Flush(7);
    }
  }
}
