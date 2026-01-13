using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [SerializationGenerator(0, false)]
    public partial class ElegantLowTable : Item
    {
        [Constructible]
        public ElegantLowTable() : base(0x2819)
        {
        }

        public override double DefaultWeight => 1.0;
    }

    [Furniture]
    [SerializationGenerator(0, false)]
    public partial class PlainLowTable : Item
    {
        [Constructible]
        public PlainLowTable() : base(0x281A)
        {
        }

        public override double DefaultWeight => 1.0;
    }

    [Furniture]
    [Flippable(0xB90, 0xB7D)]
    [SerializationGenerator(0, false)]
    public partial class LargeTable : Item
    {
        [Constructible]
        public LargeTable() : base(0xB90)
        {
        }

        public override double DefaultWeight => 1.0;
    }

    [Furniture]
    [Flippable(0xB35, 0xB34)]
    [SerializationGenerator(0, false)]
    public partial class Nightstand : Item
    {
        [Constructible]
        public Nightstand() : base(0xB35)
        {
        }

        public override double DefaultWeight => 1.0;
    }

    [Furniture]
    [Flippable(0xB8F, 0xB7C)]
    [SerializationGenerator(0, false)]
    public partial class YewWoodTable : Item
    {
        [Constructible]
        public YewWoodTable() : base(0xB8F)
        {
        }

        public override double DefaultWeight => 1.0;
    }
}
