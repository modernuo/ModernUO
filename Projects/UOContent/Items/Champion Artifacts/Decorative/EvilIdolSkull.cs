using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class EvilIdolSkull : Item
    {
        [Constructible]
        public EvilIdolSkull() : base(0x1F18)
        {
        }

        public override int LabelNumber => 1095237; // Evil Idol
    }
}
