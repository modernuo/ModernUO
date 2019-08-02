using System.Collections.Generic;

namespace Server.Mobiles
{
  public class Beekeeper : BaseVendor
  {
    private List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public Beekeeper() : base("the beekeeper")
    {
    }

    public Beekeeper(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override VendorShoeType ShoeType => VendorShoeType.Boots;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBBeekeeper());
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}