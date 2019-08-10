using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendDeathAnimation(NetState ns, Serial killed, Serial corpse)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(13));
      w.Write((byte)0xAF); // Packet ID

      w.Write(killed);
      w.Write(corpse);
      // w.Position++; w.Write(0);

      _ = ns.Flush(13);
    }

    public static void SendDeathAnimation(NetState ns, Mobile m)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(12));
      w.Write((byte)0xBF); // Extended Packet ID
      w.Write((short)12); // Length

      w.Write((short)0x19); // Subcommand
      w.Write((byte)2);
      w.Write(m.Serial);
      w.Write((short)((int)m.StrLock << 4 | (int)m.DexLock << 2 | (int)m.IntLock));

      _ = ns.Flush(12);
    }

    public static void SendBondStatus(NetState ns, Serial m, bool bonded)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(11));
      w.Write((byte)0xBF); // Extended Packet ID
      w.Write((short)11); // Length

      w.Write((short)0x19); // Command
      w.Position++; // w.Write((byte)0); // Subcommand
      w.Write(m);
      w.Write(bonded);

      _ = ns.Flush(11);
    }

    public static void SendPersonalLightLevel(NetState ns, Serial m, sbyte level)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(6));
      w.Write((byte)0x4E); // Packet ID

      w.Write(m);
      w.Write(level);

      _ = ns.Flush(6);
    }

    public static void SendPersonalLightLevelZero(NetState ns, Serial m)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(6));
      w.Write((byte)0x4E); // Packet ID

      w.Write(m);

      _ = ns.Flush(6);
    }

    public static void SendEquipUpdate(NetState ns, Item item)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(15));
      w.Write((byte)0x2E); // Packet ID

      Serial parentSerial = Serial.Zero;
      int hue = item.Hue;

      if (item.Parent is Mobile parent)
      {
        parentSerial = parent.Serial;
        if (parent.SolidHueOverride >= 0)
          hue = parent.SolidHueOverride;
      }
      else
        Console.WriteLine("Warning: EquipUpdate on item with an invalid parent");

      w.Write(item.Serial);
      w.Write((short)item.ItemID);
      w.Write((short)item.Layer);
      w.Write(parentSerial);
      w.Write((short)hue);

      _ = ns.Flush(15);
    }

    public static void SendSwing(NetState ns, Serial attacker, Serial defender)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(10));
      w.Write((byte)0x2F); // Packet ID

      w.Position++; // ?
      w.Write(attacker);
      w.Write(defender);

      _ = ns.Flush(10);
    }

    public static void SendMobileMoving(NetState ns, Mobile m, int noto)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(17));
      w.Write((byte)0x77); // Packet ID

      Point3D loc = m.Location;

      w.Write(m.Serial);
      w.Write((short)m.Body);
      w.Write((short)loc.m_X);
      w.Write((short)loc.m_Y);
      w.Write((sbyte)loc.m_Z);
      w.Write((byte)m.Direction);
      w.Write((short)(m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue));
      w.Write((byte)m.GetPacketFlags());
      w.Write((byte)noto);

      _ = ns.Flush(17);
    }

    public static void SendMobileMovingOld(NetState ns, Mobile m, int noto)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(17));
      w.Write((byte)0x77); // Packet ID

      Point3D loc = m.Location;

      w.Write(m.Serial);
      w.Write((short)m.Body);
      w.Write((short)loc.m_X);
      w.Write((short)loc.m_Y);
      w.Write((sbyte)loc.m_Z);
      w.Write((byte)m.Direction);
      w.Write((short)(m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue));
      w.Write((byte)m.GetOldPacketFlags());
      w.Write((byte)noto);

      _ = ns.Flush(17);
    }

    public static void SendDisplayPaperdoll(NetState ns, Mobile m, string text, bool canLift)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(66));
      w.Write((byte)0x88); // Packet ID

      byte flags = 0x00;

      if (m.Warmode)
        flags |= 0x01;

      if (canLift)
        flags |= 0x02;

      w.Write(m.Serial);
      w.WriteAsciiFixed(text, 60);
      w.Write(flags);

      _ = ns.Flush(66);
    }
  }
}
