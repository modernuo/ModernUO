using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class EmbroideredOakLeafCloak : BaseOuterTorso
    {
        [Constructible]
        public EmbroideredOakLeafCloak() : base(0x2684)
        {
            Hue = 0x483;
            StrRequirement = 0;

            SkillBonuses.Skill_1_Name = SkillName.Stealth;
            SkillBonuses.Skill_1_Value = 5;
        }

        public override int LabelNumber => 1094901; // Embroidered Oak Leaf Cloak [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
