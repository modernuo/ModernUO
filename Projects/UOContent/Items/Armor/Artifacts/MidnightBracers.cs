namespace Server.Items
{
    public class MidnightBracers : BoneArms
    {
        [Constructible]
        public MidnightBracers()
        {
            Hue = 0x455;
            SkillBonuses.SetValues(0, SkillName.Necromancy, 20.0);
            Attributes.SpellDamage = 10;
            ArmorAttributes.MageArmor = 1;
        }

        public MidnightBracers(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061093; // Midnight Bracers
        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 23;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1)
                PhysicalBonus = 0;
        }
    }
}
