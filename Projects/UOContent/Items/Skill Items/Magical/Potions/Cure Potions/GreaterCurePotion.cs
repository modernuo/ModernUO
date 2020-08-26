namespace Server.Items
{
  public class GreaterCurePotion : BaseCurePotion
  {
    private static readonly CureLevelInfo[] m_OldLevelInfo =
    {
      new CureLevelInfo(Poison.Lesser, 1.00), // 100% chance to cure lesser poison
      new CureLevelInfo(Poison.Regular, 1.00), // 100% chance to cure regular poison
      new CureLevelInfo(Poison.Greater, 1.00), // 100% chance to cure greater poison
      new CureLevelInfo(Poison.Deadly, 0.75), //  75% chance to cure deadly poison
      new CureLevelInfo(Poison.Lethal, 0.25) //  25% chance to cure lethal poison
    };

    private static readonly CureLevelInfo[] m_AosLevelInfo =
    {
      new CureLevelInfo(Poison.Lesser, 1.00),
      new CureLevelInfo(Poison.Regular, 1.00),
      new CureLevelInfo(Poison.Greater, 1.00),
      new CureLevelInfo(Poison.Deadly, 0.95),
      new CureLevelInfo(Poison.Lethal, 0.75)
    };

    [Constructible]
    public GreaterCurePotion() : base(PotionEffect.CureGreater)
    {
    }

    public GreaterCurePotion(Serial serial) : base(serial)
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