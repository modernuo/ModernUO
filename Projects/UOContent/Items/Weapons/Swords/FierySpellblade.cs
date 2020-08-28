namespace Server.Items
{
    public class FierySpellblade : ElvenSpellblade
    {
        [Constructible]
        public FierySpellblade() => WeaponAttributes.ResistFireBonus = 5;

        public FierySpellblade(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073515; // fiery spellblade

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
