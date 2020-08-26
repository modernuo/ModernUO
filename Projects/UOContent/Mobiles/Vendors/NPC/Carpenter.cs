using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class Carpenter : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public Carpenter() : base("the carpenter")
    {
      SetSkill(SkillName.Carpentry, 85.0, 100.0);
      SetSkill(SkillName.Lumberjacking, 60.0, 83.0);
    }

    public Carpenter(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBStavesWeapon());
      m_SBInfos.Add(new SBCarpenter());
      m_SBInfos.Add(new SBWoodenShields());

      if (IsTokunoVendor)
        m_SBInfos.Add(new SBSECarpenter());
    }

    public override void InitOutfit()
    {
      base.InitOutfit();

      AddItem(new HalfApron());
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