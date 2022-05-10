using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [Flippable(0xB32, 0xB33)]
    [SerializationGenerator(0, false)]
    public partial class Throne : Item
    {
        [Constructible]
        public Throne() : base(0xB33) => Weight = 1.0;
    }

    [Furniture]
    [Flippable(0xB2E, 0xB2F, 0xB31, 0xB30)]
    [SerializationGenerator(0, false)]
    public partial class WoodenThrone : Item
    {
        [Constructible]
        public WoodenThrone() : base(0xB2E) => Weight = 15.0;
    }
}
