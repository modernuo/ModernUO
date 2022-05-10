using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Torso : Item
    {
        [Constructible]
        public Torso() : base(0x1D9F) => Weight = 2.0;
    }
}
