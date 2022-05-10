using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class RightLeg : Item
    {
        [Constructible]
        public RightLeg() : base(0x1DA4)
        {
        }
    }
}
