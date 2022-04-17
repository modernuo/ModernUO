using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ShroudOfDeciet : BoneChest
    {
        [Constructible]
        public ShroudOfDeciet()
        {
            Hue = 0x38F;

            Attributes.RegenHits = 3;

            ArmorAttributes.MageArmor = 1;

            SkillBonuses.Skill_1_Name = SkillName.MagicResist;
            SkillBonuses.Skill_1_Value = 10;
        }

        public override int LabelNumber => 1094914; // Shroud of Deceit [Replica]

        public override int BasePhysicalResistance => 11;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 18;
        public override int BasePoisonResistance => 15;
        public override int BaseEnergyResistance => 13;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
