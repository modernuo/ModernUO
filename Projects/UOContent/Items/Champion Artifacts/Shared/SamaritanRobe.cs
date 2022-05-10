using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SamaritanRobe : Robe
    {
        [Constructible]
        public SamaritanRobe() => Hue = 0x2a3;

        public override int LabelNumber => 1094926; // Good Samaritan of Britannia [Replica]

        public override int BasePhysicalResistance => 5;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
