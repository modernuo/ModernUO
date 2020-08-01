using System;

namespace Server.Items
{
  public class FireworksWand : MagicWand
  {
    private int m_Charges;

    [Constructible]
    public FireworksWand(int charges = 100)
    {
      m_Charges = charges;
      LootType = LootType.Blessed;
    }

    public FireworksWand(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1041424; // a fireworks wand

    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
      get => m_Charges;
      set
      {
        m_Charges = value;
        InvalidateProperties();
      }
    }

    public override void AddNameProperties(ObjectPropertyList list)
    {
      base.AddNameProperties(list);

      list.Add(1060741, m_Charges.ToString()); // charges: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
      BeginLaunch(from, true);
    }

    public void BeginLaunch(Mobile from, bool useCharges)
    {
      Map map = from.Map;

      if (map == null || map == Map.Internal)
        return;

      if (useCharges)
      {
        if (Charges > 0)
        {
          --Charges;
        }
        else
        {
          from.SendLocalizedMessage(502412); // There are no charges left on that item.
          return;
        }
      }

      from.SendLocalizedMessage(502615); // You launch a firework!

      Point3D ourLoc = GetWorldLocation();

      Point3D startLoc = new Point3D(ourLoc.X, ourLoc.Y, ourLoc.Z + 10);
      Point3D endLoc = new Point3D(startLoc.X + Utility.RandomMinMax(-2, 2), startLoc.Y + Utility.RandomMinMax(-2, 2),
        startLoc.Z + 32);

      Effects.SendMovingEffect(new Entity(Serial.Zero, startLoc, map), new Entity(Serial.Zero, endLoc, map),
        0x36E4, 5, 0, false, false);

      Timer.DelayCall(TimeSpan.FromSeconds(1.0), FinishLaunch, endLoc, map);
    }

    private static void FinishLaunch(Point3D endLoc, Map map)
    {
      int hue = Utility.Random(40);

      if (hue < 8)
        hue = 0x66D;
      else if (hue < 10)
        hue = 0x482;
      else if (hue < 12)
        hue = 0x47E;
      else if (hue < 16)
        hue = 0x480;
      else if (hue < 20)
        hue = 0x47F;
      else
        hue = 0;

      if (Utility.RandomBool())
        hue = Utility.RandomList(0x47E, 0x47F, 0x480, 0x482, 0x66D);

      int renderMode = Utility.RandomList(0, 2, 3, 4, 5, 7);

      Effects.PlaySound(endLoc, map, Utility.Random(0x11B, 4));
      Effects.SendLocationEffect(endLoc, map, 0x373A + 0x10 * Utility.Random(4), 16, 10, hue, renderMode);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version

      writer.Write(m_Charges);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
          {
            m_Charges = reader.ReadInt();
            break;
          }
      }
    }
  }
}
