/***************************************************************************
 *                                Packets.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Net;
using System.Threading;
using Server.Accounting;

namespace Server.Network
{
  public enum LRReason : byte
  {
    CannotLift = 0,
    OutOfRange = 1,
    OutOfSight = 2,
    TryToSteal = 3,
    AreHolding = 4,
    Inspecific = 5
  }

  public sealed class ScrollMessage : Packet
  {
    public ScrollMessage(int type, int tip, string text) : base(0xA6)
    {
      text ??= "";

      EnsureCapacity(10 + text.Length);

      Stream.Write((byte)type);
      Stream.Write(tip);
      Stream.Write((ushort)text.Length);
      Stream.WriteAsciiFixed(text, text.Length);
    }
  }

  public sealed class CurrentTime : Packet
  {
    public CurrentTime() : base(0x5B, 4)
    {
      var now = DateTime.UtcNow;

      Stream.Write((byte)now.Hour);
      Stream.Write((byte)now.Minute);
      Stream.Write((byte)now.Second);
    }
  }

  public sealed class MapChange : Packet
  {
    public MapChange(Mobile m) : base(0xBF)
    {
      EnsureCapacity(6);

      Stream.Write((short)0x08);
      Stream.Write((byte)(m.Map?.MapID ?? 0));
    }
  }

  public sealed class SupportedFeatures : Packet
  {
    public SupportedFeatures(NetState ns) : base(0xB9, ns.ExtendedSupportedFeatures ? 5 : 3)
    {
      var flags = ExpansionInfo.CoreExpansion.SupportedFeatures;

      flags |= Value;

      if (ns.Account.Limit >= 6)
      {
        flags |= FeatureFlags.LiveAccount;
        flags &= ~FeatureFlags.UOTD;

        if (ns.Account.Limit > 6)
          flags |= FeatureFlags.SeventhCharacterSlot;
        else
          flags |= FeatureFlags.SixthCharacterSlot;
      }

      if (ns.ExtendedSupportedFeatures)
        Stream.Write((uint)flags);
      else
        Stream.Write((ushort)flags);
    }

    public static FeatureFlags Value { get; set; }

    public static SupportedFeatures Instantiate(NetState ns) => new SupportedFeatures(ns);
  }

  public static class AttributeNormalizer
  {
    public static int Maximum { get; set; } = 25;

    public static bool Enabled { get; set; } = true;

    public static void Write(PacketWriter stream, int cur, int max)
    {
      if (Enabled && max != 0)
      {
        stream.Write((short)Maximum);
        stream.Write((short)(cur * Maximum / max));
      }
      else
      {
        stream.Write((short)max);
        stream.Write((short)cur);
      }
    }

    public static void WriteReverse(PacketWriter stream, int cur, int max)
    {
      if (Enabled && max != 0)
      {
        stream.Write((short)(cur * Maximum / max));
        stream.Write((short)Maximum);
      }
      else
      {
        stream.Write((short)cur);
        stream.Write((short)max);
      }
    }
  }

  public sealed class MobileHits : Packet
  {
    public MobileHits(Mobile m) : base(0xA1, 9)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)m.HitsMax);
      Stream.Write((short)m.Hits);
    }
  }

  public sealed class MobileHitsN : Packet
  {
    public MobileHitsN(Mobile m) : base(0xA1, 9)
    {
      Stream.Write(m.Serial);
      AttributeNormalizer.Write(Stream, m.Hits, m.HitsMax);
    }
  }

  public sealed class MobileMana : Packet
  {
    public MobileMana(Mobile m) : base(0xA2, 9)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)m.ManaMax);
      Stream.Write((short)m.Mana);
    }
  }

  public sealed class MobileManaN : Packet
  {
    public MobileManaN(Mobile m) : base(0xA2, 9)
    {
      Stream.Write(m.Serial);
      AttributeNormalizer.Write(Stream, m.Mana, m.ManaMax);
    }
  }

  public sealed class MobileStam : Packet
  {
    public MobileStam(Mobile m) : base(0xA3, 9)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)m.StamMax);
      Stream.Write((short)m.Stam);
    }
  }

  public sealed class MobileStamN : Packet
  {
    public MobileStamN(Mobile m) : base(0xA3, 9)
    {
      Stream.Write(m.Serial);
      AttributeNormalizer.Write(Stream, m.Stam, m.StamMax);
    }
  }

  public sealed class MobileAttributes : Packet
  {
    public MobileAttributes(Mobile m) : base(0x2D, 17)
    {
      Stream.Write(m.Serial);

      Stream.Write((short)m.HitsMax);
      Stream.Write((short)m.Hits);

      Stream.Write((short)m.ManaMax);
      Stream.Write((short)m.Mana);

      Stream.Write((short)m.StamMax);
      Stream.Write((short)m.Stam);
    }
  }

  public sealed class MobileAttributesN : Packet
  {
    public MobileAttributesN(Mobile m) : base(0x2D, 17)
    {
      Stream.Write(m.Serial);

      AttributeNormalizer.Write(Stream, m.Hits, m.HitsMax);
      AttributeNormalizer.Write(Stream, m.Mana, m.ManaMax);
      AttributeNormalizer.Write(Stream, m.Stam, m.StamMax);
    }
  }

  public sealed class PathfindMessage : Packet
  {
    public PathfindMessage(IPoint3D p) : base(0x38, 7)
    {
      Stream.Write((short)p.X);
      Stream.Write((short)p.Y);
      Stream.Write((short)p.Z);
    }
  }

  // unsure of proper format, client crashes
  public sealed class MobileName : Packet
  {
    public MobileName(Mobile m) : base(0x98)
    {
      EnsureCapacity(37);

      Stream.Write(m.Serial);
      Stream.WriteAsciiFixed(m.Name ?? "", 30);
    }
  }

  public sealed class MobileAnimation : Packet
  {
    public MobileAnimation(Mobile m, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay) : base(0x6E, 14)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)action);
      Stream.Write((short)frameCount);
      Stream.Write((short)repeatCount);
      Stream.Write(!forward); // protocol has really "reverse" but I find this more intuitive
      Stream.Write(repeat);
      Stream.Write((byte)delay);
    }
  }

  public sealed class NewMobileAnimation : Packet
  {
    public NewMobileAnimation(Mobile m, int action, int frameCount, int delay) : base(0xE2, 10)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)action);
      Stream.Write((short)frameCount);
      Stream.Write((byte)delay);
    }
  }

  public sealed class MobileStatusCompact : Packet
  {
    public MobileStatusCompact(bool canBeRenamed, Mobile m) : base(0x11)
    {
      EnsureCapacity(43);

      Stream.Write(m.Serial);
      Stream.WriteAsciiFixed(m.Name ?? "", 30);

      AttributeNormalizer.WriteReverse(Stream, m.Hits, m.HitsMax);

      Stream.Write(canBeRenamed);

      Stream.Write((byte)0); // type
    }
  }

  public sealed class MobileStatusExtended : Packet
  {
    public MobileStatusExtended(Mobile m) : this(m, m.NetState)
    {
    }

    public MobileStatusExtended(Mobile m, NetState ns) : base(0x11)
    {
      var name = m.Name ?? "";

      int type;

      if (Core.HS && ns?.ExtendedStatus == true)
      {
        type = 6;
        EnsureCapacity(121);
      }
      else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
      {
        type = 5;
        EnsureCapacity(91);
      }
      else
      {
        type = Core.AOS ? 4 : 3;
        EnsureCapacity(88);
      }

      Stream.Write(m.Serial);
      Stream.WriteAsciiFixed(name, 30);

      Stream.Write((short)m.Hits);
      Stream.Write((short)m.HitsMax);

      Stream.Write(m.CanBeRenamedBy(m));

      Stream.Write((byte)type);

      Stream.Write(m.Female);

      Stream.Write((short)m.Str);
      Stream.Write((short)m.Dex);
      Stream.Write((short)m.Int);

      Stream.Write((short)m.Stam);
      Stream.Write((short)m.StamMax);

      Stream.Write((short)m.Mana);
      Stream.Write((short)m.ManaMax);

      Stream.Write(m.TotalGold);
      Stream.Write((short)(Core.AOS ? m.PhysicalResistance : (int)(m.ArmorRating + 0.5)));
      Stream.Write((short)(Mobile.BodyWeight + m.TotalWeight));

      if (type >= 5)
      {
        Stream.Write((short)m.MaxWeight);
        Stream.Write((byte)(m.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
      }

      Stream.Write((short)m.StatCap);

      Stream.Write((byte)m.Followers);
      Stream.Write((byte)m.FollowersMax);

      if (type >= 4)
      {
        Stream.Write((short)m.FireResistance); // Fire
        Stream.Write((short)m.ColdResistance); // Cold
        Stream.Write((short)m.PoisonResistance); // Poison
        Stream.Write((short)m.EnergyResistance); // Energy
        Stream.Write((short)m.Luck); // Luck

        var weapon = m.Weapon;

        if (weapon != null)
        {
          weapon.GetStatusDamage(m, out var min, out var max);
          Stream.Write((short)min); // Damage min
          Stream.Write((short)max); // Damage max
        }
        else
        {
          Stream.Write((short)0); // Damage min
          Stream.Write((short)0); // Damage max
        }

        Stream.Write(m.TithingPoints);
      }

      if (type >= 6)
        for (var i = 0; i < 15; ++i)
          Stream.Write((short)m.GetAOSStatus(i));
    }
  }

  public sealed class MobileStatus : Packet
  {
    public MobileStatus(Mobile beholder, Mobile beheld) : this(beholder, beheld, beheld.NetState)
    {
    }

    public MobileStatus(Mobile beholder, Mobile beheld, NetState ns) : base(0x11)
    {
      var name = beheld.Name ?? "";

      int type;

      if (beholder != beheld)
      {
        type = 0;
        EnsureCapacity(43);
      }
      else if (Core.HS && ns?.ExtendedStatus == true)
      {
        type = 6;
        EnsureCapacity(121);
      }
      else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
      {
        type = 5;
        EnsureCapacity(91);
      }
      else
      {
        type = Core.AOS ? 4 : 3;
        EnsureCapacity(88);
      }

      Stream.Write(beheld.Serial);

      Stream.WriteAsciiFixed(name, 30);

      if (beholder == beheld)
        WriteAttr(beheld.Hits, beheld.HitsMax);
      else
        WriteAttrNorm(beheld.Hits, beheld.HitsMax);

      Stream.Write(beheld.CanBeRenamedBy(beholder));

      Stream.Write((byte)type);

      if (type <= 0)
        return;

      Stream.Write(beheld.Female);

      Stream.Write((short)beheld.Str);
      Stream.Write((short)beheld.Dex);
      Stream.Write((short)beheld.Int);

      WriteAttr(beheld.Stam, beheld.StamMax);
      WriteAttr(beheld.Mana, beheld.ManaMax);

      Stream.Write(beheld.TotalGold);
      Stream.Write((short)(Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5)));
      Stream.Write((short)(Mobile.BodyWeight + beheld.TotalWeight));

      if (type >= 5)
      {
        Stream.Write((short)beheld.MaxWeight);
        Stream.Write((byte)(beheld.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
      }

      Stream.Write((short)beheld.StatCap);

      Stream.Write((byte)beheld.Followers);
      Stream.Write((byte)beheld.FollowersMax);

      if (type >= 4)
      {
        Stream.Write((short)beheld.FireResistance); // Fire
        Stream.Write((short)beheld.ColdResistance); // Cold
        Stream.Write((short)beheld.PoisonResistance); // Poison
        Stream.Write((short)beheld.EnergyResistance); // Energy
        Stream.Write((short)beheld.Luck); // Luck

        var weapon = beheld.Weapon;

        if (weapon != null)
        {
          weapon.GetStatusDamage(beheld, out var min, out var max);
          Stream.Write((short)min); // Damage min
          Stream.Write((short)max); // Damage max
        }
        else
        {
          Stream.Write((short)0); // Damage min
          Stream.Write((short)0); // Damage max
        }

        Stream.Write(beheld.TithingPoints);
      }

      if (type >= 6)
        for (var i = 0; i < 15; ++i)
          Stream.Write((short)beheld.GetAOSStatus(i));
    }

    private void WriteAttr(int current, int maximum)
    {
      Stream.Write((short)current);
      Stream.Write((short)maximum);
    }

    private void WriteAttrNorm(int current, int maximum)
    {
      AttributeNormalizer.WriteReverse(Stream, current, maximum);
    }
  }

  public sealed class HealthbarPoison : Packet
  {
    public HealthbarPoison(Mobile m) : base(0x17)
    {
      EnsureCapacity(12);

      Stream.Write(m.Serial);
      Stream.Write((short)1);

      Stream.Write((short)1);

      var p = m.Poison;

      if (p != null)
        Stream.Write((byte)(p.Level + 1));
      else
        Stream.Write((byte)0);
    }
  }

  public sealed class HealthbarYellow : Packet
  {
    public HealthbarYellow(Mobile m) : base(0x17)
    {
      EnsureCapacity(12);

      Stream.Write(m.Serial);
      Stream.Write((short)1);

      Stream.Write((short)2);

      if (m.Blessed || m.YellowHealthbar)
        Stream.Write((byte)1);
      else
        Stream.Write((byte)0);
    }
  }

  public sealed class MobileUpdate : Packet
  {
    public MobileUpdate(Mobile m) : base(0x20, 19)
    {
      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((byte)0);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetPacketFlags());
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((short)0);
      Stream.Write((byte)m.Direction);
      Stream.Write((sbyte)m.Z);
    }
  }

  // Pre-7.0.0.0 Mobile Update
  public sealed class MobileUpdateOld : Packet
  {
    public MobileUpdateOld(Mobile m) : base(0x20, 19)
    {
      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((byte)0);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetOldPacketFlags());
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((short)0);
      Stream.Write((byte)m.Direction);
      Stream.Write((sbyte)m.Z);
    }
  }

  public sealed class MobileIncoming : Packet
  {
    private static readonly ThreadLocal<int[]> m_DupedLayersTL = new ThreadLocal<int[]>(() => { return new int[256]; });
    private static readonly ThreadLocal<int> m_VersionTL = new ThreadLocal<int>();

    public MobileIncoming(Mobile beholder, Mobile beheld) : base(0x78)
    {
      var m_Version = ++m_VersionTL.Value;
      var m_DupedLayers = m_DupedLayersTL.Value;

      var eq = beheld.Items;
      var count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      EnsureCapacity(23 + count * 9);

      var hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      Stream.Write(beheld.Serial);
      Stream.Write((short)beheld.Body);
      Stream.Write((short)beheld.X);
      Stream.Write((short)beheld.Y);
      Stream.Write((sbyte)beheld.Z);
      Stream.Write((byte)beheld.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)beheld.GetPacketFlags());
      Stream.Write((byte)Notoriety.Compute(beholder, beheld));

      for (var i = 0; i < eq.Count; ++i)
      {
        var item = eq[i];

        var layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
        {
          m_DupedLayers[layer] = m_Version;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = item.ItemID & 0xFFFF;

          Stream.Write(item.Serial);
          Stream.Write((ushort)itemID);
          Stream.Write(layer);

          Stream.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0)
        if (m_DupedLayers[(int)Layer.Hair] != m_Version)
        {
          m_DupedLayers[(int)Layer.Hair] = m_Version;
          hue = beheld.HairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.HairItemID & 0xFFFF;

          Stream.Write(HairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.Hair);

          Stream.Write((short)hue);
        }

      if (beheld.FacialHairItemID > 0)
        if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
        {
          m_DupedLayers[(int)Layer.FacialHair] = m_Version;
          hue = beheld.FacialHairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.FacialHairItemID & 0xFFFF;

          Stream.Write(FacialHairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.FacialHair);

          Stream.Write((short)hue);
        }

      Stream.Write(0); // terminate
    }

    public static Packet Create(NetState ns, Mobile beholder, Mobile beheld)
    {
      if (ns.NewMobileIncoming)
        return new MobileIncoming(beholder, beheld);
      if (ns.StygianAbyss)
        return new MobileIncomingSA(beholder, beheld);
      return new MobileIncomingOld(beholder, beheld);
    }
  }

  public sealed class MobileIncomingSA : Packet
  {
    private static readonly ThreadLocal<int[]> m_DupedLayersTL = new ThreadLocal<int[]>(() => { return new int[256]; });
    private static readonly ThreadLocal<int> m_VersionTL = new ThreadLocal<int>();

    public MobileIncomingSA(Mobile beholder, Mobile beheld) : base(0x78)
    {
      var m_Version = ++m_VersionTL.Value;
      var m_DupedLayers = m_DupedLayersTL.Value;

      var eq = beheld.Items;
      var count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      EnsureCapacity(23 + count * 9);

      var hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      Stream.Write(beheld.Serial);
      Stream.Write((short)beheld.Body);
      Stream.Write((short)beheld.X);
      Stream.Write((short)beheld.Y);
      Stream.Write((sbyte)beheld.Z);
      Stream.Write((byte)beheld.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)beheld.GetPacketFlags());
      Stream.Write((byte)Notoriety.Compute(beholder, beheld));

      for (var i = 0; i < eq.Count; ++i)
      {
        var item = eq[i];

        var layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
        {
          m_DupedLayers[layer] = m_Version;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = item.ItemID & 0x7FFF;
          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(item.Serial);
          Stream.Write((ushort)itemID);
          Stream.Write(layer);

          if (writeHue)
            Stream.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0)
        if (m_DupedLayers[(int)Layer.Hair] != m_Version)
        {
          m_DupedLayers[(int)Layer.Hair] = m_Version;
          hue = beheld.HairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.HairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(HairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.Hair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      if (beheld.FacialHairItemID > 0)
        if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
        {
          m_DupedLayers[(int)Layer.FacialHair] = m_Version;
          hue = beheld.FacialHairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.FacialHairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(FacialHairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.FacialHair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      Stream.Write(0); // terminate
    }
  }

  // Pre-7.0.0.0 Mobile Incoming
  public sealed class MobileIncomingOld : Packet
  {
    private static readonly ThreadLocal<int[]> m_DupedLayersTL = new ThreadLocal<int[]>(() => { return new int[256]; });
    private static readonly ThreadLocal<int> m_VersionTL = new ThreadLocal<int>();

    public MobileIncomingOld(Mobile beholder, Mobile beheld) : base(0x78)
    {
      var m_Version = ++m_VersionTL.Value;
      var m_DupedLayers = m_DupedLayersTL.Value;

      var eq = beheld.Items;
      var count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      EnsureCapacity(23 + count * 9);

      var hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      Stream.Write(beheld.Serial);
      Stream.Write((short)beheld.Body);
      Stream.Write((short)beheld.X);
      Stream.Write((short)beheld.Y);
      Stream.Write((sbyte)beheld.Z);
      Stream.Write((byte)beheld.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)beheld.GetOldPacketFlags());
      Stream.Write((byte)Notoriety.Compute(beholder, beheld));

      for (var i = 0; i < eq.Count; ++i)
      {
        var item = eq[i];

        var layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
        {
          m_DupedLayers[layer] = m_Version;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = item.ItemID & 0x7FFF;
          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(item.Serial);
          Stream.Write((ushort)itemID);
          Stream.Write(layer);

          if (writeHue)
            Stream.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0)
        if (m_DupedLayers[(int)Layer.Hair] != m_Version)
        {
          m_DupedLayers[(int)Layer.Hair] = m_Version;
          hue = beheld.HairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.HairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(HairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.Hair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      if (beheld.FacialHairItemID > 0)
        if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
        {
          m_DupedLayers[(int)Layer.FacialHair] = m_Version;
          hue = beheld.FacialHairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.FacialHairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(FacialHairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.FacialHair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      Stream.Write(0); // terminate
    }
  }

  public sealed class PingAck : Packet
  {
    private static readonly PingAck[] m_Cache = new PingAck[0x100];

    public PingAck(byte ping) : base(0x73, 2)
    {
      Stream.Write(ping);
    }

    public static PingAck Instantiate(byte ping)
    {
      var p = m_Cache[ping];

      if (p == null)
      {
        m_Cache[ping] = p = new PingAck(ping);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class MovementRej : Packet
  {
    public MovementRej(int seq, Mobile m) : base(0x21, 8)
    {
      Stream.Write((byte)seq);
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((byte)m.Direction);
      Stream.Write((sbyte)m.Z);
    }
  }

  public sealed class MovementAck : Packet
  {
    private static readonly MovementAck[] m_Cache = new MovementAck[8 * 256];

    private MovementAck(int seq, int noto) : base(0x22, 3)
    {
      Stream.Write((byte)seq);
      Stream.Write((byte)noto);
    }

    public static MovementAck Instantiate(int seq, Mobile m)
    {
      var noto = Notoriety.Compute(m, m);

      var p = m_Cache[noto * seq];

      if (p == null)
      {
        m_Cache[noto * seq] = p = new MovementAck(seq, noto);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class LoginConfirm : Packet
  {
    public LoginConfirm(Mobile m) : base(0x1B, 37)
    {
      Stream.Write(m.Serial);
      Stream.Write(0);
      Stream.Write((short)m.Body);
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((short)m.Z);
      Stream.Write((byte)m.Direction);
      Stream.Write((byte)0);
      Stream.Write(-1);

      var map = m.Map;

      if (map == null || map == Map.Internal)
        map = m.LogoutMap;

      Stream.Write((short)0);
      Stream.Write((short)0);
      Stream.Write((short)(map?.Width ?? 6144));
      Stream.Write((short)(map?.Height ?? 4096));

      Stream.Fill();
    }
  }

  public sealed class LoginComplete : Packet
  {
    public static readonly Packet Instance = SetStatic(new LoginComplete());

    public LoginComplete() : base(0x55, 1)
    {
    }
  }

  public sealed class CharacterListUpdate : Packet
  {
    public CharacterListUpdate(IAccount a) : base(0x86)
    {
      EnsureCapacity(4 + a.Length * 60);

      var highSlot = -1;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      Stream.Write((byte)count);

      for (var i = 0; i < count; ++i)
      {
        var m = a[i];

        if (m != null)
        {
          Stream.WriteAsciiFixed(m.Name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }
      }
    }
  }

  public sealed class CharacterList : Packet
  {
    // private static MD5CryptoServiceProvider m_MD5Provider;

    public CharacterList(IAccount a, CityInfo[] info) : base(0xA9)
    {
      EnsureCapacity(11 + a.Length * 60 + info.Length * 89);

      var highSlot = -1;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      Stream.Write((byte)count);

      for (var i = 0; i < count; ++i)
        if (a[i] != null)
        {
          Stream.WriteAsciiFixed(a[i].Name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }

      Stream.Write((byte)info.Length);

      for (var i = 0; i < info.Length; ++i)
      {
        var ci = info[i];

        Stream.Write((byte)i);
        Stream.WriteAsciiFixed(ci.City, 32);
        Stream.WriteAsciiFixed(ci.Building, 32);
        Stream.Write(ci.X);
        Stream.Write(ci.Y);
        Stream.Write(ci.Z);
        Stream.Write(ci.Map.MapID);
        Stream.Write(ci.Description);
        Stream.Write(0);
      }

      var flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      Stream.Write((int)(flags | AdditionalFlags)); // Additional Flags

      Stream.Write((short)-1);
    }

    public static CharacterListFlags AdditionalFlags { get; set; }
  }

  public sealed class CharacterListOld : Packet
  {
    // private static MD5CryptoServiceProvider m_MD5Provider;

    public CharacterListOld(IAccount a, CityInfo[] info) : base(0xA9)
    {
      EnsureCapacity(9 + a.Length * 60 + info.Length * 63);

      var highSlot = -1;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      Stream.Write((byte)count);

      for (var i = 0; i < count; ++i)
        if (a[i] != null)
        {
          Stream.WriteAsciiFixed(a[i].Name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }

      Stream.Write((byte)info.Length);

      for (var i = 0; i < info.Length; ++i)
      {
        var ci = info[i];

        Stream.Write((byte)i);
        Stream.WriteAsciiFixed(ci.City, 31);
        Stream.WriteAsciiFixed(ci.Building, 31);
      }

      var flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      Stream.Write((int)(flags | CharacterList.AdditionalFlags)); // Additional Flags
    }
  }

  public sealed class ClearWeaponAbility : Packet
  {
    public static readonly Packet Instance = SetStatic(new ClearWeaponAbility());

    public ClearWeaponAbility() : base(0xBF)
    {
      EnsureCapacity(5);

      Stream.Write((short)0x21);
    }
  }

  public enum ALRReason : byte
  {
    Invalid = 0x00,
    InUse = 0x01,
    Blocked = 0x02,
    BadPass = 0x03,
    Idle = 0xFE,
    BadComm = 0xFF
  }

  public sealed class AccountLoginRej : Packet
  {
    public AccountLoginRej(ALRReason reason) : base(0x82, 2)
    {
      Stream.Write((byte)reason);
    }
  }

  [Flags]
  public enum AffixType : byte
  {
    Append = 0x00,
    Prepend = 0x01,
    System = 0x02
  }

  public sealed class ServerInfo
  {
    public ServerInfo(string name, int fullPercent, TimeZoneInfo tz, IPEndPoint address)
    {
      Name = name;
      FullPercent = fullPercent;
      TimeZone = tz.GetUtcOffset(DateTime.Now).Hours;
      Address = address;
    }

    public string Name { get; set; }

    public int FullPercent { get; set; }

    public int TimeZone { get; set; }

    public IPEndPoint Address { get; set; }
  }

  public sealed class FollowMessage : Packet
  {
    public FollowMessage(Serial serial1, Serial serial2) : base(0x15, 9)
    {
      Stream.Write(serial1);
      Stream.Write(serial2);
    }
  }

  public sealed class AccountLoginAck : Packet
  {
    public AccountLoginAck(ServerInfo[] info) : base(0xA8)
    {
      EnsureCapacity(6 + info.Length * 40);

      Stream.Write((byte)0x5D); // Unknown

      Stream.Write((ushort)info.Length);

      for (var i = 0; i < info.Length; ++i)
      {
        var si = info[i];

        Stream.Write((ushort)i);
        Stream.WriteAsciiFixed(si.Name, 32);
        Stream.Write((byte)si.FullPercent);
        Stream.Write((sbyte)si.TimeZone);
        Stream.Write(Utility.GetAddressValue(si.Address.Address));
      }
    }
  }

  public sealed class PlayServerAck : Packet
  {
    internal static int m_AuthID = -1;

    public PlayServerAck(ServerInfo si) : base(0x8C, 11)
    {
      var addr = Utility.GetAddressValue(si.Address.Address);

      Stream.Write((byte)addr);
      Stream.Write((byte)(addr >> 8));
      Stream.Write((byte)(addr >> 16));
      Stream.Write((byte)(addr >> 24));

      Stream.Write((short)si.Address.Port);
      Stream.Write(m_AuthID);
    }
  }
}
