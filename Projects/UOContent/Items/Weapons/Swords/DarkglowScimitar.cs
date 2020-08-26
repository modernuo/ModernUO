namespace Server.Items
{
  public class DarkglowScimitar : RadiantScimitar
  {
    [Constructible]
    public DarkglowScimitar() => WeaponAttributes.HitDispel = 10;

    public DarkglowScimitar(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073542; // darkglow scimitar

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