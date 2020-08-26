using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class GypsyMaiden : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public GypsyMaiden() : base("the gypsy maiden")
    {
    }

    public GypsyMaiden(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override bool GetGender() => true;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBProvisioner());
    }

    public override void InitOutfit()
    {
      base.InitOutfit();

      AddItem(
        Utility.Random(4) switch
        {
          0 => new JesterHat(Utility.RandomBrightHue()),
          1 => new Bandana(Utility.RandomBrightHue()),
          2 => new SkullCap(Utility.RandomBrightHue()),
          _ => null // 3
        }
      );

      if (Utility.RandomBool())
        AddItem(new HalfApron(Utility.RandomBrightHue()));

      Item item = FindItemOnLayer(Layer.Pants);

      if (item != null)
        item.Hue = Utility.RandomBrightHue();

      item = FindItemOnLayer(Layer.OuterLegs);

      if (item != null)
        item.Hue = Utility.RandomBrightHue();

      item = FindItemOnLayer(Layer.InnerLegs);

      if (item != null)
        item.Hue = Utility.RandomBrightHue();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
