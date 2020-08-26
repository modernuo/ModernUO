namespace Server.Items
{
  public class MagekillerLeafblade : Leafblade
  {
    [Constructible]
    public MagekillerLeafblade() => WeaponAttributes.HitLeechMana = 16;

    public MagekillerLeafblade(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073523; // maagekiller leafblade

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