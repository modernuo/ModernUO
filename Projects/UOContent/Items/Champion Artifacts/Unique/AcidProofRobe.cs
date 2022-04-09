using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AcidProofRobe : Robe
    {
        [Constructible]
        public AcidProofRobe()
        {
            Hue = 0x455;
            LootType = LootType.Blessed;
        }

        public override int LabelNumber => 1095236; // Acid-Proof Robe [Replica]

        public override int BaseFireResistance => 4;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
