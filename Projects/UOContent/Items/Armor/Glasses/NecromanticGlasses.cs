using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class NecromanticGlasses : ElvenGlasses
    {
        [Constructible]
        public NecromanticGlasses()
        {
            Attributes.LowerManaCost = 15;
            Attributes.LowerRegCost = 30;

            Hue = 0x22D;
        }

        public override int LabelNumber => 1073377; // Necromantic Reading Glasses

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 0;
        public override int BaseColdResistance => 0;
        public override int BasePoisonResistance => 0;
        public override int BaseEnergyResistance => 0;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
