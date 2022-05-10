using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class TrueAssassinSpike : AssassinSpike
    {
        [Constructible]
        public TrueAssassinSpike()
        {
            Attributes.AttackChance = 4;
            Attributes.WeaponDamage = 4;
        }

        public override int LabelNumber => 1073517; // true assassin spike
    }
}
