using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SerratedWarCleaver : WarCleaver
    {
        [Constructible]
        public SerratedWarCleaver() => Attributes.WeaponDamage = 7;

        public override int LabelNumber => 1073527; // serrated war cleaver
    }
}
