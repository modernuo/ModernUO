using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseShirt : BaseClothing
    {
        public BaseShirt(int itemID, int hue = 0) : base(itemID, Layer.Shirt, hue)
        {
        }
    }

    [Flippable(0x1efd, 0x1efe)]
    [SerializationGenerator(0, false)]
    public partial class FancyShirt : BaseShirt
    {
        [Constructible]
        public FancyShirt(int hue = 0) : base(0x1EFD, hue)
        {
        }

        public override double DefaultWeight => 2.0;
    }

    [Flippable(0x1517, 0x1518)]
    [SerializationGenerator(0, false)]
    public partial class Shirt : BaseShirt
    {
        [Constructible]
        public Shirt(int hue = 0) : base(0x1517, hue)
        {
        }

        public override double DefaultWeight => 1.0;
    }

    [Flippable(0x2794, 0x27DF)]
    [SerializationGenerator(0, false)]
    public partial class ClothNinjaJacket : BaseShirt
    {
        [Constructible]
        public ClothNinjaJacket(int hue = 0) : base(0x2794, hue) => Layer = Layer.InnerTorso;

        public override double DefaultWeight => 5.0;
    }

    [SerializationGenerator(0)]
    public partial class ElvenShirt : BaseShirt
    {
        [Constructible]
        public ElvenShirt(int hue = 0) : base(0x3175, hue)
        {
        }

        public override double DefaultWeight => 2.0;

        public override int RequiredRaces => Race.AllowElvesOnly;
    }

    [SerializationGenerator(0)]
    public partial class ElvenDarkShirt : BaseShirt
    {
        [Constructible]
        public ElvenDarkShirt(int hue = 0) : base(0x3176, hue)
        {
        }

        public override double DefaultWeight => 2.0;
    }
}
