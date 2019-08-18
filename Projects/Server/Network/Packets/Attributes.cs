using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static class AttributeNormalizer
    {
      public static int Maximum { get; set; } = 25;

      public static bool Enabled { get; set; } = true;

      public static void Write(SpanWriter w, int cur, int max)
      {
        if (Enabled && max != 0)
        {
          w.Write((short)Maximum);
          w.Write((short)(cur * Maximum / max));
        }
        else
        {
          w.Write((short)max);
          w.Write((short)cur);
        }
      }

      public static void WriteReverse(SpanWriter w, int cur, int max)
      {
        if (Enabled && max != 0)
        {
          w.Write((short)(cur * Maximum / max));
          w.Write((short)Maximum);
        }
        else
        {
          w.Write((short)cur);
          w.Write((short)max);
        }
      }
    }

    public static void SendMobileHits(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0xA1); // Packet ID

      w.Write(m.Serial);
      w.Write((short)m.HitsMax);
      w.Write((short)m.Hits);

      ns.SendCompressed(w.Span);
    }

    public static void SendNormalizedMobileHits(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0xA1); // Packet ID

      w.Write(m.Serial);
      AttributeNormalizer.Write(w, m.Hits, m.HitsMax);

      ns.SendCompressed(w.Span);
    }

    public static void SendMobileMana(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0xA2); // Packet ID

      w.Write(m.Serial);
      w.Write((short)m.ManaMax);
      w.Write((short)m.Mana);

      ns.SendCompressed(w.Span);
    }

    public static void SendNormalizedMobileMana(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0xA2); // Packet ID

      w.Write(m.Serial);
      AttributeNormalizer.Write(w, m.Mana, m.ManaMax);

      ns.SendCompressed(w.Span);
    }

    public static void SendMobileStam(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0xA3); // Packet ID

      w.Write(m.Serial);
      w.Write((short)m.StamMax);
      w.Write((short)m.Stam);

      ns.SendCompressed(w.Span);
    }

    public static void SendNormalizedMobileStam(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0xA3); // Packet ID

      w.Write(m.Serial);
      AttributeNormalizer.Write(w, m.Stam, m.StamMax);

      ns.SendCompressed(w.Span);
    }

    public static void SendMobileAttributes(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[17]);
      w.Write((byte)0x2D); // Packet ID

      w.Write(m.Serial);

      w.Write((short)m.HitsMax);
      w.Write((short)m.Hits);

      w.Write((short)m.ManaMax);
      w.Write((short)m.Mana);

      w.Write((short)m.StamMax);
      w.Write((short)m.Stam);

      ns.SendCompressed(w.Span);
    }

    public static void SendNormalizedMobileAttributes(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[17]);
      w.Write((byte)0x2D); // Packet ID

      w.Write(m.Serial);

      AttributeNormalizer.Write(w, m.Hits, m.HitsMax);
      AttributeNormalizer.Write(w, m.Mana, m.ManaMax);
      AttributeNormalizer.Write(w, m.Stam, m.StamMax);

      ns.SendCompressed(w.Span);
    }
  }
}
