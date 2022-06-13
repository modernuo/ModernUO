using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ArtsGlasses : ElvenGlasses
    {
        [Constructible]
        public ArtsGlasses()
        {
            Attributes.BonusStr = 5;
            Attributes.BonusInt = 5;
            Attributes.BonusHits = 15;

            Hue = 0x73;
        }

        public override int LabelNumber => 1073363; // Reading Glasses of the Arts

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 8;
        public override int BaseColdResistance => 8;
        public override int BasePoisonResistance => 4;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
