using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class GoldenBroadtail : BaseFish
    {
        [Constructible]
        public GoldenBroadtail() : base(0x3B03)
        {
        }

        public override int LabelNumber => 1073828; // A Golden Broadtail
    }
}
