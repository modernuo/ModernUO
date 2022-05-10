using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BasePants : BaseClothing
    {
        public BasePants(int itemID, int hue = 0) : base(itemID, Layer.Pants, hue)
        {
        }
    }

    [Flippable(0x152e, 0x152f)]
    [SerializationGenerator(0, false)]
    public partial class ShortPants : BasePants
    {
        [Constructible]
        public ShortPants(int hue = 0) : base(0x152E, hue) => Weight = 2.0;
    }

    [Flippable(0x1539, 0x153a)]
    [SerializationGenerator(0, false)]
    public partial class LongPants : BasePants
    {
        [Constructible]
        public LongPants(int hue = 0) : base(0x1539, hue) => Weight = 2.0;
    }

    [Flippable(0x279B, 0x27E6)]
    [SerializationGenerator(0, false)]
    public partial class TattsukeHakama : BasePants
    {
        [Constructible]
        public TattsukeHakama(int hue = 0) : base(0x279B, hue) => Weight = 2.0;
    }

    [Flippable(0x2FC3, 0x3179)]
    [SerializationGenerator(0)]
    public partial class ElvenPants : BasePants
    {
        [Constructible]
        public ElvenPants(int hue = 0) : base(0x2FC3, hue) => Weight = 2.0;

        public override int RequiredRaces => Race.AllowElvesOnly;
    }
}
