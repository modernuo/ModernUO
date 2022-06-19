using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x4F7C, 0x4F7D)]
    public partial class CupidStatue : Item
    {
        [Constructible]
        public CupidStatue() : base(0x4F7D) => LootType = LootType.Blessed;

        public override int LabelNumber => 1099220; // cupid statue
    }
}
