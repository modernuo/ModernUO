using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ChairInAGhostCostume : Item
    {
        [Constructible]
        public ChairInAGhostCostume() : base(0x3F26)
        {
        }

        public override double DefaultWeight => 5;
    }
}
