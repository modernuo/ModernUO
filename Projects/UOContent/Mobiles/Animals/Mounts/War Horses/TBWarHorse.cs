using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class TBWarHorse : BaseWarHorse
    {
        [Constructible]
        public TBWarHorse() : base(0x76, 0x3EB2)
        {
        }
    }
}
