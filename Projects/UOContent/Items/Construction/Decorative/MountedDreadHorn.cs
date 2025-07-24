using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x3158, 0x3159)]
    [SerializationGenerator(0)]
    public partial class MountedDreadHorn : Item
    {
        [Constructible]
        public MountedDreadHorn() : base(0x3158)
        {
        }

        public override double DefaultWeight => 1.0;

        public override int LabelNumber => 1074464; // mounted Dread Horn
    }
}
