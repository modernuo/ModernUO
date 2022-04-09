using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class HatOfTheMagi : WizardsHat
    {
        [Constructible]
        public HatOfTheMagi()
        {
            Hue = 0x481;

            Attributes.BonusInt = 8;
            Attributes.RegenMana = 4;
            Attributes.SpellDamage = 10;
        }

        public override int LabelNumber => 1061597; // Hat of the Magi

        public override int ArtifactRarity => 11;

        public override int BasePoisonResistance => 20;
        public override int BaseEnergyResistance => 20;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
