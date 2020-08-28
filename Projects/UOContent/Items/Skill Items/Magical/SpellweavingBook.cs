namespace Server.Items
{
    public class SpellweavingBook : Spellbook
    {
        [Constructible]
        public SpellweavingBook(ulong content = 0) : base(content, 0x2D50)
        {
            Hue = 0x8A2;

            Layer = Layer.OneHanded;
        }

        public SpellweavingBook(Serial serial) : base(serial)
        {
        }

        public override SpellbookType SpellbookType => SpellbookType.Arcanist;
        public override int BookOffset => 600;
        public override int BookCount => 16;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
