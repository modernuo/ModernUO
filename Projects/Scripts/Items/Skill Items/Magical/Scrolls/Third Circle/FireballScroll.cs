namespace Server.Items
{
  public class FireballScroll : SpellScroll
  {
    [Constructible]
    public FireballScroll(int amount = 1) : base(17, 0x1F3E, amount)
    {
    }

    public FireballScroll(Serial serial) : base(serial)
    {
    }

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
