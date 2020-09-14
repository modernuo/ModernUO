namespace Server.Items
{
    public class NecromancerSpellbook : Spellbook
    {
        [Constructible]
        public NecromancerSpellbook(ulong content = 0) : base(content, 0x2253) =>
            Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;

        public NecromancerSpellbook(Serial serial) : base(serial)
        {
        }

        public override SpellbookType SpellbookType => SpellbookType.Necromancer;
        public override int BookOffset => 100;
        public override int BookCount => Core.SE ? 17 : 16;

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
