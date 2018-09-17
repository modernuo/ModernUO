namespace Server.Items
{
  public class ButchersWarCleaver : WarCleaver
  {
    [Constructible]
    public ButchersWarCleaver()
    {
    }

    public ButchersWarCleaver(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073526; // butcher's war cleaver

    public override void AppendChildNameProperties(ObjectPropertyList list)
    {
      base.AppendChildNameProperties(list);

      list.Add(1072512); // Bovine Slayer
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}