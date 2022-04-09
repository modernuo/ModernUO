using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SpottedBuccaneer : BaseFish
    {
        [Constructible]
        public SpottedBuccaneer() : base(0x3B09)
        {
        }

        public override int LabelNumber => 1073833; // A Spotted Buccaneer
    }
}
