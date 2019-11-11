namespace Server.Items
{
  public class IndecipherableMap : MapItem
  {
    [Constructible]
    public IndecipherableMap() => Hue = Utility.RandomDouble() < 0.2 ? 0x965 : 0x961;

    public IndecipherableMap(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1070799; // indecipherable map

    public override void OnDoubleClick(Mobile from)
    {
      from.SendLocalizedMessage(1070801); // You cannot decipher this ruined map.
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
