using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TheMostKnowledgePerson : BaseOuterTorso
    {
        [Constructible]
        public TheMostKnowledgePerson() : base(0x2684)
        {
            Hue = 0x117;
            StrRequirement = 0;

            Attributes.BonusHits = 3 + Utility.RandomMinMax(0, 2);
        }

        public override int LabelNumber => 1094893; // The Most Knowledge Person [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        public override bool CanBeBlessed => false;
    }
}
