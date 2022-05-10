using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MaceShieldGlasses : ElvenGlasses
    {
        [Constructible]
        public MaceShieldGlasses()
        {
            _weaponAttributes.HitLowerDefend = 30;
            Attributes.BonusStr = 10;
            Attributes.BonusDex = 5;

            Hue = 0x1DD;
        }

        public override int LabelNumber => 1073381; // Mace And Shield Reading Glasses

        public override int BasePhysicalResistance => 25;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
