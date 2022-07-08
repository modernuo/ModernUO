using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MaritimeGlasses : ElvenGlasses
    {
        [Constructible]
        public MaritimeGlasses()
        {
            Attributes.Luck = 150;
            Attributes.NightSight = 1;
            Attributes.ReflectPhysical = 20;

            Hue = 0x581;
        }

        public override int LabelNumber => 1073364; // Maritime Reading Glasses

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 30;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
