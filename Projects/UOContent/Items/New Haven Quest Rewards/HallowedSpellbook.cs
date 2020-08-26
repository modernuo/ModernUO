namespace Server.Items
{
  public class HallowedSpellbook : Spellbook
  {
    [Constructible]
    public HallowedSpellbook() : base(0x3FFFFFFFF)
    {
      LootType = LootType.Blessed;

      Slayer = SlayerName.Silver;
    }

    public HallowedSpellbook(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1077620; // Hallowed Spellbook

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