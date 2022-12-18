using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class CoMWarHorse : BaseWarHorse
    {
        [Constructible]
        public CoMWarHorse() : base(0x77, 0x3EB1)
        {
        }
    }
}
