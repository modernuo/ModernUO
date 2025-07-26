using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Torso : Item
    {
        [Constructible]
        public Torso() : base(0x1D9F)
        {
        }

        public override double DefaultWeight => 2.0;
    }
}
