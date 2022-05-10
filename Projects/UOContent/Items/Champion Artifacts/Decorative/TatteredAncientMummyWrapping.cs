using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TatteredAncientMummyWrapping : Item
    {
        [Constructible]
        public TatteredAncientMummyWrapping() : base(0xE21) => Hue = 0x909;

        public override int LabelNumber => 1094912; // Tattered Ancient Mummy Wrapping [Replica]
    }
}
