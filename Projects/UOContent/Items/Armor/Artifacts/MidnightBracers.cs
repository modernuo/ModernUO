using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MidnightBracers : BoneArms
    {
        [Constructible]
        public MidnightBracers()
        {
            Hue = 0x455;
            SkillBonuses.SetValues(0, SkillName.Necromancy, 20.0);
            Attributes.SpellDamage = 10;
            ArmorAttributes.MageArmor = 1;
        }

        public override int LabelNumber => 1061093; // Midnight Bracers
        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 23;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
