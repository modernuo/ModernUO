using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class DjinnisRing : SilverRing
    {
        [Constructible]
        public DjinnisRing()
        {
            Attributes.BonusInt = 5;
            Attributes.SpellDamage = 10;
            Attributes.CastSpeed = 2;
        }

        public override int LabelNumber => 1094927; // Djinni's Ring [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;
    }
}
