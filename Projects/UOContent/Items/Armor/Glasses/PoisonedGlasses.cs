using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class PoisonedGlasses : ElvenGlasses
    {
        [Constructible]
        public PoisonedGlasses()
        {
            Attributes.BonusStam = 3;
            Attributes.RegenStam = 4;

            Hue = 0x113;
        }

        public override int LabelNumber => 1073376; // Poisoned Reading Glasses

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 30;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
