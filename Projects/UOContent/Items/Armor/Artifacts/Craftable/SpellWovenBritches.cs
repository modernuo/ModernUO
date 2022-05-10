using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SpellWovenBritches : LeafLegs
    {
        [Constructible]
        public SpellWovenBritches()
        {
            Hue = 0x487;

            SkillBonuses.SetValues(0, SkillName.Meditation, 10.0);

            Attributes.BonusInt = 8;
            Attributes.SpellDamage = 10;
            Attributes.LowerManaCost = 10;
        }

        public override int LabelNumber => 1072929; // Spell Woven Britches

        public override int BaseFireResistance => 15;
        public override int BasePoisonResistance => 16;
    }
}
