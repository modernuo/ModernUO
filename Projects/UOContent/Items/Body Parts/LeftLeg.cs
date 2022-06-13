using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LeftLeg : Item
    {
        [Constructible]
        public LeftLeg() : base(0x1DA3)
        {
        }
    }
}
