namespace Server.Items
{
  public class Luckblade : Leafblade
  {
    [Constructible]
    public Luckblade() => Attributes.Luck = 20;

    public Luckblade(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073522; // luckblade

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}