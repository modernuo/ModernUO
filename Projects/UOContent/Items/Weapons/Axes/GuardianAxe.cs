using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class GuardianAxe : OrnateAxe
    {
        [Constructible]
        public GuardianAxe()
        {
            Attributes.BonusHits = 4;
            Attributes.RegenHits = 1;
        }

        public override int LabelNumber => 1073545; // guardian axe
    }
}
