namespace Server.Items
{
  public class LeafbladeOfEase : Leafblade
  {
    [Constructible]
    public LeafbladeOfEase() => WeaponAttributes.UseBestSkill = 1;

    public LeafbladeOfEase(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073524; // leafblade of ease

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