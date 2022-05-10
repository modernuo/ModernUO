using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class StitchersMittens : LeafGloves
    {
        [Constructible]
        public StitchersMittens()
        {
            Hue = 0x481;

            SkillBonuses.SetValues(0, SkillName.Healing, 10.0);

            Attributes.BonusDex = 5;
            Attributes.LowerRegCost = 30;
        }

        public override int LabelNumber => 1072932; // Stitcher's Mittens

        public override int BasePhysicalResistance => 20;
        public override int BaseColdResistance => 20;
    }
}
