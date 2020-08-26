namespace Server.Items
{
  public class MeteorSwarmScroll : SpellScroll
  {
    [Constructible]
    public MeteorSwarmScroll(int amount = 1) : base(54, 0x1F63, amount)
    {
    }

    public MeteorSwarmScroll(Serial serial) : base(serial)
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
