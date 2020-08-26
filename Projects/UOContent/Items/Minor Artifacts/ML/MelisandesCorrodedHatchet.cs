namespace Server.Items
{
    public class MelisandesCorrodedHatchet : Hatchet
    {
        [Constructible]
        public MelisandesCorrodedHatchet()
        {
            Hue = 0x494;

            SkillBonuses.SetValues(0, SkillName.Lumberjacking, 5.0);

            Attributes.SpellChanneling = 1;
            Attributes.WeaponSpeed = 15;
            Attributes.WeaponDamage = -50;

            WeaponAttributes.SelfRepair = 4;
        }

        public MelisandesCorrodedHatchet(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072115; // Melisande's Corroded Hatchet

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
