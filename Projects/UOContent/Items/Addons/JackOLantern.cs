namespace Server.Items
{
  public class JackOLantern : BaseAddon
  {
    [Constructible]
    public JackOLantern()
      : this(Utility.Random(2) < 1)
    {
    }

    [Constructible]
    public JackOLantern(bool south)
    {
      AddComponent(new AddonComponent(5703), 0, 0, +0);

      const int hue = 1161;
      // ( 1 > Utility.Random( 5 ) ? 2118 : 1161 );

      if (!south)
      {
        AddComponent(GetComponent(3178, 0), 0, 0, -1);
        AddComponent(GetComponent(3883, hue), 0, 0, +1);
        AddComponent(GetComponent(3862, hue), 0, 0, +0);
      }
      else
      {
        AddComponent(GetComponent(3179, 0), 0, 0, +0);
        AddComponent(GetComponent(3885, hue), 0, 0, -1);
        AddComponent(GetComponent(3871, hue), 0, 0, +0);
      }
    }

    public JackOLantern(Serial serial)
      : base(serial)
    {
    }

    public override bool ShareHue => false;

    private static AddonComponent GetComponent(int itemID, int hue) =>
      new AddonComponent(itemID)
      {
        Hue = hue,
        Name = "jack-o-lantern"
      };

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write((byte)2); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadByte();


      if (version <= 1)
        Timer.DelayCall(Fix, version);
    }

    private void Fix(int version)
    {
      for (int i = 0; i < Components.Count; ++i)
      {
        var ac = Components[i];
        switch (version)
        {
          case 1:
          {
            ac.Name = "jack-o-lantern";
            goto case 0;
          }
          case 0:
          {
            if (ac.Hue == 2118)
              ac.Hue = 1161;
            break;
          }
        }
      }
    }
  }
}
