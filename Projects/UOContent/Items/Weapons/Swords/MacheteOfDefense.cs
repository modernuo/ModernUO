namespace Server.Items
{
  public class MacheteOfDefense : ElvenMachete
  {
    [Constructible]
    public MacheteOfDefense() => Attributes.DefendChance = 5;

    public MacheteOfDefense(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073535; // machete of defense

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