namespace Server.Items
{
  public class IcySpellblade : ElvenSpellblade
  {
    [Constructible]
    public IcySpellblade() => WeaponAttributes.ResistColdBonus = 5;

    public IcySpellblade(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073514; // icy spellblade

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