using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class KillerFrog : BaseFish
    {
        [Constructible]
        public KillerFrog() : base(0x3B0D)
        {
        }

        public override int LabelNumber => 1073825; // A Killer Frog
    }
}
