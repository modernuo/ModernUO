using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [SerializationGenerator(0)]
    [Flippable(0x2DDD, 0x2DDE)]
    public partial class ElvenPodium : Item
    {
        [Constructible]
        public ElvenPodium() : base(0x2DDD) => Weight = 2.0;
    }
}
