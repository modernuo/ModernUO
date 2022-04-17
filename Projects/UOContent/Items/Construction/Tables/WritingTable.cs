using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [Flippable(0xB4A, 0xB49, 0xB4B, 0xB4C)]
    [SerializationGenerator(0, false)]
    public partial class WritingTable : Item
    {
        [Constructible]
        public WritingTable() : base(0xB4A) => Weight = 1.0;
    }
}
