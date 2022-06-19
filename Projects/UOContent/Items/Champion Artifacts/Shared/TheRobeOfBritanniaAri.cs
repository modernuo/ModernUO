using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TheRobeOfBritanniaAri : BaseOuterTorso
    {
        [Constructible]
        public TheRobeOfBritanniaAri() : base(0x2684)
        {
            Hue = 0x48b;
            StrRequirement = 0;
        }

        public override int LabelNumber => 1094931; // The Robe of Britannia "Ari" [Replica]

        public override int BasePhysicalResistance => 10;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
