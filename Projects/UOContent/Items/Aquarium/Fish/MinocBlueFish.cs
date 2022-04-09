using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MinocBlueFish : BaseFish
    {
        [Constructible]
        public MinocBlueFish() : base(0x3AFE)
        {
        }

        public override int LabelNumber => 1073829; // A Minoc Blue Fish
    }
}
