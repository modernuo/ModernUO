using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class KnightsWarCleaver : WarCleaver
    {
        [Constructible]
        public KnightsWarCleaver() => Attributes.RegenHits = 3;

        public override int LabelNumber => 1073525; // knight's war cleaver
    }
}
