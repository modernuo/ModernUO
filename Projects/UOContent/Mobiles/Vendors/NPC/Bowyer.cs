using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  [TypeAlias("Server.Mobiles.Bower")]
  public class Bowyer : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public Bowyer() : base("the bowyer")
    {
      SetSkill(SkillName.Fletching, 80.0, 100.0);
      SetSkill(SkillName.Archery, 80.0, 100.0);
    }

    public Bowyer(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override VendorShoeType ShoeType => Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots;

    public override int GetShoeHue() => 0;

    public override void InitOutfit()
    {
      base.InitOutfit();

      AddItem(new Bow());
      AddItem(new LeatherGorget());
    }

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBBowyer());
      m_SBInfos.Add(new SBRangedWeapon());

      if (IsTokunoVendor)
        m_SBInfos.Add(new SBSEBowyer());
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