/***************************************************************************
 *                          VirtualHair.cs
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

using Server.Network;

namespace Server
{
  public abstract class BaseHairInfo
  {
    protected BaseHairInfo(int itemid, int hue = 0)
    {
      ItemID = itemid;
      Hue = hue;
    }

    protected BaseHairInfo(IGenericReader reader)
    {
      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
        {
          ItemID = reader.ReadInt();
          Hue = reader.ReadInt();
          break;
        }
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int ItemID{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Hue{ get; set; }

    public virtual void Serialize(IGenericWriter writer)
    {
      writer.Write(0); //version
      writer.Write(ItemID);
      writer.Write(Hue);
    }
  }

  public class HairInfo : BaseHairInfo
  {
    public HairInfo(int itemid)
      : base(itemid)
    {
    }

    public HairInfo(int itemid, int hue)
      : base(itemid, hue)
    {
    }

    public HairInfo(IGenericReader reader)
      : base(reader)
    {
    }

    // TOOD: Can we make this higher for newer clients?
    public static uint FakeSerial(Mobile parent) => 0x7FFFFFFF - 0x400 - parent.Serial * 4;
  }

  public class FacialHairInfo : BaseHairInfo
  {
    public FacialHairInfo(int itemid)
      : base(itemid)
    {
    }

    public FacialHairInfo(int itemid, int hue)
      : base(itemid, hue)
    {
    }

    public FacialHairInfo(IGenericReader reader)
      : base(reader)
    {
    }

    // TOOD: Can we make this higher for newer clients?
    public static uint FakeSerial(Mobile parent) => 0x7FFFFFFF - 0x400 - 1 - parent.Serial * 4;
  }

  public sealed class HairEquipUpdate : Packet
  {
    public HairEquipUpdate(Mobile parent)
      : base(0x2E, 15)
    {
      int hue = parent.HairHue;

      if (parent.SolidHueOverride >= 0)
        hue = parent.SolidHueOverride;

      m_Stream.Write(HairInfo.FakeSerial(parent));
      m_Stream.Write((short)parent.HairItemID);
      m_Stream.Write((byte)0);
      m_Stream.Write((byte)Layer.Hair);
      m_Stream.Write(parent.Serial);
      m_Stream.Write((short)hue);
    }
  }

  public sealed class FacialHairEquipUpdate : Packet
  {
    public FacialHairEquipUpdate(Mobile parent)
      : base(0x2E, 15)
    {
      int hue = parent.FacialHairHue;

      if (parent.SolidHueOverride >= 0)
        hue = parent.SolidHueOverride;

      m_Stream.Write(FacialHairInfo.FakeSerial(parent));
      m_Stream.Write((short)parent.FacialHairItemID);
      m_Stream.Write((byte)0);
      m_Stream.Write((byte)Layer.FacialHair);
      m_Stream.Write(parent.Serial);
      m_Stream.Write((short)hue);
    }
  }

  public sealed class RemoveHair : Packet
  {
    public RemoveHair(Mobile parent)
      : base(0x1D, 5)
    {
      m_Stream.Write(HairInfo.FakeSerial(parent));
    }
  }

  public sealed class RemoveFacialHair : Packet
  {
    public RemoveFacialHair(Mobile parent)
      : base(0x1D, 5)
    {
      m_Stream.Write(FacialHairInfo.FakeSerial(parent));
    }
  }
}