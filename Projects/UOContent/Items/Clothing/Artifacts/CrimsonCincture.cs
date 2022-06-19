using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class CrimsonCincture : HalfApron
    {
        [Constructible]
        public CrimsonCincture()
        {
            Hue = 0x485;

            Attributes.BonusDex = 5;
            Attributes.BonusHits = 10;
            Attributes.RegenHits = 2;
        }
    }
}
