namespace Server.Items
{
  public class SapOfSosaria : Item
  {
    [Constructible]
    public SapOfSosaria() : this(1)
    {
    }

    [Constructible]
    public SapOfSosaria(int amount) : base(0x1848)
    {
      LootType = LootType.Blessed;
      Stackable = true;
      Amount = amount;
    }

    public SapOfSosaria(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1074178; // Sap of Sosaria

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}