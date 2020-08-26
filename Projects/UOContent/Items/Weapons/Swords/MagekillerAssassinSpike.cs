namespace Server.Items
{
  public class MagekillerAssassinSpike : AssassinSpike
  {
    [Constructible]
    public MagekillerAssassinSpike() => WeaponAttributes.HitLeechMana = 16;

    public MagekillerAssassinSpike(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073519; // magekiller assassin spike

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