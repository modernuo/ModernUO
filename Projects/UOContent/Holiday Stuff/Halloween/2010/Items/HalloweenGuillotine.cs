using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class HalloweenGuillotine : Item
    {
        [Constructible]
        public HalloweenGuillotine() : base(0x3F27)
        {
        }

        public override double DefaultWeight => 5;
    }
}
