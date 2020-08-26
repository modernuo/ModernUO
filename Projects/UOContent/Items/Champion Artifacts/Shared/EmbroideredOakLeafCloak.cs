namespace Server.Items
{
    public class EmbroideredOakLeafCloak : BaseOuterTorso
    {
        [Constructible]
        public EmbroideredOakLeafCloak() : base(0x2684)
        {
            Hue = 0x483;
            StrRequirement = 0;

            SkillBonuses.Skill_1_Name = SkillName.Stealth;
            SkillBonuses.Skill_1_Value = 5;
        }

        public EmbroideredOakLeafCloak(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094901; // Embroidered Oak Leaf Cloak [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
