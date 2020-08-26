namespace Server.Items
{
  public class CurePotion : BaseCurePotion
  {
    private static readonly CureLevelInfo[] m_OldLevelInfo =
    {
      new CureLevelInfo(Poison.Lesser, 1.00), // 100% chance to cure lesser poison
      new CureLevelInfo(Poison.Regular, 0.75), //  75% chance to cure regular poison
      new CureLevelInfo(Poison.Greater, 0.50), //  50% chance to cure greater poison
      new CureLevelInfo(Poison.Deadly, 0.15) //  15% chance to cure deadly poison
    };

    private static readonly CureLevelInfo[] m_AosLevelInfo =
    {
      new CureLevelInfo(Poison.Lesser, 1.00),
      new CureLevelInfo(Poison.Regular, 0.95),
      new CureLevelInfo(Poison.Greater, 0.75),
      new CureLevelInfo(Poison.Deadly, 0.50),
      new CureLevelInfo(Poison.Lethal, 0.25)
    };

    [Constructible]
    public CurePotion() : base(PotionEffect.Cure)
    {
    }

    public CurePotion(Serial serial) : base(serial)
    {
    }

    public override CureLevelInfo[] LevelInfo => Core.AOS ? m_AosLevelInfo : m_OldLevelInfo;

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