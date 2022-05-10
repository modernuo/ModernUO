using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FullMoonFish : BaseFish
    {
        [Constructible]
        public FullMoonFish() : base(0x3B15)
        {
        }

        public override int LabelNumber => 1074597; // A Full Moon Fish
    }
}
