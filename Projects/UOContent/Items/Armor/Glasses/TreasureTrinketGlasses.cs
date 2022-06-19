using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TreasureTrinketGlasses : ElvenGlasses
    {
        [Constructible]
        public TreasureTrinketGlasses()
        {
            Attributes.BonusInt = 10;
            Attributes.BonusHits = 5;
            Attributes.SpellDamage = 10;

            Hue = 0x1C2;
        }

        public override int LabelNumber => 1073373; // Treasures and Trinkets Reading Glasses

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 10;
        public override int BaseColdResistance => 10;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
