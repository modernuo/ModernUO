using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class Alchemist : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public Alchemist() : base("the alchemist")
    {
      SetSkill(SkillName.Alchemy, 85.0, 100.0);
      SetSkill(SkillName.TasteID, 65.0, 88.0);
    }

    public Alchemist(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override NpcGuild NpcGuild => NpcGuild.MagesGuild;

    public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBAlchemist());
    }

    public override void InitOutfit()
    {
      base.InitOutfit();

      AddItem(new Robe(Utility.RandomPinkHue()));
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