using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseOuterLegs : BaseClothing
    {
        public BaseOuterLegs(int itemID, int hue = 0) : base(itemID, Layer.OuterLegs, hue)
        {
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x230C, 0x230B)]
    public partial class FurSarong : BaseOuterLegs
    {
        [Constructible]
        public FurSarong(int hue = 0) : base(0x230C, hue) => Weight = 3.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1516, 0x1531)]
    public partial class Skirt : BaseOuterLegs
    {
        [Constructible]
        public Skirt(int hue = 0) : base(0x1516, hue) => Weight = 4.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1537, 0x1538)]
    public partial class Kilt : BaseOuterLegs
    {
        [Constructible]
        public Kilt(int hue = 0) : base(0x1537, hue) => Weight = 2.0;
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x279A, 0x27E5)]
    public partial class Hakama : BaseOuterLegs
    {
        [Constructible]
        public Hakama(int hue = 0) : base(0x279A, hue) => Weight = 2.0;
    }
}
