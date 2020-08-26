namespace Server.Items
{
  public class DecoMagicalCrystal : Item
  {
    [Constructible]
    public DecoMagicalCrystal() : base(0x1F19)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoMagicalCrystal(Serial serial) : base(serial)
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