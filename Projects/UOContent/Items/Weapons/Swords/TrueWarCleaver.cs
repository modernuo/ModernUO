using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class TrueWarCleaver : WarCleaver
    {
        [Constructible]
        public TrueWarCleaver()
        {
            Attributes.WeaponDamage = 4;
            Attributes.RegenHits = 2;
        }

        public override int LabelNumber => 1073528; // true war cleaver
    }
}
