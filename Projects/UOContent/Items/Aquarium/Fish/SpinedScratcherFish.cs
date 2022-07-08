using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SpinedScratcherFish : BaseFish
    {
        [Constructible]
        public SpinedScratcherFish() : base(0x3B05)
        {
        }

        public override int LabelNumber => 1073832; // A Spined Scratcher Fish
    }
}
