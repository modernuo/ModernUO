using System;
using System.Collections.Generic;
using Server.Buffers;
using Server.Items;

namespace Server.Network.Packets
{
  public static partial class Packets
  {
    public static void SendDeathAnimation(NetState ns, Serial killed, Serial corpse)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[13]);
      w.Write((byte)0xAF); // Packet ID

      w.Write(killed);
      w.Write(corpse);
      // w.Position++; w.Write(0);

      ns.Send(w.RawSpan);
    }

    public static void SendDeathAnimation(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[12]);
      w.Write((byte)0xBF); // Extended Packet ID
      w.Write((short)12); // Length

      w.Write((short)0x19); // Subcommand
      w.Write((byte)2);
      w.Write(m.Serial);
      w.Write((short)((int)m.StrLock << 4 | (int)m.DexLock << 2 | (int)m.IntLock));

      ns.Send(w.RawSpan);
    }

    public static void SendBondStatus(NetState ns, Serial m, bool bonded)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[11]);
      w.Write((byte)0xBF); // Extended Packet ID
      w.Write((short)11); // Length

      w.Write((short)0x19); // Command
      w.Position++; // w.Write((byte)0); // Subcommand
      w.Write(m);
      w.Write(bonded);

      ns.Send(w.RawSpan);
    }

    public static void SendPersonalLightLevel(NetState ns, Serial m, sbyte level)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[6]);
      w.Write((byte)0x4E); // Packet ID

      w.Write(m);
      w.Write(level);

      ns.Send(w.RawSpan);
    }

    public static void SendPersonalLightLevelZero(NetState ns, Serial m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[6]);
      w.Write((byte)0x4E); // Packet ID

      w.Write(m);

      ns.Send(w.RawSpan);
    }

    public static void SendEquipUpdate(NetState ns, Item item)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[15]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendSwing(NetState ns, Serial attacker, Serial defender)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[10]);
      w.Write((byte)0x2F); // Packet ID

      w.Position++; // ?
      w.Write(attacker);
      w.Write(defender);

      ns.Send(w.RawSpan);
    }

    public static void SendMobileMoving(NetState ns, Mobile m, int noto)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[17]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendMobileMovingOld(NetState ns, Mobile m, int noto)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[17]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendDisplayPaperdoll(NetState ns, Mobile m, string text, bool canLift)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[66]);
      w.Write((byte)0x88); // Packet ID

      byte flags = 0x00;

      if (m.Warmode)
        flags |= 0x01;

      if (canLift)
        flags |= 0x02;

      w.Write(m.Serial);
      w.WriteAsciiFixed(text, 60);
      w.Write(flags);

      ns.Send(w.RawSpan);
    }

    public static void SendMobileName(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[37]);
      w.Write((byte)0x98); // Packet ID
      w.Write((ushort)37); // Dynamic Length

      w.Write(m.Serial);
      w.WriteAsciiFixed(m.Name ?? "", 30);

      ns.Send(w.RawSpan);
    }

    public static void SendMobileAnimation(NetState ns, Mobile m, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[14]);
      w.Write((byte)0x6E); // Packet ID

      w.Write(m.Serial);
      w.Write((short)action);
      w.Write((short)frameCount);
      w.Write((short)repeatCount);
      w.Write(!forward); // protocol has really "reverse" but I find this more intuitive
      w.Write(repeat);
      w.Write((byte)delay);

      ns.Send(w.RawSpan);
    }

    public static void SendNewMobileAnimation(NetState ns, Mobile m, int action, int frameCount, int delay)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[10]);
      w.Write((byte)0xE2); // Packet ID

      w.Write(m.Serial);
      w.Write((short)action);
      w.Write((short)frameCount);
      w.Write((byte)delay);

      ns.Send(w.RawSpan);
    }

    public static void SendMobileStatusCompact(NetState ns, Mobile m, bool canBeRenamed = false)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[43]);
      w.Write((byte)0x11); // Packet ID
      w.Write((ushort)43); // Length

      w.Write(m.Serial);
      w.WriteAsciiFixed(m.Name ?? "", 30);

      AttributeNormalizer.WriteReverse(w, m.Hits, m.HitsMax);

      w.Write(canBeRenamed);

      // w.Write((byte)0); // type

      ns.Send(w.RawSpan);
    }

    public static void SendMobileExtended(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      string name = m.Name ?? "";

      byte type;
      short length;

      if (Core.HS && ns?.ExtendedStatus == true)
      {
        type = 6;
        length = 121;
      }
      else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
      {
        type = 5;
        length = 91;
      }
      else if (Core.AOS)
      {
        type = 4;
        length = 88;
      }
      else
      {
        type = 3;
        length = 70;
      }

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x11); // Packet ID
      w.Write(length); // Length

      w.Write(m.Serial);
      w.WriteAsciiFixed(name, 30);

      w.Write((short)m.Hits);
      w.Write((short)m.HitsMax);

      w.Write(m.CanBeRenamedBy(m));

      w.Write(type);

      w.Write(m.Female);

      w.Write((short)m.Str);
      w.Write((short)m.Dex);
      w.Write((short)m.Int);

      w.Write((short)m.Stam);
      w.Write((short)m.StamMax);

      w.Write((short)m.Mana);
      w.Write((short)m.ManaMax);

      w.Write(m.TotalGold);
      w.Write((short)(Core.AOS ? m.PhysicalResistance : (int)(m.ArmorRating + 0.5)));
      w.Write((short)(Mobile.BodyWeight + m.TotalWeight));

      if (type >= 5)
      {
        w.Write((short)m.MaxWeight);
        w.Write((byte)(m.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
      }

      w.Write((short)m.StatCap);

      w.Write((byte)m.Followers);
      w.Write((byte)m.FollowersMax);

      if (type >= 4)
      {
        w.Write((short)m.FireResistance); // Fire
        w.Write((short)m.ColdResistance); // Cold
        w.Write((short)m.PoisonResistance); // Poison
        w.Write((short)m.EnergyResistance); // Energy
        w.Write((short)m.Luck); // Luck

        IWeapon weapon = m.Weapon;

        if (weapon != null)
        {
          weapon.GetStatusDamage(m, out int min, out int max);
          w.Write((short)min); // Damage min
          w.Write((short)max); // Damage max
        }
        else
          w.Position += 4; // Damage min, Damage max

        w.Write(m.TithingPoints);
      }

      if (type >= 6)
        for (int i = 0; i < 15; ++i)
          w.Write((short)m.GetAOSStatus(i));

      ns.Send(w.RawSpan);
    }

    public static void SendMobileStatus(NetState ns, Mobile beholder, Mobile beheld)
    {
      if (ns == null)
        return;

      string name = beheld.Name ?? "";

      int type;
      short length;

      if (beholder != beheld)
      {
        type = 0;
        length = 43;
      }
      else if (Core.HS && ns?.ExtendedStatus == true)
      {
        type = 6;
        length = 121;
      }
      else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
      {
        type = 5;
        length = 91;
      }
      else if (Core.AOS)
      {
        type = 4;
        length = 88;
      }
      else
      {
        type = 3;
        length = 70;
      }

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x11); // Packet ID
      w.Write((ushort)length); // Length

      w.Write(beheld.Serial);

      w.WriteAsciiFixed(name, 30);

      if (beholder == beheld)
      {
        w.Write((short)beheld.Hits);
        w.Write((short)beheld.HitsMax);
      }
      else
        AttributeNormalizer.WriteReverse(w, beheld.Hits, beheld.HitsMax);

      w.Write(beheld.CanBeRenamedBy(beholder));

      w.Write((byte)type);

      if (type <= 0)
        return;

      w.Write(beheld.Female);

      w.Write((short)beheld.Str);
      w.Write((short)beheld.Dex);
      w.Write((short)beheld.Int);

      w.Write((short)beheld.Stam);
      w.Write((short)beheld.StamMax);

      w.Write((short)beheld.Mana);
      w.Write((short)beheld.ManaMax);

      w.Write(beheld.TotalGold);
      w.Write((short)(Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5)));
      w.Write((short)(Mobile.BodyWeight + beheld.TotalWeight));

      if (type >= 5)
      {
        w.Write((short)beheld.MaxWeight);
        w.Write((byte)(beheld.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
      }

      w.Write((short)beheld.StatCap);

      w.Write((byte)beheld.Followers);
      w.Write((byte)beheld.FollowersMax);

      if (type >= 4)
      {
        w.Write((short)beheld.FireResistance); // Fire
        w.Write((short)beheld.ColdResistance); // Cold
        w.Write((short)beheld.PoisonResistance); // Poison
        w.Write((short)beheld.EnergyResistance); // Energy
        w.Write((short)beheld.Luck); // Luck

        IWeapon weapon = beheld.Weapon;

        if (weapon != null)
        {
          weapon.GetStatusDamage(beheld, out int min, out int max);
          w.Write((short)min); // Damage min
          w.Write((short)max); // Damage max
        }
        else
          w.Position += 2; // Damage min, Damage max

        w.Write(beheld.TithingPoints);
      }

      if (type >= 6)
        for (int i = 0; i < 15; ++i)
          w.Write((short)beheld.GetAOSStatus(i));

      ns.Send(w.RawSpan);
    }

    public static void SendHealthbarPoison(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[12]);
      w.Write((byte)0x17); // Packet ID
      w.Write((ushort)12); // Length

      w.Write(m.Serial);
      w.Write((short)1);
      w.Write((short)1);

      Poison p = m.Poison;

      if (p != null)
        w.Write((byte)(p.Level + 1));

      ns.Send(w.RawSpan);
    }

    public static void SendHealthbarYellow(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[12]);
      w.Write((byte)0x17); // Packet ID
      w.Write((ushort)12); // Length

      w.Write(m.Serial);
      w.Write((short)1);
      w.Write((short)2);

      w.Write(m.Blessed || m.YellowHealthbar);

      ns.Send(w.RawSpan);
    }

    public static void SendMobileUpdate(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[19]);
      w.Write((byte)0x20); // Packet ID

      int hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      w.Write(m.Serial);
      w.Write((short)m.Body);
      w.Position++;  // w.Write((byte)0);
      w.Write((short)hue);
      w.Write((byte)m.GetPacketFlags());
      w.Write((short)m.X);
      w.Write((short)m.Y);
      w.Position += 2; // w.Write((short)0);
      w.Write((byte)m.Direction);
      w.Write((sbyte)m.Z);

      ns.Send(w.RawSpan);
    }

    public static void SendMobileUpdateOld(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[19]);
      w.Write((byte)0x20); // Packet ID

      int hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      w.Write(m.Serial);
      w.Write((short)m.Body);
      w.Position++;  // w.Write((byte)0);
      w.Write((short)hue);
      w.Write((byte)m.GetOldPacketFlags());
      w.Write((short)m.X);
      w.Write((short)m.Y);
      w.Position += 2; // w.Write((short)0);
      w.Write((byte)m.Direction);
      w.Write((sbyte)m.Z);

      ns.Send(w.RawSpan);
    }

    public static void SendMobileIncoming(NetState ns, Mobile beholder, Mobile beheld)
    {
      if (ns == null)
        return;

      if (ns.NewMobileIncoming)
      {
        SendMobileIncomingNew(ns, beholder, beheld);
        return;
      }

      if (ns.StygianAbyss)
      {
        SendMobileIncomingSA(ns, beholder, beheld);
        return;
      }

      SendMobileIncomingOld(ns, beholder, beheld);
    }

    public static void SendMobileIncomingNew(NetState ns, Mobile beholder, Mobile beheld)
    {
      if (ns == null)
        return;

      Span<bool> dupedLayers = stackalloc bool[256];

      List<Item> eq = beheld.Items;
      int count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      SpanWriter w = new SpanWriter(stackalloc byte[23 + count * 9]);
      w.Write((byte)0x78); // Packet ID
      w.Position += 2; // Dynamic Length

      int hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      w.Write(beheld.Serial);
      w.Write((short)beheld.Body);
      w.Write((short)beheld.X);
      w.Write((short)beheld.Y);
      w.Write((sbyte)beheld.Z);
      w.Write((byte)beheld.Direction);
      w.Write((short)hue);
      w.Write((byte)beheld.GetPacketFlags());
      w.Write(Notoriety.Compute(beholder, beheld));

      for (int i = 0; i < eq.Count; ++i)
      {
        Item item = eq[i];

        byte layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && !dupedLayers[layer])
        {
          dupedLayers[layer] = true;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          w.Write(item.Serial);
          w.Write((ushort)(item.ItemID & 0xFFFF));
          w.Write(layer);
          w.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0 && !dupedLayers[(int)Layer.Hair])
      {
        hue = beheld.HairHue;

        if (beheld.SolidHueOverride >= 0)
          hue = beheld.SolidHueOverride;

        w.Write(HairInfo.FakeSerial(beheld.Serial));
        w.Write((ushort)(beheld.HairItemID & 0xFFFF));
        w.Write((byte)Layer.Hair);

        w.Write((short)hue);
      }

      if (beheld.FacialHairItemID > 0 && !dupedLayers[(int)Layer.FacialHair])
      {
        hue = beheld.FacialHairHue;

        if (beheld.SolidHueOverride >= 0)
          hue = beheld.SolidHueOverride;

        w.Write(FacialHairInfo.FakeSerial(beheld.Serial));
        w.Write((ushort)(beheld.FacialHairItemID & 0xFFFF));
        w.Write((byte)Layer.FacialHair);

        w.Write((short)hue);
      }

      w.Position += 4; // w.Write(0)

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public static void SendMobileIncomingSA(NetState ns, Mobile beholder, Mobile beheld)
    {
      if (ns == null)
        return;

      Span<bool> dupedLayers = stackalloc bool[256];

      List<Item> eq = beheld.Items;
      int count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      SpanWriter w = new SpanWriter(stackalloc byte[23 + count * 9]);
      w.Write((byte)0x78); // Packet ID
      w.Position += 2; // Dynamic Length

      int hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      w.Write(beheld.Serial);
      w.Write((short)beheld.Body);
      w.Write((short)beheld.X);
      w.Write((short)beheld.Y);
      w.Write((sbyte)beheld.Z);
      w.Write((byte)beheld.Direction);
      w.Write((short)hue);
      w.Write((byte)beheld.GetPacketFlags());
      w.Write(Notoriety.Compute(beholder, beheld));

      for (int i = 0; i < eq.Count; ++i)
      {
        Item item = eq[i];

        byte layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && !dupedLayers[layer])
        {
          dupedLayers[layer] = true;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          int itemId = item.ItemID & 0x7FFF;
          bool writeHue = hue != 0;

          if (writeHue)
            itemId |= 0x8000;

          w.Write(item.Serial);
          w.Write((ushort)itemId);
          w.Write(layer);

          if (writeHue)
            w.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0 && !dupedLayers[(int)Layer.Hair])
      {
        hue = beheld.HairHue;

        if (beheld.SolidHueOverride >= 0)
          hue = beheld.SolidHueOverride;

        int itemId = beheld.HairItemID & 0x7FFF;

        bool writeHue = hue != 0;

        if (writeHue)
          itemId |= 0x8000;

        w.Write(HairInfo.FakeSerial(beheld.Serial));
        w.Write((ushort)itemId);
        w.Write((byte)Layer.Hair);

        if (writeHue)
          w.Write((short)hue);
      }

      if (beheld.FacialHairItemID > 0 && !dupedLayers[(int)Layer.FacialHair])
      {
        hue = beheld.FacialHairHue;

        if (beheld.SolidHueOverride >= 0)
          hue = beheld.SolidHueOverride;

        int itemId = beheld.FacialHairItemID & 0x7FFF;

        bool writeHue = hue != 0;

        if (writeHue)
          itemId |= 0x8000;

        w.Write(FacialHairInfo.FakeSerial(beheld.Serial));
        w.Write((ushort)itemId);
        w.Write((byte)Layer.FacialHair);

        if (writeHue)
          w.Write((short)hue);
      }

      w.Position += 4; // w.Write(0)

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public static void SendMobileIncomingOld(NetState ns, Mobile beholder, Mobile beheld)
    {
      if (ns == null)
        return;

      Span<bool> dupedLayers = stackalloc bool[256];

      List<Item> eq = beheld.Items;
      int count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      SpanWriter w = new SpanWriter(stackalloc byte[23 + count * 9]);
      w.Write((byte)0x78); // Packet ID
      w.Position += 2; // Dynamic Length

      int hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      w.Write(beheld.Serial);
      w.Write((short)beheld.Body);
      w.Write((short)beheld.X);
      w.Write((short)beheld.Y);
      w.Write((sbyte)beheld.Z);
      w.Write((byte)beheld.Direction);
      w.Write((short)hue);
      w.Write((byte)beheld.GetOldPacketFlags());
      w.Write(Notoriety.Compute(beholder, beheld));

      for (int i = 0; i < eq.Count; ++i)
      {
        Item item = eq[i];

        byte layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && !dupedLayers[layer])
        {
          dupedLayers[layer] = true;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          int itemId = item.ItemID & 0x7FFF;
          bool writeHue = hue != 0;

          if (writeHue)
            itemId |= 0x8000;

          w.Write(item.Serial);
          w.Write((ushort)itemId);
          w.Write(layer);

          if (writeHue)
            w.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0 && !dupedLayers[(int)Layer.Hair])
      {
        hue = beheld.HairHue;

        if (beheld.SolidHueOverride >= 0)
          hue = beheld.SolidHueOverride;

        int itemId = beheld.HairItemID & 0x7FFF;

        bool writeHue = hue != 0;

        if (writeHue)
          itemId |= 0x8000;

        w.Write(HairInfo.FakeSerial(beheld.Serial));
        w.Write((ushort)itemId);
        w.Write((byte)Layer.Hair);

        if (writeHue)
          w.Write((short)hue);
      }

      if (beheld.FacialHairItemID > 0 && !dupedLayers[(int)Layer.FacialHair])
      {
        hue = beheld.FacialHairHue;

        if (beheld.SolidHueOverride >= 0)
          hue = beheld.SolidHueOverride;

        int itemId = beheld.FacialHairItemID & 0x7FFF;

        bool writeHue = hue != 0;

        if (writeHue)
          itemId |= 0x8000;

        w.Write(FacialHairInfo.FakeSerial(beheld.Serial));
        w.Write((ushort)itemId);
        w.Write((byte)Layer.FacialHair);

        if (writeHue)
          w.Write((short)hue);
      }

      w.Position += 4; // w.Write(0)

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public static void SendFollowMessage(NetState ns, Serial s1, Serial s2)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0x15); // Packet ID

      w.Write(s1);
      w.Write(s2);

      ns.Send(w.RawSpan);
    }
  }
}
