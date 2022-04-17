using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BritainCrownFish : BaseFish
    {
        [Constructible]
        public BritainCrownFish() : base(0x3AFF)
        {
        }

        public override int LabelNumber => 1074589; // Britain Crown Fish
    }
}
