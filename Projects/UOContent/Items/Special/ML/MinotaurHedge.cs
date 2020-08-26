namespace Server.Items
{
  public class MinotaurHedge : Item
  {
    [Constructible]
    public MinotaurHedge() : base(Utility.Random(3215, 4)) => Weight = 1.0;

    public MinotaurHedge(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "minotaur hedge";

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