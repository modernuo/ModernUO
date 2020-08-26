namespace Server.Items
{
  public class BookOfNinjitsu : Spellbook
  {
    [Constructible]
    public BookOfNinjitsu(ulong content = 0xFF) : base(content, 0x23A0) => Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;

    public BookOfNinjitsu(Serial serial) : base(serial)
    {
    }

    public override SpellbookType SpellbookType => SpellbookType.Ninja;
    public override int BookOffset => 500;
    public override int BookCount => 8;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version == 0 && Core.ML)
        Layer = Layer.OneHanded;
    }
  }
}
