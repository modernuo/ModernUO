using System.Collections.Generic;

namespace Server.Mobiles
{
  public class Jeweler : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public Jeweler() : base("the jeweler")
    {
      SetSkill(SkillName.ItemID, 64.0, 100.0);
    }

    public Jeweler(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBJewel());
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