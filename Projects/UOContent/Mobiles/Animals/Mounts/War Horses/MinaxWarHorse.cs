using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MinaxWarHorse : BaseWarHorse
    {
        [Constructible]
        public MinaxWarHorse() : base(0x78, 0x3EAF)
        {
        }
    }
}
