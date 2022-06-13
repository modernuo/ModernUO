using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ColoredSmallWebs : Item
    {
        [Constructible]
        public ColoredSmallWebs() : base(Utility.RandomBool() ? 0x10d6 : 0x10d7) =>
            Hue = Utility.RandomBool() ? 0x455 : 0x4E9;

        public override double DefaultWeight => 5;
    }
}
