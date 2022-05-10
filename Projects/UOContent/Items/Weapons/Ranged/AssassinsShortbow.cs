using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class AssassinsShortbow : MagicalShortbow
    {
        [Constructible]
        public AssassinsShortbow()
        {
            Attributes.AttackChance = 3;
            Attributes.WeaponDamage = 4;
        }

        public override int LabelNumber => 1073512; // assassin's shortbow
    }
}
