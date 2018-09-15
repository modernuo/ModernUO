namespace Server.Items
{
  public class BookOfChivalry : Spellbook
  {
    [Constructible]
    public BookOfChivalry() : this((ulong)0x3FF)
    {
    }

    [Constructible]
    public BookOfChivalry(ulong content) : base(content, 0x2252)
    {
      Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;
    }

    public BookOfChivalry(Serial serial) : base(serial)
    {
    }

    public override SpellbookType SpellbookType => SpellbookType.Paladin;
    public override int BookOffset => 200;
    public override int BookCount => 10;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version == 0 && Core.ML)
        Layer = Layer.OneHanded;
    }
  }
}