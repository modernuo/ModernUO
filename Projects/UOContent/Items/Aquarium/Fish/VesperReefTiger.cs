using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class VesperReefTiger : BaseFish
    {
        [Constructible]
        public VesperReefTiger() : base(0x3B08)
        {
        }

        public override int LabelNumber => 1073836; // A Vesper Reef Tiger
    }
}
