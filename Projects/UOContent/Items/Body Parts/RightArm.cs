using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class RightArm : Item
    {
        [Constructible]
        public RightArm() : base(0x1DA2)
        {
        }
    }
}
