namespace Server.Items
{
  public class CreateFoodScroll : SpellScroll
  {
    [Constructible]
    public CreateFoodScroll(int amount = 1) : base(1, 0x1F2F, amount)
    {
    }

    public CreateFoodScroll(Serial serial) : base(serial)
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
