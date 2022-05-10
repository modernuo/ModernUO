using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ANecromancerShroud : Robe
    {
        [Constructible]
        public ANecromancerShroud() => Hue = 0x455;

        public override int LabelNumber => 1094913; // A Necromancer Shroud [Replica]

        public override int BaseColdResistance => 5;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
