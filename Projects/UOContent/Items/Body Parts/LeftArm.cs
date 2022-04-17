using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LeftArm : Item
    {
        [Constructible]
        public LeftArm() : base(0x1DA1)
        {
        }
    }
}
