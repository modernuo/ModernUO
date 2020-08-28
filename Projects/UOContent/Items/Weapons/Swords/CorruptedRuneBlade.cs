namespace Server.Items
{
    public class CorruptedRuneBlade : RuneBlade
    {
        [Constructible]
        public CorruptedRuneBlade()
        {
            WeaponAttributes.ResistPhysicalBonus = -5;
            WeaponAttributes.ResistPoisonBonus = 12;
        }

        public CorruptedRuneBlade(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073540; // Corrupted Rune Blade

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
