namespace Server.Items
{
  public class DupreSuit : BaseSuit
  {
    [Constructible]
    public DupreSuit() : base(AccessLevel.GameMaster, 0x0, 0x2050)
    {
    }

    public DupreSuit(Serial serial) : base(serial)
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