using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FoldedSteelGlasses : ElvenGlasses
    {
        [Constructible]
        public FoldedSteelGlasses()
        {
            Attributes.BonusStr = 8;
            Attributes.NightSight = 1;
            Attributes.DefendChance = 15;

            Hue = 0x47E;
        }

        public override int LabelNumber => 1073380; // Folded Steel Reading Glasses

        public override int BasePhysicalResistance => 20;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
