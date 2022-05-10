using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class JackalsCollar : PlateGorget
    {
        [Constructible]
        public JackalsCollar()
        {
            Hue = 0x6D1;
            Attributes.BonusDex = 15;
            Attributes.RegenHits = 2;
        }

        public override int LabelNumber => 1061594; // Jackal's Collar
        public override int ArtifactRarity => 11;

        public override int BaseFireResistance => 23;
        public override int BaseColdResistance => 17;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
