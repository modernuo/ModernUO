namespace Server.Items
{
    public class TrueSpellblade : ElvenSpellblade
    {
        [Constructible]
        public TrueSpellblade()
        {
            Attributes.SpellChanneling = 1;
            Attributes.CastSpeed = -1;
        }

        public TrueSpellblade(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073513; // true spellblade

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
