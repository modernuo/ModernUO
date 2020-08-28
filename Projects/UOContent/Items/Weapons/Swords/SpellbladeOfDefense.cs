namespace Server.Items
{
    public class SpellbladeOfDefense : ElvenSpellblade
    {
        [Constructible]
        public SpellbladeOfDefense() => Attributes.DefendChance = 5;

        public SpellbladeOfDefense(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073516; // spellblade of defense

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
