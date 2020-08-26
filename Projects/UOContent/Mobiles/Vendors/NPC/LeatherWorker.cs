using System.Collections.Generic;

namespace Server.Mobiles
{
  public class LeatherWorker : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public LeatherWorker() : base("the leather worker")
    {
    }

    public LeatherWorker(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBLeatherArmor());
      m_SBInfos.Add(new SBStuddedArmor());
      m_SBInfos.Add(new SBLeatherWorker());
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