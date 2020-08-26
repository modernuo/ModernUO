namespace Server.Items
{
  public class RuneBladeOfKnowledge : RuneBlade
  {
    [Constructible]
    public RuneBladeOfKnowledge() => Attributes.SpellDamage = 5;

    public RuneBladeOfKnowledge(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073539; // rune blade of knowledge

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