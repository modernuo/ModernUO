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

namespace Server.Items
{
  public abstract class BaseHairInfo
  {
    protected BaseHairInfo(int itemid, int hue = 0)
    {
      ItemID = itemid;
      Hue = hue;
    }

    protected BaseHairInfo(GenericReader reader)
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

    public virtual void Serialize(GenericWriter writer)
    {
      writer.Write(0); //version
      writer.Write(ItemID);
      writer.Write(Hue);
    }
  }

  public class HairInfo : BaseHairInfo
  {
    public HairInfo(int itemid, int hue = 0)
      : base(itemid, hue)
    {
    }

    public HairInfo(GenericReader reader)
      : base(reader)
    {
    }

    // TODO: Can we make this higher for newer clients?
    public static uint FakeSerial(Serial parent) => 0x7FFFFFFF - 0x400 - parent * 4;
  }

  public class FacialHairInfo : BaseHairInfo
  {
    public FacialHairInfo(int itemid, int hue = 0)
      : base(itemid, hue)
    {
    }

    public FacialHairInfo(GenericReader reader)
      : base(reader)
    {
    }

    // TOOD: Can we make this higher for newer clients?
    public static uint FakeSerial(Serial parent) => 0x7FFFFFFF - 0x400 - 1 - parent * 4;
  }
}
