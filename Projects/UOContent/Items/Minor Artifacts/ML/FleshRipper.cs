namespace Server.Items
{
    public class FleshRipper : AssassinSpike
    {
        [Constructible]
        public FleshRipper()
        {
            Hue = 0x341;

            SkillBonuses.SetValues(0, SkillName.Anatomy, 10.0);

            Attributes.BonusStr = 5;
            Attributes.AttackChance = 15;
            Attributes.WeaponSpeed = 40;

            WeaponAttributes.UseBestSkill = 1;
            // TODO: Mage Slayer
        }

        public FleshRipper(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075045; // Flesh Ripper

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

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
