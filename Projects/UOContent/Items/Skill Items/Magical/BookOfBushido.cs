namespace Server.Items
{
    public class BookOfBushido : Spellbook
    {
        [Constructible]
        public BookOfBushido(ulong content = 0x3F) : base(content, 0x238C) =>
            Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;

        public BookOfBushido(Serial serial) : base(serial)
        {
        }

        public override SpellbookType SpellbookType => SpellbookType.Samurai;
        public override int BookOffset => 400;
        public override int BookCount => 6;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version == 0 && Core.ML)
            {
                Layer = Layer.OneHanded;
            }
        }
    }
}
