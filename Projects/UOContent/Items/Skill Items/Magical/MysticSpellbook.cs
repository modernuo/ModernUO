namespace Server.Items
{
  public class MysticSpellbook : Spellbook
  {
    [Constructible]
    public MysticSpellbook(ulong content = 0)
      : base(content, 0x2D9D) =>
      Layer = Layer.OneHanded;

    public MysticSpellbook(Serial serial)
      : base(serial)
    {
    }

    public override SpellbookType SpellbookType => SpellbookType.Mystic;

    public override int BookOffset => 677;
    public override int BookCount => 16;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      /*int version = */
      reader.ReadInt();
    }
  }
}
