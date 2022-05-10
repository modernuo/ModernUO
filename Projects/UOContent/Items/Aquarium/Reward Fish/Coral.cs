using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Coral : BaseFish
    {
        [Constructible]
        public Coral() : base(Utility.RandomList(0x3AF9, 0x3AFA, 0x3AFB))
        {
        }

        public override int LabelNumber => 1074588; // Coral
    }
}
