using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [SerializationGenerator(0, false)]
    [Flippable(0xB2D, 0xB2C)]
    public partial class WoodenBench : Item
    {
        [Constructible]
        public WoodenBench() : base(0xB2D) => Weight = 6;
    }
}
