namespace Server.Items
{
  public class EarringBoxSet : RedVelvetGiftBox
  {
    [Constructible]
    public EarringBoxSet()
    {
      DropItem(new EarringsOfProtection(AosElementAttribute.Physical));
      DropItem(new EarringsOfProtection(AosElementAttribute.Fire));
      DropItem(new EarringsOfProtection(AosElementAttribute.Cold));
      DropItem(new EarringsOfProtection(AosElementAttribute.Poison));
      DropItem(new EarringsOfProtection(AosElementAttribute.Energy));
    }

    public EarringBoxSet(Serial serial)
      : base(serial)
    {
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

  public class EarringsOfProtection : BaseJewel
  {
    private AosElementAttribute m_Attribute;

    [Constructible]
    public EarringsOfProtection() : this(RandomType())
    {
    }

    [Constructible]
    public EarringsOfProtection(AosElementAttribute element)
      : base(0x1087, Layer.Earrings)
    {
      Resistances[element] = 2;

      m_Attribute = element;
      LootType = LootType.Blessed;
    }

    public EarringsOfProtection(Serial serial)
      : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual AosElementAttribute Attribute => m_Attribute;

    public override int LabelNumber => GetItemData(m_Attribute, true);

    public override int Hue => GetItemData(m_Attribute, false);

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
      writer.Write((int)m_Attribute);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
      m_Attribute = (AosElementAttribute)reader.ReadInt();
    }

    public static AosElementAttribute RandomType() => GetTypes(Utility.Random(5));

    public static AosElementAttribute GetTypes(int value)
    {
      return value switch
      {
        0 => AosElementAttribute.Physical,
        1 => AosElementAttribute.Fire,
        2 => AosElementAttribute.Cold,
        3 => AosElementAttribute.Poison,
        _ => AosElementAttribute.Energy
      };
    }

    public static int GetItemData(AosElementAttribute element, bool label)
    {
      return element switch
      {
        AosElementAttribute.Physical => label ? 1071091 : 0, // Earring of Protection (Physical)  1071091
        AosElementAttribute.Fire => label ? 1071092 : 0x4ec, // Earring of Protection (Fire)      1071092
        AosElementAttribute.Cold => label ? 1071093 : 0x4f2, // Earring of Protection (Cold)      1071093
        AosElementAttribute.Poison => label ? 1071094 : 0x4f8, // Earring of Protection (Poison)    1071094
        AosElementAttribute.Energy => label ? 1071095 : 0x4fe, // Earring of Protection (Energy)    1071095
        _ => -1
      };
    }
  }
}
