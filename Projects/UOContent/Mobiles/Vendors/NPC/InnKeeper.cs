using System.Collections.Generic;

namespace Server.Mobiles
{
  public class InnKeeper : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public InnKeeper() : base("the innkeeper")
    {
    }

    public InnKeeper(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBInnKeeper());

      if (IsTokunoVendor)
        m_SBInfos.Add(new SBSEFood());
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