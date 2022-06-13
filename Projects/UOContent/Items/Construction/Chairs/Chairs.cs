using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [Flippable(0xB4F, 0xB4E, 0xB50, 0xB51)]
    [SerializationGenerator(0, false)]
    public partial class FancyWoodenChairCushion : Item
    {
        [Constructible]
        public FancyWoodenChairCushion() : base(0xB4F) => Weight = 20.0;
    }

    [Furniture]
    [Flippable(0xB53, 0xB52, 0xB54, 0xB55)]
    [SerializationGenerator(0, false)]
    public partial class WoodenChairCushion : Item
    {
        [Constructible]
        public WoodenChairCushion() : base(0xB53) => Weight = 20.0;
    }

    [Furniture]
    [Flippable(0xB57, 0xB56, 0xB59, 0xB58)]
    [SerializationGenerator(0, false)]
    public partial class WoodenChair : Item
    {
        [Constructible]
        public WoodenChair() : base(0xB57) => Weight = 20.0;
    }

    [Furniture]
    [Flippable(0xB5B, 0xB5A, 0xB5C, 0xB5D)]
    [SerializationGenerator(0, false)]
    public partial class BambooChair : Item
    {
        [Constructible]
        public BambooChair() : base(0xB5B) => Weight = 20.0;
    }

    [DynamicFlipping]
    [Flippable(0x1218, 0x1219, 0x121A, 0x121B)]
    [SerializationGenerator(0, false)]
    public partial class StoneChair : Item
    {
        [Constructible]
        public StoneChair() : base(0x1218) => Weight = 20;
    }

    [DynamicFlipping]
    [Flippable(0x2DE3, 0x2DE4, 0x2DE5, 0x2DE6)]
    [SerializationGenerator(0)]
    public partial class OrnateElvenChair : Item
    {
        [Constructible]
        public OrnateElvenChair() : base(0x2DE3) => Weight = 1.0;
    }

    [DynamicFlipping]
    [Flippable(0x2DEB, 0x2DEC, 0x2DED, 0x2DEE)]
    [SerializationGenerator(0)]
    public partial class BigElvenChair : Item
    {
        [Constructible]
        public BigElvenChair() : base(0x2DEB)
        {
        }
    }

    [DynamicFlipping]
    [Flippable(0x2DF5, 0x2DF6)]
    [SerializationGenerator(0)]
    public partial class ElvenReadingChair : Item
    {
        [Constructible]
        public ElvenReadingChair() : base(0x2DF5)
        {
        }
    }
}
