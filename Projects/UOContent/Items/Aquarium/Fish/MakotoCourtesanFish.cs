using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class MakotoCourtesanFish : BaseFish
    {
        [Constructible]
        public MakotoCourtesanFish() : base(0x3AFD)
        {
        }

        public override int LabelNumber => 1073835; // A Makoto Courtesan Fish
    }
}
