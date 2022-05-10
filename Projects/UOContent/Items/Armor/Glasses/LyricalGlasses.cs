using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LyricalGlasses : ElvenGlasses
    {
        [Constructible]
        public LyricalGlasses()
        {
            _weaponAttributes.HitLowerDefend = 20;
            Attributes.NightSight = 1;
            Attributes.ReflectPhysical = 15;

            Hue = 0x47F;
        }

        public override int LabelNumber => 1073382; // Lyrical Reading Glasses

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
