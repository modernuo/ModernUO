namespace Server.Items
{
  public class BlueSnowflake : Item
  {
    [Constructible]
    public BlueSnowflake() : base(0x232E)
    {
      Weight = 1.0;
      LootType = LootType.Blessed;
    }

    public BlueSnowflake(Serial serial) : base(serial)
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

  public class WhiteSnowflake : Item
  {
    [Constructible]
    public WhiteSnowflake() : base(0x232F)
    {
      Weight = 1.0;
      LootType = LootType.Blessed;
    }

    public WhiteSnowflake(Serial serial) : base(serial)
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
}