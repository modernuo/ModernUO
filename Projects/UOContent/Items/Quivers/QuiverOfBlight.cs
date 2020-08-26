namespace Server.Items
{
  public class QuiverOfBlight : ElvenQuiver
  {
    [Constructible]
    public QuiverOfBlight() => Hue = 0x4F3;

    public QuiverOfBlight(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073111; // Quiver of Blight

    public override void AlterBowDamage(ref int phys, ref int fire, ref int cold, ref int pois, ref int nrgy,
      ref int chaos, ref int direct)
    {
      phys = fire = nrgy = chaos = direct = 0;
      cold = pois = 50;
    }

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