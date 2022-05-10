using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MetallicClothDyetub : DyeTub
    {
        [Constructible]
        public MetallicClothDyetub() => LootType = LootType.Blessed;

        public override int LabelNumber => 1152920; // Metallic Cloth ...

        public override bool MetallicHues => true;
    }
}
