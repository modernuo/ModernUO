using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class TavernKeeper : BaseVendor
  {
    private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public TavernKeeper() : base("the tavern keeper")
    {
    }

    public TavernKeeper(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBTavernKeeper());
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