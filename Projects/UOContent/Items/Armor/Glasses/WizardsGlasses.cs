using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class WizardsGlasses : ElvenGlasses
    {
        [Constructible]
        public WizardsGlasses()
        {
            Attributes.BonusMana = 10;
            Attributes.RegenMana = 3;
            Attributes.SpellDamage = 15;

            Hue = 0x2B0;
        }

        public override int LabelNumber => 1073374; // Wizard's Crystal Reading Glasses

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
