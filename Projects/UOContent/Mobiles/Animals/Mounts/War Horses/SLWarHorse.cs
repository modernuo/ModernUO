using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SLWarHorse : BaseWarHorse
    {
        [Constructible]
        public SLWarHorse() : base(0x79, 0x3EB0)
        {
        }
    }
}
