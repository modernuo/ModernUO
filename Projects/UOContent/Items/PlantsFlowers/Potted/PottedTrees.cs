namespace Server.Items
{
  public class PottedTree : Item
  {
    [Constructible]
    public PottedTree() : base(0x11C8) => Weight = 100;

    public PottedTree(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class PottedTree1 : Item
  {
    [Constructible]
    public PottedTree1() : base(0x11C9) => Weight = 100;

    public PottedTree1(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}