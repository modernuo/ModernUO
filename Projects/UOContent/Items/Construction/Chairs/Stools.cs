using ModernUO.Serialization;

namespace Server.Items
{
    [Furniture]
    [SerializationGenerator(0, false)]
    public partial class Stool : Item
    {
        [Constructible]
        public Stool() : base(0xA2A)
        {
        }

        public override double DefaultWeight => 10.0;
    }

    [Furniture]
    [SerializationGenerator(0, false)]
    public partial class FootStool : Item
    {
        [Constructible]
        public FootStool() : base(0xB5E)
        {
        }

        public override double DefaultWeight => 6.0;
    }
}
