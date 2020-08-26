namespace Server.Items
{
  public class StrangleScroll : SpellScroll
  {
    [Constructible]
    public StrangleScroll(int amount = 1) : base(110, 0x226A, amount)
    {
    }

    public StrangleScroll(Serial serial) : base(serial)
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
